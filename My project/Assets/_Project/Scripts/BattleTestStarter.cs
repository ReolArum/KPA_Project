// ===== BattleTestStarter.cs =====

using UnityEngine;
using UnityEngine.InputSystem;

public class BattleTestStarter : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private int testSTR = 15;
    [SerializeField] private int testAGI = 12;
    [SerializeField] private int testDEX = 10;
    [SerializeField] private int testEND = 13;
    [SerializeField] private ArenaRank testRank = ArenaRank.Bronze;

void Start()
{
    StartCoroutine(DelayedStart());
}

System.Collections.IEnumerator DelayedStart()
{
    yield return null; // 1프레임 대기 (Logger 등록 후 실행)
    StartTestBattle();
}

    void StartTestBattle()
    {
        var state = new GameState();
        state.stats[TrainingStat.Strength] = testSTR;
        state.stats[TrainingStat.Agility] = testAGI;
        state.stats[TrainingStat.Dexterity] = testDEX;
        state.stats[TrainingStat.Endurance] = testEND;
        state.arena.currentRank = testRank;
        state.day = 3;

        Debug.Log("===== 전투 테스트 시작 =====");
        Debug.Log($"스탯 - 힘:{testSTR} 민첩:{testAGI} 재주:{testDEX} 지구력:{testEND}");
        Debug.Log($"랭크: {testRank}");

        var bm = BattleManager.Instance;
        if (bm != null)
        {
            bm.OnBattleEnd += OnTestBattleEnd;
            bm.StartBattle(state);
        }
        else
        {
            Debug.LogError("BattleManager를 찾을 수 없습니다!");
        }
    }

    void OnTestBattleEnd(BattleReport report)
    {
        BattleManager.Instance.OnBattleEnd -= OnTestBattleEnd;
        Debug.Log("===== 전투 테스트 종료 =====");
        Debug.Log(report.ToReportString());
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // R키: 전투 재시작
        if (keyboard.rKey.wasPressedThisFrame)
        {
            Debug.Log("===== 전투 재시작 =====");
            StartTestBattle();
        }

        // 1~4키: 방침 변경
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            BattleManager.Instance?.ChangeDirective(BattleDirective.Aggressive);
            Debug.Log("방침 변경: 밀어붙여");
        }
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            BattleManager.Instance?.ChangeDirective(BattleDirective.Normal);
            Debug.Log("방침 변경: 평소대로");
        }
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            BattleManager.Instance?.ChangeDirective(BattleDirective.Defensive);
            Debug.Log("방침 변경: 버텨");
        }
        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            BattleManager.Instance?.ChangeDirective(BattleDirective.Technical);
            Debug.Log("방침 변경: 기술위주");
        }
    }
}
