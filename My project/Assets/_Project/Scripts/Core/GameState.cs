// ===== GameState.cs =====
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FighterSlot
{
    public FighterSlotType type = FighterSlotType.Rest;
    public TrainingStat trainingStat = TrainingStat.Strength;
    public float efficiencyMultiplier = 1.0f; // [ADD] 개별 슬롯별 효율 (보너스 등)
}

[Serializable]
public class GameState
{
    // ===== 상수 =====
    public const int DaySlotCount = 4;
    public const int MaxPlayerActions = 4;

    // ===== [MOD] 모듈화된 데이터 그룹 =====
    public PlayerData player = new PlayerData();
    public FighterData fighter = new FighterData(DaySlotCount);
    public Inventory inventory = new Inventory();

    // ===== [NEW] 장소별 시설/상태 데이터 =====
    public int facilityUpgradeLevel = 0; // 훈련장 업그레이드 단계
    public float trainingEfficiency = 1.0f; // 음식 등에 의한 훈련 효율 버프

    // ===== 밤 =====
    public NightActionType nightChoice = NightActionType.Rest;
    public bool nightCompleted = false;

    // ===== 로그 (일일 활동 기록) =====
    public List<string> dailyActivityLogs = new();

    // ===== 숙련도 =====
    public Dictionary<ProficiencyType, Proficiency> proficiencies = new();

    // ===== 엔딩 변수 =====
    public EndingVariables endingVars = new();

    // ===== 아레나 =====
    public ArenaSystem arena = new();

    // ===== 퀘스트 =====
    public QuestSystem quests = new();

    // [MOD] 루트 필드 삭제 (PlayerData/FighterData의 필드와 중복)

    [Header("Exploration Results")]
    public int explorationGoldTotal = 0;
    public List<string> explorationFoundKeys = new List<string>();
    public List<string> revealedStageNodeIds = new List<string>(); // [ADD] 낮 행동을 통해 발견된 탐사 노드 ID 리스트
    public string lastExplorationStatus = ""; // [ADD] 마지막 탐사 결과 상태 (Success/Failed)

    // ===== 전투 데이터 ★ =====
    public PlayerCombatData combatData = new PlayerCombatData();

    // ===== 마지막 전투 리포트 ★ =====
    public string lastBattleReport = "";

    // ===== [NEW] 자원 및 상태 관리 (중앙 집중식) =====
    public void AddGold(int amount)
    {
        player.gold += amount;
        if (amount > 0) player.todayGoldEarned += amount;
        GameEvents.RaiseRefreshRequested(this, GameManager.Instance.Phase);
    }

    public void AddReputation(int amount)
    {
        player.reputation += amount;
        GameEvents.RaiseRefreshRequested(this, GameManager.Instance.Phase);
    }

    public void AddStat(TrainingStat s, int amount)
    {
        float multiplier = GetProf(ProficiencyType.Training).TrainingStatMultiplier;
        fighter.AddStat(s, amount, multiplier);
        GameEvents.RaiseRefreshRequested(this, GameManager.Instance.Phase);
    }

    public bool ConsumeActionSlot(int amount = 1)
    {
        if (player.actionsUsed + amount > MaxPlayerActions) return false;
        player.actionsUsed += amount;
        GameEvents.RaiseRefreshRequested(this, GameManager.Instance.Phase);
        return true;
    }

    public bool AddProficiencyExp(ProficiencyType type, int amount)
    {
        var prof = GetProf(type);
        bool leveledUp = prof.AddExp(amount);
        if (leveledUp)
        {
            GameEvents.RaiseProficiencyLevelUp(type, prof.level);
        }
        GameEvents.RaiseRefreshRequested(this, GameManager.Instance.Phase);
        return leveledUp;
    }

    public void ModifyEndingVar(EndingVar varType, int amount)
    {
        endingVars.Modify(varType, amount);
        GameEvents.RaiseRefreshRequested(this, GameManager.Instance.Phase);
    }

    // ===== 생성자 =====
    public GameState()
    {
        foreach (ProficiencyType p in Enum.GetValues(typeof(ProficiencyType)))
            proficiencies[p] = new Proficiency();
            
        // FighterData 및 PlayerData는 선언 시 초기화됨
    }

    // ===== 숙련도 접근 =====
    public Proficiency GetProf(ProficiencyType p)
    {
        if (!proficiencies.ContainsKey(p))
            proficiencies[p] = new Proficiency();
        return proficiencies[p];
    }

    // ===== 날짜/판정 =====
    public string DateString => CalendarSystem.FormatDate(player.day);
    public bool IsArenaOpen => CalendarSystem.IsArenaDay(player.day);
    public bool IsPromotionDay => CalendarSystem.IsPromotionDay(player.day);
    public bool IsDayOver => player.actionsUsed >= MaxPlayerActions;
    public int RemainingActions => MaxPlayerActions - player.actionsUsed;

    // ===== 헬퍼 메서드 (UI용) =====
    public int GetStat(TrainingStat s) => fighter.GetStat(s);
    public int GetTotalPower() => fighter.CalculateTotalPower();

    // ===== 전투 스탯 계산 ★ =====
    public CombatBaseStats GetCombatStats()
    {
        return CombatStatProcessor.CalculateStats(this, combatData);
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

    // ===== 하루 리셋 (Data Reset Only) =====
    public void ResetForNewDay()
    {
        for (int i = 0; i < DaySlotCount; i++)
        {
            fighter.yesterdaySchedule[i].type = fighter.schedule[i].type;
            fighter.yesterdaySchedule[i].trainingStat = fighter.schedule[i].trainingStat;
        }

        player.day++;
        fighter.slotProgress = 0;
        player.actionsUsed = 0;
        player.location = MapLocation.None;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        player.todayGoldEarned = 0;
        trainingEfficiency = 1.0f;
        dailyActivityLogs.Clear();
    }

    // ===== 전체 초기화 (새 게임) =====
    public void ResetAll()
    {
        fighter.slotProgress = 0;
        player.actionsUsed = 0;
        player.location = MapLocation.None;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        fighter.todayTrainingCount = 0;
        player.todayGoldEarned = 0;
        player.gold = 0;
        player.reputation = 0;

        foreach (TrainingStat s in Enum.GetValues(typeof(TrainingStat)))
        {
            if (s != TrainingStat.None)
                fighter.stats[s] = 0;
        }

        foreach (ProficiencyType p in Enum.GetValues(typeof(ProficiencyType)))
            proficiencies[p] = new Proficiency();

        for (int i = 0; i < DaySlotCount; i++)
        {
            fighter.schedule[i] = new FighterSlot();
            fighter.yesterdaySchedule[i] = new FighterSlot();
        }

        endingVars = new EndingVariables();
        arena = new ArenaSystem();
        quests = new QuestSystem();
        combatData = new PlayerCombatData();
        lastBattleReport = "";
    }
}
