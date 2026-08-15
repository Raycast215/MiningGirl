using Data;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 힐 — 최대 체력의 EffectValue% 만큼 회복합니다.
    public class HealSkillEffect : ISkillCardEffect
    {
        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return context?.HealPlayerByRatio != null;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            // 데이터는 30(=30%) 형태라 비율로 바꿔 넘깁니다.
            var ratio = Mathf.Max(0f, row.EffectValue) * 0.01f;

            context.HealPlayerByRatio.Invoke(ratio);
        }
    }
}
