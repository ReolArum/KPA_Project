using UnityEngine;

[CreateAssetMenu(fileName = "NewScheduleAction", menuName = "KPA/Schedule/ActionData")]
public class ScheduleActionData : ScriptableObject
{
    public string actionName;
    public FighterSlotType category;
    
    [Header("Effects")]
    public float statIncreaseAmount; // 스탯 상승량 (훈련 시)
    public TrainingStat targetStat;
    public int goldChange;           // 골드 변화 (알바 수익 등)
    public int stressChange;         // 스트레스 변화 (휴식 등)
    
    [Header("Visuals")]
    public Sprite icon;
    [TextArea] public string description;
}
