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

    public void SetType(DaySlotType t,
        Color training, Color partTime, Color shop,
        Color investigation, Color relationship, Color rest)
    {
        if (typeText != null)
            typeText.text = UIController.GetDaySlotName(t);

        if (background != null)
            background.color = UIController.GetSlotColor(t,
                training, partTime, shop, investigation, relationship, rest);
    }

    public void SetProgressVisual(int currentSlot)
    {
        bool past = Index < currentSlot;
        bool now = Index == currentSlot;

        if (background == null) return;

        var c = background.color;

        if (past) c.a = 0.4f;
        else c.a = 1f;

        if (now)
        {
            c.r = Mathf.Clamp01(c.r + 0.15f);
            c.g = Mathf.Clamp01(c.g + 0.15f);
            c.b = Mathf.Clamp01(c.b + 0.15f);
        }

        background.color = c;
    }
}
