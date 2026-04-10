using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "KPA/Quest/QuestData")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string questName;
    [TextArea(3, 5)]
    public string description;
    
    public MapLocation pickupLocation;   // 수령 장소
    public MapLocation deliverLocation;  // 배달 장소
    
    public int goldReward;
    public int repReward;

    [Header("Requirements (Optional)")]
    public TrainingStat requiredStat;
    public int          requiredValue;
}
