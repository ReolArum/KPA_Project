using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ScheduleSlot
{
    public ScheduleActionData actionData;
    public bool isBonusApplied = false;
}

public class FighterScheduleManager : MonoBehaviour
{
    public static FighterScheduleManager Instance { get; private set; }

    public const int TotalScheduleSlots = 4;
    private ScheduleSlot[] _slots = new ScheduleSlot[TotalScheduleSlots];

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < TotalScheduleSlots; i++) _slots[i] = new ScheduleSlot();
    }

    public void SetSlot(int index, ScheduleActionData data)
    {
        if (index < 0 || index >= TotalScheduleSlots) return;
        _slots[index].actionData = data;
        _slots[index].isBonusApplied = false;
    }

    /// <summary>
    /// 조기 종료 보너스 적용 (중복 불가)
    /// </summary>
    public bool ApplyBonus(int index)
    {
        if (index < 0 || index >= TotalScheduleSlots) return false;
        if (_slots[index].actionData == null) return false;
        if (_slots[index].isBonusApplied) return false; // 이미 적용됨

        _slots[index].isBonusApplied = true;
        return true;
    }

    public ScheduleSlot[] GetCurrentSchedule() => _slots;

    [Header("Data References")]
    [SerializeField] private List<ScheduleActionData> _actionDatabase;

    /// <summary>
    /// 하루 종료 시 최종 보정된 효율로 결과 계산하며 GameState에 반영
    /// </summary>
    public void ProcessResults()
    {
        var state = GameManager.Instance.State;
        if (state == null) return;

        string resultLog = "[Schedule Results]\n";

        for (int i = 0; i < TotalScheduleSlots; i++)
        {
            var slot = _slots[i];
            var action = slot.actionData;
            if (action == null) continue;

            float efficiency = slot.isBonusApplied ? 1.5f : 1.0f;
            
            // 1. 스탯 반영
            if (action.statIncreaseAmount > 0)
            {
                int finalStat = Mathf.RoundToInt(action.statIncreaseAmount * efficiency);
                state.AddStat(action.targetStat, finalStat);
                resultLog += $"- {action.actionName}: {action.targetStat} +{finalStat} (효율: {efficiency:P0})\n";
            }

            // 2. 재화 및 컨디션 반영
            state.player.gold += Mathf.RoundToInt(action.goldChange * efficiency);
            state.fighter.stress = Mathf.Max(0, state.fighter.stress + action.stressChange);
            
            // TODO: 필요 시 피로도(fatigue) 등 추가 항목 정산
        }

        Debug.Log(resultLog);
        GameEvents.RaiseActionResult("모든 일정이 정산되었습니다.");
        GameEvents.RaiseGameStateChanged(state);
    }
}
