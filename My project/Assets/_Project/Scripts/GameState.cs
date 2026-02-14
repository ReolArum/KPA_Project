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
    public int currentDaySlot = 0; // 0~3 진행중, 4면 낮 종료

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

    // ===== 오늘 누적 =====
    public int todayTrainingCount = 0;
    public int todayGoldEarned = 0;

    // ===== 아레나 =====
    public bool IsArenaOpen => day % 3 == 0; // 3일마다 오픈

    // ===== 초기화 =====
    public void ResetForNewDay()
    {
        day += 1;
        currentDaySlot = 0;
        nightChoice = NightActionType.Rest;
        nightCompleted = false;
        todayTrainingCount = 0;
        todayGoldEarned = 0;

        for (int i = 0; i < DaySlotCount; i++)
            daySchedule[i] = DaySlotType.Rest;
    }
}
