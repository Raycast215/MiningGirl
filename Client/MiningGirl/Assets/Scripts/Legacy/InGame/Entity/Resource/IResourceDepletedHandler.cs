namespace Legacy.Scene.InGame.Entity.Resource
{
    // 광물이 다 채굴되어(체력 0) 사라질 때 통지받는 대상에 대한 추상화.
    // Resource는 이 인터페이스만 알고, 실제 처리(ResourceController의 풀 반환 등)에는 의존하지 않습니다.
    // (Monster의 IMonsterDeathHandler와 동일한 주입 패턴)
    public interface IResourceDepletedHandler
    {
        void OnResourceDepleted(Resource resource);
    }
}
