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
        var state = GameManager.Instance.State;
        _availableBonusCount = GameManager.GetDayBonusCount(state);
        _selectedSlots.Clear();

        if (_availableBonusCount <= 0)
        {
            ConfirmExit(); 
            return;
        }

        _popupRoot.SetActive(true);
    }

    public void ConfirmExit()
    {
        var state = GameManager.Instance.State;
        foreach (int index in _selectedSlots)
        {
            TrainingManager.Instance.ApplyBonus(index, 1.2f);
        }

        _popupRoot.SetActive(false);
        GameManager.Instance.FinishDay();
    }
}
