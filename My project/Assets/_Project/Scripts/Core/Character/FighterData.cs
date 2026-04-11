using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FighterData
{
    // ===== 전투체 스탯 (6대 스탯) =====
    public Dictionary<TrainingStat, int> stats = new();
    
    // ===== 컨디션 =====
    public int stress = 0;
    public int fatigue = 0;

    // ===== 스케줄 =====
    public FighterSlot[] schedule;
    public FighterSlot[] yesterdaySchedule;
    public int slotProgress = 0;

    // ===== 일일 카운터 =====
    public int todayTrainingCount = 0;

    public FighterData(int daySlotCount)
    {
        foreach (TrainingStat s in Enum.GetValues(typeof(TrainingStat)))
            stats[s] = 0;

        schedule = new FighterSlot[daySlotCount];
        yesterdaySchedule = new FighterSlot[daySlotCount];
        for (int i = 0; i < daySlotCount; i++)
        {
            schedule[i] = new FighterSlot();
            yesterdaySchedule[i] = new FighterSlot();
        }
    }

    public int GetStat(TrainingStat s) => stats.ContainsKey(s) ? stats[s] : 0;

    public void AddStat(TrainingStat s, int amount, float multiplier = 1.0f)
    {
        if (!stats.ContainsKey(s)) stats[s] = 0;
        stats[s] += Mathf.RoundToInt(amount * multiplier);
    }

    /// <summary>
    /// 6대 스탯의 합산을 통해 현재 전투력을 계산합니다.
    /// </summary>
    public int CalculateTotalPower()
    {
        int total = 0;
        foreach (var stat in stats)
        {
            if (stat.Key != TrainingStat.None)
                total += stat.Value;
        }
        return total;
    }
}
