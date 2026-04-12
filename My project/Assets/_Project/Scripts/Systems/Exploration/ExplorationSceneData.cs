// ===== ExplorationSceneData.cs =====
using UnityEngine;

public static class ExplorationSceneData
{
    public static GameState gameState;
    public static bool explorationCompleted = false;
    private static string backupGameStateJson; // [ADD] 실패 시 복구를 위한 백업 데이터

    public static void SetupExploration(GameState state)
    {
        gameState = state;
        explorationCompleted = false;

        // [ADD] 탐사 시작 시점의 포인트를 저장 (깊은 복사를 위해 JSON 사용)
        backupGameStateJson = JsonUtility.ToJson(state);
        Debug.Log("[Exploration] GameState backup created.");
    }

    /// <summary>
    /// 탐사 실패 시 호출하여 진입 전 상태로 되돌립니다.
    /// </summary>
    public static void RestoreBackup()
    {
        if (!string.IsNullOrEmpty(backupGameStateJson) && GameManager.Instance != null)
        {
            // 기존 인스턴스에 백업 데이터를 덮어씌움
            JsonUtility.FromJsonOverwrite(backupGameStateJson, GameManager.Instance.State);
            Debug.Log("[Exploration] GameState restored from backup.");
        }
    }

    public static void CompleteExploration()
    {
        explorationCompleted = true;
    }

    public static void Clear()
    {
        gameState = null;
        explorationCompleted = false;
    }
}
