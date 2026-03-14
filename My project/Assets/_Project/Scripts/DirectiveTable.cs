// ===== DirectiveTable.cs =====

using System.Collections.Generic;

public static class DirectiveTable
{
    // [방침][카테고리] = 보정값
    private static readonly Dictionary<BattleDirective, Dictionary<SkillCategory, float>> table = new()
    {
        {
            BattleDirective.Aggressive, new Dictionary<SkillCategory, float>
            {
                { SkillCategory.Strike, 0.15f },
                { SkillCategory.Defense, -0.10f },
                { SkillCategory.Mobility, 0.05f },
                { SkillCategory.Tactics, -0.10f }
            }
        },
        {
            BattleDirective.Normal, new Dictionary<SkillCategory, float>
            {
                { SkillCategory.Strike, 0f },
                { SkillCategory.Defense, 0f },
                { SkillCategory.Mobility, 0f },
                { SkillCategory.Tactics, 0f }
            }
        },
        {
            BattleDirective.Defensive, new Dictionary<SkillCategory, float>
            {
                { SkillCategory.Strike, -0.15f },
                { SkillCategory.Defense, 0.15f },
                { SkillCategory.Mobility, -0.05f },
                { SkillCategory.Tactics, 0.05f }
            }
        },
        {
            BattleDirective.Technical, new Dictionary<SkillCategory, float>
            {
                { SkillCategory.Strike, -0.10f },
                { SkillCategory.Defense, -0.05f },
                { SkillCategory.Mobility, 0.05f },
                { SkillCategory.Tactics, 0.10f }
            }
        }
    };

    public static float GetModifier(BattleDirective directive, SkillCategory category)
    {
        if (table.TryGetValue(directive, out var catMap))
            if (catMap.TryGetValue(category, out float val))
                return val;
        return 0f;
    }
}
