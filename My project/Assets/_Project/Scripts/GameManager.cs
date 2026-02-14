using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIController ui;

    public GameState State { get; private set; } = new GameState();
    public GamePhase Phase { get; private set; } = GamePhase.Title;

    // ===== 상수 =====
    public const int DaySlotCount = GameState.DaySlotCount; // 4
    public const int HoursPerSlot = 3;
    // 낮: 08:00 시작, 슬롯 4개 × 3시간 = 12시간 → 20:00 종료
    // 밤: 20:00~22:00 (밤 행동 1회)
    public const int DayStartHour = 8;
    public const int NightStartHour = 20;
    public const int SleepHour = 22;

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
        SetPhase(GamePhase.DayProgress);
    }

    // ===== Day Progress: 슬롯 하나씩 실행 =====
    public void OnClickExecuteNextSlot()
    {
        if (Phase != GamePhase.DayProgress) return;
        if (State.currentDaySlot >= DaySlotCount)
        {
            // 낮 종료 → 밤으로
            TransitionToNight();
            return;
        }

        DaySlotType action = State.daySchedule[State.currentDaySlot];
        ExecuteDaySlot(action);

        State.currentDaySlot++;

        if (State.currentDaySlot >= DaySlotCount)
        {
            // 낮 모든 슬롯 소진 → 밤으로
            TransitionToNight();
        }
        else
        {
            ui.RefreshAll(State, Phase);
        }
    }

    void ExecuteDaySlot(DaySlotType slot)
    {
        switch (slot)
        {
            case DaySlotType.Training:
                // TODO: 훈련 세부 스탯 선택 UI 연동 (2단계)
                // 지금은 힘 +1 기본
                State.statStrength += 1;
                State.todayTrainingCount++;
                State.fatigue += 2;
                State.stress += 1;
                break;

            case DaySlotType.PartTime:
                State.gold += 10;
                State.todayGoldEarned += 10;
                State.fatigue += 1;
                break;

            case DaySlotType.Shop:
                // TODO: 상점 UI (후순위)
                break;

            case DaySlotType.Investigation:
                // TODO: 조사 콘텐츠 (후순위)
                State.stress += 2;
                break;

            case DaySlotType.Relationship:
                // TODO: 호감도 이벤트 (후순위)
                State.stress = Mathf.Max(0, State.stress - 1);
                break;

            case DaySlotType.Rest:
                State.fatigue = Mathf.Max(0, State.fatigue - 3);
                State.stress = Mathf.Max(0, State.stress - 2);
                break;
        }
    }

    // ===== Night =====
    void TransitionToNight()
    {
        State.nightCompleted = false;
        SetPhase(GamePhase.NightChoice);
    }

    public void OnClickNightChoice(int choiceIndex)
    {
        if (Phase != GamePhase.NightChoice) return;

        NightActionType choice = (NightActionType)choiceIndex;

        // 아레나 체크
        if (choice == NightActionType.Arena && !State.IsArenaOpen)
        {
            // 아레나가 열린 날이 아님 → 무시 또는 경고
            ui.ShowArenaClosedWarning();
            return;
        }

        // 스트레스 제한 체크
        if (choice != NightActionType.Rest && State.stress >= 80)
        {
            // 스트레스 과다 → 휴식 강제
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
                // TODO: 탐사 콘텐츠 (후순위, 지금은 더미)
                State.stress += 5;
                State.fatigue += 3;
                // 더미 보상
                State.gold += 5;
                break;

            case NightActionType.Arena:
                // TODO: 전투 시스템 연동 (후순위, 지금은 더미)
                State.stress += 3;
                State.fatigue += 5;
                State.gold += 20; // 더미 승리 보상
                break;

            case NightActionType.Rest:
                State.stress = Mathf.Max(0, State.stress - 5);
                State.fatigue = Mathf.Max(0, State.fatigue - 5);
                break;
        }

        State.nightCompleted = true;

        // 밤 행동 완료 → 결산
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

    /// <summary>슬롯 인덱스 → 시각 문자열 (08:00, 11:00, 14:00, 17:00)</summary>
    public static string DaySlotToTimeLabel(int slotIndex)
    {
        int hour = DayStartHour + slotIndex * HoursPerSlot;
        return $"{hour:00}:00";
    }

    /// <summary>슬롯 인덱스 → 종료 시각</summary>
    public static string DaySlotToEndTimeLabel(int slotIndex)
    {
        int hour = DayStartHour + (slotIndex + 1) * HoursPerSlot;
        return $"{hour:00}:00";
    }

    /// <summary>현재 시각 문자열</summary>
    public static string GetCurrentTimeLabel(GameState state, GamePhase phase)
    {
        if (phase == GamePhase.NightChoice || phase == GamePhase.NightAction)
            return $"{NightStartHour:00}:00";
        if (phase == GamePhase.DaySummary)
            return $"{SleepHour:00}:00";

        int hour = DayStartHour + state.currentDaySlot * HoursPerSlot;
        return $"{hour:00}:00";
    }

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
}
