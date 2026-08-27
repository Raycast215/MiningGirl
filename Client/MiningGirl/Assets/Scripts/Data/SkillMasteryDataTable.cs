#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    // 강화스킬(마스터리) 한 종류.
    //
    // SkillUpgradeDataTable에 넣지 않은 이유가 셋입니다.
    //  - 조건이 강화 항목 두 개의 누적 횟수입니다. 기존은 RequireSkillLevel 하나뿐입니다
    //  - 런당 1회 제한이 있습니다. 기존 강화는 무제한 반복입니다
    //  - 효과가 ESkillUpgradeType 네 종류로 표현되지 않습니다
    // 기존 테이블에 컬럼을 더하면 기존 행 전부가 그 컬럼을 비워 두게 됩니다.
    [Serializable]
    [DataFile("SkillMasteryDataTable")]
    public class SkillMasteryDataTableRow : DataTableRowBase
    {
        // 어느 스킬에 붙는지. SkillDataTable의 Id입니다.
        public string SkillId { get; set; }

        public string Name { get; set; }
        public string? Desc { get; set; }

        // 조건 — 이 스킬에서 아래 두 강화를 각각 몇 번 골랐는가.
        //
        // 종류를 데이터로 둔 이유는 조건이 이미 한 번 바뀌었기 때문입니다.
        // "5레벨 이상"에서 "데미지 3회 + 발사체 3회"로요. 코드에 박으면 또 바뀔 때 코드를 고칩니다.
        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillUpgradeType RequireUpgradeTypeA { get; set; }

        public int RequireCountA { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillUpgradeType RequireUpgradeTypeB { get; set; }

        public int RequireCountB { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EMasteryType MasteryType { get; set; }

        // 거동 파라미터. 종류마다 의미가 다릅니다.
        //  ChainOnHit  추가로 나가는 발사체 수
        //  FanBurst    추가되는 발사체 수
        //  Explosion   폭발 피해 배율(본체 위력 대비)
        public float EffectValue { get; set; }

        // 거동의 반경 또는 각도. 안 쓰는 종류는 0입니다.
        //  FanBurst    부채꼴 각도(도)
        //  Explosion   폭발 반경(유닛)
        public float EffectRange { get; set; }

        // 이 강화스킬을 고른 뒤의 쿨다운(초). 0이면 스킬의 쿨다운을 그대로 씁니다.
        //
        // 감산이 아니라 대입입니다 - 감산이면 스킬마다 결과가 달라져 시트 한 칸만
        // 보고는 결과를 못 읽습니다. 대입이면 칸이 곧 결과입니다.
        public float Cooldown { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EStatusEffectType StatusType { get; set; }

        public float StatusDuration { get; set; }

        // 상태이상의 세기. 화상만 씁니다 — 초당 피해 = 스킬 위력 x 이 값.
        public float StatusValue { get; set; }

        // 0 또는 1. 명중한 대상을 밀어냅니다.
        public int Knockback { get; set; }

        public string? IconAssetId { get; set; }
        public string? EffectAssetId { get; set; }

        // 3택 가중치. 조건을 채우고 아직 안 골랐을 때만 후보에 들어갑니다.
        public int Weight { get; set; }

        public bool HasKnockback => Knockback != 0;
    }

    public class SkillMasteryDataTable : DataTableBase<SkillMasteryDataTableRow>
    {
        public SkillMasteryDataTable(IReadOnlyList<SkillMasteryDataTableRow> rows) : base(rows)
        {
        }

        // 이 스킬에 붙는 강화스킬을 찾습니다. 스킬 하나에 하나입니다.
        public SkillMasteryDataTableRow? FindBySkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || Rows == null)
                return null;

            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i] != null && Rows[i].SkillId == skillId)
                    return Rows[i];
            }

            return null;
        }
    }
}
