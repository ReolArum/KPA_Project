using UnityEngine;
using TMPro;

public class DayTimeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private string _format = "남은 이동 기회: {0} / 8";

    private void Start()
    {
        if (DayTimeManager.Instance != null)
        {
            DayTimeManager.Instance.OnDayTimeUpdated += UpdateDisplay;
            UpdateDisplay(DayTimeManager.Instance.RemainingSlots);
        }
    }

    private void OnDestroy()
    {
        if (DayTimeManager.Instance != null)
        {
            DayTimeManager.Instance.OnDayTimeUpdated -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(int remainingSlots)
    {
        if (_timeText != null)
        {
            _timeText.text = string.Format(_format, remainingSlots);
        }
    }
}
