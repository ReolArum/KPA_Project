// ===== GameEvents.cs =====
// 중앙 이벤트 버스: 시스템 간 커플링 없이 이벤트를 전파
using System;
using System.Collections.Generic;

public static class GameEvents
{
    // ====================================================
    //  페이즈 변경
    // ====================================================
    public static event Action<GamePhase> OnPhaseChanged;
    public static void RaisePhaseChanged(GamePhase phase)
        => OnPhaseChanged?.Invoke(phase);

    // ====================================================
    //  전체 UI 갱신 요청
    // ====================================================
    public static event Action<GameState, GamePhase> OnRefreshRequested;
    public static void RaiseRefreshRequested(GameState s, GamePhase p) => OnRefreshRequested?.Invoke(s, p);

    public static event Action<GameState> OnGameStateChanged;
    public static void RaiseGameStateChanged(GameState s) => OnGameStateChanged?.Invoke(s);

    // ── 탐사 관련 이벤트 ──
    public static event Action<ExplorationStageData, ExplorationState> OnExplorationStarted;
    public static void RaiseExplorationStarted(ExplorationStageData d, ExplorationState s) => OnExplorationStarted?.Invoke(d, s);

    public static event Action<ExplorationState> OnExplorationUpdated;
    public static void RaiseExplorationUpdated(ExplorationState s) => OnExplorationUpdated?.Invoke(s);

    // ===== 탐사 이벤트 로직 =====

    public static event Action<DialogueChoiceData> OnExplorationChoiceSelected;
    public static void RaiseExplorationChoiceSelected(DialogueChoiceData choice) => 
        OnExplorationChoiceSelected?.Invoke(choice);

    public static event Action<string> OnExplorationEnvObjectFound;
    public static void RaiseExplorationEnvObjectFound(string objId) => OnExplorationEnvObjectFound?.Invoke(objId);

    // [MOD] 콜백에 ExplorationChoiceType을 전달하여 선택지 타입에 따른 노드 처리가 가능하게 함
    public static event Action<List<DialogueStep>, DialogueNodeData, Action<ExplorationChoiceType>> OnExplorationVNStarted;
    public static void RaiseExplorationVNStarted(List<DialogueStep> steps, DialogueNodeData node, Action<ExplorationChoiceType> onComplete) => 
        OnExplorationVNStarted?.Invoke(steps, node, onComplete);

    public static event Action<ExplorationPhase> OnExplorationPhaseChanged;
    public static void RaiseExplorationPhaseChanged(ExplorationPhase phase) => OnExplorationPhaseChanged?.Invoke(phase);

    // ── 알림 및 메시지 ──
    //  행동 결과 메시지
    // ====================================================
    public static event Action<string> OnActionResult;
    public static void RaiseActionResult(string message)
        => OnActionResult?.Invoke(message);

    // ====================================================
    //  전투체 슬롯 결과
    // ====================================================
    public static event Action<string> OnFighterSlotResult;
    public static void RaiseFighterSlotResult(string message)
        => OnFighterSlotResult?.Invoke(message);

    // ====================================================
    //  전투 결과
    // ====================================================
    public static event Action<ArenaBattleResult> OnBattleResult;
    public static void RaiseBattleResult(ArenaBattleResult result)
        => OnBattleResult?.Invoke(result);

    // ====================================================
    //  숙련도 레벨업
    // ====================================================
    public static event Action<ProficiencyType, int> OnProficiencyLevelUp;
    public static void RaiseProficiencyLevelUp(ProficiencyType type, int newLevel)
        => OnProficiencyLevelUp?.Invoke(type, newLevel);

    // ====================================================
    //  아레나 휴무 / 스트레스 경고
    // ====================================================
    public static event Action OnArenaClosedWarning;
    public static void RaiseArenaClosedWarning()
        => OnArenaClosedWarning?.Invoke();

    public static event Action OnStressWarning;
    public static void RaiseStressWarning()
        => OnStressWarning?.Invoke();

    // ====================================================
    //  캘린더
    // ====================================================
    public static event Action<GameState> OnShowCalendar;
    public static void RaiseShowCalendar(GameState state)
        => OnShowCalendar?.Invoke(state);

    public static event Action OnHideCalendar;
    public static void RaiseHideCalendar()
        => OnHideCalendar?.Invoke();
}
