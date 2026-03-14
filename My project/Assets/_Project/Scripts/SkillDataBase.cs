// ===== SkillDatabase.cs =====
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Combat/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillData> allSkills = new List<SkillData>();

    public List<SkillData> GetSkillsByCategory(SkillCategory category)
    {
        return allSkills.FindAll(s => s.category == category);
    }

    public List<SkillData> GetSkillsBySchool(SchoolType school)
    {
        List<SkillData> result = new List<SkillData>();
        foreach (var skill in allSkills)
        {
            if (skill.unlockCondition != null &&
                skill.unlockCondition.unlockType == SkillUnlockType.SchoolLevel &&
                skill.unlockCondition.requiredSchoolType == school)
            {
                result.Add(skill);
            }
        }
        return result;
    }
}
