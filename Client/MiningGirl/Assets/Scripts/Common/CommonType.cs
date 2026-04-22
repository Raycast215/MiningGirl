public enum EVisibleType 
{ 
    Hide,
    Show,
    DevOnly 
}

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

public enum EItemType
{
    Gold,
    Stone,
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