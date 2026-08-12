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

public enum ESkillType
{
    Attack, // 공격형
    Assist, // 보조형
    Support // 서포트형
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
    
    MonsterKillGold,    // 적 처치 시 획득 골드
    ResourceMineGold,   // 광물 채굴 시 획득 골드
    InstantGold,        // 즉시 골드 획득
    InstantExp,         // 즉시 경험치 획득
}
