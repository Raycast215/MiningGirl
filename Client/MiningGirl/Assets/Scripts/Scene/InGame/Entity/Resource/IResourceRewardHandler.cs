namespace Scene.InGame.Entity.Resource
{
    // 광물 채굴 완료 시 지급할 보상을 전달받는 대상에 대한 추상화.
    // 실제 재화/인벤토리 시스템이 아직 없어서, 지금은 이 인터페이스를 통해 값만 넘기고
    // ResourceController는 그 값이 어떻게 쓰이는지 알 필요가 없습니다.
    // (나중에 실제 재화 시스템이 생기면 이 인터페이스의 구현체만 주입해주면 됩니다.)
    public interface IResourceRewardHandler
    {
        void OnResourceMined(int stoneReward, int expReward);
    }
}
