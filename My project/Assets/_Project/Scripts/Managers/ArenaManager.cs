using UnityEngine;

/// <summary>
/// 아레나의 승패, 승급, 보상을 판정하는 매니저.
/// </summary>
public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 전투 결과를 분석하고 보상을 지급하며 승급 여부를 결정합니다.
    /// </summary>
    public ArenaBattleResult ProcessMatchResult(bool won, int baseGold, int baseRep)
    {
        var state = GameManager.Instance.State;
        var arena = state.arena;
        var result = new ArenaBattleResult { won = won };

        result.oldRank = arena.currentRank;

        if (won)
        {
            arena.wins++;
            arena.promotionWins++;
            result.goldReward = baseGold;
            result.reputationChange = baseRep;
            result.message = "승리했습니다!";

            // 승급 조건 체크 (예: 3승 시 승급)
            if (arena.promotionWins >= 3)
            {
                result.isPromotion = true;
                arena.Promote();
                arena.promotionWins = 0;
                arena.promotionLosses = 0;
                result.message += $" {arena.GetRankName()}으로 승급했습니다!";
            }
        }
        else
        {
            arena.losses++;
            arena.promotionLosses++;
            result.goldReward = baseGold / 4; // 패배 시 골드 25%만 지급
            result.reputationChange = -baseRep/2;
            result.message = "패배했습니다...";
        }

        result.newRank = arena.currentRank;
        
        // 실제 보상 적용
        state.AddGold(result.goldReward);
        state.AddReputation(result.reputationChange);

        SaveSystem.Save(state);
        return result;
    }
}
