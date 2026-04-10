using System;

[Serializable]
public class PlayerData
{
    public int gold = 100;
    public int reputation = 0;
    
    // ===== 플레이어 위치 및 행동 =====
    public int actionsUsed = 0;
    public MapLocation location = MapLocation.None;

    public int todayGoldEarned = 0;
}
