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
    /// 지정된 스케줄 슬롯을 하나 실행합니다.
    /// </summary>
    public void ExecuteSlot(FighterSlot slot)
    {
        var state = GameManager.Instance.State;
        var profTrain = state.GetProf(ProficiencyType.Training);
        var profPart = state.GetProf(ProficiencyType.PartTime);

        switch (slot.type)
        {
            case FighterSlotType.Training:
                int amount = baseTrainAmount + state.facilityUpgradeLevel; // 시설 레벨 보너스
                amount = Mathf.RoundToInt(amount * state.trainingEfficiency); // 음식 버프

                state.AddStat(slot.trainingStat, amount);
                state.fighter.fatigue += trainFatigue;
                state.fighter.stress += trainStress;
                state.fighter.todayTrainingCount++;
                
                if (profTrain.AddExp(amount)) 
                    GameEvents.RaiseProficiencyLevelUp(ProficiencyType.Training, profTrain.level);

                GameEvents.RaiseFighterSlotResult($"전투체: 훈련({GameManager.GetStatName(slot.trainingStat)}) 완료 (+{amount})");
                break;

            case FighterSlotType.PartTime:
                bool isBigSuccess = Random.value < (0.1f + profPart.PartTimeBigSuccessBonus);
                int reward = isBigSuccess ? partTimeGoldBonus : partTimeGoldBase;
                
                state.player.gold += reward;
                state.player.todayGoldEarned += reward;
                state.fighter.fatigue += 1;
                
                if (profPart.AddExp(2)) 
                    GameEvents.RaiseProficiencyLevelUp(ProficiencyType.PartTime, profPart.level);

                GameEvents.RaiseFighterSlotResult($"전투체: 알바 {(isBigSuccess ? "대성공" : "완료")} (+{reward}G)");
                break;

            case FighterSlotType.Rest:
                state.fighter.fatigue = Mathf.Max(0, state.fighter.fatigue - restFatigueRecovery);
                state.fighter.stress = Mathf.Max(0, state.fighter.stress - restStressRecovery);
                GameEvents.RaiseFighterSlotResult($"전투체: 휴식 (피로 -{restFatigueRecovery}, 스트레스 -{restStressRecovery})");
                break;
        }
    }

    /// <summary>
    /// 전체 스케줄에 따른 일일 예상 결과 수치를 문자열로 반환합니다.
    /// </summary>
    public string GetPredictedOutcome(FighterSlot[] schedule, int facilityLevel, float efficiency, int currentFatigue)
    {
        int tStr = 0, tAgi = 0, tDex = 0, tEnd = 0, tFat = 0, tStrss = 0;

        foreach (var s in schedule)
        {
            if (s.type == FighterSlotType.Training)
            {
                int val = Mathf.RoundToInt((baseTrainAmount + facilityLevel) * efficiency);
                if (s.trainingStat == TrainingStat.Strength) tStr += val;
                else if (s.trainingStat == TrainingStat.Agility) tAgi += val;
                else if (s.trainingStat == TrainingStat.Dexterity) tDex += val;
                else if (s.trainingStat == TrainingStat.Endurance) tEnd += val;
                
                tFat += trainFatigue; 
                tStrss += trainStress;
            }
            else if (s.type == FighterSlotType.Rest)
            {
                tFat -= restFatigueRecovery; 
                tStrss -= restStressRecovery;
            }
        }

        tFat = Mathf.Max(-currentFatigue, tFat);
        return $"[일간 예상 수치]\n힘: +{tStr}, 민: +{tAgi}, 기: +{tDex}, 체: +{tEnd}\n피로: {(tFat >= 0 ? "+" : "")}{tFat}, 스트레스: {(tStrss >= 0 ? "+" : "")}{tStrss}";
    }
}
