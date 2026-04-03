using System.Collections.Generic;
using UnityEngine;

public class ExplorationEventProcessor : MonoBehaviour
{
    public static ExplorationEventProcessor Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void ProcessEvent(ExplorationNodeData node)
    {
        Debug.Log($"Processing Event: {node.nodeId} ({node.eventType})");

        // 2. 가용한 선택지 필터링 (유저 요청: 조건 미충족 시 아예 안 보임)
        List<ExplorationChoiceData> visibleChoices = FilterChoices(node.choices);

        var expState = ExplorationManager.Instance.currentState;

        // [New] 선택권 소진 혹은 유효 선택지 전무 시 강제 패널티 후 즉시 재개
        if (expState.remainingChoices == 0 || visibleChoices.Count == 0)
        {
            Debug.LogWarning("No available choices or turns! Applying force penalty.");
            
            // 패널티 적용
            expState.remainingTime = Mathf.Max(0, expState.remainingTime - node.forcePenaltyTime);
            expState.collectedGold = Mathf.Max(0, expState.collectedGold - node.forcePenaltyGold);
            
            GameEvents.RaiseActionResult($"패널티 적용: 시간 -{node.forcePenaltyTime}s / 골드 -{node.forcePenaltyGold}G");
            
            // 즉시 이동 재개
            ExplorationManager.Instance.ResumeMovement(false);
            return;
        }

        // UI 오픈 로직 (ExplorationUIController가 이 이벤트를 구독하여 팝업을 엶)
        GameEvents.RaiseExplorationEventTriggered(node, visibleChoices);
    }

    private List<ExplorationChoiceData> FilterChoices(List<ExplorationChoiceData> allChoices)
    {
        List<ExplorationChoiceData> filtered = new List<ExplorationChoiceData>();
        var state = GameManager.Instance.State;
        var expState = ExplorationManager.Instance.currentState;

        foreach (var choice in allChoices)
        {
            // '선택 횟수' 제한 체크 (유연한 설계: -1이면 통과)
            if (expState.remainingChoices == 0) continue; 

            // 개별 선택지 노출 조건 체크
            if (CheckRequirements(choice.ownRequirements, state, expState))
            {
                filtered.Add(choice);
            }
        }

        return filtered;
    }

    private bool CheckRequirements(List<ExplorationRequirement> reqs, GameState state, ExplorationState expState)
    {
        if (reqs == null || reqs.Count == 0) return true;

        foreach (var req in reqs)
        {
            switch (req.type)
            {
                case ExplorationRequirement.RequirementType.StatAtLeast:
                    if (state.GetStat(req.statType) < req.minValue) return false;
                    break;

                case ExplorationRequirement.RequirementType.HasItem:
                    // 현재 아이템 시스템 구현 여부에 따라 체크 필요
                    // if (!state.inventory.Has(req.targetId)) return false;
                    break;

                case ExplorationRequirement.RequirementType.HasEnvObject:
                    if (!expState.foundObjectIds.Contains(req.targetId)) return false;
                    break;

                case ExplorationRequirement.RequirementType.HasClue:
                    if (!expState.foundObjectIds.Contains(req.targetId)) return false;
                    break;
            }
        }
        return true;
    }

    public void ApplyChoiceEffect(ExplorationChoiceData choice)
    {
        var expState = ExplorationManager.Instance.currentState;

        // 1. 선택 횟수 차감
        if (expState.remainingChoices > 0)
        {
            expState.remainingChoices--;
        }

        // 2. 시간 패널티 소모
        expState.remainingTime = Mathf.Max(0, expState.remainingTime - choice.timePenalty);

        // 3. 보상 반영
        expState.collectedGold += choice.goldReward;
        if (!string.IsNullOrEmpty(choice.rewardObjectId))
        {
            // 중복 획득 방지
            if (!expState.foundObjectIds.Contains(choice.rewardObjectId))
            {
                expState.foundObjectIds.Add(choice.rewardObjectId);
                GameEvents.RaiseExplorationClueFound(choice.rewardObjectId);
            }
        }

        // 4. [ADD] 소모성 오브젝트 처리
        if (!string.IsNullOrEmpty(choice.consumedObjectId))
        {
            if (expState.foundObjectIds.Contains(choice.consumedObjectId))
            {
                expState.foundObjectIds.Remove(choice.consumedObjectId);
                Debug.Log($"Object Consumed: {choice.consumedObjectId}");
            }
        }

        Debug.Log($"Choice Applied: {choice.label}. Redraw: {choice.shouldRedrawPath}");

        // 4. 탐사 재개 (경로 재작성 여부 전달)
        ExplorationManager.Instance.ResumeMovement(choice.shouldRedrawPath);
    }
}
