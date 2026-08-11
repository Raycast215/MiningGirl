namespace MainGame.Entity
{
    // 몬스터 사망을 통지받는 대상에 대한 추상화.
    // 몬스터는 이 인터페이스만 알고, 실제 처리(MonsterController의 풀 반환 등)에는 의존하지 않습니다.
    // (IFloatingDamagePresenter 등과 동일한 주입 패턴)
    public interface IMonsterDeathHandler
    {
        void OnMonsterDeath(Monster.Monster monster);
    }
}
