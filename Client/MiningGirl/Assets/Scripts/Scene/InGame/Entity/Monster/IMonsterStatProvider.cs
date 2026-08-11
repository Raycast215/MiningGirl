namespace MainGame.Entity.Monster
{
    // 몬스터 1종의 기본 스탯. 엑셀 시트의 컬럼과 1:1로 매핑될 순수 데이터입니다.
    [System.Serializable]
    public struct MonsterBaseStat
    {
        public string MonsterId;
        public float Hp;
        public float Damage;
        public float MoveSpeed;
        public float AttackDelay;
        public float AttackDistance;
        public int GoldReward;
    }

    // 몬스터 기본 스탯 데이터 소스. 지금은 임시 구현(TempMonsterStatProvider)을 쓰고,
    // 추후 엑셀 시트 기반 임포터로 구현체만 교체합니다.
    public interface IMonsterStatProvider
    {
        MonsterBaseStat GetBaseStat(string monsterId);
    }
}
