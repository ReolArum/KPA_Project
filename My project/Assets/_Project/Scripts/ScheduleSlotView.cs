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

    public void SetDirect(string label, Color color)
    {
        if (typeText != null) typeText.text = label;
        if (background != null) background.color = color;
    }

    public void SetSelectionState(bool isSelected)
    {
        if (background == null) return;
        
        // 고전 스타일 선택 효과: 테두리 강조 혹은 색상 반전 등
        var outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = isSelected;
        
        // 배경색 강조
        if (isSelected) background.color = new Color(background.color.r, background.color.g, background.color.b, 1f);
    }

    public void SetProgressVisual(int currentSlot, bool isSelected = false)
    {
        bool past = Index < currentSlot;
        bool now = Index == currentSlot;

        if (background == null) return;

        var c = background.color;
        if (past) c.a = 0.4f;
        else c.a = 1f;

        if (now || isSelected)
        {
            // 선택되었거나 현재 진행 중인 슬롯은 조금 더 밝게
            c.r = Mathf.Clamp01(c.r + 0.15f);
            c.g = Mathf.Clamp01(c.g + 0.15f);
            c.b = Mathf.Clamp01(c.b + 0.15f);
        }

        background.color = c;
        
        SetSelectionState(isSelected);
    }
}
