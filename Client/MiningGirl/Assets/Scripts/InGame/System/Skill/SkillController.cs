using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.System.Skill.UI;
using UnityEngine;

namespace InGame.System.Skill
{
    public class SkillData
    {
        public string Id { get; set; }
        public int Cost { get; set; }
        public bool IsChainable  { get; set; }
        public int Weight { get; set; }
        public string IconAssetName { get; set; }
    }

    public class GameSettingData
    {
        public int HandCount { get; set; }
        public int MaxCost  { get; set; }
        public float CostUpdateTime { get; set; }
        public int CostIncreaseCount { get; set; }
    }

    public interface ISkillDataHandler
    {
        public SkillData GetSkillData();
    }
    
    public class SkillController : GameInitializer, ISkillDataHandler, IDisposable
    {
        [SerializeField]
        private SkillUIController skillUIController;

        private GameSettingData _gameSettingData;
        private bool _isGameStarted;
        private CancellationTokenSource _cts;
        private SkillCardDrawController _cardDrawController;
        
        public void Init()
        {
            IsInitialized = false;
            _isGameStarted = false;
            
            _gameSettingData = new GameSettingData
            {
                HandCount = 3,
                MaxCost = 10,
                CostUpdateTime = 2.0f,
                CostIncreaseCount = 1,
            };
            
            var testSkillList = new List<SkillData>()
            {
                new SkillData { Id = "Skill_001", Cost = 1, IsChainable = true, Weight = 30, IconAssetName = "Skill_Icon_001"},
                new SkillData { Id = "Skill_002", Cost = 3, IsChainable = true, Weight = 10, IconAssetName = "Skill_Icon_002"},
                new SkillData { Id = "Skill_003", Cost = 3, IsChainable = true, Weight = 10, IconAssetName = "Skill_Icon_003"},
            };
            
            _cardDrawController = new SkillCardDrawController(_gameSettingData.HandCount, testSkillList);
            skillUIController.Init(this, _gameSettingData.MaxCost);
            
            IsInitialized = true;
        }

        public void Appear()
        {
            skillUIController.AppearUI().Forget();
        }

        public async UniTaskVoid ExecuteSkillPointGauge()
        {
            _isGameStarted = true;
            Dispose();
            _cts ??= new CancellationTokenSource();
            
            try
            {
                while (_isGameStarted)
                {
                    await UniTask.WaitForSeconds(_gameSettingData.CostUpdateTime, cancellationToken: _cts.Token);
                    
                    // 2초마다 스킬 포인트 1 회복
                    skillUIController.GetSkillPointGaugeUIHandler().UpdateGaugeUI(_gameSettingData.CostIncreaseCount);
                }
            }
            catch (OperationCanceledException)
            {
                _isGameStarted = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isGameStarted = false;
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

#region ISkillDataHandler

        public SkillData GetSkillData()
        {
            return _cardDrawController.GetSkillData();
        }

#endregion
        
#region IDisposable

        public void Dispose()
        {
            if (_cts == null)
                return;
            
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

#endregion
    }
}