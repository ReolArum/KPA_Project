using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainGameUIController : MonoBehaviour
{
    [Header("Module Controllers")]
    [SerializeField] private GlobalHUDController globalHUD;

    [Header("Panels")]
    [SerializeField] private GameObject panelSchedule;
    [SerializeField] private GameObject panelDayMap;
    [SerializeField] private GameObject panelDayPlaceAction;
    [SerializeField] private GameObject panelDaySummary;

    [Header("Fighter Schedule")]
    [SerializeField] private Transform scheduleGridRoot;
    [SerializeField] private ScheduleSlotView slotPrefab;
    [SerializeField] private Button btnStartDay; 
    [SerializeField] private Button btnApplyYesterday;
    [SerializeField] private Button btnResetSchedule;
    [SerializeField] private TMP_Text textSchedulePreviewResult;

    [Header("Action Tabs")]
    [SerializeField] private Button btnTabTraining;
    [SerializeField] private Button btnTabWork;
    [SerializeField] private Button btnTabRest;
    [SerializeField] private GameObject panelTrainingContent;
    [SerializeField] private GameObject panelWorkContent;
    [SerializeField] private GameObject panelRestContent;

    [Header("Day Map")]
    [SerializeField] private Button btnMapHome;
    [SerializeField] private Button btnMapShop;
    [SerializeField] private Button btnMapTrainingGround;
    [SerializeField] private Button btnMapCafe;
    [SerializeField] private Button btnMapQuestBoard;

    [Header("Place Action Results")]
    [SerializeField] private TMP_Text textPlaceActionResult;

    [Header("Day Summary")]
    [SerializeField] private TMP_Text textSummary;
    [SerializeField] private Button btnNextDay;

    [Header("Calendar")]
    [SerializeField] private GameObject panelCalendar;
    [SerializeField] private Button btnOpenCalendar;
    [SerializeField] private Button btnCalendarClose;

    [Header("Colors")]
    [SerializeField] private Color colorTraining = new Color(0.48f, 0.72f, 0.89f);
    [SerializeField] private Color colorPartTime = new Color(0.98f, 0.82f, 0.25f);
    [SerializeField] private Color colorRest = new Color(0.44f, 0.74f, 0.23f);

    private readonly List<ScheduleSlotView> slotViews = new();
    private GameManager gm;
    private int selectedScheduleSlotIndex = 0;

    void Awake()
    {
        gm = GameManager.Instance; // Uses persistent singleton
        
        BuildScheduleGrid();
        SetupActionTabs();
        SetupMapButtons();
        SetupScheduleButtons();
        SetupCalendarButtons();
        if (btnNextDay) btnNextDay.onClick.AddListener(() => gm.OnClickNextDay());
    }

    void OnEnable()
    {
        GameEvents.OnPhaseChanged += HandlePhaseChanged;
        GameEvents.OnRefreshRequested += HandleRefreshRequested;
        GameEvents.OnActionResult += HandleActionResult;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    void OnDisable()
    {
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        GameEvents.OnRefreshRequested -= HandleRefreshRequested;
        GameEvents.OnActionResult -= HandleActionResult;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    // --- Event Handlers ---
    void HandlePhaseChanged(GamePhase phase) => ShowPhase(phase);
    void HandleRefreshRequested(GameState state, GamePhase phase) => RefreshAll(state, phase);
    void HandleActionResult(string msg) { if (textPlaceActionResult) textPlaceActionResult.text = msg; }
    void HandleGameStateChanged(GameState state) => RefreshAll(state, gm.Phase);

    // --- Setup ---
    void BuildScheduleGrid()
    {
        if (scheduleGridRoot == null || slotPrefab == null) return;
        foreach (Transform child in scheduleGridRoot) Destroy(child.gameObject);
        slotViews.Clear();

        for (int i = 0; i < GameState.DaySlotCount; i++)
        {
            var v = Instantiate(slotPrefab, scheduleGridRoot);
            v.Init(this, i); // Modified ScheduleSlotView required
            slotViews.Add(v);
        }
        SetScheduleTimeLabels();
    }

    void SetScheduleTimeLabels()
    {
        string[] times = { "8am-11am", "11am-2pm", "2pm-5pm", "5pm-8pm" };
        for (int i = 0; i < slotViews.Count && i < times.Length; i++)
            slotViews[i].SetTimeLabel(times[i]);
    }

    void SetupActionTabs()
    {
        if (btnTabTraining) btnTabTraining.onClick.AddListener(() => SwitchActionTab(0));
        if (btnTabWork) btnTabWork.onClick.AddListener(() => SwitchActionTab(1));
        if (btnTabRest) btnTabRest.onClick.AddListener(() => SwitchActionTab(2));
        SwitchActionTab(0);
    }

    void SwitchActionTab(int index)
    {
        if (panelTrainingContent) panelTrainingContent.SetActive(index == 0);
        if (panelWorkContent) panelWorkContent.SetActive(index == 1);
        if (panelRestContent) panelRestContent.SetActive(index == 2);
    }

    void SetupMapButtons()
    {
        if (btnMapHome) btnMapHome.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Home));
        if (btnMapShop) btnMapShop.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Shop));
        if (btnMapTrainingGround) btnMapTrainingGround.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.TrainingGround));
        if (btnMapCafe) btnMapCafe.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Cafe));
        if (btnMapQuestBoard) btnMapQuestBoard.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.QuestBoard));
    }

    void SetupScheduleButtons()
    {
        if (btnApplyYesterday) btnApplyYesterday.onClick.AddListener(() => gm.OnClickApplyYesterdaySchedule());
        if (btnResetSchedule) btnResetSchedule.onClick.AddListener(() => gm.OnClickResetSchedule());
        if (btnStartDay) btnStartDay.onClick.AddListener(() => gm.OnClickStartDay());
    }

    void SetupCalendarButtons()
    {
        if (btnOpenCalendar) btnOpenCalendar.onClick.AddListener(() => gm.OnClickOpenCalendar());
        if (btnCalendarClose) btnCalendarClose.onClick.AddListener(() => gm.OnClickCloseCalendar());
    }

    // --- Logic ---
    public void OnClickScheduleSlot(int index)
    {
        selectedScheduleSlotIndex = Mathf.Clamp(index, 0, GameState.DaySlotCount - 1);
        RefreshAll(gm.State, gm.Phase);
    }

    public void OnActionSelected(FighterSlotType type, TrainingStat stat)
    {
        gm.SetScheduleSlot(selectedScheduleSlotIndex, type, stat);
        selectedScheduleSlotIndex = (selectedScheduleSlotIndex + 1) % GameState.DaySlotCount;
        RefreshAll(gm.State, gm.Phase);
    }

    void ShowPhase(GamePhase phase)
    {
        if (panelSchedule) panelSchedule.SetActive(phase == GamePhase.ScheduleSetting);
        if (panelDayMap) panelDayMap.SetActive(phase == GamePhase.DayMap);
        if (panelDayPlaceAction) panelDayPlaceAction.SetActive(phase == GamePhase.DayPlaceAction);
        if (panelDaySummary) panelDaySummary.SetActive(phase == GamePhase.DaySummary);
    }

    public void RefreshAll(GameState state, GamePhase phase)
    {
        if (globalHUD) globalHUD.Refresh(state, phase);
        RefreshScheduleGrid(state);
        if (textSchedulePreviewResult) textSchedulePreviewResult.text = gm.GetTotalPredictedOutcome();
        if (phase == GamePhase.DaySummary) RefreshDaySummary(state);
    }

    void RefreshScheduleGrid(GameState state)
    {
        for (int i = 0; i < slotViews.Count && i < state.fighterSchedule.Length; i++)
        {
            var slot = state.fighterSchedule[i];
            string label = slot.type switch {
                FighterSlotType.Training => GameManager.GetStatName(slot.trainingStat),
                FighterSlotType.PartTime => "알바",
                FighterSlotType.Rest => "휴식",
                _ => "미정"
            };
            Color c = slot.type switch {
                FighterSlotType.Training => colorTraining,
                FighterSlotType.PartTime => colorPartTime,
                FighterSlotType.Rest => colorRest,
                _ => Color.gray
            };
            slotViews[i].SetDirect(label, c);
            slotViews[i].SetProgressVisual(state.fighterSlotProgress, i == selectedScheduleSlotIndex);
        }
    }

    void RefreshDaySummary(GameState state)
    {
        if (textSummary == null) return;
        textSummary.text = $"===== {state.DateString} 결산 =====\n\n" +
            $"힘: {state.GetStat(TrainingStat.Strength)}  민: {state.GetStat(TrainingStat.Agility)}  기: {state.GetStat(TrainingStat.Dexterity)}  체: {state.GetStat(TrainingStat.Endurance)}\n" +
            $"Gold: {state.gold}  스트레스: {state.stress}  피로: {state.fatigue}";
    }
}
