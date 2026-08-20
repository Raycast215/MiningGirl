using System;
using System.Collections.Generic;
using Data;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Player;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 이동 — 카드를 놓은 자리에서 가장 가까운 광물로 캐릭터를 보냅니다.
    //
    // 채굴 타겟은 원래 캐릭터 AI가 정하고 유저가 건드릴 수 없습니다.
    // 이 카드는 그 결정을 유저가 가로채는 유일한 수단이라,
    // 어느 광물로 갈지도 직접 찍을 수 있어야 의미가 있습니다.
    // (예전에는 '가장 안전한 광물'을 코드가 자동으로 골랐습니다.)
    public class TargetChangeSkillEffect : ISkillCardEffect, ITargetPreviewEffect
    {
        // 시트에 범위가 없을 때 쓰는 기본 반경.
        // 공격 스킬보다 넓습니다 — 도망치려고 쓰는 카드라 멀리 갈 수 있어야 합니다.
        private const float DefaultRange = 5f;

        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return GetPlayer(context) != null && CollectTargets(context, row).Count > 0;
        }

        // 드래그 중 미리보기용 — Execute가 실제로 고를 광물과 반드시 같아야 합니다.
        public IReadOnlyList<IEntity> CollectTargets(SkillCardContext context, SkillCardDataTableRow row)
        {
            if (context == null)
                return Array.Empty<IEntity>();

            return context.FindResourcesInRangeFrom(context.DropWorldPosition, GetRange(row), GetTargetCount(row));
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var player = GetPlayer(context);
            var targets = CollectTargets(context, row);

            if (player == null || targets.Count == 0)
                return;

            // 지금 캐던 광물을 버리고 지정한 광물로 갑니다.
            player.SetTarget(targets[0]);
        }

        // 조준 표시(사거리 원)가 판정과 같은 값을 쓰도록 그대로 넘겨줍니다.
        public float GetPreviewRange(SkillCardDataTableRow row)
        {
            return GetRange(row);
        }

        private static Player GetPlayer(SkillCardContext context)
        {
            return context?.Player as Player;
        }

        private static float GetRange(SkillCardDataTableRow row)
        {
            return row != null && row.EffectRange > 0f ? row.EffectRange : DefaultRange;
        }

        // 광물은 한 곳만 고를 수 있습니다. 시트가 비어 있어도 1로 보정합니다.
        private static int GetTargetCount(SkillCardDataTableRow row)
        {
            return row != null ? Mathf.Max(1, row.TargetCount) : 1;
        }
    }
}
