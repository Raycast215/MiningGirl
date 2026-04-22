using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Data;
using Manager;
using UnityEngine;

namespace Scene.InGame
{
    public interface IInGameDataHandler
    {
        public InGameStatData GetStatData(EStatType statType);
        public bool CheckLevelUpState(EStatType statType);
        public void LevelUpStat(EStatType statType);
        public float GetItemCount(EItemType itemType);
        public void AddItemCount(EItemType itemType, float add);
    }
    
    public class InGameStatData
    {
        public EStatType StatType { get; set; } // 스탯 타입
        public int Level { get; set; } // 현재 레벨
        public float Value { get; set; } // 레벨에 따른 수치
        public bool IsMaxLevel { get; set; } // 최대 레벨 플래그
        public float Cost { get; set; } // 레벨업에 필요한 비용
        public float AddValue { get; set; } // 레벨업 시 증가할 수치
        public float MinValue { get; set; } // 최소 수치
        public float MaxValue { get; set; } // 최대 수치
    }
    
    public class InGameData : IInGameDataHandler
    {
        public bool IsInitialized { get; private set; }
        private Dictionary<EStatType, InGameStatData> InGameStatDataDic { get; set; }
        private Dictionary<EItemType, float> ItemCountDic { get; set; }
        private string CharacterId { get; set; }
        
        public void Init(string characterId)
        {
            CharacterId = characterId;

            InitStatData();
            InitItemData();
            
            IsInitialized = true;
        }

        private void InitStatData()
        {
            var statInfoTable = DataTableManager.Instance.CharacterStatDataTable.GetRow(CharacterId);
            var statTypeList = Enum.GetValues(typeof(EStatType)).Cast<EStatType>();
            
            InGameStatDataDic = new Dictionary<EStatType, InGameStatData>();
            
            Debug.Log(CharacterId == null);
            Debug.Log(CharacterId);
            Debug.Log(DataTableManager.Instance.CharacterStatDataTable == null);
            Debug.Log(statInfoTable == null);

            foreach (var d in DataTableManager.Instance.CharacterStatDataTable.Rows)
            {
                Debug.Log(d.Id);
            }
            
            foreach (var statType in statTypeList)
            {
                SetData(statType, statInfoTable);
            }
        }

        private void InitItemData()
        {
            var itemTypeList = Enum.GetValues(typeof(EItemType)).Cast<EItemType>();
            
            ItemCountDic = new Dictionary<EItemType, float>();
            
            foreach (var itemType in itemTypeList)
            {
                ItemCountDic.Add(itemType, 0);
            }
        }
        
        private void SetData(EStatType statType, CharacterStatDataRow row)
        {
            var statGrowthInfoData = DataTableManager.Instance.CharacterStatGrowthDataTable.Rows
                .FirstOrDefault(x => x.StatType == statType);

            if (statGrowthInfoData == null)
                return;

            if (InGameStatDataDic.ContainsKey(statType))
                return;
            
            InGameStatDataDic.Add(statType, new InGameStatData
            {
                StatType = statType,
                Level = 1,
                Value = GetValue(statType, row),
                IsMaxLevel = false,
                Cost = ConstData.BaseStatLevelUpCost,
                AddValue = statGrowthInfoData.GrowthValue,
                MinValue = statGrowthInfoData.MinValue,
                MaxValue = statGrowthInfoData.MaxValue
            });
        }

        private float GetValue(EStatType statType, CharacterStatDataRow row)
        {
            var value = statType switch
            {
                EStatType.Damage => row.Damage,
                EStatType.AttackDelay => row.AttackDelay,
                EStatType.AttackDistance => row.AttackDistance,
                EStatType.MoveSpeed => row.MoveSpeed,
                EStatType.CriDamage => row.CriDamage,
                EStatType.CriRate => row.CriRate,
                EStatType.ExtraHitRate => row.ExtraHitRate,
                _ => 0.0f
            };

            return value;
        }

#region IInGameDataHandler

        public InGameStatData GetStatData(EStatType statType)
        {
            return InGameStatDataDic[statType];
        }

        public bool CheckLevelUpState(EStatType statType)
        {
            var statData = InGameStatDataDic[statType];
            var curGold = ItemCountDic[EItemType.Gold];
            
            return statData.Cost <= curGold;
        }

        public void LevelUpStat(EStatType statType)
        {
            var statData = InGameStatDataDic[statType];
            var toValue = statData.Value + statData.AddValue;
            
            ItemCountDic[EItemType.Gold] -= statData.Cost;
            
            statData.Value = Mathf.Clamp(toValue, statData.MinValue, statData.MaxValue);
            statData.Level += 1;
            statData.Cost = ConstData.BaseStatLevelUpCost * Mathf.Pow(ConstData.BaseStatLevelUpFactor, statData.Level);
        }

        public float GetItemCount(EItemType itemType)
        {
            return ItemCountDic[itemType];
        }

        public void AddItemCount(EItemType itemType, float add)
        {
            ItemCountDic[itemType] += add;
        }

#endregion
    }
}