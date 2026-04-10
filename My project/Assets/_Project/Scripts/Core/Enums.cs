// ===== 게임 페이즈 =====
public enum GamePhase
{
    Title,
    MorningSchedule, // [NEW] 아침 스케줄 설정
    DayMap,          // [NEW] 낮 지도 활동
    NightTransition, // [NEW] 밤 전환 (거점 이동)
    NightAction,     // [NEW] 밤 메인 행동 (탐사/아레나)
    LateNightReport  // [NEW] 심야 정산 및 리포트
}

// ===== 전투체 낮 스케줄 =====
public enum FighterSlotType
{
    Training,
    Work,
    Rest
}

// ===== 세부 행동 분류 =====
public enum ScheduleWorkType { Courier, Garden, Restaurant }
public enum ScheduleRestType { Vacation, CityTour, Picnic }
public enum ScheduleTrainingType { BasicPhysical, Skill, SimulationBattle }

// ===== 플레이어 지도 장소 =====
public enum MapLocation
{
    None = -1,
    Base,           // 거점
    GeneralStore,   // 잡화점
    EquipmentShop,  // 장비상점
    Agency,         // 흥신소
    JunkYard,       // 폐기물처리장
    Cafe,           // 카페
    HardwareStore   // 만물상
}

// ===== 장소 내 행동 =====
public enum PlaceActionType
{
    Talk,
    Investigate,
    AcceptQuest,
    DeliverQuest,
    BuyItem,
    SellItem,      // [NEW] 판매
    Rest,
    UpgradeFacility, // [NEW] 시설 업그레이드
    SupportTraining, // [NEW] 훈련 보조
    BuyFood,         // [NEW] 음식 구매
    RerollQuests     // [NEW] 의뢰 리롤
}

// ===== 밤 선택지 =====
public enum NightActionType
{
    Exploration,
    Arena,
    Rest
}

// ===== 훈련 세부 스탯 (6대 스탯) =====
public enum TrainingStat
{
    Strength,     // STR (힘)
    Agility,      // AGI (민첩)
    Vitality,     // VIT (내구)
    Intelligence, // INT (지능)
    Guts,         // GUT (근성)
    Sensitivity,  // SEN (감각)
    None = -1
}


// ── 탐사 관련 ──

public enum ExplorationEventType
{
    None,
    EnvObject,     // [MOD] 환경 오브젝트 (범위 진입 시 자동 수집, 이벤트 없음)
    Enemy,         // [ADD] 이동형 적 (범위 진입 시 이벤트 발생, 선택권 사용 가능)
    Hazard,        // 함정/위험 (범위 진입 시 이벤트 발생)
    Obstacle,      // 장애물 (범위 진입 시 이벤트 발생)
    Interactive,   // 상호작용 오브젝트 (범위 진입 시 이벤트 발생)
    Reward,        // 보상 상자 (범위 진입 시 이벤트 발생)
    Exit           // 탈출구 (범위 진입 시 이벤트 발생)
}

public enum ExplorationChoiceType
{
    Interact,   // 상호작용 (스탯 체크, 일반 조사 등)
    Combat,     // 전투 (적 조우 시 선택권 소모)
    Escape,     // 회피/후퇴
    Exit        // 탐사 탈출 성공
}

public enum ExplorationPhase
{
    Planning,      // 경로 설정 중
    Moving,        // 자동 이동 중
    EventProcessing, // 이벤트 조우 중
    Result         // 정산 창
}

// ===== 숙련도 카테고리 =====
public enum ProficiencyType
{
    Training,
    Investigation,
    Exploration,
    PartTime
}

// ===== 승급 등급 =====
public enum ArenaRank
{
    Bronze,
    Silver,
    Gold,
    Platinum,
    Champion
}

// ===== GameEnums.cs에 추가할 enum =====

// 장비 등급
public enum EquipmentGrade
{
    Common,     // 일반
    Uncommon,   // 고급
    Rare,       // 희귀
    Epic,       // 영웅
    Legendary   // 전설
}

public enum ItemCategory
{
    Consumable, // 소모품 (회복)
    Gift,       // 선물 (호감도)
    Info,       // 정보 (탐사)
    Part        // 부품 (시설)
}
