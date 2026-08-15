using MainGame.Bonus;

namespace MainGame.Card.Effects
{
    // EXP 부스트 — 일정 시간 동안 경험치 획득량이 증가합니다.
    public class ExpGainUpSkillEffect : BuffSkillEffectBase
    {
        protected override TemporaryBuffState.EBuffType BuffType => TemporaryBuffState.EBuffType.ExpGain;
    }
}
