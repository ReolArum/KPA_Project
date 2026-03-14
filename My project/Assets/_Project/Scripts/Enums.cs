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
    Home,
    Shop,
    InvestigationHQ,
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
    Rest
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
