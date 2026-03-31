// ===== CombatResolver.cs =====

using UnityEngine;

public class CombatResolver
{
    /// <summary>
    /// 공격 판정 파이프라인:
    /// 명중 판정 → 회피 판정 → 크리티컬 판정 → 데미지 계산
    /// CombatDerivedStats에 유파 보너스가 이미 반영되어 있음
    /// </summary>
    public DamageResult Resolve(CombatUnit attacker, CombatUnit defender, SkillData skill)
    {
        var result = new DamageResult
        {
            attacker    = attacker,
            defender    = defender,
            skill       = skill,
            outcome     = HitOutcome.Miss,
            finalDamage = 0f
        };

        if (skill == null) return result;

        // ── 1. 명중 판정 ──
        if (Random.Range(0f, 100f) > attacker.derived.HitRate)
        {
            result.outcome = HitOutcome.Miss;
            return result;
        }

        // ── 2. 회피 판정 (초과 명중률로 회피 상쇄) ──
        float excessHit     = Mathf.Max(0f, attacker.derived.HitRate - 80f);
        float actualEvasion = Mathf.Max(2f, defender.derived.EvasionRate - excessHit);
        if (Random.Range(0f, 100f) < actualEvasion)
        {
            result.outcome = HitOutcome.Evaded;
            return result;
        }

        // ── 3. 크리티컬 판정 ──
        bool isCrit = Random.Range(0f, 100f) < attacker.derived.CritRate;

        // ── 4. 데미지 계산 ──
        // 기본 공격력 × 스킬 배율 × 유파 배율(이미 derived에 반영) / 방어 공식
        float dmg = attacker.derived.PhysAtk * skill.damageMultiplier;

        // 방어 무시 스킬
        float defense = skill.ignoreDefense ? 0f : defender.derived.PhysDef * defender.GetDefenseMultiplier();

        // 방어 공식: 100 / (200 + 방어)
        dmg *= 100f / (200f + defense);

        // 크리티컬 배율 (CombatDerivedStats.CritDamage = 1.75 기본 + 유파 보너스)
        if (isCrit)
        {
            dmg            *= attacker.derived.CritDamage;
            result.outcome  = HitOutcome.Critical;
        }
        else
        {
            result.outcome = HitOutcome.Hit;
        }

        // 최소 데미지 1 보장
        result.finalDamage = Mathf.Max(1f, Mathf.Floor(dmg));

        return result;
    }
}

[System.Serializable]
public class DamageResult
{
    public CombatUnit  attacker;
    public CombatUnit  defender;
    public SkillData   skill;
    public HitOutcome  outcome;
    public float       finalDamage;
}
