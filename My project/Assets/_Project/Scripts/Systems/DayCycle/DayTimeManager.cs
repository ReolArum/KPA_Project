using UnityEngine;
using System;

public class DayTimeManager : MonoBehaviour
{
    public static DayTimeManager Instance { get; private set; }

    public const int MaxDaySlots = 8;
    [SerializeField] private int _remainingSlots = MaxDaySlots;

    public event Action<int> OnDayTimeUpdated;
    public event Action OnDayTimeExhausted;

    public int RemainingSlots => _remainingSlots;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResetDay()
    {
        _remainingSlots = MaxDaySlots;
        OnDayTimeUpdated?.Invoke(_remainingSlots);
    }

    /// <summary>
    /// 장소 이동 시 호출 (1슬롯 소모)
    /// </summary>
    public bool ConsumeSlot(int amount = 1)
    {
        if (_remainingSlots <= 0) return false;

        _remainingSlots = Mathf.Max(0, _remainingSlots - amount);
        OnDayTimeUpdated?.Invoke(_remainingSlots);

        if (_remainingSlots == 0)
        {
            OnDayTimeExhausted?.Invoke();
        }
        return true;
    }

    /// <summary>
    /// 조기 종료 시 획득 가능한 보너스 횟수 계산 (남은 시간 / 2)
    /// </summary>
    public int GetBonusCount()
    {
        return Mathf.FloorToInt(_remainingSlots / 2.0f);
    }
}
