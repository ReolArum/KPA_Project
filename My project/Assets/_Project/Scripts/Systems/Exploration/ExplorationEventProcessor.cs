using System.Collections.Generic;
using UnityEngine;

public class ExplorationEventProcessor : MonoBehaviour
{
    public static ExplorationEventProcessor Instance { get; private set; }
    private ExplorationNodeData currentEventNode; // [ADD] 현재 처리 중인 노드 저장

    void Awake()
    {
        Instance = this;
    }

    public void ProcessEvent(ExplorationNodeData node)
    {
        currentEventNode = node; // [ADD] 현재 노드 캐싱
        Debug.Log($"Processing Event: {node.nodeId} ({node.eventType})");

        // 2. 가용한 선택지 필터링 (유저 요청: 조건 미충족 시 아예 안 보임)
        List<ExplorationChoiceData> visibleChoices = FilterChoices(node.choices);

        var expState = ExplorationManager.Instance.currentState;

        // 3. [FIX] 강제 패널티 조건 수정
        // (필터링 단계에서 이미 함정 유무와 선택권 개수를 체크했으므로, 
        // 최종 가용 선택지가 0개일 때만 패널티를 적용하면 됩니다.)
        if (visibleChoices.Count == 0)
        {
            Debug.LogWarning("No available choices! Applying force penalty.");
            
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

        // [FIX] GameManager가 없는 환경(씬 단독 실행 등)에서의 NullReferenceException 방지
        GameState state = (GameManager.Instance != null) ? GameManager.Instance.State : new GameState();
        
        var expManager = ExplorationManager.Instance;
        if (expManager == null) return allChoices; // 에러 방지: Manager가 없으면 필터링 없이 반환

        var expState = expManager.currentState;

        foreach (var choice in allChoices)
        {
            // [FIX] 탈출(Exit) 타입은 선택권이 없어도(0이라도) 항상 노출되어야 함
            bool isExitChoice = choice.type == ExplorationChoiceType.Exit;
            
            // [New] 함정(Hazard)이 아닌 경우에는 선택권 소모가 없으므로 항상 노출
            bool isHazard = currentEventNode != null && currentEventNode.eventType == ExplorationEventType.Hazard;
            bool canShowWithoutChoice = !isHazard || isExitChoice;

            if (expState.remainingChoices <= 0 && !canShowWithoutChoice) continue; 

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

        // 1. 선택 횟수 차감 (유저 요청: 함정 조우 시 && 취소가 아닐 때만 소모)
        bool isHazard = currentEventNode != null && currentEventNode.eventType == ExplorationEventType.Hazard;
        bool isCancel = choice.type == ExplorationChoiceType.Cancel;

        if (isHazard && !isCancel && expState.remainingChoices > 0)
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

        Debug.Log($"Choice Applied: {choice.label}. Type: {choice.type}. Redraw: {choice.shouldRedrawPath}");
        
        // 4. 탈출 선택지일 경우 즉시 정산 종료
        if (choice.type == ExplorationChoiceType.Exit)
        {
            ExplorationManager.Instance.OnExplorationSucceeded();
            return;
        }

        // 5. 탐사 재개 (경로 재작성 여부 전달)
        ExplorationManager.Instance.ResumeMovement(choice.shouldRedrawPath);
    }
}
