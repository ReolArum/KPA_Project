// ===== BattleReport.cs =====

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActionLog
{
    public string actorName;
    public string skillName;
    public HitOutcome outcome;
    public float damage;
    public int turnNumber;
}

[System.Serializable]
public class SkillSummary
{
    public string skillName;
    public int uses;
    public int hits;
    public int crits;
    public float totalDamage;
}

public class BattleReport
{
    public List<ActionLog> logs = new();
    public Dictionary<string, SkillSummary> playerSkillSummaries = new();
    public float totalDamageDealt;
    public float totalDamageReceived;
    public bool playerWon;
    public int totalTurns;

    public void LogAction(CombatUnit actor, SkillData skill, DamageResult result, int turn, bool isPlayer)
    {
        logs.Add(new ActionLog
        {
            actorName = actor.unitName,
            skillName = skill != null ? skill.skillName : "일반공격",
            outcome = result.outcome,
            damage = result.finalDamage,
            turnNumber = turn
        });

        if (isPlayer && skill != null)
        {
            if (!playerSkillSummaries.ContainsKey(skill.skillName))
            {
                playerSkillSummaries[skill.skillName] = new SkillSummary
                {
                    skillName = skill.skillName
                };
            }

            var summary = playerSkillSummaries[skill.skillName];
            summary.uses++;
            if (result.outcome == HitOutcome.Hit || result.outcome == HitOutcome.Critical)
            {
                summary.hits++;
                summary.totalDamage += result.finalDamage;
            }
            if (result.outcome == HitOutcome.Critical)
                summary.crits++;
        }

        if (isPlayer)
            totalDamageDealt += result.finalDamage;
        else
            totalDamageReceived += result.finalDamage;

        totalTurns = turn;
    }

    /// <summary>피드백 생성: 훈련 가이드</summary>
    public List<string> GenerateFeedback()
    {
        var feedback = new List<string>();

        foreach (var kvp in playerSkillSummaries)
        {
            var s = kvp.Value;
            if (s.uses == 0) continue;

            float hitRate = (float)s.hits / s.uses;
            if (hitRate < 0.5f)
                feedback.Add($"'{s.skillName}' 명중률 부족 ({hitRate:P0}) → 감각(SEN) 훈련 권장");

            if (s.uses >= 3 && s.crits == 0)
                feedback.Add($"'{s.skillName}' 크리티컬 없음 → 감각(SEN) 훈련 권장");
        }

        if (totalDamageReceived > totalDamageDealt * 1.5f)
            feedback.Add("받은 데미지가 높음 → 내구(VIT) 또는 민첩(AGI) 훈련 권장");

        if (feedback.Count == 0)
            feedback.Add("전체적으로 양호한 전투였습니다.");

        return feedback;
    }

    /// <summary>텍스트 리포트 생성</summary>
    public string ToReportString()
    {
        string report = $"===== 전투 결과: {(playerWon ? "승리" : "패배")} =====\n\n";

        foreach (var kvp in playerSkillSummaries)
        {
            var s = kvp.Value;
            report += $"  {s.skillName}: {s.uses}회 사용 / {s.hits}회 명중 / {s.totalDamage:F0} Dmg\n";
        }

        report += $"\n총 가한 데미지: {totalDamageDealt:F0}\n";
        report += $"총 받은 데미지: {totalDamageReceived:F0}\n";
        report += $"총 턴 수: {totalTurns}\n\n";

        report += "[피드백]\n";
        foreach (var fb in GenerateFeedback())
            report += $"  → {fb}\n";

        return report;
    }
}
