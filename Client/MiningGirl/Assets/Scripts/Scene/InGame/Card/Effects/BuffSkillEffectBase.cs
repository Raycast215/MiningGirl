using Data;
using MainGame.Bonus;

namespace MainGame.Card.Effects
{
    // 일정 시간 동안 스탯을 올려주는 버프 계열의 공통 구현.
    // EffectValue = 증가 퍼센트, DurationTime = 지속시간(초)
    //
    // 파생 클래스는 어떤 버프인지만 지정하면 됩니다.
    public abstract class BuffSkillEffectBase : ISkillCardEffect
    {
        protected abstract TemporaryBuffState.EBuffType BuffType { get; }

        // 버프는 대상 조건이 없어 언제든 사용할 수 있습니다.
        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return context?.Buffs != null && row.DurationTime > 0f;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            context.Buffs.Apply(BuffType, row.EffectValue, row.DurationTime);
        }
    }
}
