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

        public readonly EStatusEffectType StatusType;
        public readonly float StatusDuration;
        public readonly float StatusValue;
        public readonly bool Knockback;

        public MasterySpec(SkillMasteryDataTableRow row)
        {
            HasValue = row != null;

            if (row == null)
            {
                Type = EMasteryType.ChainOnHit;
                Value = 0f;
                Range = 0f;
                StatusType = EStatusEffectType.None;
                StatusDuration = 0f;
                StatusValue = 0f;
                Knockback = false;

                return;
            }

            Type = row.MasteryType;
            Value = row.EffectValue;
            Range = row.EffectRange;
            StatusType = row.StatusType;
            StatusDuration = row.StatusDuration;
            StatusValue = row.StatusValue;
            Knockback = row.HasKnockback;
        }
    }
}
