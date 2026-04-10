using UnityEngine;

/// <summary>
/// 캐릭터의 비전투적 성장(숙련도, 명성, 엔딩 변수)을 총괄 관리하는 매니저.
/// </summary>
public class GrowthManager : MonoBehaviour
{
    public static GrowthManager Instance { get; private set; }

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
    /// 플레이어의 명성을 수정하고 관련 이벤트를 발생시킵니다.
    /// </summary>
    public void ModifyReputation(int amount)
    {
        var player = GameManager.Instance.State.player;
        player.reputation += amount;
        
        Debug.Log($"[GrowthManager] Reputation modified by {amount}. Current: {player.reputation}");
        
        // 향후 명성치에 따른 실시간 월드 변화나 해금 로직 추가 가능
    }

    /// <summary>
    /// 엔딩 관련 변수를 수정합니다.
    /// </summary>
    public void ModifyEndingVar(EndingVar varType, int amount)
    {
        var state = GameManager.Instance.State;
        state.endingVars.Modify(varType, amount);
        
        int newValue = state.endingVars.Get(varType);
        Debug.Log($"[GrowthManager] EndingVar {varType} modified by {amount}. New Value: {newValue}");

        // 특정 수치 도달 시의 즉각적인 연출이나 조건 체크 로직 추가 지점
    }

    /// <summary>
    /// 특정 숙련도에 대한 경험치를 추가하고 레벨업 여부를 반환합니다.
    /// </summary>
    public bool AddProficiencyExp(ProficiencyType type, int amount)
    {
        var prof = GameManager.Instance.State.GetProf(type);
        bool leveledUp = prof.AddExp(amount);
        
        if (leveledUp)
        {
            GameEvents.RaiseProficiencyLevelUp(type, prof.level);
        }
        
        return leveledUp;
    }
}
