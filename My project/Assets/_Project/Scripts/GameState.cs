using System;
using UnityEngine;

[Serializable]
public class FighterSlot
{
    public FighterSlotType type = FighterSlotType.Rest;
    public TrainingStat trainingStat = TrainingStat.Strength; // 훈련일 때만 사용
}

[Serializable]
public class GameState
{
    // ===== 기본 =====
    public int day = 1;
    public int gold = 0;

    // ===== 전투체 낮 스케줄 (4블록) =====
    public const int DaySlotCount = 4;
    public FighterSlot[] fighterSchedule = new FighterSlot[DaySlotCount];
    public int fighterSlotProgress = 0;

    // ===== 플레이어 낮 행동 =====
    public const int MaxPlayerActions = 4;
    public int playerActionsUsed = 0;
    public MapLocation playerLocation = MapLocation.Home;

    // ===== 밤 =====
    public NightActionType nightChoice = NightActionType.Rest;
    public bool nightCompleted = false;

    // ===== 전투 스탯 (4종) =====
    public int statStrength = 0;
    public int statAgility = 0;
    public int statDexterity = 0;
    public int statEndurance = 0;

    // ===== 스트레스/피로 =====
    public int stress = 0;
    public int fatigue = 0;

    // ===== 숙련도 (4종) =====
    public Proficiency profTraining = new() { type = ProficiencyType.Training };
    public Proficiency profInvestigation = new() { type = ProficiencyType.Investigation };
    public Proficiency profExploration = new() { type = ProficiencyType.Exploration };
    public Proficiency profPartTime = new() { type = ProficiencyType.PartTime };

    // ===== 엔딩 변수 =====
    public EndingVariables endingVars = new();

    // ===== 아레나 =====
    public ArenaSystem arena = new();

    // ===== 의뢰 =====
    public QuestSystem quests = new();

    // ===== 오늘 누적 =====
    public int todayTrainingCount = 0;
    public int todayGoldEarned = 0;

    // ===== 판정 =====
    public bool IsArenaOpen => CalendarSystem.IsArenaDay(day);
    public bool IsPromotionDay => CalendarSystem.IsPromotionDay(day);
    public string DateString => CalendarSystem.FormatDate(day);
    public bool IsDayOver => playerActionsUsed >= MaxPlayerActions;

    // ===== 숙련도 조회 =====
    public Proficiency GetProficiency(ProficiencyType type) => type switch
    {
        ProficiencyType.Training => profTraining,
        ProficiencyType.Investigation => profInvestigation,
        ProficiencyType.Exploration => profExploration,
        ProficiencyType.PartTime => profPartTime,
        _ => profTraining
    };

    // ===== 스탯 적용 =====
    public void AddStat(TrainingStat stat, int baseAmount)
    {
        float multiplier = profTraining.TrainingStatMultiplier;
        int final_amount = Mathf.Max(1, Mathf.RoundToInt(baseAmount * multiplier));

        switch (stat)
        {
            case TrainingStat.Strength: statStrength += final_amount; break;
            case TrainingStat.Agility: statAgility += final_amount; break;
            case TrainingStat.Dexterity: statDexterity += final_amount; break;
            case TrainingStat.Endurance: statEndurance += final_amount; break;
        }
    }

    public int GetStat(TrainingStat stat) => stat switch
    {
        TrainingStat.Strength => statStrength,
        TrainingStat.Agility => statAgility,
        TrainingStat.Dexterity => statDexterity,
        TrainingStat.Endurance => statEndurance,
        _ => 0
    };

    // ===== 초기화 =====
    public GameState()
    {
        for (int i = 0; i < DaySlotCount; i++)
            fighterSchedule[i] = new FighterSlot();
    }

    public void ResetForNewDay()
    {
        day += 1;
        fighterSlotProgress = 0;
        playerActionsUsed = 0;
        playerLocation = MapLocation.Home;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        todayTrainingCount = 0;
        todayGoldEarned = 0;

        for (int i = 0; i < DaySlotCount; i++)
        {
            fighterSchedule[i].type = FighterSlotType.Rest;
            fighterSchedule[i].trainingStat = TrainingStat.Strength;
        }

        quests.GenerateDailyQuests(day);
    }
}
