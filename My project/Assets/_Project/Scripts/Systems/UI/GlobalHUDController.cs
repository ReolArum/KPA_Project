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
    [SerializeField] private Slider barVitality;
    [SerializeField] private Slider barIntelligence;
    [SerializeField] private Slider barGuts;
    [SerializeField] private Slider barSensitivity;
    [SerializeField] private Slider barStress;
    [SerializeField] private Slider barFatigue;
    [SerializeField] private Slider barReputation;
    [SerializeField] private Slider barEvaluation;

    [Header("Stat Value Texts (Optional)")]
    [SerializeField] private TMP_Text valStrength;
    [SerializeField] private TMP_Text valVitality;
    [SerializeField] private TMP_Text valStress;

    [Header("Buttons")]
    [SerializeField] private Button btnOpenCalendar;

    public void Refresh(GameState state, GamePhase phase)
    {
        if (state == null) return;

        if (textDay) textDay.text = $"{state.DateString} (Day {state.player.day})";
        if (textGold) textGold.text = $"{state.player.gold}";
        if (textTime) textTime.text = GameManager.GetCurrentTimeLabel(state, phase);
        
        // Stats
        UpdateBar(barStrength, valStrength, state.GetStat(TrainingStat.Strength), 100);
        UpdateBar(barAgility, null, state.GetStat(TrainingStat.Agility), 100);
        UpdateBar(barVitality, valVitality, state.GetStat(TrainingStat.Vitality), 100);
        UpdateBar(barIntelligence, null, state.GetStat(TrainingStat.Intelligence), 100);
        UpdateBar(barGuts, null, state.GetStat(TrainingStat.Guts), 100);
        UpdateBar(barSensitivity, null, state.GetStat(TrainingStat.Sensitivity), 100);
        
        UpdateBar(barStress, valStress, state.fighter.stress, 100);
        UpdateBar(barFatigue, null, state.fighter.fatigue, 100);
        UpdateBar(barReputation, null, state.player.reputation, 1000);
        UpdateBar(barEvaluation, null, state.GetTotalPower(), 1000); 

        if (textActions)
        {
            int remaining = GameState.MaxPlayerActions - state.player.actionsUsed;
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
