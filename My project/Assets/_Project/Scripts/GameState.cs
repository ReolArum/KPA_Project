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
    public int gold = 100; // Changed initial value from 0 to 100
    public int reputation = 0;

    // ===== 전투체 스케줄 =====
    public FighterSlot[] fighterSchedule;
    public int fighterSlotProgress = 0;

    // [NEW] 장소별 시설/상태 데이터
    public int facilityUpgradeLevel = 0; // 훈련장 업그레이드 단계
    public bool dailyRerollUsed = false; // 오늘 의뢰 리롤 사용 여부
    public float trainingEfficiency = 1.0f; // 음식 등에 의한 훈련 효율 버프

    // ===== 플레이어 =====
    public int playerActionsUsed = 0;
    public MapLocation playerLocation = MapLocation.None;  // 초기/복귀 시 None, 이동 시에만 값 세팅

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

    [Header("Exploration Results")]
    public int explorationGoldTotal = 0;
    public List<string> explorationFoundKeys = new List<string>();

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
    // 버그 수정: 키 없을 때 new Proficiency()를 반환하면 AddExp 결과가 버려짐
    // → 없으면 딕셔너리에 새로 추가 후 반환 (항상 동일 참조 보장)
    public Proficiency GetProf(ProficiencyType p)
    {
        if (!proficiencies.ContainsKey(p))
            proficiencies[p] = new Proficiency();
        return proficiencies[p];
    }

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
        dailyRerollUsed = false; // Added new field reset
        // 피로도에 따른 기본 효율 조정 (버프는 초기화)
        trainingEfficiency = 1.0f; // Added new field reset
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
        playerLocation = MapLocation.None;
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
