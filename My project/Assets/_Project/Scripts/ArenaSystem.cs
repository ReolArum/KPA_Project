using System;
using UnityEngine;

[Serializable]
public class ArenaSystem
{
    public ArenaRank currentRank = ArenaRank.Bronze;
    public int wins = 0;
    public int losses = 0;
    public int promotionWins = 0;
    public int promotionLosses = 0;

    /// <summary>
    /// 기존 확률 기반 일반 전투. BattleManager가 없을 때 폴백으로 사용.
    /// BattleManager가 있을 경우 이 메서드는 호출되지 않음.
    /// </summary>
    public ArenaBattleResult ProcessNormalBattle(GameState state)
    {
        int totalStat = state.GetTotalPower();

        float winChance = Mathf.Clamp01(0.3f + totalStat * 0.005f);
        bool won = UnityEngine.Random.value < winChance;

        if (won)
        {
            wins++;
            return new ArenaBattleResult
            {
                won = true,
                goldReward = 20,
                reputationChange = 3,
                message = "아레나 승리!"
            };
        }
        else
        {
            losses++;
            return new ArenaBattleResult
            {
                won = false,
                goldReward = 5,
                reputationChange = -1,
                message = "아레나 패배..."
            };
        }
    }

    /// <summary>
    /// 기존 확률 기반 승급전. BattleManager가 없을 때 폴백으로 사용.
    /// BattleManager가 있을 경우 이 메서드는 호출되지 않음.
    /// </summary>
    public ArenaBattleResult ProcessPromotionBattle(GameState state)
    {
        int totalStat = state.GetTotalPower();

        float winChance = Mathf.Clamp01(0.2f + totalStat * 0.005f);
        bool won = UnityEngine.Random.value < winChance;

        if (won)
        {
            promotionWins++;
            ArenaRank oldRank = currentRank;
            PromoteRank();

            return new ArenaBattleResult
            {
                won = true,
                goldReward = 50,
                reputationChange = 10,
                isPromotion = true,
                oldRank = oldRank,
                newRank = currentRank,
                message = $"승급전 승리! {oldRank} → {currentRank}"
            };
        }
        else
        {
            promotionLosses++;
            return new ArenaBattleResult
            {
                won = false,
                goldReward = 10,
                reputationChange = -3,
                isPromotion = true,
                oldRank = currentRank,
                newRank = currentRank,
                message = "승급전 패배... 다음 기회를 노리세요."
            };
        }
    }

    void PromoteRank()
    {
        if (currentRank < ArenaRank.Champion)
            currentRank++;
    }

    public string GetRankName() => currentRank switch
    {
        ArenaRank.Bronze => "브론즈",
        ArenaRank.Silver => "실버",
        ArenaRank.Gold => "골드",
        ArenaRank.Platinum => "플래티넘",
        ArenaRank.Champion => "챔피언",
        _ => "?"
    };
}

[Serializable]
public class ArenaBattleResult
{
    public bool won;
    public int goldReward;
    public int reputationChange;
    public bool isPromotion;
    public ArenaRank oldRank;
    public ArenaRank newRank;
    public string message;
}
