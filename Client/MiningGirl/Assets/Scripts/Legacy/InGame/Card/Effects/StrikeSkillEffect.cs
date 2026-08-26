using Data;

namespace Legacy.MainGame.Card.Effects
{
    // 스트라이크 — 카드를 놓은 자리에서 가까운 순으로 TargetCount 명을 EffectValue 만큼 공격합니다.
    public class StrikeSkillEffect : SingleTargetAttackEffectBase
    {
        public override void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var targets = CollectTargets(context, row);

            for (var i = 0; i < targets.Count; i++)
                targets[i].Hit(row.EffectValue, false);
        }
    }
}
