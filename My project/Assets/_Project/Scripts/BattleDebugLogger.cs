using System.Collections.Generic;
using UnityEngine;

public class BattleDebugLogger : MonoBehaviour
{
    private BattleManager bm;

    void Start()
    {
        bm = BattleManager.Instance;
        if (bm == null)
        {
            Debug.LogError("[Logger] BattleManager를 찾을 수 없습니다!");
            return;
        }

        bm.OnTurnStart += OnTurn;
        bm.OnSkillSelected += OnSkill;
        bm.OnDamageApplied += OnDamage;
        bm.OnBattleEnd += OnEnd;

        Debug.Log("[Logger] 이벤트 등록 완료");
    }

    void OnDestroy()
    {
        if (bm == null) return;

        bm.OnTurnStart -= OnTurn;
        bm.OnSkillSelected -= OnSkill;
        bm.OnDamageApplied -= OnDamage;
        bm.OnBattleEnd -= OnEnd;
    }

    void OnTurn(CombatUnit actor)
    {
        Debug.Log($"──── {actor.unitName}의 턴 | HP: {actor.currentHP:F0}/{actor.derived.MaxHP:F0} ────");
    }

    void OnSkill(CombatUnit actor, SkillData skill)
    {
        string name = skill != null ? skill.skillName : "없음";
        Debug.Log($"  스킬 선택: {name} (카테고리: {skill?.category})");
    }

    void OnDamage(DamageResult result)
    {
        switch (result.outcome)
        {
            case HitOutcome.Miss:
                Debug.Log("  → 빗나감!");
                break;
            case HitOutcome.Evaded:
                Debug.Log($"  → {result.defender.unitName} 회피!");
                break;
            case HitOutcome.Hit:
                Debug.Log($"  → {result.defender.unitName}에게 {result.finalDamage:F0} 데미지! (남은HP: {result.defender.currentHP:F0})");
                break;
            case HitOutcome.Critical:
                Debug.Log($"  → 크리티컬! {result.defender.unitName}에게 {result.finalDamage:F0} 데미지! (남은HP: {result.defender.currentHP:F0})");
                break;
        }
    }

    void OnEnd(BattleReport report)
    {
        Debug.Log("========================================");
        Debug.Log($"전투 종료! {(report.playerWon ? "승리" : "패배")}");
        Debug.Log($"총 {report.totalTurns}턴 | 가한 데미지: {report.totalDamageDealt:F0} | 받은 데미지: {report.totalDamageReceived:F0}");
        foreach (var fb in report.GenerateFeedback())
            Debug.Log($"  피드백: {fb}");
        Debug.Log("========================================");
    }
}
