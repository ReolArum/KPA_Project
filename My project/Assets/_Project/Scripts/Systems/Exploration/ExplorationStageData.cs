using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewExplorationStage", menuName = "KPA/Exploration/StageData")]
public class ExplorationStageData : ScriptableObject
{
    public string stageName;
    public float  limitTime = 120f;   // 기본 2분
    public int    maxChoices = 5;     // -1이면 무제한 (유저 요청: 유연한 설계)
    public Vector3 startPosition;     // [ADD] 탐사 시작 지점

    [Header("Map Visuals")]
    public Sprite blueprintSprite;    // 지도 배경 (청사진)
    public GameObject mapPrefab;      // 실제 3D 맵 (있을 경우)

    [Header("Event Nodes")]
    public List<ExplorationNodeData> nodes = new List<ExplorationNodeData>();
}

[System.Serializable]
public class ExplorationNodeData
{
    public string               nodeId;
    public string               nodeName;         // UI에 표시될 이름 (단서 등)
    public Vector3              worldPosition;
    public ExplorationEventType eventType;

    [Header("Ranges")]
    public float clueRange = 2.0f;        // 단서 자동 획득 범위
    public float interactionRange = 1.5f; // 상호작용 프롬프트 노출 범위

    [Header("Visual Novel Cutscene")]
    public List<VNDialogueStep> vnSequence = new List<VNDialogueStep>();
    public string               interactPrompt = "조사하기";

    [Header("Conditions (Requirements)")]
    public List<ExplorationRequirement> requirements = new List<ExplorationRequirement>();

    [Header("Choices (Displayed if type matches)")]
    public List<ExplorationChoiceData> choices = new List<ExplorationChoiceData>();
}

[System.Serializable]
public class VNDialogueStep
{
    public string characterName;
    [TextArea(3, 5)]
    public string dialogueText;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite backgroundOverride;
}

[System.Serializable]
public class ExplorationRequirement
{
    public enum RequirementType { None, StatAtLeast, HasItem, HasEnvObject, HasClue }
    
    public RequirementType type;
    public TrainingStat    statType; // StatAtLeast 일 때 사용
    public int             minValue;
    public string          targetId; // Item, EnvObject, Clue 일 때 이름/ID
}

[System.Serializable]
public class ExplorationChoiceData
{
    public string                label;
    public ExplorationChoiceType type;
    
    [Header("Result")]
    public int   goldReward;
    public float timePenalty;         // 소모 시간
    public float timeGain;            // 시각 획득 (있을 경우)
    public string rewardObjectId;     // 획득하는 단서/오브젝트 ID
    public bool   shouldRedrawPath;   // 이 선택 후 경로를 다시 그려야 하는지 여부
    
    // 이 선택지가 보이기 위한 조건 (유저 요청: 미충족 시 안 보임)
    public List<ExplorationRequirement> ownRequirements = new List<ExplorationRequirement>();
}
