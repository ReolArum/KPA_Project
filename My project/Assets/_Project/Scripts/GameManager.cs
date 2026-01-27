using UnityEngine;

public class GameManager : MonoBehaviour
{
    public const int MaxBlocks = 26;

    [Header("Refs")]
    [SerializeField] private UIController ui;

    public GameState State { get; private set; } = new GameState();
    public GamePhase Phase { get; private set; } = GamePhase.Title;

    void Awake()
    {
        if (ui == null) ui = FindFirstObjectByType<UIController>();

        // 최초 스케줄 기본값(Rest)로 채우기
        for (int i = 0; i < MaxBlocks; i++)
            State.schedule[i] = SlotType.Rest;

        SetPhase(GamePhase.Title);
        ui.RefreshAll(State, Phase);
    }

    // ====== Title ======
    public void OnClickStart()
    {
        SetPhase(GamePhase.ScheduleSetting);
    }

    // ====== Schedule ======
    public void OnClickConfirmSchedule()
    {
        SetPhase(GamePhase.Town);
    }

    // ====== Town ======
    // Unity Button에서 enum 인자를 넘기기 번거로우면 아래 래퍼들을 쓰면 편합니다.
    public void OnClickGoShop() => GoPlace(PlaceType.Shop);
    public void OnClickGoPartTime() => GoPlace(PlaceType.PartTime);
    public void OnClickGoExplore() => GoPlace(PlaceType.Explore);
    public void OnClickGoArenaDesk() => GoPlace(PlaceType.ArenaDesk);

    public void GoPlace(PlaceType place)
    {
        if (Phase != GamePhase.Town) return;

        State.currentPlace = place;
        SetPhase(GamePhase.Place);
    }

    // ====== Place ======
    public void OnClickBackToTown()
    {
        if (Phase != GamePhase.Place) return;
        SetPhase(GamePhase.Town);
    }

    public void OnClickDoAction()
    {
        if (Phase != GamePhase.Place) return;

        int cost = GetActionCost(State.currentPlace);

        // (선택) 알바는 골드 +10
        if (State.currentPlace == PlaceType.PartTime)
            State.gold += 10;

        AdvanceTime(cost);
    }

    int GetActionCost(PlaceType place)
    {
        return place switch
        {
            PlaceType.Shop => 4,
            PlaceType.PartTime => 4,
            PlaceType.Explore => 6,
            PlaceType.ArenaDesk => 2,
            _ => 4
        };
    }

    // ====== DaySummary ======
    public void OnClickNextDay()
    {
        if (Phase != GamePhase.DaySummary) return;

        State.ResetForNewDay();
        SetPhase(GamePhase.ScheduleSetting);
    }

    // ====== Core Time Advance ======
    void AdvanceTime(int costBlocks)
    {
        // 결산 상태면 입력 무시
        if (Phase == GamePhase.DaySummary) return;

        // 이미 끝났으면 결산
        if (State.currentBlock >= MaxBlocks)
        {
            ShowDaySummary();
            return;
        }

        // 초과 시도: 정책 통일 => 결산창으로
        if (State.currentBlock + costBlocks > MaxBlocks)
        {
            State.currentBlock = MaxBlocks;
            ShowDaySummary();
            return;
        }

        // 자동훈련 누적 (현재블록 ~ 현재+cost-1)
        for (int i = 0; i < costBlocks; i++)
        {
            int idx = State.currentBlock + i;
            SlotType t = State.schedule[idx];

            if (t == SlotType.Strength) State.todayStrengthTrain += 1;
            else if (t == SlotType.Stamina) State.todayStaminaTrain += 1;
        }

        State.currentBlock += costBlocks;

        // 22:00 도달 즉시 결산 (B안)
        if (State.currentBlock == MaxBlocks)
        {
            ShowDaySummary();
            return;
        }

        ui.RefreshAll(State, Phase);
    }

    void ShowDaySummary()
    {
        SetPhase(GamePhase.DaySummary);
        ui.RefreshAll(State, Phase);
    }

    void SetPhase(GamePhase next)
    {
        Phase = next;
        ui.ShowPhase(next);
        ui.RefreshAll(State, Phase);
    }

    // ====== Time Util ======
    public static string BlockToTimeLabel(int block)
    {
        // block 0 => 09:00, block 1 => 09:30 ... block 25 => 21:30, block 26 => 22:00(표시용)
        int totalMinutes = 9 * 60 + block * 30;
        int hh = totalMinutes / 60;
        int mm = totalMinutes % 60;
        return $"{hh:00}:{mm:00}";
    }

    public static string BlocksToHourMinute(int blocks)
    {
        int totalMinutes = blocks * 30;
        int hh = totalMinutes / 60;
        int mm = totalMinutes % 60;
        return $"{hh:00}:{mm:00}";
    }

// ===== Debug (Editor) =====
public void DebugAdd1Block() => DebugAdvanceTime(1);
public void DebugAdd4Blocks() => DebugAdvanceTime(4);

public void DebugAddGold10()
{
    State.gold += 10;
    ui.RefreshAll(State, Phase);
}

public void DebugForceDaySummary()
{
    if (Phase == GamePhase.DaySummary) return;

    State.currentBlock = MaxBlocks;
    ShowDaySummary(); // 기존 private 메서드 호출
}

public void DebugResetToday()
{
    if (Phase == GamePhase.DaySummary) return; // 원하면 DaySummary에서도 허용해도 됨

    State.currentBlock = 0;
    State.todayStrengthTrain = 0;
    State.todayStaminaTrain = 0;
    ui.RefreshAll(State, Phase);
}

void DebugAdvanceTime(int blocks)
{
    // 어디서든 허용(요청사항)
    // 단, Title에서는 굳이 의미 없으니 막는 걸 추천
    if (Phase == GamePhase.Title) return;
    if (Phase == GamePhase.DaySummary) return;

    AdvanceTime(blocks);
}


}
