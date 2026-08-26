using System;
using System.Collections.Generic;
using Data;
using Legacy.Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Legacy.MainGame.Card.Effects
{
    // 에어샷 — 카드를 놓은 자리 주변(EffectRange) 적을 최대 TargetCount 명까지
    // 공격하고 바깥쪽으로 밀어냅니다.
    //
    // 위험할 때 포위를 푸는 용도라 대상이 하나도 없으면 사용되지 않습니다.
    public class AirShotSkillEffect : ISkillCardEffect, ITargetPreviewEffect
    {
        // 밀려나는 거리
        private const float KnockbackDistance = 2.5f;

        // 데이터에 범위가 없으면 쓰는 기본 범위
        private const float DefaultRange = 3f;

        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return CollectTargets(context, row).Count > 0;
        }

        // 드래그 중 미리보기용 — Execute가 실제로 때릴 대상과 반드시 같아야 합니다.
        public IReadOnlyList<IEntity> CollectTargets(SkillCardContext context, SkillCardDataTableRow row)
        {
            if (context == null)
                return Array.Empty<IEntity>();

            return context.FindMonstersInRangeFrom(context.DropWorldPosition, GetRange(row), GetTargetCount(row));
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var targets = CollectTargets(context, row);

            if (targets.Count == 0)
                return;

            // 밀어내는 중심도 카드를 놓은 자리입니다.
            // 타겟 판정과 기준이 같아야 '여기를 치면 이 적들이 이렇게 밀린다'가 예측됩니다.
            var origin = context.DropWorldPosition;

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                target.Hit(row.EffectValue, false);

                // 죽은 대상은 밀어낼 필요가 없습니다.
                if (!target.GetActiveState())
                    continue;

                // 위치를 직접 대입하면 순간이동처럼 보이므로 트윈으로 밀어냅니다.
                var pushable = target as Legacy.MainGame.Entity.Monster.Monster;

                if (pushable != null)
                    pushable.PushFrom(origin, KnockbackDistance);
            }
        }

        // 조준 표시(사거리 원)가 판정과 같은 값을 쓰도록 그대로 넘겨줍니다.
        public float GetPreviewRange(SkillCardDataTableRow row)
        {
            return GetRange(row);
        }

        private static float GetRange(SkillCardDataTableRow row)
        {
            return row != null && row.EffectRange > 0f ? row.EffectRange : DefaultRange;
        }

        // 범위 안에 적이 넘쳐도 이 수만큼만, 카드에서 가까운 순으로 맞습니다.
        private static int GetTargetCount(SkillCardDataTableRow row)
        {
            return row != null ? Mathf.Max(1, row.TargetCount) : 1;
        }
    }
}
