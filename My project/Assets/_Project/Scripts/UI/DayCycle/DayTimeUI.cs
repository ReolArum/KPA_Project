using UnityEngine;
using TMPro;

public class DayTimeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private string _format = "남은 이동 기회: {0} / 4";

    private void OnEnable()
    {
        GameEvents.OnRefreshRequested += HandleRefresh;
        if (GameManager.Instance != null && GameManager.Instance.State != null)
            UpdateDisplay(GameManager.Instance.State.RemainingActions);
    }

    private void OnDisable()
    {
        GameEvents.OnRefreshRequested -= HandleRefresh;
    }

    private void HandleRefresh(GameState state, GamePhase phase)
    {
        UpdateDisplay(state.RemainingActions);
    }

    private void UpdateDisplay(int remainingSlots)
    {
        if (_timeText != null)
        {
            _timeText.text = string.Format(_format, remainingSlots);
        }
    }
}
