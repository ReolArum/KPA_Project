using UnityEngine;

/// <summary>
/// 캐릭터의 기초 스탯, 장비, 유파 정보를 종합하여 최종 전투 스테이터스를 산출하는 프로세서.
/// </summary>
public static class CombatStatProcessor
{
    /// <summary>
    /// 현재 게임 상태와 전투 데이터를 기반으로 최종 전투 능력치를 계산합니다.
    /// </summary>
    public static CombatBaseStats CalculateStats(GameState state, PlayerCombatData combatData)
    {
        // 1. 기초 훈련 스탯 → 전투 스탯 변환
        CombatBaseStats result = CombatBaseStats.FromTrainingStats(state.fighter.stats);

        // 2. 장비 스탯 합산
        foreach (var gear in combatData.equippedGear.Values)
        {
            if (gear != null)
            {
                gear.ApplyTo(result);
            }
        }

        // 3. (Optional) 현재 활성화된 버프나 특수 효과 적용 지점
        
        return result;
    }

    /// <summary>
    /// 모든 유파의 레벨 보너스와 장비의 특수 보너스를 합산한 최종 유파 보너스를 계산합니다.
    /// </summary>
    public static SchoolBonus CalculateTotalSchoolBonus(PlayerCombatData combatData, SchoolDatabase schoolDB)
    {
        SchoolBonus total = new SchoolBonus();
        if (schoolDB == null) return total;

        // 유파 레벨 보너스 합산
        foreach (var kvp in combatData.schoolLevels)
        {
            if (kvp.Value <= 0) continue;

            var schoolData = schoolDB.GetSchool(kvp.Key);
            if (schoolData != null)
            {
                var bonus = schoolData.GetCumulativeBonus(kvp.Value);
                total.Add(bonus);
            }
        }

        // 장비 특수 보너스 합산
        foreach (var gear in combatData.equippedGear.Values)
        {
            if (gear != null && gear.specialBonus != null)
            {
                total.Add(gear.specialBonus);
            }
        }

        return total;
    }
}
