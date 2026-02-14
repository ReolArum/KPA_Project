using System;
using UnityEngine;

[Serializable]
public class ArenaSystem
{
    public ArenaRank currentRank = ArenaRank.Bronze;
    public int wins = 0;
    public int losses = 0;
    public int promotionWins = 0;   // 승급전 승리 횟수
    public int promotionLosses = 0; // 승급전 패배 횟수

    /// <summary>일반 아레나 전투 결과 처리 (더미)</summary>
    public ArenaBattleResult ProcessNormalBattle(GameState state)
    {
        // TODO: 실제 전투 시스템 연동
        // 지금은 스탯 합산으로 간단한 승률 계산
        int totalStat = state.statStrength + state.statAgility
                      + state.statDexterity + state.statEndurance;

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

    /// <summary>승급전 결과 처리</summary>
    public ArenaBattleResult ProcessPromotionBattle(GameState state)
    {
        int totalStat = state.statStrength + state.statAgility
                      + state.statDexterity + state.statEndurance;

        // 승급전은 좀 더 어려움
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
