using Data;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 코스트 — 사용 즉시 코스트를 EffectValue 만큼 돌려받습니다.
    //
    // 코스트 1을 내고 3을 받으니 실질 +2입니다.
    // 최대치를 넘겨 쌓이지는 않게 해서, 모아두는 용도가 아니라
    // '지금 비싼 카드를 쓰기 위한 마중물'로 쓰이게 합니다.
    public class CostUpSkillEffect : ISkillCardEffect
    {
        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return context?.AddCost != null;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var amount = Mathf.RoundToInt(row.EffectValue);

            if (amount <= 0)
                return;

            context.AddCost.Invoke(amount);
        }
    }
}
