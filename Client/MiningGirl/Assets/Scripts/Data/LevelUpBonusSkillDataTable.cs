using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("LevelUpBonusSkillDataTable")]  
    public class LevelUpBonusSkillDataTableRow : DataTableRowBase
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public float EffectValue { get; set; }
        public int Weight { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public EEffectValueType ValueType { get; set; }
        // 이 보너스가 무엇에 작용하는지. 코드는 Id 대신 이 값을 보고 동작합니다.
        [JsonConverter(typeof(StringEnumConverter))]
        public ELevelUpBonusEffectType EffectType { get; set; }
        public int MaxLevel { get; set; }

        // --- 강화 팝업용 ---

        // 어느 탭에 표시할지
        [JsonConverter(typeof(StringEnumConverter))]
        public EUpgradeTabType TabType { get; set; }

        // 1레벨을 살 때의 기본 가격. 레벨이 오를수록 아래 배율만큼 비싸집니다.
        public int BasePrice { get; set; }

        // 레벨당 가격 배율(1.5면 40 → 60 → 90 …). 0이면 가격이 오르지 않습니다.
        public float PriceGrowth { get; set; }

        // 이 항목이 열리는 스테이지. 0이면 처음부터 열려 있습니다.
        public int UnlockStage { get; set; }

        // 선행 조건: 이 스킬이 아래 레벨 이상이어야 구매할 수 있습니다.
        public int RequireId { get; set; }
        public int RequireLevel { get; set; }

        // 레벨에 따른 실제 가격을 계산합니다(level은 이번에 살 레벨, 1부터).
        public int GetPrice(int level)
        {
            if (BasePrice <= 0)
                return 0;

            if (PriceGrowth <= 0f || level <= 1)
                return BasePrice;

            return UnityEngine.Mathf.RoundToInt(BasePrice * UnityEngine.Mathf.Pow(PriceGrowth, level - 1));
        }
    }
    
    public class LevelUpBonusSkillDataTable : DataTableBase<LevelUpBonusSkillDataTableRow>
    {
        public LevelUpBonusSkillDataTable(IReadOnlyList<LevelUpBonusSkillDataTableRow> rows) : base(rows)
        {
        }
    }
}
