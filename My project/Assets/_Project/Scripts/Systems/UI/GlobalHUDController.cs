using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GlobalHUDController : MonoBehaviour
{
    [Header("HUD Text Elements")]
    [SerializeField] private TMP_Text textDay;
    [SerializeField] private TMP_Text textTime;
    [SerializeField] private TMP_Text textGold;
    [SerializeField] private TMP_Text textActions;

    [Header("Stat Bars (Sliders)")]
    [SerializeField] private Slider barStrength;
    [SerializeField] private Slider barAgility;
    [SerializeField] private Slider barDexterity;
    [SerializeField] private Slider barEndurance;
    [SerializeField] private Slider barStress;
    [SerializeField] private Slider barFatigue;
    [SerializeField] private Slider barReputation;
    [SerializeField] private Slider barEvaluation;

    [Header("Stat Value Texts (Optional)")]
    [SerializeField] private TMP_Text valStrength;
    [SerializeField] private TMP_Text valEndurance;
    [SerializeField] private TMP_Text valStress;

    [Header("Buttons")]
    [SerializeField] private Button btnOpenCalendar;

    public void Refresh(GameState state, GamePhase phase)
    {
        if (state == null) return;

        if (textDay) textDay.text = $"{state.DateString} (Day {state.day})";
        if (textGold) textGold.text = $"{state.gold}";
        if (textTime) textTime.text = GameManager.GetCurrentTimeLabel(state, phase);
        
        // Stats
        UpdateBar(barStrength, valStrength, state.GetStat(TrainingStat.Strength), 100);
        UpdateBar(barAgility, null, state.GetStat(TrainingStat.Agility), 100);
        UpdateBar(barDexterity, null, state.GetStat(TrainingStat.Dexterity), 100);
        UpdateBar(barEndurance, valEndurance, state.GetStat(TrainingStat.Endurance), 100);
        
        UpdateBar(barStress, valStress, state.stress, 100);
        UpdateBar(barFatigue, null, state.fatigue, 100);
        UpdateBar(barReputation, null, state.reputation, 1000);
        UpdateBar(barEvaluation, null, state.GetTotalPower(), 500); // 전사평가 = 총합 기준

        if (textActions)
        {
            int remaining = GameState.MaxPlayerActions - state.playerActionsUsed;
            textActions.text = $"{remaining}/{GameState.MaxPlayerActions}";
            textActions.color = remaining <= 0 ? Color.red : Color.white;
        }
    }

    private void UpdateBar(Slider bar, TMP_Text text, float value, float max)
    {
        if (bar != null)
        {
            bar.maxValue = max;
            bar.value = value;
        }
        if (text != null)
        {
            text.text = value.ToString();
        }
    }
}
