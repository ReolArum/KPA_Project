// ===== 게임 페이즈 =====
public enum GamePhase
{
    Title,
    ScheduleSetting,
    DayMap,
    DayPlaceAction,
    NightChoice,
    NightAction,
    DaySummary,
    BattlePreparation,
    Battle
}

// ===== 전투체 낮 스케줄 =====
public enum FighterSlotType
{
    Training,
    PartTime,
    Rest
}

// ===== 플레이어 지도 장소 =====
public enum MapLocation
{
    None = -1,   // 미선택 상태 (지도 복귀 시 기준점)
    Home,
    Shop,
    TrainingGround,
    Cafe,
    QuestBoard
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

// ===== 훈련 세부 스탯 =====
public enum TrainingStat
{
    Strength,
    Agility,
    Dexterity,
    Endurance,
    None = -1
}


// ── 탐사 관련 ──

public enum ExplorationEventType
{
    None,
    Clue,          // [ADD] 단서 아이템 (범위 진입 시 자동 수집, 이벤트 없음)
    Hazard,        // 함정/위험 (범위 진입 시 이벤트 발생)
    Obstacle,      // 장애물 (범위 진입 시 이벤트 발생)
    Interactive,   // 상호작용 오브젝트 (범위 진입 시 이벤트 발생)
    Reward,        // 보상 상자 (범위 진입 시 이벤트 발생)
    Exit           // 탈출구 (범위 진입 시 이벤트 발생)
}

public enum ExplorationChoiceType
{
    StatCheck,     // 스탯 기반
    ItemUse,       // 아이템 사용
    EnvObjectUse,  // 발견한 환경 오브젝트 사용
    Bypass,        // 우회 (시간 소모)
    BruteForce,    // 강행 (위험 감수)
    Cancel         // 물러나기
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
