using Data;
using Scene.InGame.Entity.Interface;

namespace MainGame.Card.Effects
{
    // 단일 대상 공격 계열의 공통 구현.
    //
    // EffectRange가 -1이면 '사거리 제한 없음'이지만, 화면 밖 적을 때리면
    // 유저 눈에는 아무 일도 안 일어난 것처럼 보이므로 화면 안에서만 찾습니다.
    // 대상이 없으면 CanExecute가 false가 되어 카드도 코스트도 소모되지 않습니다.
    public abstract class SingleTargetAttackEffectBase : ISkillCardEffect
    {
        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return FindTarget(context, row) != null;
        }

        public abstract void Execute(SkillCardContext context, SkillCardDataTableRow row);

        protected IEntity FindTarget(SkillCardContext context, SkillCardDataTableRow row)
        {
            var range = row.EffectRange > 0f ? row.EffectRange : -1f;

            return context.FindNearestMonsterOnScreen(range);
        }
    }
}
