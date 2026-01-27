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

    public void SetProgressVisual(int currentBlock)
    {
        // 지난 시간은 흐리게
        bool past = Index < currentBlock;
        bool now = Index == currentBlock;

        if (background != null)
        {
            var c = background.color;

            // past: 알파 0.45, now: 약간 밝게, future: 원래
            if (past) c.a = 0.45f;
            else c.a = 1f;

            // now 하이라이트(밝기 업)
            if (now)
            {
                c.r = Mathf.Clamp01(c.r + 0.15f);
                c.g = Mathf.Clamp01(c.g + 0.15f);
                c.b = Mathf.Clamp01(c.b + 0.15f);
            }

            background.color = c;
        }
    }


}
