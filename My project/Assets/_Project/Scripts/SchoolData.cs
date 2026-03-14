// ===== SchoolData.cs =====
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSchool", menuName = "Combat/School Data")]
public class SchoolData : ScriptableObject
{
    [Header("기본 정보")]
    public string schoolName;           // 유파 이름
    public SchoolType schoolType;       // enum 타입
    public Sprite icon;                 // 유파 아이콘
    [TextArea] public string description; // 유파 설명

    [Header("레벨별 혜택")]
    public List<SchoolLevel> levels = new List<SchoolLevel>();

    /// <summary>
    /// 특정 레벨까지의 누적 보너스를 반환
    /// </summary>
    public SchoolBonus GetCumulativeBonus(int currentLevel)
    {
        SchoolBonus total = new SchoolBonus();
        for (int i = 0; i < currentLevel && i < levels.Count; i++)
        {
            total.Add(levels[i].bonus);
        }
        return total;
    }

    /// <summary>
    /// 특정 레벨에서 해금되는 스킬 목록
    /// </summary>
    public List<SkillData> GetUnlockedSkills(int currentLevel)
    {
        List<SkillData> skills = new List<SkillData>();
        for (int i = 0; i < currentLevel && i < levels.Count; i++)
        {
            if (levels[i].unlockedSkill != null)
                skills.Add(levels[i].unlockedSkill);
        }
        return skills;
    }

    public int MaxLevel => levels.Count;
}

[Serializable]
public class SchoolLevel
{
    public string levelName;            // "초식", "중식", "오의" 등
    [TextArea] public string description;
    public SchoolBonus bonus;
    public SkillData unlockedSkill;     // 이 레벨에서 해금되는 스킬 (없으면 null)
}

[Serializable]
public class SchoolBonus
{
    [Header("스탯 보너스 (%)")]
    public float physAtkBonus;      // 물리 공격력 %
    public float physDefBonus;      // 물리 방어력 %
    public float spdBonus;          // 속도 %
    public float maxHPBonus;        // 최대 HP %
    public float hitRateBonus;      // 명중률 (고정값)
    public float evasionBonus;      // 회피율 (고정값)
    public float critRateBonus;     // 크리티컬 확률 (고정값)
    public float critDamageBonus;   // 크리티컬 데미지 %

    [Header("특수 효과")]
    public float ignoreDefenseChance;   // 방어 무시 확률 %
    public float counterAttackChance;   // 반격 확률 %
    public float damageReduction;       // 피해 감소 %
    public float skillCooldownReduction;// 스킬 AV 감소 %

    public void Add(SchoolBonus other)
    {
        physAtkBonus += other.physAtkBonus;
        physDefBonus += other.physDefBonus;
        spdBonus += other.spdBonus;
        maxHPBonus += other.maxHPBonus;
        hitRateBonus += other.hitRateBonus;
        evasionBonus += other.evasionBonus;
        critRateBonus += other.critRateBonus;
        critDamageBonus += other.critDamageBonus;
        ignoreDefenseChance += other.ignoreDefenseChance;
        counterAttackChance += other.counterAttackChance;
        damageReduction += other.damageReduction;
        skillCooldownReduction += other.skillCooldownReduction;
    }
}
