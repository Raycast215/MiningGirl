using Data;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 에어샷 — 주변(EffectRange) 적을 모두 공격하고 바깥으로 밀어냅니다.
    // 위험할 때 포위를 푸는 용도라 대상이 하나도 없으면 사용되지 않습니다.
    public class AirShotSkillEffect : ISkillCardEffect
    {
        // 밀려나는 거리
        private const float KnockbackDistance = 2.5f;

        // 데이터에 범위가 없으면 쓰는 기본 범위
        private const float DefaultRange = 3f;

        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return context.FindMonstersInRange(GetRange(row)).Count > 0;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var targets = context.FindMonstersInRange(GetRange(row));
            if (targets.Count == 0)
                return;

            var origin = context.Player.GetPosition();

            foreach (var target in targets)
            {
                target.Hit(row.EffectValue, false);

                // 죽은 대상은 밀어낼 필요가 없습니다.
                if (!target.GetActiveState())
                    continue;

                // 위치를 직접 대입하면 순간이동처럼 보이므로 트윈으로 밀어냅니다.
                var pushable = target as MainGame.Entity.Monster.Monster;

                if (pushable != null)
                    pushable.PushFrom(origin, KnockbackDistance);
            }
        }

        private float GetRange(SkillCardDataTableRow row)
        {
            return row.EffectRange > 0f ? row.EffectRange : DefaultRange;
        }
    }
}
