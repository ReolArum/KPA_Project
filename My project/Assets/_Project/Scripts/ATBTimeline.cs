// ===== ATBTimeline.cs =====

using System.Collections.Generic;
using UnityEngine;

public class ATBTimeline
{
    private List<CombatUnit> units = new();

    public void Initialize(params CombatUnit[] combatUnits)
    {
        units.Clear();
        foreach (var u in combatUnits)
        {
            u.currentAV = CalculateBaseAV(u.derived.SPD);
            units.Add(u);
        }
    }

    public float CalculateBaseAV(float spd)
    {
        return spd > 0 ? 10000f / spd : 10000f;
    }

    /// <summary>다음 행동할 유닛을 반환하고, 시간을 진행시킨다.</summary>
    public CombatUnit AdvanceAndGetNext()
    {
        if (units.Count == 0) return null;

        // 가장 AV가 낮은 유닛 찾기
        CombatUnit next = units[0];
        foreach (var u in units)
        {
            if (u.currentAV < next.currentAV)
                next = u;
        }

        // 시간 경과: 모든 유닛의 AV를 경과분만큼 감소
        float elapsed = next.currentAV;
        foreach (var u in units)
            u.currentAV -= elapsed;

        // 행동한 유닛의 AV를 리셋
        next.currentAV = CalculateBaseAV(next.derived.SPD);

        return next;
    }

    /// <summary>전진 (AV 감소 = 행동 앞당김)</summary>
    public void AdvanceUnit(CombatUnit unit, float amount)
    {
        unit.currentAV = Mathf.Max(0, unit.currentAV - amount);
    }

    /// <summary>지연 (AV 증가 = 행동 미룸)</summary>
    public void DelayUnit(CombatUnit unit, float amount)
    {
        unit.currentAV += amount;
    }

    /// <summary>SPD 변경 시 호출. 남은 비율 유지하며 AV 재계산.</summary>
    public void OnSpeedChanged(CombatUnit unit, float oldSPD, float newSPD)
    {
        float oldBase = CalculateBaseAV(oldSPD);
        float ratio = oldBase > 0 ? unit.currentAV / oldBase : 1f;
        unit.currentAV = ratio * CalculateBaseAV(newSPD);
    }

    /// <summary>현재 행동 순서 프리뷰 (UI 타임라인 표시용)</summary>
    public List<(CombatUnit unit, float av)> GetTimeline()
    {
       var result = new List<(CombatUnit unit, float av)>();
        foreach (var u in units)
            result.Add((u, u.currentAV));
        result.Sort((a, b) => a.av.CompareTo(b.av));
        return result;
    }
}
