// ===== 게임 페이즈 =====
public enum GamePhase
{
    Title,
    ScheduleSetting,   // 낮 스케줄 설정 (4슬롯)
    DayProgress,       // 낮 자동 진행 (슬롯 순서대로 실행)
    NightChoice,       // 밤 선택 UI (탐사/아레나/휴식)
    NightAction,       // 밤 행동 실행 중
    DaySummary         // 하루 결산
}

// ===== 낮 슬롯 타입 (6종) =====
public enum DaySlotType
{
    Training,       // 훈련 (세부 스탯 선택은 진입 후)
    PartTime,       // 아르바이트
    Shop,           // 상점
    Investigation,  // 조사
    Relationship,   // 호감도
    Rest            // 휴식
}

// ===== 밤 선택지 (3종) =====
public enum NightActionType
{
    Exploration,    // 탐사
    Arena,          // 아레나
    Rest            // 휴식
}

// ===== 훈련 세부 스탯 (4종) =====
public enum TrainingStat
{
    Strength,   // 힘
    Agility,    // 민첩
    Dexterity,  // 재주
    Endurance   // 지구력
}

// ===== 장소 (낮 행동 시 이동 목적지) =====
public enum PlaceType
{
    TrainingGround,
    PartTimeJob,
    Shop,
    InvestigationSite,
    RelationshipSpot,
    RestArea
}