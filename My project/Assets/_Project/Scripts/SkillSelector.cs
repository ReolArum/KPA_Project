// ===== SkillSelector.cs =====

using System.Collections.Generic;
using UnityEngine;

public class SkillSelector
{
    /// <summary>
    /// 스킬 선택 로직.
    ///
    /// 우선순위:
    /// 1. 일반공격(cooldownTurns == 0 && category == Strike)은 폴백 전용
    /// 2. 일반공격 외 스킬 중 쿨타임이 아닌 것들 → 가중치 랜덤 선택
    /// 3. 일반공격 외 스킬이 전부 쿨타임 중 → 일반공격 강제 사용
    /// 4. 일반공격도 없으면 → 쿨타임 무시하고 첫 번째 스킬 사용
    /// </summary>
    public SkillData SelectSkill(CombatUnit actor, CombatUnit target, BattleDirective directive)
    {
        if (actor.equippedSkills == null || actor.equippedSkills.Count == 0) return null;

        // 일반공격 식별 (cooldownTurns == 0 이고 category == Strike)
        SkillData normalAttack = FindNormalAttack(actor);

        // 일반공격 외 스킬 중 쿨타임이 아닌 것들
        var candidates = new List<(SkillData skill, float score)>();
        float totalScore = 0f;

        foreach (var skill in actor.equippedSkills)
        {
            // 일반공격은 후보에서 제외 (폴백 전용)
            if (IsNormalAttack(skill)) continue;

            // 쿨타임 중이면 건너뜀
            if (!actor.IsSkillReady(skill)) continue;

            float tendencyVal = actor.tendency.GetValueOrDefault(skill.category, 0.25f);
            float directiveMod = DirectiveTable.GetModifier(directive, skill.category);
            float finalScore = skill.weight * (tendencyVal + directiveMod);
            finalScore = Mathf.Max(finalScore, 0.01f);
            candidates.Add((skill, finalScore));
            totalScore += finalScore;
        }

        // 일반공격 외 스킬이 전부 쿨타임 중 → 일반공격 폴백
        if (candidates.Count == 0)
        {
            if (normalAttack != null)
            {
                Debug.Log("[SkillSelector] 모든 스킬 쿨타임 중 → 일반공격 사용");
                return normalAttack;
            }
            // 일반공격도 없으면 쿨타임 무시하고 첫 번째 스킬
            Debug.LogWarning("[SkillSelector] 일반공격 없음 → 첫 번째 스킬 강제 사용");
            return actor.equippedSkills[0];
        }

        // 가중치 랜덤 선택
        SkillData selected = WeightedRandom(candidates, totalScore);

        // 지능(INT) 재선택 판정
        if (ShouldReselect(actor, target, selected))
        {
            float rerollChance = Mathf.Min(actor.effectiveStats.INT * 0.7f, 70f);
            if (Random.Range(0f, 100f) < rerollChance)
            {
                selected = WeightedRandom(candidates, totalScore);
            }
        }

        return selected;
    }

    /// <summary>일반공격 여부 판별: cooldownTurns == 0 이고 category == Strike</summary>
    private bool IsNormalAttack(SkillData skill)
    {
        return skill != null && skill.cooldownTurns == 0 && skill.category == SkillCategory.Strike;
    }

    /// <summary>장착 스킬 중 일반공격 반환 (없으면 null)</summary>
    private SkillData FindNormalAttack(CombatUnit actor)
    {
        foreach (var skill in actor.equippedSkills)
        {
            if (IsNormalAttack(skill)) return skill;
        }
        return null;
    }

    private bool ShouldReselect(CombatUnit actor, CombatUnit target, SkillData chosen)
    {
        float targetHPRatio = target.derived.MaxHP > 0f
            ? target.currentHP / target.derived.MaxHP
            : 0f;

        // 상대 HP 20% 이하인데 방어 스킬 선택
        if (targetHPRatio <= 0.2f && chosen.category == SkillCategory.Defense)
            return true;

        // 자신 HP 20% 이하인데 공격 스킬 선택
        float selfHPRatio = actor.derived.MaxHP > 0f
            ? actor.currentHP / actor.derived.MaxHP
            : 0f;
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
