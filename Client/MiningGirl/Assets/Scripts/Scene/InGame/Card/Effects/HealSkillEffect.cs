using Data;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 힐 — 최대 스태미나의 EffectValue% 만큼 회복합니다.
    // (체력 시스템은 없어졌고 실패 판정은 스태미나 하나입니다.)
    public class HealSkillEffect : ISkillCardEffect
    {
        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return context?.RecoverStaminaByRatio != null;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            // 데이터는 30(=30%) 형태라 비율로 바꿔 넘깁니다.
            var ratio = Mathf.Max(0f, row.EffectValue) * 0.01f;

            context.RecoverStaminaByRatio.Invoke(ratio);
        }
    }
}
