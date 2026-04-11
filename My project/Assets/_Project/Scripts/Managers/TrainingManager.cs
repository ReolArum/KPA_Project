using UnityEngine;

/// <summary>
/// 전투체의 훈련, 알바, 휴식 등 스케줄 실행과 성장 수치를 관리하는 매니저.
/// </summary>
public class TrainingManager : MonoBehaviour
{
    public static TrainingManager Instance { get; private set; }

    // ===== 설정 상수 (향후 SO로 분리 가능) =====
    [Header("Training Balance")]
    public int baseTrainAmount = 10;
    public int trainFatigue = 15;
    public int trainStress = 10;

    [Header("Rest Balance")]
    public int restFatigueRecovery = 20;
    public int restStressRecovery = 15;

    [Header("Part-time Balance")]
    public int partTimeGoldBase = 10;
    public int partTimeGoldBonus = 20;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지정된 스케줄 슬롯을 하나 실행하고 결과를 반환합니다.
    /// </summary>
    public string ExecuteSlot(FighterSlot slot)
    {
        var state = GameManager.Instance.State;
        var profTrain = state.GetProf(ProficiencyType.Training);
        var profPart = state.GetProf(ProficiencyType.PartTime);
        float totalEff = state.trainingEfficiency * slot.efficiencyMultiplier;

        string log = "";

        switch (slot.type)
        {
            case FighterSlotType.Training:
                int amount = baseTrainAmount + state.facilityUpgradeLevel;
                amount = Mathf.RoundToInt(amount * totalEff);

                state.AddStat(slot.trainingStat, amount);
                state.fighter.fatigue += trainFatigue;
                state.fighter.stress += trainStress;
                state.fighter.todayTrainingCount++;
                
                if (profTrain.AddExp(amount)) 
                    GameEvents.RaiseProficiencyLevelUp(ProficiencyType.Training, profTrain.level);

                log = $"훈련({GameManager.GetStatName(slot.trainingStat)}) 완료 (+{amount})";
                break;

            case FighterSlotType.Work:
                bool isBigSuccess = Random.value < (0.1f + profPart.PartTimeBigSuccessBonus);
                int reward = isBigSuccess ? partTimeGoldBonus : partTimeGoldBase;
                reward = Mathf.RoundToInt(reward * slot.efficiencyMultiplier);
                
                state.AddGold(reward);
                state.fighter.fatigue += 1;
                
                if (profPart.AddExp(2)) 
                    GameEvents.RaiseProficiencyLevelUp(ProficiencyType.PartTime, profPart.level);

                log = $"알바 {(isBigSuccess ? "대성공" : "완료")} (+{reward}G)";
                break;

            case FighterSlotType.Rest:
                int recoveryFat = Mathf.RoundToInt(restFatigueRecovery * slot.efficiencyMultiplier);
                int recoveryStr = Mathf.RoundToInt(restStressRecovery * slot.efficiencyMultiplier);
                state.fighter.fatigue = Mathf.Max(0, state.fighter.fatigue - recoveryFat);
                state.fighter.stress = Mathf.Max(0, state.fighter.stress - recoveryStr);
                log = $"휴식 (피로 -{recoveryFat}, 스트레스 -{recoveryStr})";
                break;
        }

        GameEvents.RaiseFighterSlotResult($"전투체: {log}");
        return log;
    }

    /// <summary>
    /// 전체 스케줄을 순차적으로 실행하고 요약 결과를 반환합니다.
    /// </summary>
    public string ExecuteSchedule(GameState state)
    {
        string fullLog = "오늘의 일과 정산:\n";
        for (int i = 0; i < GameState.DaySlotCount; i++)
        {
            string slotLog = ExecuteSlot(state.fighter.schedule[i]);
            fullLog += $"- {slotLog}\n";
        }
        return fullLog;
    }

    /// <summary>
    /// 전체 스케줄에 따른 일일 예상 결과 수치를 문자열로 반환합니다.
    /// </summary>
    public string GetPredictedOutcome(FighterSlot[] schedule, int facilityLevel, float efficiency, int currentFatigue)
    {
        int tStr = 0, tAgi = 0, tVit = 0, tInt = 0, tGut = 0, tSen = 0, tFat = 0, tStrss = 0, tGold = 0;

        foreach (var s in schedule)
        {
            float totalEff = efficiency * s.efficiencyMultiplier;

            if (s.type == FighterSlotType.Training)
            {
                int val = Mathf.RoundToInt((baseTrainAmount + facilityLevel) * totalEff);
                if (s.trainingStat == TrainingStat.Strength) tStr += val;
                else if (s.trainingStat == TrainingStat.Agility) tAgi += val;
                else if (s.trainingStat == TrainingStat.Vitality) tVit += val;
                else if (s.trainingStat == TrainingStat.Intelligence) tInt += val;
                else if (s.trainingStat == TrainingStat.Guts) tGut += val;
                else if (s.trainingStat == TrainingStat.Sensitivity) tSen += val;
                
                tFat += trainFatigue; 
                tStrss += trainStress;
            }
            else if (s.type == FighterSlotType.Work)
            {
                tGold += Mathf.RoundToInt(partTimeGoldBase * s.efficiencyMultiplier);
                tFat += 1;
            }
            else if (s.type == FighterSlotType.Rest)
            {
                tFat -= Mathf.RoundToInt(restFatigueRecovery * s.efficiencyMultiplier); 
                tStrss -= Mathf.RoundToInt(restStressRecovery * s.efficiencyMultiplier);
            }
        }

        string result = "[일간 예상 수치]\n";
        if (tStr > 0) result += $"힘+{tStr} ";
        if (tAgi > 0) result += $"민첩+{tAgi} ";
        if (tVit > 0) result += $"내구+{tVit} ";
        if (tInt > 0) result += $"지능+{tInt} ";
        if (tGut > 0) result += $"근성+{tGut} ";
        if (tSen > 0) result += $"감각+{tSen} ";
        if (tGold > 0) result += $"골드+{tGold} ";

        result += $"\n피로: {(tFat >= 0 ? "+" : "")}{tFat}, 스트레스: {(tStrss >= 0 ? "+" : "")}{tStrss}";
        return result;
    }

    /// <summary>
    /// 특정 스케줄 슬롯에 보너스 효율을 적용합니다. (훈련 보조 등)
    /// </summary>
    public void ApplyBonus(int slotIndex, float multiplier)
    {
        var state = GameManager.Instance.State;
        if (slotIndex < 0 || slotIndex >= GameState.DaySlotCount) return;
        
        state.fighter.schedule[slotIndex].efficiencyMultiplier *= multiplier;
        GameEvents.RaiseRefreshRequested(state, GameManager.Instance.Phase);
    }
}
