using Data;

namespace MainGame.Card
{
    // 스킬 카드 하나의 효과. 스킬 타입마다 클래스를 따로 만듭니다.
    //
    // CanExecute가 false면 카드는 소모되지 않고 코스트도 나가지 않습니다.
    // (예: 화면 안에 때릴 적이 없는 경우)
    public interface ISkillCardEffect
    {
        bool CanExecute(SkillCardContext context, SkillCardDataTableRow row);

        void Execute(SkillCardContext context, SkillCardDataTableRow row);
    }
}
