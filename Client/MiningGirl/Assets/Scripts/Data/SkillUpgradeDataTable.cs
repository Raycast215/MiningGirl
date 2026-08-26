#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    // 한 행 = 스킬 하나의 강화 하나입니다.
    // SkillId로 연결되므로 스킬마다 증가량·가중치·등장 조건을 다르게 줄 수 있습니다.
    // 어떤 스킬에 특정 강화를 주고 싶지 않으면 그 행을 지우거나 Weight를 0으로 둡니다.
    [Serializable]
    [DataFile("SkillUpgradeDataTable")]
    public class SkillUpgradeDataTableRow : DataTableRowBase
    {
        // 이 강화가 붙는 스킬. SkillDataTable의 Id입니다.
        public string SkillId { get; set; }

        public string Name { get; set; }
        public string? Desc { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillUpgradeType UpgradeType { get; set; }

        // Add면 EffectValue를 더하고, Mul이면 (1 + EffectValue)를 곱합니다.
        [JsonConverter(typeof(StringEnumConverter))]
        public EEffectValueType ValueType { get; set; }

        public float EffectValue { get; set; }

        // 대상 스킬이 이 레벨 이상일 때만 3택에 나옵니다.
        public int RequireSkillLevel { get; set; }

        // 3택 등장 가중치. 0이면 나오지 않습니다.
        public int Weight { get; set; }
    }

    public class SkillUpgradeDataTable : DataTableBase<SkillUpgradeDataTableRow>
    {
        public SkillUpgradeDataTable(IReadOnlyList<SkillUpgradeDataTableRow> rows) : base(rows)
        {
        }
    }
}
