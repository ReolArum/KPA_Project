// ===== CombatUnit.cs =====

using System.Collections.Generic;
using UnityEngine;

public class CombatUnit
{
    public string unitName;

    // 원본 스탯 (훈련 + 장비 기반)
    public CombatBaseStats rawStats    = new CombatBaseStats();
    // 버프 적용 후 실효 스탯
    public CombatBaseStats effectiveStats = new CombatBaseStats();
    public CombatDerivedStats derived     = new CombatDerivedStats();

    // 전투 상태
    public float currentHP;
    public float currentAV;

    // 장비 (슬롯 4개: Head, Body, Arms, Legs)
    public EquipmentData[] equipment = new EquipmentData[4];

    // 스킬 슬롯
    public List<SkillData> equippedSkills = new List<SkillData>();

    // 유파
    public SchoolType schoolType  = SchoolType.None;
    public int        schoolLevel = 0;

    // AI 성향 (유파/전략에 따라 결정)
    public Dictionary<SkillCategory, float> tendency = new Dictionary<SkillCategory, float>
    {
        { SkillCategory.Strike,   0.30f },
        { SkillCategory.Defense,  0.20f },
        { SkillCategory.Mobility, 0.25f },
        { SkillCategory.Tactics,  0.25f }
    };

    // 활성 버프 목록
    public List<BuffInstance> activeBuffs = new List<BuffInstance>();

    // 스킬 쿨타임 추적 (스킬명 → 남은 턴 수)
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    // ====================================================
    //  쿨타임 관련
    // ====================================================

    /// <summary>스킬 사용 후 쿨타임 등록</summary>
    public void SetCooldown(SkillData skill)
    {
        if (skill == null || skill.cooldownTurns <= 0) return;
        skillCooldowns[skill.skillName] = skill.cooldownTurns;
    }

    /// <summary>턴 시작 시 모든 쿨타임 1 감소</summary>
    public void TickCooldowns()
    {
        var keys = new List<string>(skillCooldowns.Keys);
        foreach (var k in keys)
        {
            skillCooldowns[k]--;
            if (skillCooldowns[k] <= 0)
                skillCooldowns.Remove(k);
        }
    }

    /// <summary>스킬이 현재 사용 가능한지 (쿨타임 0 이하)</summary>
    public bool IsSkillReady(SkillData skill)
    {
        if (skill == null) return false;
        return !skillCooldowns.ContainsKey(skill.skillName);
    }

    /// <summary>스킬 남은 쿨타임 반환 (0 = 사용 가능)</summary>
    public int GetCooldown(SkillData skill)
    {
        if (skill == null) return 0;
        return skillCooldowns.TryGetValue(skill.skillName, out int v) ? v : 0;
    }

    // ====================================================
    //  팩토리 메서드
    // ====================================================

    /// <summary>GameState → 플레이어 전투 유닛 생성</summary>
    public static CombatUnit CreateFromGameState(GameState state)
    {
        var unit      = new CombatUnit();
        unit.unitName = "내 클론";

        // [MOD] CombatStatProcessor를 통해 스탯 계산
        unit.rawStats = CombatStatProcessor.CalculateStats(state, state.combatData);

        // 장비 참조 복사
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
        {
            var equip = state.combatData.GetEquippedItem(slot);
            unit.equipment[(int)slot] = equip;
        }

        // 유파 정보 복사
        unit.schoolType  = state.combatData.activeSchool;
        unit.schoolLevel = state.combatData.GetSchoolLevel(unit.schoolType);

        // 장착 스킬 복사 (없으면 폴백은 BattleSceneController에서 처리)
        unit.equippedSkills.Clear();
        unit.equippedSkills.AddRange(state.combatData.equippedSkills);

        unit.Recalculate();
        unit.currentHP = unit.derived.MaxHP;
        unit.currentAV = unit.derived.SPD > 0 ? 10000f / unit.derived.SPD : 10000f;

        return unit;
    }

