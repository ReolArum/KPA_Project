// ===== ExplorationSceneData.cs =====
using UnityEngine;

public static class ExplorationSceneData
{
    public static GameState gameState;
    public static bool explorationCompleted = false;

    public static void SetupExploration(GameState state)
    {
        gameState = state;
        explorationCompleted = false;
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
