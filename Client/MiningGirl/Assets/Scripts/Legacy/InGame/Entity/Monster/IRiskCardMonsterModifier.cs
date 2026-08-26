namespace Legacy.MainGame.Entity.Monster
{
    // 플레이어가 선택한 "위험" 카드에 따른 몬스터 강화/약화 및 스폰 보정.
    // 지금은 임시 구현을 쓰고, 추후 IInGameDataHandler(EStatType 위험 카테고리) 연동으로 교체합니다.
    public interface IRiskCardMonsterModifier
    {
        float GetHpMultiplier();       // 예: 위험(특수) "적 체력 2배"
        float GetGoldMultiplier();     // 예: 위험 "적 처치 골드 증가" 누적 레벨
        int GetExtraSpawnCount();      // 예: 위험 "스테이지 적 등장 수 증가" 누적 레벨
        float GetSpawnIntervalRate();  // 예: 위험 "적 등장 주기 감소" 누적 레벨 (0~1, 감소 비율)
        float GetGradeUpRate();        // 예: 위험 "적 등급업 확률 증가" 누적 레벨 (0~1, 확률)
    }

    // 임시 구현체 — 위험 카드 시스템이 완성되기 전까지는 아무 보정도 없는 상태로 동작합니다.
    public class TempRiskCardMonsterModifier : IRiskCardMonsterModifier
    {
        public float GetHpMultiplier() => 1f;
        public float GetGoldMultiplier() => 1f;
        public int GetExtraSpawnCount() => 0;
        public float GetSpawnIntervalRate() => 0f;
        public float GetGradeUpRate() => 0f;
    }
}
