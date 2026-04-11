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
        var schedule = state.fighter.schedule;
        
        for (int i = 0; i < _slotUIs.Length; i++)
        {
            if (i >= schedule.Length) break;

            var slot = schedule[i];
            
            string actionName = slot.type switch
            {
                FighterSlotType.Training => GameManager.GetStatName(slot.trainingStat),
                FighterSlotType.Work => "알바",
                FighterSlotType.Rest => "휴식",
                _ => "공백"
            };

            _slotUIs[i].actionNameText.text = actionName;
            _slotUIs[i].bonusText.text = slot.efficiencyMultiplier > 1.0f ? "<color=yellow>X1.5</color>" : "";
            
            // 아이콘은 현재 타입에 따라 기본 아이콘을 넣거나 생략 (현재 Slot에 Icon 필드 없음)
            if (_slotUIs[i].iconImage != null) _slotUIs[i].iconImage.gameObject.SetActive(false);
        }
    }
}