    /// <summary>랭크 기반 NPC 상대 생성</summary>
    public static CombatUnit CreateOpponent(ArenaRank rank, int day)
    {
        var unit      = new CombatUnit();
        unit.unitName = GenerateOpponentName(rank);

        int baseStat = rank switch
        {
            ArenaRank.Bronze   => 8,
            ArenaRank.Silver   => 15,
            ArenaRank.Gold     => 25,
            ArenaRank.Platinum => 38,
            ArenaRank.Champion => 55,
            _                  => 10
        };

        unit.rawStats = new CombatBaseStats
        {
            STR = baseStat + Random.Range(-3, 4),
            AGI = baseStat + Random.Range(-3, 4),
            VIT = baseStat + Random.Range(-3, 4),
            INT = baseStat + Random.Range(-3, 4),
            GUT = baseStat + Random.Range(-3, 4),
            SEN = baseStat + Random.Range(-3, 4)
        };

        // 랜덤 유파 부여
        var schools = new[] { SchoolType.Crusher, SchoolType.Ironclad, SchoolType.Agile, SchoolType.Tactician };
        unit.schoolType  = schools[Random.Range(0, schools.Length)];
        unit.schoolLevel = Mathf.Clamp(rank switch
        {
            ArenaRank.Bronze   => 1,
            ArenaRank.Silver   => 2,
            ArenaRank.Gold     => 3,
            ArenaRank.Platinum => 4,
            ArenaRank.Champion => 5,
            _                  => 1
        }, 1, 5);

        unit.Recalculate();
        unit.currentHP = unit.derived.MaxHP;
        unit.currentAV = unit.derived.SPD > 0 ? 10000f / unit.derived.SPD : 10000f;

        return unit;
    }

    // ====================================================
    //  스탯 재계산
    // ====================================================
    public void Recalculate()
    {
        // 원본 스탯 복사
        effectiveStats = new CombatBaseStats
        {
            STR = rawStats.STR,
            AGI = rawStats.AGI,
            VIT = rawStats.VIT,
            INT = rawStats.INT,
            GUT = rawStats.GUT,
            SEN = rawStats.SEN
        };

        // 활성 버프 적용
        foreach (var buff in activeBuffs)
        {
            effectiveStats.STR += buff.data.modSTR;
            effectiveStats.AGI += buff.data.modAGI;
            effectiveStats.VIT += buff.data.modVIT;
            effectiveStats.INT += buff.data.modINT;
            effectiveStats.GUT += buff.data.modGUT;
            effectiveStats.SEN += buff.data.modSEN;
        }

        // 파생 스탯 계산
        derived.Calculate(effectiveStats, schoolType, schoolLevel);
    }

    // ====================================================
    //  유파 전투 보정 (CombatResolver에서 사용)
    // ====================================================
    public float GetDamageMultiplier()
    {
        float bonus = schoolType switch
        {
            SchoolType.Crusher    => 0.05f * schoolLevel,
            SchoolType.Agile      => 0.03f * schoolLevel,
            SchoolType.Tactician  => 0.03f * schoolLevel,
            SchoolType.Ironclad   => 0.01f * schoolLevel,
            _                     => 0f
        };
        return 1f + bonus;
    }

    public float GetCritDamageBonus()
        => schoolType == SchoolType.Crusher ? 0.10f * schoolLevel : 0f;

    public float GetDefenseMultiplier()
    {
        float bonus = schoolType == SchoolType.Ironclad ? 0.05f * schoolLevel : 0f;
        return 1f + bonus;
    }

    // ====================================================
    //  유틸
    // ====================================================
    static string GenerateOpponentName(ArenaRank rank)
    {
        string[] names  = { "강철 주먹", "그림자", "불꽃", "빙결", "독사", "폭풍", "번개", "바위" };
        string[] titles = { "무명의", "떠도는", "성난", "차가운", "교활한" };
        return $"{titles[Random.Range(0, titles.Length)]} {names[Random.Range(0, names.Length)]}";
    }
}
