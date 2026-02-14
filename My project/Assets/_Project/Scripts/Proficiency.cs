using System;
using UnityEngine;

[Serializable]
public class Proficiency
{
    public ProficiencyType type;
    public int level = 1;   // 1~5
    public int exp = 0;

    // 레벨별 필요 경험치 (레벨 1→2, 2→3, ...)
    // level 5가 최대이므로 index 4는 사용 안 함
    static readonly int[] ExpThresholds = { 0, 10, 25, 50, 80 };

    public int ExpToNext
    {
        get
        {
            if (level >= 5) return 0;
            return ExpThresholds[level] - exp;
        }
    }

    public float ExpRatio
    {
        get
        {
            if (level >= 5) return 1f;
            int required = ExpThresholds[level];
            return required > 0 ? (float)exp / required : 0f;
        }
    }

    public bool IsMaxLevel => level >= 5;

    /// <summary>경험치 추가. 레벨업 시 true 반환.</summary>
    public bool AddExp(int amount)
    {
        if (level >= 5) return false;

        exp += amount;
        bool leveled = false;

        while (level < 5 && exp >= ExpThresholds[level])
        {
            exp -= ExpThresholds[level];
            level++;
            leveled = true;
        }

        if (level >= 5) exp = 0; // 만렙이면 잔여 exp 리셋

        return leveled;
    }

    // ===== 숙련도 레벨별 효과 조회 =====

    /// <summary>훈련 스탯 상승 보너스 (레벨 2: +5%, 기본 1.0)</summary>
    public float TrainingStatMultiplier => level >= 2 ? 1.05f : 1.0f;

    /// <summary>훈련 피로 경감 (레벨 3: -1)</summary>
    public int TrainingFatigueReduction => level >= 3 ? 1 : 0;

    /// <summary>부상 확률 감소 보정 (레벨 4)</summary>
    public float InjuryReduction => level >= 4 ? 0.5f : 0f;

    /// <summary>상급 훈련 해금 (레벨 5)</summary>
    public bool AdvancedTrainingUnlocked => level >= 5;

    /// <summary>조사 성공률 보너스 (레벨 2: +5%)</summary>
    public float InvestigationSuccessBonus => level >= 2 ? 0.05f : 0f;

    /// <summary>단서 추가 획득 확률 (레벨 3)</summary>
    public float ExtraClueChance => level >= 3 ? 0.15f : 0f;

    /// <summary>고급 조사 해금 (레벨 4)</summary>
    public bool AdvancedInvestigationUnlocked => level >= 4;

    /// <summary>탐사 제한시간 추가 블록 (레벨 2)</summary>
    public int ExplorationTimeBonus => level >= 2 ? 2 : 0;

    /// <summary>아르바이트 대성공률 보너스</summary>
    public float PartTimeBigSuccessBonus
    {
        get
        {
            float bonus = 0f;
            if (level >= 2) bonus += 0.03f;
            if (level >= 4) bonus += 0.05f;
            return bonus;
        }
    }

    /// <summary>고급 아르바이트 해금 (레벨 5)</summary>
    public bool AdvancedPartTimeUnlocked => level >= 5;
}
