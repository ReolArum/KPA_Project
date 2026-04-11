using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 의뢰(Quest) 진행 상태와 보상, 일치 여부를 판정하는 매니저.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Data Pool")]
    [SerializeField] private List<QuestData> questPool = new List<QuestData>();
    [SerializeField] private bool dailyRerollUsed = false;

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

    public bool IsRerollUsed => dailyRerollUsed;
    public void SetRerollUsed(bool used) => dailyRerollUsed = used;

    /// <summary>
    /// 등록된 퀘스트 풀에서 오늘의 의뢰를 생성합니다.
    /// </summary>
    public void GenerateDailyQuests(int day)
    {
        var state = GameManager.Instance.State;
        state.quests.availableQuests.Clear();

        if (questPool == null || questPool.Count == 0) return;

        // 오늘의 시드 기반 무작위 추첨 (2~3개)
        Random.InitState(day * 13);
        int count = Mathf.Min(questPool.Count, Random.Range(2, 4));
        
        List<QuestData> selected = new List<QuestData>(questPool);
        // Shuffle
        for (int i = 0; i < selected.Count; i++)
        {
            int rnd = Random.Range(0, selected.Count);
            var tmp = selected[i];
            selected[i] = selected[rnd];
            selected[rnd] = tmp;
        }

        for (int i = 0; i < count; i++)
        {
            var qData = selected[i];
            state.quests.availableQuests.Add(new Quest
            {
                id = i, 
                questId = qData.questId,
                title = qData.questName,
                description = qData.description,
                pickupLocation = qData.pickupLocation,
                deliverLocation = qData.deliverLocation,
                goldReward = qData.goldReward,
                repReward = qData.repReward
            });
        }
    }

    public void AcceptQuest(Quest q)
    {
        var state = GameManager.Instance.State;
        q.isAccepted = true;
        state.quests.activeQuests.Add(q);
        state.quests.availableQuests.Remove(q);
        GameEvents.RaiseActionResult($"{q.title} 수령 완료!");
    }

    public Quest CheckDelivery(MapLocation location)
    {
        var state = GameManager.Instance.State;
        return state.quests.activeQuests.Find(q => q.deliverLocation == location && q.isAccepted && !q.isCompleted);
    }

    public void CompleteQuest(Quest quest)
    {
        var state = GameManager.Instance.State;
        
        state.AddGold(quest.goldReward);
        state.AddReputation(quest.repReward);

        quest.isCompleted = true;
        state.quests.activeQuests.Remove(quest);
        state.quests.completedQuests.Add(quest);
        
        GameEvents.RaiseActionResult($"{quest.title} 배달 성공! (+{quest.goldReward}G)");
        SaveSystem.Save(state);
    }
}
