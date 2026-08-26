using System;
using System.Collections.Generic;
using Data;
using Legacy.Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Legacy.MainGame.Card.Effects
{
    // 카드로 조준해서 적을 때리는 공격 계열의 공통 구현.
    //
    // 대상은 '카드를 놓은 자리'에서 가까운 순으로 시트의 TargetCount 만큼입니다.
    // 예전에는 플레이어 기준으로 한 명만 잡았는데, 그러면 카드를 어디에 놓든
    // 항상 같은 적이 맞아서 '조준'이라는 행위 자체가 없었습니다.
    //
    // 화면 밖 적은 제외합니다 — 맞아도 유저 눈에는 아무 일도 안 일어난 것처럼 보입니다.
    // 대상이 하나도 없으면 CanExecute가 false가 되어 카드도 코스트도 소모되지 않습니다.
    public abstract class SingleTargetAttackEffectBase : ISkillCardEffect, ITargetPreviewEffect
    {
        // 시트에 범위가 없을 때(-1) 쓰는 기본 반경
        private const float DefaultRange = 3f;

        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return CollectTargets(context, row).Count > 0;
        }

        public abstract void Execute(SkillCardContext context, SkillCardDataTableRow row);

        // 드래그 중 미리보기용 — Execute가 실제로 때릴 대상과 반드시 같아야 합니다.
        public IReadOnlyList<IEntity> CollectTargets(SkillCardContext context, SkillCardDataTableRow row)
        {
            if (context == null)
                return Array.Empty<IEntity>();

            return context.FindMonstersInRangeFrom(context.DropWorldPosition, GetRange(row), GetTargetCount(row));
        }

        // 조준 표시(사거리 원)가 판정과 같은 값을 쓰도록 그대로 넘겨줍니다.
        public float GetPreviewRange(SkillCardDataTableRow row)
        {
            return GetRange(row);
        }

        protected static float GetRange(SkillCardDataTableRow row)
        {
            return row != null && row.EffectRange > 0f ? row.EffectRange : DefaultRange;
        }

        // 조준형 스킬인데 시트가 -1로 비어 있으면 최소 한 명은 잡게 둡니다.
        // (데이터 실수로 카드가 아예 안 쓰이는 상황을 막습니다.)
        protected static int GetTargetCount(SkillCardDataTableRow row)
        {
            return row != null ? Mathf.Max(1, row.TargetCount) : 1;
        }
    }
}
