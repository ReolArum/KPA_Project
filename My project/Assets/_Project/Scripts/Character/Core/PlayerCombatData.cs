// ===== PlayerCombatData.cs =====
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerCombatData
{
    // ===== 유파 =====
    public SchoolType activeSchool = SchoolType.None;
    public Dictionary<SchoolType, int> schoolLevels = new Dictionary<SchoolType, int>()
    {
        { SchoolType.Crusher, 0 },
        { SchoolType.Ironclad, 0 },
        { SchoolType.Agile, 0 },
        { SchoolType.Tactician, 0 }
    };

    // ===== 장비 =====
    public Dictionary<EquipSlot, EquipmentData> equippedGear = new Dictionary<EquipSlot, EquipmentData>()
    {
        { EquipSlot.Head, null },
        { EquipSlot.Body, null },
        { EquipSlot.Arms, null },
        { EquipSlot.Legs, null }
    };

    // ===== 인벤토리 =====
    public List<EquipmentData> ownedEquipment = new List<EquipmentData>();

    // ===== 스킬 =====
    public List<SkillData> unlockedSkills = new List<SkillData>();
    public List<SkillData> equippedSkills = new List<SkillData>();
    public int maxSkillSlots = 3;

    // ===== 스킬 사용 기록 =====
    public Dictionary<string, int> skillUsageCount = new Dictionary<string, int>();

    // ===== 완료된 이벤트 =====
    public List<string> completedEvents = new List<string>();

    // ========================================
    //  유파 관련
    // ========================================
    public int GetSchoolLevel(SchoolType type)
    {
        return schoolLevels.ContainsKey(type) ? schoolLevels[type] : 0;
    }

    public void SetSchoolLevel(SchoolType type, int level)
    {
        schoolLevels[type] = level;
    }

    public void LevelUpSchool(SchoolType type)
    {
        if (!schoolLevels.ContainsKey(type))
            schoolLevels[type] = 0;
        schoolLevels[type]++;
    }

    // ========================================
    //  장비 관련
    // ========================================
    public void EquipItem(EquipmentData equipment)
    {
        if (equipment == null) return;

        // 기존 장비 해제
        EquipmentData current = GetEquippedItem(equipment.slot);
        if (current != null)
            UnequipItem(equipment.slot);

        equippedGear[equipment.slot] = equipment;
    }

    public void UnequipItem(EquipSlot slot)
    {
        equippedGear[slot] = null;
    }

    public EquipmentData GetEquippedItem(EquipSlot slot)
    {
        return equippedGear.ContainsKey(slot) ? equippedGear[slot] : null;
    }

    // ========================================
    //  스킬 관련
    // ========================================
    public bool EquipSkill(SkillData skill)
    {
        if (skill == null) return false;
        if (equippedSkills.Contains(skill)) return false;
        if (equippedSkills.Count >= maxSkillSlots) return false;
        if (!unlockedSkills.Contains(skill)) return false;

        equippedSkills.Add(skill);
        return true;
    }

    public void UnequipSkill(SkillData skill)
    {
        equippedSkills.Remove(skill);
    }

    public void RecordSkillUsage(SkillData skill)
    {
        if (skill == null) return;
        string key = skill.skillName;
        if (!skillUsageCount.ContainsKey(key))
            skillUsageCount[key] = 0;
        skillUsageCount[key]++;
    }

    public int GetSkillUsageCount(SkillData skill)
    {
        if (skill == null) return 0;
        string key = skill.skillName;
        return skillUsageCount.ContainsKey(key) ? skillUsageCount[key] : 0;
    }

    // ========================================
    //  이벤트 관련
    // ========================================
    public void CompleteEvent(string eventId)
    {
        if (!completedEvents.Contains(eventId))
            completedEvents.Add(eventId);
    }

    public bool IsEventCompleted(string eventId)
    {
        return completedEvents.Contains(eventId);
    }

    // ========================================
    //  스탯 조회 (GameState 훈련 스탯 → 전투 스탯)
    // ========================================
    private GameState _cachedGameState;

    public void LinkGameState(GameState gameState)
    {
        _cachedGameState = gameState;
    }

    public int GetBaseStat(CombatStat stat)
    {
        if (_cachedGameState == null) return 0;

        // [MOD] 계산 로직을 CombatStatProcessor로 위임
        CombatBaseStats stats = CombatStatProcessor.CalculateStats(_cachedGameState, this);
        return stats.Get(stat);
    }

    // ========================================
    //  스킬 해금 체크
    // ========================================
    public void CheckAndUnlockSkills(List<SkillData> allSkills)
    {
        foreach (var skill in allSkills)
        {
            if (unlockedSkills.Contains(skill)) continue;
            if (skill.unlockCondition != null && skill.unlockCondition.IsMet(this))
            {
                unlockedSkills.Add(skill);
                Debug.Log($"[스킬 해금] {skill.skillName} 해금!");
            }
        }
    }

    // 훈련 스탯 + 장비 합산 및 유파 보너스 계산 로직은 
    // CombatStatProcessor.cs 로 이관되었습니다.

}
