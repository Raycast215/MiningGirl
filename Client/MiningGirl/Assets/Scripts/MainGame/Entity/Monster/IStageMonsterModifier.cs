namespace MainGame.Entity.Monster
{
    // 스테이지 인덱스에 따른 몬스터 능력치 보정. 지금은 임시 구현을 쓰고,
    // 추후 스테이지 데이터(엑셀 등)로 구현체만 교체합니다.
    public interface IStageMonsterModifier
    {
        float GetHpMultiplier(int stageIndex);
        float GetDamageMultiplier(int stageIndex);
        float GetMoveSpeedMultiplier(int stageIndex);
    }

    // 임시 구현체 — 스테이지가 올라갈수록 완만하게 강해지는 정도만 대략 반영합니다.
    public class TempStageMonsterModifier : IStageMonsterModifier
    {
        public float GetHpMultiplier(int stageIndex) => 1f + stageIndex * 0.1f;
        public float GetDamageMultiplier(int stageIndex) => 1f + stageIndex * 0.1f;
        public float GetMoveSpeedMultiplier(int stageIndex) => 1f;
    }
}
