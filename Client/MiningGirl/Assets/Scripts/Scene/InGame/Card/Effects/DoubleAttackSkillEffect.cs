using Cysharp.Threading.Tasks;
using Data;

namespace MainGame.Card.Effects
{
    // 더블 어택 — 가장 가까운 적 하나를 EffectValue 만큼 두 번 공격합니다.
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
            var target = FindTarget(context, row);
            if (target == null)
                return;

            target.Hit(row.EffectValue, false);

            await UniTask.WaitForSeconds(SecondHitDelay);

            // 첫 타로 죽었을 수 있으니 다시 확인합니다.
            if (target.GetActiveState())
                target.Hit(row.EffectValue, false);
        }
    }
}
