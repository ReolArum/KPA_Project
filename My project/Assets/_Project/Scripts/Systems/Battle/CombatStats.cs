// ===== CombatStats.cs =====

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CombatBaseStats
{
    public int STR;
    public int AGI;
    public int VIT;
    public int INT;
    public int GUT;
    public int SEN;

    public int Get(CombatStat stat) => stat switch
    {
        CombatStat.STR => STR,
        CombatStat.AGI => AGI,
        CombatStat.VIT => VIT,
        CombatStat.INT => INT,
        CombatStat.GUT => GUT,
        CombatStat.SEN => SEN,
        _              => 0
    };

    public void Set(CombatStat stat, int value)
    {
        switch (stat)
        {
            case CombatStat.STR: STR = value; break;
            case CombatStat.AGI: AGI = value; break;
            case CombatStat.VIT: VIT = value; break;
            case CombatStat.INT: INT = value; break;
            case CombatStat.GUT: GUT = value; break;
            case CombatStat.SEN: SEN = value; break;
        }
    }

    public void Add(CombatStat stat, int amount) => Set(stat, Get(stat) + amount);

    /// <summary>
    /// 훈련 스탯 → 전투 기본 스탯 변환 (브릿지)
    /// Strength  → STR  |  Agility → AGI  |  Dexterity → SEN  |  Endurance → VIT
    /// GUT / INT는 기본값 5 (장비·유파로 보완)
    /// </summary>
    public static CombatBaseStats FromTrainingStats(Dictionary<TrainingStat, int> training)
    {
        return new CombatBaseStats
        {
            STR = training.GetValueOrDefault(TrainingStat.Strength,  0),
            AGI = training.GetValueOrDefault(TrainingStat.Agility,   0),
            SEN = training.GetValueOrDefault(TrainingStat.Dexterity, 0),
            VIT = training.GetValueOrDefault(TrainingStat.Endurance, 0),
            GUT = 5,
            INT = 5
        };
    }
}

[Serializable]
public class CombatDerivedStats
{
    public float SPD;
    public float MaxHP;
    public float PhysAtk;
    public float PhysDef;
    public float HitRate;
    public float EvasionRate;
    public float CritRate;
    public float CritDamage;    // 크리티컬 데미지 배율 (1.75 기본)

    /// <summary>기본 계산 (유파 보너스 없음)</summary>
    public void Calculate(CombatBaseStats b)
    {
        SPD          = Mathf.Max(1f, b.AGI * 1.0f + b.SEN * 0.2f);
        MaxHP        = 100f + b.VIT * 5f + b.GUT * 1f;
        PhysAtk      = b.STR * 1.0f + b.GUT * 0.1f;
        PhysDef      = b.VIT * 0.4f + b.STR * 0.1f;
        HitRate      = 80f + b.SEN * 0.4f + b.AGI * 0.1f;
        EvasionRate  = Mathf.Min(b.AGI * 0.3f + b.SEN * 0.1f, 75f);
        CritRate     = Mathf.Min(5f + b.SEN * 0.3f, 60f);   // 버그 수정: SEN*0.2 + SEN*0.1 → SEN*0.3
        CritDamage   = 1.75f;
    }

    /// <summary>유파 SchoolBonus 적용 버전</summary>
    public void Calculate(CombatBaseStats b, SchoolType school, int schoolLevel)
    {
        Calculate(b);

        if (school == SchoolType.None || schoolLevel <= 0) return;

        // 유파별 파생스탯 보정 (퍼센트 적용)
        float atkBonus  = 0f, defBonus = 0f, spdBonus = 0f, hpBonus = 0f;
        float hitBonus  = 0f, evaBonus = 0f, critBonus = 0f, critDmgBonus = 0f;

        switch (school)
        {
            case SchoolType.Crusher:
                atkBonus    = 0.05f * schoolLevel;   // 공격 +5%/레벨
                critBonus   = 2.0f  * schoolLevel;   // 크리 확률 +2/레벨
                critDmgBonus = 0.10f * schoolLevel;  // 크리 데미지 +10%/레벨
                break;
            case SchoolType.Ironclad:
                defBonus    = 0.07f * schoolLevel;   // 방어 +7%/레벨
                hpBonus     = 0.05f * schoolLevel;   // HP +5%/레벨
                break;
            case SchoolType.Agile:
                spdBonus    = 0.06f * schoolLevel;   // 속도 +6%/레벨
                evaBonus    = 3.0f  * schoolLevel;   // 회피 +3/레벨
                break;
            case SchoolType.Tactician:
                hitBonus    = 2.0f  * schoolLevel;   // 명중 +2/레벨
                atkBonus    = 0.03f * schoolLevel;   // 공격 +3%/레벨
                break;
        }

        PhysAtk     *= (1f + atkBonus);
        PhysDef     *= (1f + defBonus);
        SPD         *= (1f + spdBonus);
        SPD          = Mathf.Max(1f, SPD);
        MaxHP       *= (1f + hpBonus);
        HitRate     += hitBonus;
        EvasionRate  = Mathf.Min(EvasionRate + evaBonus, 75f);
        CritRate     = Mathf.Min(CritRate + critBonus, 60f);
        CritDamage  += critDmgBonus;
    }
}
