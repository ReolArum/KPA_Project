using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GlobalHUDController : MonoBehaviour
{
    [Header("HUD Text Elements")]
    [SerializeField] private TMP_Text textDay;
    [SerializeField] private TMP_Text textTime;
    [SerializeField] private TMP_Text textGold;
    [SerializeField] private TMP_Text textStress;
    [SerializeField] private TMP_Text textFatigue;
    [SerializeField] private TMP_Text textRank;
    [SerializeField] private TMP_Text textActions;

    [Header("Buttons")]
    [SerializeField] private Button btnOpenCalendar;

    public void Refresh(GameState state, GamePhase phase)
    {
        if (state == null) return;

        if (textDay) textDay.text = $"{state.DateString} (Day {state.day})";
        if (textGold) textGold.text = $"Gold: {state.gold}";
        if (textTime) textTime.text = GameManager.GetCurrentTimeLabel(state, phase);
        if (textStress) textStress.text = $"스트레스: {state.stress}";
        if (textFatigue) textFatigue.text = $"피로: {state.fatigue}";
        if (textRank) textRank.text = $"등급: {state.arena.GetRankName()}";
        
        if (textActions)
        {
            int remaining = GameState.MaxPlayerActions - state.playerActionsUsed;
            textActions.text = $"남은 행동: {remaining}/{GameState.MaxPlayerActions}";
            textActions.color = remaining <= 0 ? Color.red : Color.white;
        }
    }
}
