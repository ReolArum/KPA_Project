using System;
using UnityEngine;

[Serializable]
public class GameState
{
    // ===== 기본 =====
    public int day = 1;
    public int gold = 0;

    // ===== 낮 스케줄 (4슬롯) =====
    public const int DaySlotCount = 4;
    public DaySlotType[] daySchedule = new DaySlotType[DaySlotCount];
    public int currentDaySlot = 0;

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

    // ===== 오늘 누적 =====
    public int todayTrainingCount = 0;
    public int todayGoldEarned = 0;

    // ===== 훈련 대기 (세부 선택 필요 플래그) =====
    public bool waitingForTrainingChoice = false;

    // ===== 아레나 =====
    public bool IsArenaOpen => day % 3 == 0;

    // ===== 숙련도 조회 헬퍼 =====
    public Proficiency GetProficiency(ProficiencyType type) => type switch
    {
        ProficiencyType.Training => profTraining,
        ProficiencyType.Investigation => profInvestigation,
        ProficiencyType.Exploration => profExploration,
        ProficiencyType.PartTime => profPartTime,
        _ => profTraining
    };

    // ===== 스탯 적용 헬퍼 =====
    public void AddStat(TrainingStat stat, int baseAmount)
    {
        // 훈련 숙련도 보너스 적용
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
    public void ResetForNewDay()
    {
        day += 1;
        currentDaySlot = 0;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        waitingForTrainingChoice = false;
        todayTrainingCount = 0;
        todayGoldEarned = 0;

        for (int i = 0; i < DaySlotCount; i++)
            daySchedule[i] = DaySlotType.Rest;
    }
}
