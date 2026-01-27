using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScheduleSlotView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timeLabelText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private Image background;
    [SerializeField] private Button button;

    public int Index { get; private set; }

    UIController owner;

    public void Init(UIController owner, int index)
    {
        this.owner = owner;
        Index = index;

        if (button == null) button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => owner.OnClickScheduleSlot(Index));
    }

    public void SetTimeLabel(string label)
    {
        if (timeLabelText != null) timeLabelText.text = label;
    }

    public void SetType(SlotType t, Color strength, Color stamina, Color rest)
    {
        if (typeText != null) typeText.text = t switch
        {
            SlotType.Strength => "Str",
            SlotType.Stamina => "Stam",
            _ => "Rest"
        };

        if (background != null)
        {
            background.color = t switch
            {
                SlotType.Strength => strength,
                SlotType.Stamina => stamina,
                _ => rest
            };
        }
    }
}
