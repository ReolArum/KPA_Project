// ===== SkillData.cs =====
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Combat/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    public SkillCategory category;      // Strike, Defense, Mobility, Tactics
    public Sprite icon;
    [TextArea] public string description;

    [Header("전투 수치")]
    public float weight = 1f;           // AI 선택 가중치
    public float damageMultiplier = 1f;
    public float avAdvance;             // 사용자 AV 감소
    public float avDelay;               // 대상 AV 증가
    public bool ignoreDefense;

    [Header("쿨타임")]
    public int cooldownTurns = 0;       // 0 = 쿨타임 없음, N = N턴 후 재사용 가능

    [Header("버프/디버프")]
    public BuffData appliedBuff;
    public BuffData appliedDebuff;

    [Header("해금 조건")]
    public SkillUnlockCondition unlockCondition;
}

[Serializable]
public class SkillUnlockCondition
{
    public SkillUnlockType unlockType = SkillUnlockType.None;

    [Header("스탯 조건 (StatThreshold)")]
    public CombatStat requiredStat;
    public int requiredStatValue;

    [Header("스킬 사용 횟수 조건 (SkillUsage)")]
    public SkillData requiredSkill;     // 이 스킬을 N회 사용
    public int requiredUsageCount;

    [Header("유파 레벨 조건 (SchoolLevel)")]
    public SchoolType requiredSchoolType;
    public int requiredSchoolLevel;

    [Header("이벤트 조건 (Event)")]
    public string requiredEventId;      // 이벤트 ID

    /// <summary>
    /// 해금 조건을 충족하는지 확인
    /// </summary>
    public bool IsMet(PlayerCombatData playerData)
    {
        switch (unlockType)
        {
            case SkillUnlockType.None:
                return true; // 조건 없음, 항상 해금

            case SkillUnlockType.StatThreshold:
                return playerData.GetBaseStat(requiredStat) >= requiredStatValue;

            case SkillUnlockType.SkillUsage:
                return playerData.GetSkillUsageCount(requiredSkill) >= requiredUsageCount;

            case SkillUnlockType.SchoolLevel:
                return playerData.GetSchoolLevel(requiredSchoolType) >= requiredSchoolLevel;

            case SkillUnlockType.Event:
                return playerData.IsEventCompleted(requiredEventId);

            default:
                return false;
        }
    }
}

public enum SkillUnlockType
{
    None,           // 기본 스킬 (조건 없음)
    StatThreshold,  // 스탯 일정 수치 이상
    SkillUsage,     // 특정 스킬 N회 사용
    SchoolLevel,    // 유파 레벨 달성
    Event           // 이벤트 완료
}
