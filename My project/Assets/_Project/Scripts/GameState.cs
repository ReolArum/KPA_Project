// ===== GameState.cs =====
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FighterSlot
{
    public FighterSlotType type = FighterSlotType.Rest;
    public TrainingStat trainingStat = TrainingStat.Strength;
}

[Serializable]
public class GameState
{
    // ===== 상수 =====
    public const int DaySlotCount = 4;
    public const int MaxPlayerActions = 4;

    // ===== 기본 =====
    public int day = 1;
    public int gold = 0;
    public int reputation = 0;

    // ===== 전투체 스케줄 =====
    public FighterSlot[] fighterSchedule;
    public int fighterSlotProgress = 0;

    // ===== 플레이어 =====
    public int playerActionsUsed = 0;
    public MapLocation playerLocation = MapLocation.Home;

    // ===== 밤 =====
    public NightActionType nightChoice = NightActionType.Rest;
    public bool nightCompleted = false;

    // ===== 스탯 =====
    public Dictionary<TrainingStat, int> stats = new();

    // ===== 숙련도 =====
    public Dictionary<ProficiencyType, Proficiency> proficiencies = new();

    // ===== 컨디션 =====
    public int stress = 0;
    public int fatigue = 0;

    // ===== 엔딩 변수 =====
    public EndingVariables endingVars = new();

    // ===== 아레나 =====
    public ArenaSystem arena = new();

    // ===== 퀘스트 =====
    public QuestSystem quests = new();

    // ===== 일일 카운터 =====
    public int todayTrainingCount = 0;
    public int todayGoldEarned = 0;

    // ===== 전투 데이터 ★ =====
    public PlayerCombatData combatData = new PlayerCombatData();

    // ===== 마지막 전투 리포트 ★ =====
    public string lastBattleReport = "";

    // ===== 생성자 =====
    public GameState()
    {
        foreach (TrainingStat s in Enum.GetValues(typeof(TrainingStat)))
            stats[s] = 0;

        foreach (ProficiencyType p in Enum.GetValues(typeof(ProficiencyType)))
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

    // ===== 날짜/판정 =====
    public string DateString => CalendarSystem.FormatDate(day);
    public bool IsArenaOpen => CalendarSystem.IsArenaDay(day);
    public bool IsPromotionDay => CalendarSystem.IsPromotionDay(day);
    public bool IsDayOver => playerActionsUsed >= MaxPlayerActions;

    // ===== 전투 스탯 계산 ★ =====
    public CombatBaseStats GetCombatStats()
    {
        return combatData.CalculateCombatStats(this);
    }

    // ===== 스킬 슬롯 수 ★ =====
    public int MaxSkillSlots
    {
        get
        {
            combatData.maxSkillSlots = arena.currentRank switch
            {
                ArenaRank.Bronze => 3,
                ArenaRank.Silver => 4,
                ArenaRank.Gold => 5,
                ArenaRank.Platinum => 6,
                ArenaRank.Champion => 7,
                _ => 3
            };
            return combatData.maxSkillSlots;
        }
    }

    // ===== 하루 리셋 =====
    public void ResetForNewDay()
    {
        day++;
        fighterSlotProgress = 0;
        playerActionsUsed = 0;
        playerLocation = MapLocation.None;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        todayTrainingCount = 0;
        todayGoldEarned = 0;
        // 스케줄(type, trainingStat)은 유지 → 플레이어가 설정한 스케줄이 다음 날도 유지됨
        // fighterSchedule은 건드리지 않음
    }

    // ===== 전체 초기화 (새 게임) =====
    public void ResetAll()
    {
        day = 1;
        gold = 0;
        reputation = 0;
        stress = 0;
        fatigue = 0;
        fighterSlotProgress = 0;
        playerActionsUsed = 0;
        playerLocation = MapLocation.Home;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        todayTrainingCount = 0;
        todayGoldEarned = 0;

        foreach (TrainingStat s in Enum.GetValues(typeof(TrainingStat)))
            stats[s] = 0;

        foreach (ProficiencyType p in Enum.GetValues(typeof(ProficiencyType)))
            proficiencies[p] = new Proficiency();

        fighterSchedule = new FighterSlot[DaySlotCount];
        for (int i = 0; i < DaySlotCount; i++)
            fighterSchedule[i] = new FighterSlot();

        endingVars = new EndingVariables();
        arena = new ArenaSystem();
        quests = new QuestSystem();
        combatData = new PlayerCombatData();
        lastBattleReport = "";
    }
}
