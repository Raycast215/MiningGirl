using Cysharp.Threading.Tasks;
using Data;

namespace MainGame.Card.Effects
{
    // 더블 어택 — 카드를 놓은 자리에서 가까운 순으로 TargetCount 명을 두 번씩 공격합니다.
    public class DoubleAttackSkillEffect : SingleTargetAttackEffectBase
    {
        // 두 번째 타격까지의 간격(초). 한 번에 두 번 들어가면 연출이 겹쳐 보입니다.
        private const float SecondHitDelay = 0.15f;

        public override void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            ExecuteAsync(context, row).Forget();
        }

        private async UniTaskVoid ExecuteAsync(SkillCardContext context, SkillCardDataTableRow row)
        {
            // 대상은 놓는 순간에 확정됩니다.
            // 두 번째 타격 직전에 다시 찾으면 그새 들어온 다른 적이 맞아
            // 조준한 결과와 어깰납니다.
            var targets = CollectTargets(context, row);

            if (targets.Count == 0)
                return;

            for (var i = 0; i < targets.Count; i++)
                targets[i].Hit(row.EffectValue, false);

            await UniTask.WaitForSeconds(SecondHitDelay);

            // 첫 타로 죽었을 수 있으니 살아있는 대상만 다시 때립니다.
            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && targets[i].GetActiveState())
                    targets[i].Hit(row.EffectValue, false);
            }
        }
    }
}
