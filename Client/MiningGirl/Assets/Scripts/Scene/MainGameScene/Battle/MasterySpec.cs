using Data;

namespace Scene.MainGameScene.Battle
{
    // 발사체 한 발에 얹히는 강화스킬 효과.
    //
    // 런당 하나뿐이고 스킬 하나에만 붙으므로, 그 스킬이 쏘는 발사체에만 실립니다.
    // 값의 의미는 종류마다 다릅니다 - SkillMasteryDataTableRow의 주석을 보십시오.
    public readonly struct MasterySpec
    {
        public static readonly MasterySpec None = default;

        public readonly bool HasValue;
        public readonly EMasteryType Type;
        public readonly float Value;
        public readonly float Range;

        // 이 강화스킬이 덮어쓰는 쿨다운(초). 0이면 스킬의 쿨다운을 그대로 씁니다.
        public readonly float Cooldown;

        public readonly EStatusEffectType StatusType;
        public readonly float StatusDuration;
        public readonly float StatusValue;
        public readonly bool Knockback;

        // 발동 순간 제자리에서 재생할 이펙트. 폭발만 씁니다 - 연쇄와 부채꼴은
        // 발사체를 새로 내보내므로 그 발사체가 스킬의 이펙트를 그대로 달고 나갑니다.
        public readonly string EffectAssetId;

        public MasterySpec(SkillMasteryDataTableRow row)
        {
            HasValue = row != null;

            if (row == null)
            {
                Type = EMasteryType.ChainOnHit;
                Value = 0f;
                Range = 0f;
                Cooldown = 0f;
                StatusType = EStatusEffectType.None;
                StatusDuration = 0f;
                StatusValue = 0f;
                Knockback = false;
                EffectAssetId = null;

                return;
            }

            Type = row.MasteryType;
            Value = row.EffectValue;
            Range = row.EffectRange;
            Cooldown = row.Cooldown;
            StatusType = row.StatusType;
            StatusDuration = row.StatusDuration;
            StatusValue = row.StatusValue;
            Knockback = row.HasKnockback;
            EffectAssetId = row.EffectAssetId;
        }
    }
}
