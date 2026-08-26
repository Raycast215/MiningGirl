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

        // 아래 셋은 강화로 늘어나기 전의 기본값입니다.
        // 실제 값은 SkillUpgradeDataTable에서 고른 강화가 누적된 뒤에 나옵니다.

        // 한 번 발사할 때 나가는 발사체 수.
        public int ProjectileCount { get; set; }

        // 발사체 하나가 첫 명중 뒤에 더 뚫고 지나가는 수.
        // 0이면 한 마리만 맞히고 사라지고, 1이면 두 마리까지 맞힙니다.
        public int PierceCount { get; set; }

        // 명중 판정에 쓰는 반경(월드 유닛). 몬스터 반경에 더해집니다.
        public float HitRange { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EProjectileMoveType ProjectileMoveType { get; set; }

        // Sine일 때만 씁니다. [진폭(유닛), 초당 사이클 수] 순서입니다.
        // 진폭은 진행 방향에 수직으로 벗어나는 최대 거리이고, 타겟에 가까워질수록 0으로 줄어듭니다.
        [JsonConverter(typeof(ListFromStringConverter<float>))]
        public List<float>? ProjectileWaveList { get; set; }

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
