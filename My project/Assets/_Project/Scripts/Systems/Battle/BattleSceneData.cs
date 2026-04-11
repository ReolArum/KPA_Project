// ===== BattleSceneData.cs =====
using UnityEngine;

public static class BattleSceneData
{
    // 씬 이름 상수 (Scene_[Name] 컨벤션 적용)
    public const string SceneMain   = "Scene_MainGame";
    public const string SceneBattle = "Scene_Battle";

    // 메인 씬 → 전투 씬으로 넘길 데이터
    public static GameState gameState;
    public static CombatUnit playerUnit;
    public static CombatUnit opponentUnit;

    // 전투 씬 → 메인 씬으로 돌려줄 데이터
    public static BattleReport battleReport;
    public static bool battleCompleted = false;

    /// <summary>
    /// 전투 준비 완료 후 호출
    /// </summary>
    public static void SetupBattle(GameState state)
    {
        gameState = state;
        playerUnit = CombatUnit.CreateFromGameState(state);
        opponentUnit = CombatUnit.CreateOpponent(state.arena.currentRank, state.player.day);
        battleReport = null;
        battleCompleted = false;
    }

    /// <summary>
    /// 전투 종료 후 호출
    /// </summary>
    public static void CompleteBattle(BattleReport report)
    {
        battleReport = report;
        battleCompleted = true;
    }

    /// <summary>
    /// 데이터 초기화
    /// </summary>
    public static void Clear()
    {
        gameState = null;
        playerUnit = null;
        opponentUnit = null;
        battleReport = null;
        battleCompleted = false;
    }
}
