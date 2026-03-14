// ===== CombatResolver.cs =====

using UnityEngine;

public class CombatResolver
{
    public DamageResult Resolve(CombatUnit attacker, CombatUnit defender, SkillData skill)
    {
        var result = new DamageResult
        {
            attacker = attacker,
            defender = defender,
            skill = skill,
            outcome = HitOutcome.Miss,
            finalDamage = 0
        };

        if (skill == null) return result;

        // ── 1단계: 명중 판정 ──
        float hitChance = attacker.derived.HitRate;
        if (Random.Range(0f, 100f) > hitChance)
        {
            result.outcome = HitOutcome.Miss;
            return result;
        }

        // ── 2단계: 회피 판정 ──
        float excessHit = Mathf.Max(0, attacker.derived.HitRate - 80f);
        float actualEvasion = Mathf.Max(5f, defender.derived.EvasionRate - excessHit);
        if (Random.Range(0f, 100f) < actualEvasion)
        {
            result.outcome = HitOutcome.Evaded;
            return result;
        }

        // ── 3단계: 크리티컬 판정 ──
        bool isCrit = Random.Range(0f, 100f) < attacker.derived.CritRate;

        // ── 데미지 계산 파이프라인 ──
        // 1. 기본 공격력
        float dmg = attacker.derived.PhysAtk;

        // 2. 스킬 배율
        dmg *= skill.damageMultiplier;

        // 3. 유파 보정
        dmg *= (1f + attacker.GetSchoolDamageBonus());

        // 4. 약점간파 (방어 무시)
        float defense = defender.derived.PhysDef;
        if (skill.ignoreDefense) defense = 0f;

        // 5. 방어 공식: 100 / (200 + 방어)
        dmg *= 100f / (200f + defense);

        // 6. 크리티컬
        if (isCrit)
        {
            dmg *= (1.75f + attacker.GetCritDamageBonus());
            result.outcome = HitOutcome.Critical;
        }
        else
        {
            result.outcome = HitOutcome.Hit;
        }

        // 7. 최종 데미지 (최소 1 보장)
        result.finalDamage = Mathf.Max(1f, Mathf.Floor(dmg));

        return result;
    }
}

[System.Serializable]
public class DamageResult
{
    public CombatUnit attacker;
    public CombatUnit defender;
    public SkillData skill;
    public HitOutcome outcome;
    public float finalDamage;
}
