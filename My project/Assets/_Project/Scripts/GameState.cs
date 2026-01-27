using System;
using UnityEngine;

[Serializable]
public class GameState
{
    public int day = 1;
    public int gold = 0;

    // 0..26 (26이면 22:00 도달)
    public int currentBlock = 0;

    // 길이 26, 각 칸: Strength/Stamina/Rest
    public SlotType[] schedule = new SlotType[GameManager.MaxBlocks];

    public int todayStrengthTrain = 0;
    public int todayStaminaTrain = 0;

    public PlaceType currentPlace = PlaceType.Shop;

    public void ResetForNewDay()
    {
        day += 1;
        currentBlock = 0;
        todayStrengthTrain = 0;
        todayStaminaTrain = 0;

        for (int i = 0; i < schedule.Length; i++)
            schedule[i] = SlotType.Rest;
    }
}
