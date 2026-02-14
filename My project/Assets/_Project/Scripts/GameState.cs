using System.Collections.Generic;
using UnityEngine;

// 전투체 스케줄 슬롯
[System.Serializable]
public class FighterSlot
{
    public FighterSlotType type = FighterSlotType.Rest;
    public TrainingStat trainingStat = TrainingStat.Strength;
}

public class GameState
{
    // 상수
    public const int DaySlotCount = 4;
    public const int MaxPlayerActions = 4;

    // 기본
    public int day = 1;
    public int gold = 0;

    // 전투체 스케줄
    public FighterSlot[] fighterSchedule;
    public int fighterSlotProgress = 0;

    // 플레이어
    public int playerActionsUsed = 0;
    public MapLocation playerLocation = MapLocation.Home;

    // 밤
    public NightActionType nightChoice = NightActionType.Rest;
    public bool nightCompleted = false;

    // ===== 스탯 (Dictionary) =====
    public Dictionary<TrainingStat, int> stats = new();

    // ===== 숙련도 (Dictionary) =====
    public Dictionary<ProficiencyType, Proficiency> proficiencies = new();

    // 컨디션
    public int stress = 0;
    public int fatigue = 0;

    // 엔딩 변수 (기존 EndingVariables.cs 사용)
    public EndingVariables endingVars = new();

    // 아레나 (기존 ArenaSystem.cs 사용)
    public ArenaSystem arena = new();

    // 퀘스트
    public QuestSystem quests = new();

    // 일일 카운터
    public int todayTrainingCount = 0;
    public int todayGoldEarned = 0;

    // ===== 생성자 =====
    public GameState()
    {
        foreach (TrainingStat s in System.Enum.GetValues(typeof(TrainingStat)))
            stats[s] = 0;

        foreach (ProficiencyType p in System.Enum.GetValues(typeof(ProficiencyType)))
            proficiencies[p] = new Proficiency();

        fighterSchedule = new FighterSlot[DaySlotCount];
        for (int i = 0; i < DaySlotCount; i++)
            fighterSchedule[i] = new FighterSlot();
    }

    // ===== 스탯 접근 =====
    public int GetStat(TrainingStat s) => stats.ContainsKey(s) ? stats[s] : 0;

    public void AddStat(TrainingStat s, int amount)
    {
        if (!stats.ContainsKey(s)) stats[s] = 0;
        float multiplier = GetProf(ProficiencyType.Training).TrainingStatMultiplier;
        stats[s] += Mathf.RoundToInt(amount * multiplier);
    }

    public int GetTotalPower()
    {
        int total = 0;
        foreach (var kv in stats)
            total += kv.Value;
        return total;
    }

    // ===== 숙련도 접근 =====
    public Proficiency GetProf(ProficiencyType p) =>
        proficiencies.ContainsKey(p) ? proficiencies[p] : new Proficiency();

    // ===== 날짜 =====
    public string DateString =>
        $"{CalendarSystem.GetYear(day)}년 {CalendarSystem.GetMonth(day)}월 {CalendarSystem.GetDayOfMonth(day)}일";
    public bool IsArenaOpen => CalendarSystem.IsArenaDay(day);
    public bool IsPromotionDay => CalendarSystem.IsPromotionDay(day);
    public bool IsDayOver => playerActionsUsed >= MaxPlayerActions;

    // ===== 하루 리셋 =====
    public void ResetForNewDay()
    {
        day++;
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
