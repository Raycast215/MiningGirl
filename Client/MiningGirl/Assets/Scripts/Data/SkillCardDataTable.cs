#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("SkillCardDataTable")]  
    public class SkillCardDataTableRow : DataTableRowBase
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillCategoryType SkillCategoryType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillType SkillType { get; set; }
        public int Cost { get; set; }
        public float DurationTime { get; set; }
        public float EffectRange { get; set; }
        public float EffectValue { get; set; }
        public int Weight { get; set; }
                public string AssetId { get; set; }

        // 한 번에 잡는 대상 수. -1은 대상을 고를 필요가 없는 스킬입니다(버프·힐·소환 등).
        // 범위 안에 적이 넘치면 카드에서 가까운 순으로 이 수만큼만 맞습니다.
        public int TargetCount { get; set; }
    }
    
    public class SkillCardDataTable : DataTableBase<SkillCardDataTableRow>
    {
        public SkillCardDataTable(IReadOnlyList<SkillCardDataTableRow> rows) : base(rows)
        {
        }
    }
}
