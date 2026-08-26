using Legacy.MainGame.Bonus;

namespace Legacy.MainGame.Card.Effects
{
    // 채굴 가속 — 일정 시간 동안 채굴 속도가 증가합니다(공격 주기 단축).
    public class MiningSpeedUpSkillEffect : BuffSkillEffectBase
    {
        protected override TemporaryBuffState.EBuffType BuffType => TemporaryBuffState.EBuffType.MiningSpeed;
    }
}
