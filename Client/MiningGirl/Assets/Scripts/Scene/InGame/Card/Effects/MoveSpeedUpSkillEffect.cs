using MainGame.Bonus;

namespace MainGame.Card.Effects
{
    // 가속 — 일정 시간 동안 이동 속도가 증가합니다.
    public class MoveSpeedUpSkillEffect : BuffSkillEffectBase
    {
        protected override TemporaryBuffState.EBuffType BuffType => TemporaryBuffState.EBuffType.MoveSpeed;
    }
}
