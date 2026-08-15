using Data;

namespace MainGame.Card.Effects
{
    // 스트라이크 — 가장 가까운 적 하나를 EffectValue 만큼 공격합니다.
    public class StrikeSkillEffect : SingleTargetAttackEffectBase
    {
        public override void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var target = FindTarget(context, row);
            if (target == null)
                return;

            target.Hit(row.EffectValue, false);
        }
    }
}
