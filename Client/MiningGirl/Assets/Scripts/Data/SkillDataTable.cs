#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("SkillDataTable")]
    public class SkillDataTableRow : DataTableRowBase
    {
        public string Name { get; set; }
        public string? Desc { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillType SkillType { get; set; }

        // 레벨업 3택 등장 가중치. 0이면 선택지에 나오지 않습니다.
        public int Weight { get; set; }

        // 레벨별 쿨다운(초). 배열 길이가 곧 최대 레벨입니다.
        [JsonConverter(typeof(ListFromStringConverter<float>))]
        public List<float>? CooldownList { get; set; }

        // 레벨별 위력.
        [JsonConverter(typeof(ListFromStringConverter<float>))]
        public List<float>? EffectValueList { get; set; }

        public float ProjectileSpeed { get; set; }

        public string? IconAssetId { get; set; }

        // Assets/Prefabs/InGame/Effect/ 아래 프리팹 이름입니다.
        public string? EffectAssetId { get; set; }
    }

    public class SkillDataTable : DataTableBase<SkillDataTableRow>
    {
        public SkillDataTable(IReadOnlyList<SkillDataTableRow> rows) : base(rows)
        {
        }
    }
}
