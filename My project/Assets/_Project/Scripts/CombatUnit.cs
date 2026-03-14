// ===== CombatUnit.cs =====

using System.Collections.Generic;
using UnityEngine;

public class CombatUnit
{
    public string unitName;

    // 원본 스탯 (훈련 + 장비 기본)
    public CombatBaseStats rawStats;
    // 버프 적용 후 실효 스탯
    public CombatBaseStats effectiveStats;
    public CombatDerivedStats derived = new();

    // 전투 상태
    public float currentHP;
    public float currentAV;

    // 장비
    public EquipmentData[] equipment = new EquipmentData[4]; // Head, Body, Arms, Legs

    // 스킬 슬롯 (초기 3개)
    public List<SkillData> equippedSkills = new();

    // 유파
    public SchoolType school = SchoolType.None;
    public int schoolLevel = 1;

    // AI 성향 (유파/레벨업으로 결정)
    public Dictionary<SkillCategory, float> tendency = new()
    {
        { SkillCategory.Strike, 0.3f },
        { SkillCategory.Defense, 0.2f },
        { SkillCategory.Mobility, 0.25f },
        { SkillCategory.Tactics, 0.25f }
    };

    // 버프 목록
    public List<BuffInstance> activeBuffs = new();

    // ===== 초기화 =====

    /// <summary>GameState로부터 전투 유닛 생성 (플레이어 클론)</summary>
    public static CombatUnit CreateFromGameState(GameState state)
    {
        var unit = new CombatUnit();
        unit.unitName = "내 클론";
        unit.rawStats = CombatBaseStats.FromTrainingStats(state.stats);

        // 장비 스탯 적용
        foreach (var equip in unit.equipment)
            equip?.ApplyTo(unit.rawStats);

        unit.Recalculate();
        unit.currentHP = unit.derived.MaxHP;
        unit.currentAV = 10000f / unit.derived.SPD;

        return unit;
    }

    /// <summary>상대 NPC 생성 (랭크 기반)</summary>
    public static CombatUnit CreateOpponent(ArenaRank rank, int day)
    {
        var unit = new CombatUnit();
        unit.unitName = GenerateOpponentName(rank);

        // 랭크 + 날짜 기반 스탯 스케일링
        int base_stat = rank switch
        {
            ArenaRank.Bronze => 8,
            ArenaRank.Silver => 15,
            ArenaRank.Gold => 25,
            ArenaRank.Platinum => 38,
            ArenaRank.Champion => 55,
            _ => 10
        };

        // 약간의 랜덤 편차
        unit.rawStats = new CombatBaseStats
        {
            STR = base_stat + Random.Range(-3, 4),
            AGI = base_stat + Random.Range(-3, 4),
            VIT = base_stat + Random.Range(-3, 4),
            INT = base_stat + Random.Range(-3, 4),
            GUT = base_stat + Random.Range(-3, 4),
            SEN = base_stat + Random.Range(-3, 4)
        };

        // 랜덤 유파 부여
        var schools = new[] { SchoolType.Crusher, SchoolType.Ironclad, SchoolType.Agile, SchoolType.Tactician };
        unit.school = schools[Random.Range(0, schools.Length)];

        unit.Recalculate();
        unit.currentHP = unit.derived.MaxHP;
        unit.currentAV = 10000f / unit.derived.SPD;

        return unit;
    }

    // ===== 스탯 재계산 =====

    public void Recalculate()
    {
        // 원본 복사
        effectiveStats = new CombatBaseStats
        {
            STR = rawStats.STR,
            AGI = rawStats.AGI,
            VIT = rawStats.VIT,
            INT = rawStats.INT,
            GUT = rawStats.GUT,
            SEN = rawStats.SEN
        };

        // 버프 적용
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
        derived.Calculate(effectiveStats);
    }

    // ===== 유파 보정값 =====

    public float GetSchoolDamageBonus()
    {
        // 유파 레벨에 따른 데미지 보정 (예시)
        return school switch
        {
            SchoolType.Crusher => 0.05f * schoolLevel,
            SchoolType.Ironclad => 0.02f * schoolLevel,
            SchoolType.Agile => 0.03f * schoolLevel,
            SchoolType.Tactician => 0.03f * schoolLevel,
            _ => 0f
        };
    }

    public float GetCritDamageBonus()
    {
        return school == SchoolType.Crusher ? 0.1f * schoolLevel : 0f;
    }

    // ===== 유틸 =====

    static string GenerateOpponentName(ArenaRank rank)
    {
        string[] names = { "강철 주먹", "그림자", "불꽃", "빙결", "독사", "폭풍", "번개", "바위" };
        string[] titles = { "무명의", "떠도는", "성난", "차가운", "교활한" };
        return $"{titles[Random.Range(0, titles.Length)]} {names[Random.Range(0, names.Length)]}";
    }
}
