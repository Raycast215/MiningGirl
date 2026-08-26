using Legacy.MainGame.Bonus;

namespace Legacy.MainGame.Card.Effects
{
    // 골드 부스트 — 일정 시간 동안 골드 획득량이 증가합니다.
    public class GoldGainUpSkillEffect : BuffSkillEffectBase
    {
        protected override TemporaryBuffState.EBuffType BuffType => TemporaryBuffState.EBuffType.GoldGain;
    }
}
