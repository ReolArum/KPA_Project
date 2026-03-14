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
        _ => 0
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

    public void Add(CombatStat stat, int amount)
    {
        Set(stat, Get(stat) + amount);
    }

    /// <summary>
    /// 기존 TrainingStat → CombatBaseStats 변환.
    /// 훈련 시스템과 전투 시스템의 브릿지.
    /// </summary>
    public static CombatBaseStats FromTrainingStats(Dictionary<TrainingStat, int> training)
    {
        // 매핑 규칙 (프로젝트 사정에 맞게 조정)
        // Strength  → STR
        // Agility   → AGI
        // Dexterity → SEN (재주 ≈ 감각/손재주)
        // Endurance → VIT
        // GUT, INT  → 유파/장비/성장으로 별도 확보
        var stats = new CombatBaseStats();
        stats.STR = training.GetValueOrDefault(TrainingStat.Strength, 0);
        stats.AGI = training.GetValueOrDefault(TrainingStat.Agility, 0);
        stats.SEN = training.GetValueOrDefault(TrainingStat.Dexterity, 0);
        stats.VIT = training.GetValueOrDefault(TrainingStat.Endurance, 0);
        stats.GUT = 5; // 기본값 (추후 훈련/장비로 확보)
        stats.INT = 5; // 기본값
        return stats;
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

    public void Calculate(CombatBaseStats b)
    {
        SPD       = b.AGI * 1.0f + b.SEN * 0.2f;
        MaxHP     = 100f + b.VIT * 5f + b.GUT * 1f;
        PhysAtk   = b.STR * 1.0f + b.GUT * 0.1f;
        PhysDef   = b.VIT * 0.4f + b.STR * 0.1f;
        HitRate   = 80f + b.SEN * 0.4f + b.AGI * 0.1f;
        EvasionRate = Mathf.Min(b.AGI * 0.3f + b.SEN * 0.1f, 75f);
        CritRate  = Mathf.Min(5f + b.SEN * 0.2f + b.SEN * 0.1f, 100f);


        // 최소값 보장
        if (SPD < 1f) SPD = 1f;
    }
}
