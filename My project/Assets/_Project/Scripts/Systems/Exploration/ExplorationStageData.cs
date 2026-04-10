using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewExplorationStage", menuName = "KPA/Exploration/StageData")]
public class ExplorationStageData : ScriptableObject
{
    public string stageName;
    public float  limitTime = 120f;   
    public int    maxEnemyTickets = 5; 
    public Vector3 startPosition;     

    [Header("Event Nodes")]
    public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
}
