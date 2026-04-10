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

    // ===== [MOD] 모듈화된 데이터 그룹 =====
    public PlayerData player = new PlayerData();
    public FighterData fighter = new FighterData(DaySlotCount);
    public Inventory inventory = new Inventory();

    // ===== 기존 호환성 프로퍼티 (추후 순차적 제거 가능) =====
    public int day { get => playerDay; set => playerDay = value; }
    private int playerDay = 1;

    public int gold { get => player.gold; set => player.gold = value; }
    public int stress { get => fighter.stress; set => fighter.stress = value; }
    public int fatigue { get => fighter.fatigue; set => fighter.fatigue = value; }

    // ===== [NEW] 장소별 시설/상태 데이터 =====
    public int facilityUpgradeLevel = 0; // 훈련장 업그레이드 단계
    public float trainingEfficiency = 1.0f; // 음식 등에 의한 훈련 효율 버프

    // ===== 플레이어 =====
    public int playerActionsUsed = 0;
    public MapLocation playerLocation = MapLocation.None;  // 초기/복귀 시 None, 이동 시에만 값 세팅

    // ===== 밤 =====
    public NightActionType nightChoice = NightActionType.Rest;
    public bool nightCompleted = false;

    // ===== 숙련도 =====
    public Dictionary<ProficiencyType, Proficiency> proficiencies = new();

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
        foreach (ProficiencyType p in Enum.GetValues(typeof(ProficiencyType)))
            proficiencies[p] = new Proficiency();
            
        // FighterData 및 PlayerData는 선언 시 초기화됨
    }

    // ===== [MOD] 스탯 접근 (Fighter 모듈로 위임) =====
    public int GetStat(TrainingStat s) => fighter.GetStat(s);
 
    public void AddStat(TrainingStat s, int amount)
    {
        float multiplier = GetProf(ProficiencyType.Training).TrainingStatMultiplier;
        fighter.AddStat(s, amount, multiplier);
    }
 
    public int GetTotalPower()
    {
        int total = 0;
        foreach (var kv in fighter.stats)
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

    // ===== 하루 리셋 =====
    public void ResetForNewDay()
    {
        // [NEW] 하루가 넘어가기 전 현재 스케줄을 '어제 스케줄'로 백업
        for (int i = 0; i < DaySlotCount; i++)
        {
            fighter.yesterdaySchedule[i].type = fighter.schedule[i].type;
            fighter.yesterdaySchedule[i].trainingStat = fighter.schedule[i].trainingStat;
        }

        playerDay++;
        fighter.slotProgress = 0;
        player.actionsUsed = 0;
        player.location = MapLocation.None;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        fighter.todayTrainingCount = 0;
        player.todayGoldEarned = 0;
        trainingEfficiency = 1.0f;

        // [QuestManager가 있다면 리롤 상태 리셋]
        if (QuestManager.Instance != null) QuestManager.Instance.SetRerollUsed(false);
    }

    // ===== 전체 초기화 (새 게임) =====
    public void ResetAll()
    {
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
        yesterdaySchedule = new FighterSlot[DaySlotCount];
        for (int i = 0; i < DaySlotCount; i++)
        {
            fighterSchedule[i] = new FighterSlot();
            yesterdaySchedule[i] = new FighterSlot();
        }

        endingVars = new EndingVariables();
        arena = new ArenaSystem();
        quests = new QuestSystem();
        combatData = new PlayerCombatData();
        lastBattleReport = "";
    }
}
