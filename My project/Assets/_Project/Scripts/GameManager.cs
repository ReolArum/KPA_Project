using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIController ui;

    public GameState State { get; private set; } = new GameState();
    public GamePhase Phase { get; private set; } = GamePhase.Title;

    // ===== 상수 =====
    public const int DaySlotCount = GameState.DaySlotCount;
    public const int HoursPerSlot = 3;
    public const int DayStartHour = 8;
    public const int NightStartHour = 20;
    public const int SleepHour = 22;

    // ===== 훈련 기본 수치 =====
    const int BaseTrainAmount = 2;
    const int TrainFatigue = 3;
    const int TrainStress = 1;

    void Awake()
    {
        if (ui == null) ui = FindFirstObjectByType<UIController>();

        for (int i = 0; i < DaySlotCount; i++)
            State.daySchedule[i] = DaySlotType.Rest;

        SaveSystem.Load(State);
        SetPhase(GamePhase.Title);
    }

    // ====================================================
    //  Phase Transitions
    // ====================================================

    void SetPhase(GamePhase next)
    {
        Phase = next;
        ui.ShowPhase(next);
        ui.RefreshAll(State, Phase);
    }

    // ===== Title =====
    public void OnClickStart()
    {
        SetPhase(GamePhase.ScheduleSetting);
    }

    // ===== Schedule Setting → Day Progress =====
    public void OnClickConfirmSchedule()
    {
        State.currentDaySlot = 0;
        State.waitingForTrainingChoice = false;
        SetPhase(GamePhase.DayProgress);
    }

    // ====================================================
    //  Day Progress
    // ====================================================

    public void OnClickExecuteNextSlot()
    {
        if (Phase != GamePhase.DayProgress) return;

        // 훈련 선택 대기 중이면 무시 (팝업에서 선택해야 함)
        if (State.waitingForTrainingChoice) return;

        if (State.currentDaySlot >= DaySlotCount)
        {
            TransitionToNight();
            return;
        }

        DaySlotType action = State.daySchedule[State.currentDaySlot];

        // 훈련이면 → 세부 선택 팝업
        if (action == DaySlotType.Training)
        {
            State.waitingForTrainingChoice = true;
            ui.ShowTrainingChoicePopup(true);
            return;
        }

        // 훈련 외 → 즉시 실행
        ExecuteDaySlot(action);
        AdvanceDaySlot();
    }

    /// <summary>훈련 세부 스탯 선택 완료 (UI에서 호출)</summary>
    public void OnTrainingStatChosen(TrainingStat stat)
    {
        if (Phase != GamePhase.DayProgress) return;
        if (!State.waitingForTrainingChoice) return;

        // 스탯 상승 (숙련도 보너스 자동 적용)
        State.AddStat(stat, BaseTrainAmount);

        // 피로/스트레스 (숙련도 레벨 3 경감 적용)
        int fatigueGain = Mathf.Max(0, TrainFatigue - State.profTraining.TrainingFatigueReduction);
        State.fatigue += fatigueGain;
        State.stress += TrainStress;

        // 훈련 숙련도 경험치
        bool leveledUp = State.profTraining.AddExp(3);

        State.todayTrainingCount++;
        State.waitingForTrainingChoice = false;

        // 레벨업 알림
        if (leveledUp)
            ui.ShowLevelUpNotice(ProficiencyType.Training, State.profTraining.level);

        ui.ShowTrainingChoicePopup(false);

        AdvanceDaySlot();
    }

    void ExecuteDaySlot(DaySlotType slot)
    {
        switch (slot)
        {
            case DaySlotType.Training:
                // OnTrainingStatChosen에서 처리하므로 여기 도달하지 않음
                break;

            case DaySlotType.PartTime:
                // 대성공 판정
                float bigSuccessChance = 0.1f + State.profPartTime.PartTimeBigSuccessBonus;
                bool bigSuccess = Random.value < bigSuccessChance;
                int reward = bigSuccess ? 20 : 10;

                State.gold += reward;
                State.todayGoldEarned += reward;
                State.fatigue += 1;

                // 숙련도 경험치
                bool lvl = State.profPartTime.AddExp(2);
                if (lvl) ui.ShowLevelUpNotice(ProficiencyType.PartTime, State.profPartTime.level);

                if (bigSuccess) ui.ShowActionResult("아르바이트 대성공! +20 Gold");
                else ui.ShowActionResult("아르바이트 완료. +10 Gold");
                break;

            case DaySlotType.Shop:
                ui.ShowActionResult("상점 방문. (준비 중)");
                break;

            case DaySlotType.Investigation:
                State.stress += 2;
                bool ilvl = State.profInvestigation.AddExp(3);
                if (ilvl) ui.ShowLevelUpNotice(ProficiencyType.Investigation, State.profInvestigation.level);
                ui.ShowActionResult("조사 수행. (준비 중)");
                break;

            case DaySlotType.Relationship:
                State.stress = Mathf.Max(0, State.stress - 1);
                ui.ShowActionResult("관계 이벤트. (준비 중)");
                break;

            case DaySlotType.Rest:
                State.fatigue = Mathf.Max(0, State.fatigue - 3);
                State.stress = Mathf.Max(0, State.stress - 2);
                ui.ShowActionResult("휴식 완료. 피로 -3, 스트레스 -2");
                break;
        }
    }

    void AdvanceDaySlot()
    {
        State.currentDaySlot++;

        if (State.currentDaySlot >= DaySlotCount)
            TransitionToNight();
        else
            ui.RefreshAll(State, Phase);
    }

    // ====================================================
    //  Night
    // ====================================================

    void TransitionToNight()
    {
        State.nightCompleted = false;
        SetPhase(GamePhase.NightChoice);
    }

    public void OnClickNightChoice(int choiceIndex)
    {
        if (Phase != GamePhase.NightChoice) return;

        NightActionType choice = (NightActionType)choiceIndex;

        if (choice == NightActionType.Arena && !State.IsArenaOpen)
        {
            ui.ShowArenaClosedWarning();
            return;
        }

        if (choice != NightActionType.Rest && State.stress >= 80)
        {
            ui.ShowStressWarning();
            return;
        }

        State.nightChoice = choice;
        SetPhase(GamePhase.NightAction);
        ExecuteNightAction(choice);
    }

    void ExecuteNightAction(NightActionType action)
    {
        switch (action)
        {
            case NightActionType.Exploration:
                State.stress += 5;
                State.fatigue += 3;
                State.gold += 5;
                bool elvl = State.profExploration.AddExp(4);
                if (elvl) ui.ShowLevelUpNotice(ProficiencyType.Exploration, State.profExploration.level);
                break;

            case NightActionType.Arena:
                State.stress += 3;
                State.fatigue += 5;
                State.gold += 20;
                break;

            case NightActionType.Rest:
                State.stress = Mathf.Max(0, State.stress - 5);
                State.fatigue = Mathf.Max(0, State.fatigue - 5);
                break;
        }

        State.nightCompleted = true;
        SetPhase(GamePhase.DaySummary);
    }

    // ===== Day Summary → Next Day =====
    public void OnClickNextDay()
    {
        if (Phase != GamePhase.DaySummary) return;

        State.ResetForNewDay();
        SaveSystem.Save(State);
        SetPhase(GamePhase.ScheduleSetting);
    }

    // ====================================================
    //  Time Utility
    // ====================================================

    public static string DaySlotToTimeLabel(int slotIndex)
    {
        int hour = DayStartHour + slotIndex * HoursPerSlot;
        return $"{hour:00}:00";
    }

    public static string DaySlotToEndTimeLabel(int slotIndex)
    {
        int hour = DayStartHour + (slotIndex + 1) * HoursPerSlot;
        return $"{hour:00}:00";
    }

    public static string GetCurrentTimeLabel(GameState state, GamePhase phase)
    {
        if (phase == GamePhase.NightChoice || phase == GamePhase.NightAction)
            return $"{NightStartHour:00}:00";
        if (phase == GamePhase.DaySummary)
            return $"{SleepHour:00}:00";

        int hour = DayStartHour + state.currentDaySlot * HoursPerSlot;
        return $"{hour:00}:00";
    }

    public static string GetStatName(TrainingStat stat) => stat switch
    {
        TrainingStat.Strength => "힘",
        TrainingStat.Agility => "민첩",
        TrainingStat.Dexterity => "재주",
        TrainingStat.Endurance => "지구력",
        _ => "?"
    };

    // ====================================================
    //  Debug
    // ====================================================

    public void DebugAddGold10()
    {
        State.gold += 10;
        ui.RefreshAll(State, Phase);
    }

    public void DebugReduceStress()
    {
        State.stress = Mathf.Max(0, State.stress - 20);
        ui.RefreshAll(State, Phase);
    }

    public void DebugForceDaySummary()
    {
        State.currentDaySlot = DaySlotCount;
        State.nightCompleted = true;
        SetPhase(GamePhase.DaySummary);
    }

    public void DebugClearSave()
    {
        SaveSystem.Clear();
    }

    public void DebugAddProfExp()
    {
        State.profTraining.AddExp(10);
        State.profInvestigation.AddExp(10);
        State.profExploration.AddExp(10);
        State.profPartTime.AddExp(10);
        ui.RefreshAll(State, Phase);
    }
}
