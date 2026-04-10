using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScheduleStatusUI : MonoBehaviour
{
    [System.Serializable]
    public struct SlotUIElements
    {
        public TMP_Text actionNameText;
        public TMP_Text bonusText;
        public Image iconImage;
    }

    [SerializeField] private SlotUIElements[] _slotUIs;

    private void Start()
    {
        GameEvents.OnGameStateChanged += Refresh;
        Refresh(GameManager.Instance.State);
    }

    private void OnDestroy()
    {
        GameEvents.OnGameStateChanged -= Refresh;
    }

    public void Refresh(GameState state)
    {
        var schedule = FighterScheduleManager.Instance.GetCurrentSchedule();
        
        for (int i = 0; i < _slotUIs.Length; i++)
        {
            if (i >= schedule.Length) break;

            var slot = schedule[i];
            if (slot.actionData != null)
            {
                _slotUIs[i].actionNameText.text = slot.actionData.actionName;
                _slotUIs[i].bonusText.text = slot.isBonusApplied ? "<color=yellow>X1.5</color>" : "";
                if (_slotUIs[i].iconImage != null) _slotUIs[i].iconImage.sprite = slot.actionData.icon;
            }
            else
            {
                _slotUIs[i].actionNameText.text = "공백";
                _slotUIs[i].bonusText.text = "";
            }
        }
    }
}
