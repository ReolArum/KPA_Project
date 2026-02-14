// ===== 게임 페이즈 =====
public enum GamePhase
{
    Title,
    ScheduleSetting,   // 전투체 낮 스케줄 설정 (4블록)
    DayMap,            // 플레이어 지도 이동 (낮)
    DayPlaceAction,    // 플레이어 장소 도착 후 행동
    NightChoice,       // 밤 선택 (합류)
    NightAction,       // 밤 행동 실행
    DaySummary         // 하루 결산
}

// ===== 전투체 낮 스케줄 (3종) =====
public enum FighterSlotType
{
    Training,
    PartTime,
    Rest
}

// ===== 플레이어 지도 장소 =====
public enum MapLocation
{
    Home,           // 집 (시작 위치)
    Shop,           // 상점
    InvestigationHQ,// 조사 거점
    TrainingGround, // 훈련소
    Cafe,           // 카페 (호감도)
    QuestBoard      // 의뢰 게시판
}

// ===== 장소 내 행동 =====
public enum PlaceActionType
{
    Talk,           // 대화 (호감도)
    Investigate,    // 조사
    AcceptQuest,    // 의뢰 수령
    DeliverQuest,   // 의뢰 배달
    BuyItem,        // 구매
    Rest            // 쉬기
}

// ===== 밤 선택지 (3종) =====
public enum NightActionType
{
    Exploration,
    Arena,
    Rest
}

// ===== 훈련 세부 스탯 (4종) =====
public enum TrainingStat
{
    Strength,
    Agility,
    Dexterity,
    Endurance
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
