namespace MainGame.Entity
{
    // 경험치 획득을 통지받는 대상에 대한 추상화.
    // 몬스터 처치 등에서 호출되며, 실제 레벨 계산/표시는 구현체(UI 쪽) 책임입니다.
    // (IFloatingDamagePresenter 등과 동일한 주입 패턴)
    public interface IExpRewardHandler
    {
        void OnExpGained(int amount);
    }
}
