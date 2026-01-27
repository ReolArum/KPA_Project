using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelTitle;
    [SerializeField] private GameObject panelSchedule;
    [SerializeField] private GameObject panelTown;
    [SerializeField] private GameObject panelPlace;
    [SerializeField] private GameObject panelDaySummary;

    [Header("TopBar (Always On)")]
    [SerializeField] private TMP_Text textDay;
    [SerializeField] private TMP_Text textTime;
    [SerializeField] private TMP_Text textGold;
    [SerializeField] private TMP_Text textTodayTrain; // optional

    [Header("Schedule Grid")]
    [SerializeField] private Transform scheduleGridRoot;
    [SerializeField] private ScheduleSlotView slotPrefab;

    [Header("Place Panel")]
    [SerializeField] private TMP_Text textPlaceName;
    [SerializeField] private TMP_Text textPlaceInfo; // optional (준비중/비용 등)

    [Header("Day Summary")]
    [SerializeField] private TMP_Text textSummary;

    [Header("Slot Dropdown Popup")]
    [SerializeField] private GameObject popupSlotDropdown;
    [SerializeField] private Button btnStrength;
    [SerializeField] private Button btnStamina;
    [SerializeField] private Button btnRest;
    [SerializeField] private Button btnClosePopup; // optional: 배경 버튼으로 닫기

    [Header("Colors")]
    [SerializeField] private Color colorStrength = new Color(0.85f, 0.35f, 0.35f);
    [SerializeField] private Color colorStamina = new Color(0.35f, 0.55f, 0.90f);
    [SerializeField] private Color colorRest = new Color(0.55f, 0.55f, 0.55f);

    readonly List<ScheduleSlotView> slotViews = new();

    GameManager gm;
    int popupTargetIndex = -1;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();

        BuildScheduleGridIfNeeded();

        // 팝업 버튼 연결
        if (btnStrength) btnStrength.onClick.AddListener(() => SelectSlotType(SlotType.Strength));
        if (btnStamina) btnStamina.onClick.AddListener(() => SelectSlotType(SlotType.Stamina));
        if (btnRest) btnRest.onClick.AddListener(() => SelectSlotType(SlotType.Rest));
        if (btnClosePopup) btnClosePopup.onClick.AddListener(CloseSlotPopup);

        CloseSlotPopup();
    }

    void BuildScheduleGridIfNeeded()
    {
        if (scheduleGridRoot == null || slotPrefab == null) return;

        // 이미 만들어져 있으면 재생성 안함
        if (slotViews.Count == GameManager.MaxBlocks) return;

        // 기존 자식 삭제(에디터에서 미리 넣었다면)
        for (int i = scheduleGridRoot.childCount - 1; i >= 0; i--)
            Destroy(scheduleGridRoot.GetChild(i).gameObject);

        slotViews.Clear();

        for (int i = 0; i < GameManager.MaxBlocks; i++)
        {
            var v = Instantiate(slotPrefab, scheduleGridRoot);
            v.Init(this, i);
            v.SetTimeLabel(GameManager.BlockToTimeLabel(i));
            slotViews.Add(v);
        }
    }

    // ===== Panel Switching =====
    public void ShowPhase(GamePhase phase)
    {
        if (panelTitle) panelTitle.SetActive(phase == GamePhase.Title);
        if (panelSchedule) panelSchedule.SetActive(phase == GamePhase.ScheduleSetting);
        if (panelTown) panelTown.SetActive(phase == GamePhase.Town);
        if (panelPlace) panelPlace.SetActive(phase == GamePhase.Place);
        if (panelDaySummary) panelDaySummary.SetActive(phase == GamePhase.DaySummary);

        // 결산 때는 팝업 닫아두기
        if (phase == GamePhase.DaySummary) CloseSlotPopup();
    }

    // ===== Refresh =====
    public void RefreshAll(GameState state, GamePhase phase)
    {
        RefreshTopBar(state);
        RefreshScheduleGrid(state);

        if (phase == GamePhase.Place)
            RefreshPlace(state);

        if (phase == GamePhase.DaySummary)
            RefreshDaySummary(state);
    }

    void RefreshTopBar(GameState state)
    {
        if (textDay) textDay.text = $"Day {state.day}";
        if (textGold) textGold.text = $"Gold {state.gold}";

        if (textTime)
        {
            string clock = GameManager.BlockToTimeLabel(state.currentBlock); // 09:00~22:00
            int remainBlocks = Mathf.Clamp(GameManager.MaxBlocks - state.currentBlock, 0, GameManager.MaxBlocks);
            string remain = GameManager.BlocksToHourMinute(remainBlocks);     // 00:00~13:00

            textTime.text = $"시간 {clock} ({state.currentBlock}/{GameManager.MaxBlocks})  남은 {remain}";
        }

        if (textTodayTrain)
            textTodayTrain.text = $"힘 {state.todayStrengthTrain} / 체력 {state.todayStaminaTrain}";
    }


    void RefreshScheduleGrid(GameState state)
    {
        if (slotViews.Count != GameManager.MaxBlocks) return;

        for (int i = 0; i < GameManager.MaxBlocks; i++)
        {
            slotViews[i].SetType(state.schedule[i], colorStrength, colorStamina, colorRest);
            slotViews[i].SetProgressVisual(state.currentBlock);
        }
    }


    void RefreshPlace(GameState state)
    {
        if (textPlaceName)
        {
            textPlaceName.text = state.currentPlace switch
            {
                PlaceType.Shop => "상점",
                PlaceType.PartTime => "아르바이트",
                PlaceType.Explore => "탐험",
                PlaceType.ArenaDesk => "아레나 접수처",
                _ => "장소"
            };
        }

        if (textPlaceInfo)
        {
            int cost = state.currentPlace switch
            {
                PlaceType.Shop => 4,
                PlaceType.PartTime => 4,
                PlaceType.Explore => 6,
                PlaceType.ArenaDesk => 2,
                _ => 4
            };

            int remainBlocks = Mathf.Clamp(GameManager.MaxBlocks - state.currentBlock, 0, GameManager.MaxBlocks);

            string costHM = GameManager.BlocksToHourMinute(cost);
            string remainHM = GameManager.BlocksToHourMinute(remainBlocks);

            bool willEndDay = state.currentBlock + cost >= GameManager.MaxBlocks; // 정확히 26이면 결산, 초과도 결산
            bool overflow = state.currentBlock + cost > GameManager.MaxBlocks;

            string extra = state.currentPlace == PlaceType.PartTime ? " (Gold +10)" : "";

            string warn = "";
            if (overflow) warn = "\n시간 부족: 실행 시 바로 결산됩니다.";
            else if (willEndDay) warn = "\n이번 행동으로 22:00에 도달하여 결산됩니다.";

            textPlaceInfo.text =
                $"행동 비용: {cost} blocks ({costHM}){extra}\n" +
                $"남은 시간: {remainBlocks} blocks ({remainHM})" +
                warn;
        }
    }


    void RefreshDaySummary(GameState state)
    {
        if (textSummary == null) return;

        string used = GameManager.BlocksToHourMinute(GameManager.MaxBlocks); // 13:00
        string today = GameManager.BlocksToHourMinute(state.currentBlock);   // 보통 13:00
        string endClock = GameManager.BlockToTimeLabel(GameManager.MaxBlocks); // 22:00

        textSummary.text =
            $"오늘 시간: {today} / {used}  ({endClock} 종료)\n" +
            $"훈련 누적: 힘 {state.todayStrengthTrain}블록, 체력 {state.todayStaminaTrain}블록\n" +
            $"Gold: {state.gold}";
    }

    // ===== Schedule Slot Click -> Popup =====
    public void OnClickScheduleSlot(int index)
    {
        // 스케줄 설정 상태에서만 변경 가능하게 하고 싶으면 아래 조건 사용
        if (gm != null && gm.Phase != GamePhase.ScheduleSetting) return;

        popupTargetIndex = index;
        OpenSlotPopup();
    }

    void OpenSlotPopup()
    {
        if (popupSlotDropdown) popupSlotDropdown.SetActive(true);
    }

    void CloseSlotPopup()
    {
        popupTargetIndex = -1;
        if (popupSlotDropdown) popupSlotDropdown.SetActive(false);
    }

    void SelectSlotType(SlotType t)
    {
        if (gm == null) return;
        if (gm.Phase != GamePhase.ScheduleSetting) { CloseSlotPopup(); return; }
        if (popupTargetIndex < 0 || popupTargetIndex >= GameManager.MaxBlocks) { CloseSlotPopup(); return; }

        gm.State.schedule[popupTargetIndex] = t;

        RefreshScheduleGrid(gm.State);
        CloseSlotPopup();
    }
}
