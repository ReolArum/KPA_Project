using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterStatusTab : MonoBehaviour
{
    [Header("Stats (6 Core)")]
    [SerializeField] private TMP_Text textSTR;
    [SerializeField] private TMP_Text textAGI;
    [SerializeField] private TMP_Text textVIT;
    [SerializeField] private TMP_Text textINT;
    [SerializeField] private TMP_Text textGUT;
    [SerializeField] private TMP_Text textSEN;

    [Header("Condition Bars")]
    [SerializeField] private Slider sliderHP;
    [SerializeField] private Slider sliderStress;
    [SerializeField] private Slider sliderFatigue;
    [SerializeField] private TMP_Text textHP;
    [SerializeField] private TMP_Text textStress;
    [SerializeField] private TMP_Text textFatigue;

    public void Refresh(FighterData data)
    {
        if (data == null) return;

        textSTR.text = $"STR: {data.GetStat(TrainingStat.Strength)}";
        textAGI.text = $"AGI: {data.GetStat(TrainingStat.Agility)}";
        textVIT.text = $"VIT: {data.GetStat(TrainingStat.Vitality)}";
        textINT.text = $"INT: {data.GetStat(TrainingStat.Intelligence)}";
        textGUT.text = $"GUT: {data.GetStat(TrainingStat.Guts)}";
        textSEN.text = $"SEN: {data.GetStat(TrainingStat.Sensitivity)}";

        sliderStress.value = data.stress / 100f;
        sliderFatigue.value = data.fatigue / 100f;
        
        textStress.text = $"{data.stress}/100";
        textFatigue.text = $"{data.fatigue}/100";
        
        sliderHP.value = 1.0f;
        textHP.text = "HP: 100/100";
    }
}
