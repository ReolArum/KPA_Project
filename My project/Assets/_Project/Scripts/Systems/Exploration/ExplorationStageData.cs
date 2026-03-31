using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewExplorationStage", menuName = "KPA/Exploration/StageData")]
public class ExplorationStageData : ScriptableObject
{
    public string stageName;
    public float  limitTime = 120f;   // 기본 2분
    public int    maxChoices = 5;     // -1이면 무제한 (유저 요청: 유연한 설계)

    [Header("Map Visuals")]
    public Sprite blueprintSprite;    // 지도 배경 (청사진)
    public GameObject mapPrefab;      // 실제 3D 맵 (있을 경우)

    [Header("Event Nodes")]
    public List<ExplorationNodeData> nodes = new List<ExplorationNodeData>();
}

// [ExplorationState는 별도 파일 ExplorationState.cs에 정의됨]

[System.Serializable]
public class ExplorationNodeData
{
    public string               nodeId;
    public Vector3              worldPosition;
    public ExplorationEventType eventType;

    [Header("Conditions (Requirements)")]
    public List<ExplorationRequirement> requirements = new List<ExplorationRequirement>();

    [Header("Choices (Displayed if type matches)")]
    public List<ExplorationChoiceData> choices = new List<ExplorationChoiceData>();
}

[System.Serializable]
public class ExplorationRequirement
{
    public enum RequirementType { None, StatAtLeast, HasItem, HasEnvObject }
    
    public RequirementType type;
    public TrainingStat    statType; // StatAtLeast 일 때 사용
    public int             minValue;
    public string          targetId; // Item이나 EnvObject 일 때 이름/ID
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
    
    // 이 선택지가 보이기 위한 조건 (유저 요청: 미충족 시 안 보임)
    public List<ExplorationRequirement> ownRequirements = new List<ExplorationRequirement>();
}
