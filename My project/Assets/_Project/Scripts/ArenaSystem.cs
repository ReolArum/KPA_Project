// ===== ArenaSystem.cs =====
// 아레나 전적/랭크 관리만 담당
// 실제 전투 판정은 BattleSceneController + GameManager.OnBattleFinished에서 처리
using System;
using UnityEngine;

[Serializable]
public class ArenaSystem
{
    public ArenaRank currentRank    = ArenaRank.Bronze;
    public int       wins           = 0;
    public int       losses         = 0;
    public int       promotionWins  = 0;
    public int       promotionLosses = 0;

    public string GetRankName() => currentRank switch
    {
        ArenaRank.Bronze   => "브론즈",
        ArenaRank.Silver   => "실버",
        ArenaRank.Gold     => "골드",
        ArenaRank.Platinum => "플래티넘",
        ArenaRank.Champion => "챔피언",
        _                  => "?"
    };

    /// <summary>승급 처리 (GameManager에서 호출)</summary>
    public void Promote()
    {
        if (currentRank < ArenaRank.Champion)
            currentRank++;
    }
}

[Serializable]
public class ArenaBattleResult
{
    public bool      won;
    public int       goldReward;
    public int       reputationChange;
    public bool      isPromotion;
    public ArenaRank oldRank;
    public ArenaRank newRank;
    public string    message;
}
