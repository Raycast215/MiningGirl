public enum EUnitRank
{
    R, 
    SR,
    SSR, 
    UR 
}

public enum ESkillRank
{
    Normal, // 일발
    Rare,   // 레어
    Epic,   // 에픽
    Special // 스페셜
}

public enum ESkillEffectType
{
    TargetHit,
    IncreaseCost,
    RangeAll,
    Draw,
}

public enum ETextType
{
    Int,
    Float,
    Percent,
}

public enum EStageType
{
    Default,
    Skill,
    Boss,
}

public enum EItemType
{
    Gold,
    Stone,
    Exp,
}

public enum EStatType
{
    Damage,
    AttackDelay,
    AttackDistance,
    MoveSpeed,
    CriDamage,
    CriRate,
    ExtraHitRate,
}

public enum EEffectValueType
{
    Add, // 합연산
    Mul, // 곱연산
}
// 레벨업 보너스가 무엇에 작용하는지 나타냅니다.
// 코드는 Id가 아니라 이 타입만 보고 동작하므로,
// 같은 타입의 스킬을 시트에 추가할 때는 코드 수정이 필요 없습니다.
public enum ELevelUpBonusEffectType
{
    None,
    MiningDamage,       // 채굴 데미지
    MiningSpeed,        // 채굴 속도
    MoveSpeed,          // 이동 속도
    CriDamage,          // 크리티털 데미지
    CriRate,            // 크리티컬 확률
    ExtraHitRate,       // 추가타 확률
    MaxHealth,          // 최대 체력
    
    MonsterKillGold,    // 적 처치 시 획득 골드
    ResourceMineGold,   // 광물 채굴 시 획득 골드
    InstantGold,        // 즉시 골드 획득
    InstantExp,         // 즉시 경험치 획득
}

public enum ESkillCategoryType
{
    Attack, // 공격형
    Assist, // 보조형
    Support // 서포트형
}

// 게임 전역 상수. 값은 GameConstantDataTable에서 옵니다.
public enum EGameConstantType
{
    StageTime,          // 스테이지 제한 시간(초)
    CardDeckSize,       // 시작 덱 장수
    CardRerollCost,     // 카드 리롤(버리기) 비용
    MonsterSpawnCount,  // 몬스터 기본 스폰 수
    ResourceSpawnCount, // 광물 기본 스폰 수
}

public enum ESkillType
{
    AirShot,        // 주변 적 공격 + 넉백
    Strike,         // 단일 공격
    DoubleAttack,   // 단일 2회 공격

    MoveSpeedUp,    // 이동속도 증가
    MiningSpeedUp,  // 채굴속도 증가
    GoldGainUp,     // 골드 획득량 증가
    ExpGainUp,      // 경험치 획득량 증가
    
    Heal,           // 체력 회복
    TargetChange,   // 근처 광물로 이동
    CostUp,         // 코스트 즉시 획득
    FireBall,       // 놓은 자리에 불덩이 소환(지속 피해)
    SpecialResource,// 놓은 자리에 황금 광물 소환
}