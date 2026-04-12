using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum DialogueEffectType
{
    Gold,
    Time,           // 탐사 시 사용
    EnemyTickets,   // 탐사 시 사용
    EnvObjectGain,  // 탐사 시 사용
    EnvObjectLoss,  // 탐사 시 사용
    ItemGain,       // 인벤토리 아이템 획득
    ItemLoss,       // 인벤토리 아이템 소실
    DialogueEvent,  // [NEW] 커스텀 이벤트 발생용
    StatChange      // [ADD] 전투체 6대 스탯 변경
}

[System.Serializable]
public class DialogueEffect
{
    public DialogueEffectType type;
    public float              amount;   // 수량
    public string             targetId; // 아이템 ID 또는 오브젝트 ID
    public TrainingStat       statType; // [ADD] 스탯 변경 시 대상 스탯
}

[System.Serializable]
public class DialogueStep
{
    public string characterName;
    [TextArea(3, 5)]
    public string dialogueText;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite backgroundOverride;
}

[System.Serializable]
public class DialogueRequirement
{
    public enum RequirementType { None, StatAtLeast, HasItem, HasEnvObject }
    
    public RequirementType type;
    public TrainingStat    statType; 
    public int             minValue;
    public string          targetId; 
}

[System.Serializable]
public class DialogueChoiceData
{
    public string                  label;
    public ExplorationChoiceType   type; // 기존 Enum 유지 (Combat, Exit 등)
    
    [Header("Results")]
    public List<DialogueEffect>    effects = new List<DialogueEffect>();
    public bool                    shouldRedrawPath;   
    
    [Header("Conditions (Choice Visibility)")]
    public List<DialogueRequirement> ownRequirements = new List<DialogueRequirement>();
}

[System.Serializable]
public class DialogueNodeData
{
    public string               nodeId;
    public string               nodeName;         
    public Vector3              worldPosition;
    public ExplorationEventType eventType;
    public bool                 isOneTime = true; 
    public bool                 isInfoRequired = false; // [ADD] 낮에 정보를 얻어야만 노출되는지 여부

    [Header("Visual Novel Cutscene")]
    public List<DialogueStep>   vnSequence = new List<DialogueStep>();

    [Header("Conditions (Entry Requirements)")]
    public List<DialogueRequirement> requirements = new List<DialogueRequirement>();

    [Header("Choices")]
    public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();

    [Header("Force Effects (If no choices available)")]
    public List<DialogueEffect> forceEffects = new List<DialogueEffect>();
    public string forceFailMessage;

    [Header("Exploration Only")]
    public float envObjectRange = 2.0f;    
    public float interactionRange = 1.5f; 
}
