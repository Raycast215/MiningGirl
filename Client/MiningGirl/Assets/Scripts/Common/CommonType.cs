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

// 골드가 어디서 들어왔는지. 결과 창이 출처별로 나눠 보여주는 데 씁니다.
public enum EGoldSource
{
    Other,     // 클리어 보상, 카드 즉시 골드 등
    Monster,   // 몬스터 처치
    Resource,  // 광물 채굴
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

    // ── 웨이브 디펜스 개편 ──
    SkillSlotMax,        // 한 런에 들 수 있는 스킬 수
    LevelUpChoiceCount,  // 레벨업 시 제시하는 스킬 장수
    WaveStartDelay,      // 스테이지 시작 후 1웨이브까지 대기(초)
    WaveClearDelay,      // 웨이브 전멸 후 다음 웨이브까지 대기(초)
    LevelUpCurveRate,    // 레벨업 곡선 강도. 마지막 구간 필요량 ÷ 첫 구간 필요량. 1이면 균등

    Star3HealthRate,     // 별 3개 기준. 남은 타워 체력 비율이 이 값 이상(0~1)
    Star2HealthRate,     // 별 2개 기준. 미만이면 별 1개, 실패는 0개

    LevelUpRerollCount,  // 한 런에서 쓸 수 있는 3택 다시 뽑기 횟수. 스테이지마다 초기화되고 이월되지 않습니다

    LevelUpFirstStepExp, // 레벨 1에서 2로 오를 때 필요한 경험치. 예전에는 총 몬스터 수 / 웨이브 수로 냈습니다
}

public enum EMonsterType
{
    Normal,
    Elite,
    Boss,
}

// 발사체가 날아가는 모양. 위력·탄속과는 무관하고 그림만 다릅니다.
public enum EProjectileMoveType
{
    Linear, // 직진
    Sine,   // 진행 방향에 수직으로 흔들리며 나아갑니다. 타겟에 가까워질수록 흔들림이 잦아듭니다
}

// 레벨업 3택에서 고르는 스킬 강화의 종류.
// 스킬 하나마다 따로 쌓이며, 값은 SkillUpgradeDataTable에서 옵니다.
public enum ESkillUpgradeType
{
    Damage,          // 위력
    ProjectileCount, // 한 번에 나가는 발사체 수
    PierceCount,     // 발사체 하나가 관통하는 수
    HitRange,        // 명중 판정 반경
}

// 강화스킬(마스터리)이 발사체에 얹는 거동.
//
// 런당 하나만 고를 수 있고 조건을 채워야 3택에 나옵니다.
// 값의 의미는 종류마다 다르므로 SkillMasteryDataTable의 주석을 함께 보십시오.
public enum EMasteryType
{
    ChainOnHit, // 명중하면 그 지점에서 다른 적으로 한 발 더 나갑니다
    FanBurst,   // 부채꼴로 한꺼번에 뿌립니다. 조준하지 않습니다
    Explosion,  // 착탄 지점 주변에 범위 피해를 줍니다
}

// 몬스터에게 걸리는 지속 상태.
//
// 3종만 있고 확장 구조를 두지 않습니다. 강화스킬이 런당 하나뿐이라
// 두 상태가 겹칠 수 없습니다. 같은 상태를 다시 걸면 지속시간을 갱신합니다(합산 아님).
public enum EStatusEffectType
{
    None,
    Freeze, // 이동과 공격이 멈춥니다
    Burn,   // 초당 피해를 받습니다
}

public enum ESkillType
{
    AirShot,        // 주변 적 공격 + 넉백
    Strike,         // 단일 공격
    DoubleAttack,   // 단일 2회 공격
    IceBolt,        // 놓은 방향으로 직선 발사, 처음 맞은 적에게 피해

    MoveSpeedUp,    // 이동속도 증가
    MiningSpeedUp,  // 채굴속도 증가
    GoldGainUp,     // 골드 획득량 증가

    Heal,           // 스태미나 회복
    TargetChange,   // 지정한 광물로 이동
    CostUp,         // 코스트 즉시 획득
    FireBall,       // 놓은 자리에 불덩이 소환(지속 피해)
    SpecialResource,// 놓은 자리에 황금 광물 소환

    // ── 웨이브 디펜스 개편 ──
    Bolt,   // 단일 대상 발사체. 세 볼트 스킬이 공유합니다.
}
