using Data;

namespace Legacy.MainGame.Card.Effects
{
    // 특수 광물 — 카드를 놓은 자리에 황금 광물을 소환하고
    // 캐릭터가 그것을 우선 채굴하러 갑니다.
    //
    // 채굴 타겟은 캐릭터 AI가 정하지만, 이 카드는 '어디를 캘지'를
    // 유저가 간접적으로 정하는 유일한 수단입니다.
    public class SpecialResourceSkillEffect : ISkillCardEffect
    {
        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return context?.SpawnSpecialResource != null;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            context.SpawnSpecialResource.Invoke(context.DropWorldPosition);
        }
    }
}
