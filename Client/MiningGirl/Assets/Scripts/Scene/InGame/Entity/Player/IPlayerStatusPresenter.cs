namespace Scene.InGame.Entity.Player
{
    // 플레이어의 체력/무적 상태를 표시하는 뷰에 대한 추상화.
    // 플레이어는 이 인터페이스만 알고, 실제 UI 구현에는 의존하지 않습니다.
    public interface IPlayerStatusPresenter
    {
        // healthRatio: 0~1 체력 비율
        // gaugeRatio: 0~1 쓰러진 뒤 회복까지 남은 비율 (다운 상태에서만 표시)
        // isDown: 체력이 0이라 쓰러진 상태인지
        void SetStatus(float healthRatio, float gaugeRatio, bool isDown);
    }
}
