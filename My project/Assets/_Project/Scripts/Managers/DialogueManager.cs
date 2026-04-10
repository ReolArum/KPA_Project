using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전역에서 범용적으로 사용되는 대화(VN) 시스템 매니저.
/// 탐사 시스템에 종속되지 않고, 어디서든 DialogueNodeData를 전달받아 대화를 실행합니다.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

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
    /// 새로운 대화 노드를 실행합니다.
    /// </summary>
    public void StartDialogue(DialogueNodeData node, System.Action onComplete = null)
    {
        if (node == null) return;

        Debug.Log($"[DialogueManager] Starting Dialogue: {node.nodeId}");

        // 1. 선택지 필터링 (범용 조건 체크)
        List<DialogueChoiceData> visibleChoices = FilterChoices(node.choices);

        // 2. 가용한 선택지가 없고, 강제 효과가 있다면 처리
        if (visibleChoices.Count == 0 && (node.choices != null && node.choices.Count > 0))
        {
            Debug.LogWarning("[DialogueManager] No available choices. Applying force effects.");
            ApplyEffectList(node.forceEffects);
            GameEvents.RaiseActionResult(node.forceFailMessage ?? "조건을 만족하지 못해 진행할 수 없습니다.");
            onComplete?.Invoke();
            return;
        }

        // 3. UI 알림 (기존 탐사 UI와 호환되거나 범용 UI로 확장)
        // 현재는 탐사 UI가 이 이벤트를 구독하고 있음
        GameEvents.RaiseExplorationVNStarted(node.vnSequence, () => {
            // 대화(VN)가 끝난 후, 선택지가 있다면 표시
            if (visibleChoices.Count > 0)
            {
                GameEvents.RaiseExplorationEventTriggered(node, visibleChoices);
            }
            else
            {
                // 선택지 없는 노드라면 공통 효과 즉시 적용
                ApplyEffectList(node.forceEffects);
                onComplete?.Invoke();
            }
        });
    }

    /// <summary>
    /// 범용적인 선택지 필터링 (스탯 체크 등)
    /// </summary>
    public List<DialogueChoiceData> FilterChoices(List<DialogueChoiceData> allChoices)
    {
        if (allChoices == null) return new List<DialogueChoiceData>();

        List<DialogueChoiceData> filtered = new List<DialogueChoiceData>();
        GameState state = GameManager.Instance.State;

        foreach (var choice in allChoices)
        {
            if (CheckRequirements(choice.ownRequirements, state))
            {
                filtered.Add(choice);
            }
        }
        return filtered;
    }

    private bool CheckRequirements(List<DialogueRequirement> reqs, GameState state)
    {
        if (reqs == null || reqs.Count == 0) return true;

        foreach (var req in reqs)
        {
            switch (req.type)
            {
                case DialogueRequirement.RequirementType.StatAtLeast:
                    if (state.GetStat(req.statType) < req.minValue) return false;
                    break;
                case DialogueRequirement.RequirementType.HasItem:
                    if (!state.inventory.HasItem(req.targetId, 1)) return false;
                    break;
            }
        }
        return true;
    }

    /// <summary>
    /// 공통 효과 적용 (Gold, Item 등)
    /// </summary>
    public void ApplyEffectList(List<DialogueEffect> effects)
    {
        if (effects == null) return;
        
        GameState gameState = GameManager.Instance.State;

        foreach (var effect in effects)
        {
            switch (effect.type)
            {
                case DialogueEffectType.Gold:
                    gameState.player.gold += (int)effect.amount;
                    break;
                case DialogueEffectType.ItemGain:
                    gameState.inventory.AddItem(effect.targetId, (int)effect.amount, 99);
                    break;
                case DialogueEffectType.ItemLoss:
                    gameState.inventory.RemoveItem(effect.targetId, (int)effect.amount);
                    break;
                case DialogueEffectType.DialogueEvent:
                    // TODO: 커스텀 이벤트 처리 (퀘스트 수락 등)
                    Debug.Log($"[DialogueManager] Custom Event: {effect.targetId}");
                    break;
            }
        }
    }
}
