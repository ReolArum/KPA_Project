// ===== SkillSelector.cs =====

using System.Collections.Generic;
using UnityEngine;

public class SkillSelector
{
    /// <summary>
    /// 스킬 선택 로직.
    /// 선택점수 = 스킬가중치 × (성향[카테고리] + 방침보정[카테고리])
    /// 쿨타임 중인 스킬은 제외
    /// </summary>
    public SkillData SelectSkill(CombatUnit actor, CombatUnit target, BattleDirective directive)
    {
        if (actor.equippedSkills.Count == 0) return null;

        // 1. 각 스킬의 선택 점수 계산 (쿨타임 중 스킬 제외)
        var scores = new List<(SkillData skill, float score)>();
        float totalScore = 0f;

        foreach (var skill in actor.equippedSkills)
        {
            // 쿨타임 중이면 건너뜀
            if (!actor.IsSkillReady(skill)) continue;

            float tendencyVal = actor.tendency.GetValueOrDefault(skill.category, 0.25f);
            float directiveMod = DirectiveTable.GetModifier(directive, skill.category);
            float finalScore = skill.weight * (tendencyVal + directiveMod);
            finalScore = Mathf.Max(finalScore, 0.01f);
            scores.Add((skill, finalScore));
            totalScore += finalScore;
        }

        // 모든 스킬이 쿨타임 중이면 첫 번째 스킬 강제 사용
        if (scores.Count == 0)
        {
            return actor.equippedSkills[0];
        }

        // 2. 가중치 랜덤 선택
        SkillData selected = WeightedRandom(scores, totalScore);

        // 3. 지능(INT) 재선택 판정
        if (ShouldReselect(actor, target, selected))
        {
            float rerollChance = Mathf.Min(actor.effectiveStats.INT * 0.7f, 70f);
            if (Random.Range(0f, 100f) < rerollChance)
            {
                selected = WeightedRandom(scores, totalScore);
            }
        }

        return selected;
    }

    private bool ShouldReselect(CombatUnit actor, CombatUnit target, SkillData chosen)
    {
        float targetHPRatio = target.currentHP / target.derived.MaxHP;

        // 상대 HP 20% 이하인데 방어 스킬 선택
        if (targetHPRatio <= 0.2f && chosen.category == SkillCategory.Defense)
            return true;

        // 자신 HP 20% 이하인데 공격 스킬 선택 (방어하는 게 나을 수 있음)
        float selfHPRatio = actor.currentHP / actor.derived.MaxHP;
        if (selfHPRatio <= 0.2f && chosen.category == SkillCategory.Strike)
            return true;

        return false;
    }

    private SkillData WeightedRandom(List<(SkillData skill, float score)> scores, float totalScore)
    {
        float roll = Random.Range(0f, totalScore);
        float cumulative = 0f;

        foreach (var (skill, score) in scores)
        {
            cumulative += score;
            if (roll <= cumulative)
                return skill;
        }

        return scores[scores.Count - 1].skill;
    }
}
