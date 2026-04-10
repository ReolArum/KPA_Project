using UnityEngine;
using System.Collections.Generic;

public class EarlyExitUI : MonoBehaviour
{
    [SerializeField] private GameObject _popupRoot;
    private int _availableBonusCount = 0;
    private List<int> _selectedSlots = new List<int>();

    /// <summary>
    /// 조기 종료 버튼 클릭 시 호출
    /// </summary>
    public void OpenPopup()
    {
        if (DayTimeManager.Instance == null) return;

        _availableBonusCount = DayTimeManager.Instance.GetBonusCount();
        _selectedSlots.Clear();

        if (_availableBonusCount <= 0)
        {
            ConfirmExit(); // 보너스가 없으면 바로 종료
            return;
        }

        _popupRoot.SetActive(true);
        Debug.Log($"[EarlyExit] 보너스 횟수 획득: {_availableBonusCount}회. 슬롯을 선택하세요.");
    }

    /// <summary>
    /// 버튼에서 슬롯 인덱스를 전달받아 보너스 적용
    /// </summary>
    public void ToggleBonusSlot(int slotIndex)
    {
        if (_selectedSlots.Contains(slotIndex))
        {
            _selectedSlots.Remove(slotIndex);
        }
        else if (_selectedSlots.Count < _availableBonusCount)
        {
            _selectedSlots.Add(slotIndex);
        }

        // TODO: UI 상의 선택 상태 업데이트 (Highlight 등)
    }

    public void ConfirmExit()
    {
        foreach (int index in _selectedSlots)
        {
            FighterScheduleManager.Instance.ApplyBonus(index);
        }

        FighterScheduleManager.Instance.ProcessResults();
        _popupRoot.SetActive(false);
        
        // TODO: 밤 페이즈 전환 로직 호출
        Debug.Log("[EarlyExit] 보너스 적용 완료. 밤으로 전환합니다.");
    }
}
