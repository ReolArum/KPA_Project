// ===== BuffSystem.cs =====

using System.Collections.Generic;

public class BuffSystem
{
    private ATBTimeline timeline;

    public BuffSystem(ATBTimeline timeline)
    {
        this.timeline = timeline;
    }

    public void ApplyBuff(CombatUnit target, BuffData data)
    {
        if (data == null) return;

        float oldSPD = target.derived.SPD;

        // 갱신형: 동일 ID는 지속시간만 초기화
        var existing = target.activeBuffs.Find(b => b.data.id == data.id);
        if (existing != null)
        {
            existing.remainingTurns = data.duration;
        }
        else
        {
            target.activeBuffs.Add(new BuffInstance
            {
                data = data,
                remainingTurns = data.duration
            });
        }

        // 즉시 스탯 재계산
        target.Recalculate();

        // SPD 변경 시 AV 즉시 재계산
        if (data.AffectsSPD)
        {
            timeline.OnSpeedChanged(target, oldSPD, target.derived.SPD);
        }
    }

    /// <summary>해당 유닛의 턴 시작 시 호출. 지속시간 감소 & 만료 제거.</summary>
    public void OnUnitTurnStart(CombatUnit unit)
    {
        float oldSPD = unit.derived.SPD;
        bool spdChanged = false;

        for (int i = unit.activeBuffs.Count - 1; i >= 0; i--)
        {
            unit.activeBuffs[i].remainingTurns--;
            if (unit.activeBuffs[i].remainingTurns <= 0)
            {
                if (unit.activeBuffs[i].data.AffectsSPD)
                    spdChanged = true;
                unit.activeBuffs.RemoveAt(i);
            }
        }

        unit.Recalculate();

        if (spdChanged)
        {
            timeline.OnSpeedChanged(unit, oldSPD, unit.derived.SPD);
        }
    }
}
