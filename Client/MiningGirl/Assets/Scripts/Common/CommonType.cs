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

// 강화 팝업의 탭 구분
public enum EUpgradeTabType
{
    Character,  // 캐릭터 스탯·스태미나
    Monster,    // 몬스터 수량·효율 (리스크를 사서 수입을 늘림)
    Resource,   // 광물 수량·효율
    Etc,        // 코스트·손패·밀치기·클리어 보상 같은 규칙 항목
}

// 레벨업 보너스가 무엇에 작용하는지 나타냅니다.
// 코드는 Id가 아니라 이 타입만 보고 동작하므로,
// 같은 타입의 스킬을 시트에 추가할 때는 코드 수정이 필요 없습니다.
public enum ELevelUpBonusEffectType
{
    MaxStamina,        // 최대 스태미나
    MiningStaminaCost, // 채굴 1회 소모 감소
    HitStaminaCost,    // 피격 1회 소모 감소
    MonsterMaxCount,   // 몬스터 최대 수
    ResourceMaxCount,  // 광물 수량
    StageClearGold,    // 클리어 보상 골드
    HandSize,          // 손패 장수
    ResourceHealth,    // 광물 내구도 감소(값이 클수록 적게 때려도 캐집니다)
    KillStaminaRecover,// 몬스터 처치 시 스태미나 회복
    MaxCost,           // 보유 가능한 최대 코스트
    CostRegen,         // 코스트 회복 속도

    None,
    MiningDamage,       // 채굴 데미지
    MiningSpeed,        // 채굴 속도
    MoveSpeed,          // 이동 속도
    CriDamage,          // 크리티컬 데미지
    CriRate,            // 크리티컬 확률
    ExtraHitRate,       // 추가타 확률

    MonsterKillGold,    // 적 처치 시 획득 골드
    ResourceMineGold,   // 광물 채굴 시 획득 골드
    InstantGold,        // 즉시 골드 획득
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
    CardDeckSize,       // 시작 덱 장수
    CardRerollCost,     // 카드 리롤(버리기) 비용
    MonsterSpawnCount,  // 스테이지 1의 몬스터 최대 소환 수
    ResourceSpawnCount, // 스테이지 1의 광물 수량(초기 배치 수 겸 최대 수량)

    MonsterSpawnCountPerStage,  // 스테이지가 오를 때마다 몬스터 최대 수에 더해지는 양
    ResourceSpawnCountPerStage, // 스테이지가 오를 때마다 광물 수량에 더해지는 양

    MonsterSpawnInterval,  // 몬스터 스폰 간격(초)
    ResourceSpawnInterval, // 광물 스폰 간격(초)

    MaxStage, // 마지막 스테이지 번호(이 스테이지를 깨면 런이 끝납니다)

    // ── 코스트 ──
    MaxCost,                 // 보유 가능한 최대 코스트
    CostRegenInterval,       // 코스트 1이 차오르는 데 걸리는 시간(초)
    CostLateSpeedMultiplier, // 스테이지 후반 회복 속도 배율(2면 두 배)
    CostSpeedUpProgress,     // 채굴 진행도가 이 값을 넘기면 회복이 빨라집니다(0~1)

    // ── 클리어 조건(채굴) ──
    MiningGoalBase,          // 스테이지 1의 목표 채굴량
    MiningGoalPerStage,      // 스테이지가 오를 때마다 목표에 더해지는 양

    // ── 스태미나 ──
    MaxStamina,              // 기본 최대 스태미나(강화 보정 전)
    MiningStaminaCost,       // 광물 하나를 캘 때 소모
    HitStaminaCost,          // 몬스터에게 한 번 맞을 때 소모
}

public enum ESkillType
{
    AirShot,        // 주변 적 공격 + 넉백
    Strike,         // 단일 공격
    DoubleAttack,   // 단일 2회 공격

    MoveSpeedUp,    // 이동속도 증가
    MiningSpeedUp,  // 채굴속도 증가
    GoldGainUp,     // 골드 획득량 증가

    Heal,           // 스태미나 회복
    TargetChange,   // 지정한 광물로 이동
    CostUp,         // 코스트 즉시 획득
    FireBall,       // 놓은 자리에 불덩이 소환(지속 피해)
    SpecialResource,// 놓은 자리에 황금 광물 소환
}
