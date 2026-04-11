using System.Collections.Generic;
using UnityEngine;

public class ExplorationEventProcessor : MonoBehaviour
{
    public static ExplorationEventProcessor Instance { get; private set; }
    private DialogueNodeData currentEventNode; 

    void Awake()
    {
        Instance = this;
    }

    public void ProcessEvent(DialogueNodeData node)
    {
        currentEventNode = node; 
        Debug.Log($"Processing Event: {node.nodeId} ({node.eventType})");

        List<DialogueChoiceData> visibleChoices = FilterChoices(node.choices);

        if (visibleChoices.Count == 0)
        {
            Debug.LogWarning("No available choices! Applying force effects and retreating.");
            ApplyEffectList(node.forceEffects);
            GameEvents.RaiseActionResult("조건 미충족: 더 이상 진행할 수 없어 후퇴합니다.");
            ExplorationManager.Instance.ResumeMovement(true); 
            return;
        }

        GameEvents.RaiseExplorationEventTriggered(node, visibleChoices);
    }

    public List<DialogueChoiceData> FilterChoices(List<DialogueChoiceData> allChoices)
    {
        List<DialogueChoiceData> filtered = new List<DialogueChoiceData>();

        GameState state = (GameManager.Instance != null) ? GameManager.Instance.State : new GameState();
        var expManager = ExplorationManager.Instance;
        if (expManager == null) return allChoices; 

        var expState = expManager.currentState;

        foreach (var choice in allChoices)
        {
            if (choice.type == ExplorationChoiceType.Combat && expState.remainingEnemyTickets <= 0) 
            {
                continue; 
            }

            if (CheckRequirements(choice.ownRequirements, state, expState))
            {
                filtered.Add(choice);
            }
        }

        return filtered;
    }

    public bool CheckRequirementsExternal(List<DialogueRequirement> reqs)
    {
        GameState state = (GameManager.Instance != null) ? GameManager.Instance.State : new GameState();
        var expManager = ExplorationManager.Instance;
        if (expManager == null) return true;
        return CheckRequirements(reqs, state, expManager.currentState);
    }

    private bool CheckRequirements(List<DialogueRequirement> reqs, GameState state, ExplorationState expState)
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
                    if (!state.inventory.HasItem(req.targetId, (int)req.minValue)) return false;
                    break;
                case DialogueRequirement.RequirementType.HasEnvObject:
                    if (!expState.foundEnvObjectIds.Contains(req.targetId)) return false;
                    break;
            }
        }
        return true;
    }

    public void ApplyChoiceEffect(DialogueChoiceData choice)
    {
        var expState = ExplorationManager.Instance.currentState;

        if (choice.type == ExplorationChoiceType.Combat && expState.remainingEnemyTickets > 0)
        {
            expState.remainingEnemyTickets--;
        }

        ApplyEffectList(choice.effects);

        if (choice.type == ExplorationChoiceType.Exit)
        {
            ExplorationManager.Instance.OnExplorationSucceeded();
            return;
        }

        ExplorationManager.Instance.ResumeMovement(choice.shouldRedrawPath);
    }

    private void ApplyEffectList(List<DialogueEffect> effects)
    {
        if (effects == null) return;
        
        var expManager = ExplorationManager.Instance;
        if (expManager == null) return;
        var expState = expManager.currentState;
        var gameState = (GameManager.Instance != null) ? GameManager.Instance.State : null;

        foreach (var effect in effects)
        {
            switch (effect.type)
            {
                case DialogueEffectType.Gold:
                    expState.collectedGold += (int)effect.amount;
                    break;
                case DialogueEffectType.Time:
                    expState.remainingTime = Mathf.Max(0, expState.remainingTime - effect.amount);
                    break;
                case DialogueEffectType.EnemyTickets:
                    expState.remainingEnemyTickets = (int)Mathf.Max(0, expState.remainingEnemyTickets - effect.amount);
                    break;
                case DialogueEffectType.EnvObjectGain:
                    if (!expState.foundEnvObjectIds.Contains(effect.targetId))
                    {
                        expState.foundEnvObjectIds.Add(effect.targetId);
                        GameEvents.RaiseExplorationEnvObjectFound(effect.targetId);
                    }
                    break;
                case DialogueEffectType.EnvObjectLoss:
                    expState.foundEnvObjectIds.Remove(effect.targetId);
                    break;
                case DialogueEffectType.ItemGain:
                    if (gameState != null) gameState.inventory.AddItem(effect.targetId, (int)effect.amount, 99); // Placeholder maxStack
                    break;
                case DialogueEffectType.ItemLoss:
                    if (gameState != null) gameState.inventory.RemoveItem(effect.targetId, (int)effect.amount);
                    break;
            }
        }
    }
}
