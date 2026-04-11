using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainGameUIController : MonoBehaviour
{
    public static MainGameUIController Instance { get; private set; }
    [Header("Module Controllers")]
    [SerializeField] private GlobalHUDController globalHUD;

    [Header("VN Dialogue UI")]
    [SerializeField] private GameObject  panelVN;
    [SerializeField] private TMP_Text    textVNName;
    [SerializeField] private TMP_Text    textVNDialogue;
    [SerializeField] private Image       imgVNLeft;
    [SerializeField] private Image       imgVNRight;
    [SerializeField] private Image       imgVNBackground;
    [SerializeField] private Button      btnVNDialogueBox;

    private List<DialogueStep>  currentVNSteps;
    private int                 currentVNIndex;
    private System.Action       onVNComplete;
    private DialogueNodeData    curEventNode;

    [Header("Panels")]
    [SerializeField] private GameObject panelSchedule;
    [SerializeField] private GameObject panelDayMap;
    [SerializeField] private GameObject panelDayPlaceAction;
    [SerializeField] private GameObject panelDaySummary;
    [SerializeField] private GameObject panelNightChoice;

    [Header("Night Choice Buttons")]
    [SerializeField] private Button btnNightExploration;
    [SerializeField] private Button btnNightArena;
    [SerializeField] private Button btnNightRest;

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

    [Header("Navigation Buttons")]
    [SerializeField] private Button btnOpenCharacterUI;

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
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (panelVN) panelVN.SetActive(false);
        if (btnVNDialogueBox) btnVNDialogueBox.onClick.AddListener(OnVNClick);

        gm = GameManager.Instance; // Uses persistent singleton
        
        btnMapHome.onClick.AddListener(()           => GameManager.Instance.OnClickMapLocation((int)MapLocation.Base));

        BuildScheduleGrid();
        SetupActionTabs();
        SetupMapButtons();
        SetupNightButtons();
        SetupScheduleButtons();
        SetupCalendarButtons();
        if (btnNextDay) btnNextDay.onClick.AddListener(() => gm.OnClickNextDay());
        if (btnOpenCharacterUI) btnOpenCharacterUI.onClick.AddListener(() => GameManager.Instance.ToggleCharacterUI());
    }

    void OnEnable()
    {
        GameEvents.OnPhaseChanged += HandlePhaseChanged;
        GameEvents.OnRefreshRequested += HandleRefreshRequested;
        GameEvents.OnActionResult += HandleActionResult;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnExplorationVNStarted += HandleVNStarted;
        GameEvents.OnExplorationEventTriggered += HandleEventTriggered;
    }

    void OnDisable()
    {
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        GameEvents.OnRefreshRequested -= HandleRefreshRequested;
        GameEvents.OnActionResult -= HandleActionResult;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnExplorationVNStarted -= HandleVNStarted;
        GameEvents.OnExplorationEventTriggered -= HandleEventTriggered;
    }

    // --- Event Handlers ---
    void HandlePhaseChanged(GamePhase phase) => ShowPhase(phase);
    void HandleRefreshRequested(GameState state, GamePhase phase) => RefreshAll(state, phase);
    void HandleActionResult(string msg) { if (textPlaceActionResult) textPlaceActionResult.text = msg; }
    void HandleGameStateChanged(GameState state) => RefreshAll(state, gm.Phase);

    void HandleVNStarted(List<DialogueStep> steps, System.Action onComplete)
    {
        currentVNSteps = steps;
        currentVNIndex = 0;
        onVNComplete = onComplete;
        if (panelVN) panelVN.SetActive(true);
        ShowVNStep();
    }

    void HandleEventTriggered(DialogueNodeData node, List<DialogueChoiceData> choices)
    {
        curEventNode = node;
    }

    void OnVNClick()
    {
        currentVNIndex++;
        if (currentVNIndex < currentVNSteps.Count)
        {
            ShowVNStep();
        }
        else
        {
            if (panelVN) panelVN.SetActive(false);
            onVNComplete?.Invoke();
        }
    }

    void ShowVNStep()
    {
        if (currentVNSteps == null || currentVNIndex < 0 || currentVNIndex >= currentVNSteps.Count) return;

        var step = currentVNSteps[currentVNIndex];
        if (textVNName) textVNName.text = step.characterName;
        if (textVNDialogue) textVNDialogue.text = step.dialogueText;

        if (imgVNLeft) 
        {
            imgVNLeft.sprite = step.leftSprite;
            imgVNLeft.gameObject.SetActive(step.leftSprite != null);
        }
        if (imgVNRight) 
        {
            imgVNRight.sprite = step.rightSprite;
            imgVNRight.gameObject.SetActive(step.rightSprite != null);
        }
        if (imgVNBackground && step.backgroundOverride != null) 
        {
            imgVNBackground.sprite = step.backgroundOverride;
        }
    }

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
        if (btnMapHome) btnMapHome.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Base));
        if (btnMapShop) btnMapShop.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.GeneralStore));
        if (btnMapTrainingGround) btnMapTrainingGround.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.TrainingGround));
        if (btnMapCafe) btnMapCafe.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Cafe));
        if (btnMapQuestBoard) btnMapQuestBoard.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Agency));
    }

    void SetupScheduleButtons()
    {
        if (btnApplyYesterday) btnApplyYesterday.onClick.AddListener(() => gm.CopyYesterdaySchedule());
        if (btnResetSchedule) btnResetSchedule.onClick.AddListener(() => gm.ResetFighterSchedule());
        if (btnStartDay) btnStartDay.onClick.AddListener(() => gm.OnClickStartDay());
    }

    void SetupCalendarButtons()
    {
        if (btnOpenCalendar) btnOpenCalendar.onClick.AddListener(() => gm.OnClickOpenCalendar());
        if (btnCalendarClose) btnCalendarClose.onClick.AddListener(() => gm.OnClickCloseCalendar());
    }

    void SetupNightButtons()
    {
        if (btnNightExploration) btnNightExploration.onClick.AddListener(() => gm.OnClickTransitionToNight((int)NightActionType.Exploration));
        if (btnNightArena) btnNightArena.onClick.AddListener(() => gm.OnClickTransitionToNight((int)NightActionType.Arena));
        if (btnNightRest) btnNightRest.onClick.AddListener(() => gm.OnClickTransitionToNight((int)NightActionType.Rest));
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
        if (panelSchedule) panelSchedule.SetActive(phase == GamePhase.MorningSchedule);
        if (panelDayMap) panelDayMap.SetActive(phase == GamePhase.DayMap);
        if (panelDayPlaceAction) panelDayPlaceAction.SetActive(phase == GamePhase.DayPlaceAction);
        if (panelNightChoice) panelNightChoice.SetActive(phase == GamePhase.NightTransition);
        if (panelDaySummary) panelDaySummary.SetActive(phase == GamePhase.LateNightReport);
    }

    public void RefreshUI(GameState state)
    {
        RefreshAll(state, gm.Phase);
    }

    public void RefreshAll(GameState state, GamePhase phase)
    {
        if (globalHUD) globalHUD.Refresh(state, phase);
        RefreshScheduleGrid(state);
        
        if (textSchedulePreviewResult) textSchedulePreviewResult.text = gm.GetTotalPredictedOutcome();
        
        if (phase == GamePhase.NightTransition)
        {
            if (btnNightArena) btnNightArena.interactable = state.IsArenaOpen;
        }

        if (phase == GamePhase.LateNightReport) 
            RefreshDaySummary(state);
    }

    void RefreshScheduleGrid(GameState state)
    {
        for (int i = 0; i < slotViews.Count && i < state.fighter.schedule.Length; i++)
        {
            var slot = state.fighter.schedule[i];
            string label = slot.type switch {
                FighterSlotType.Training => GameManager.GetStatName(slot.trainingStat),
                FighterSlotType.Work => "?�바",
                FighterSlotType.Rest => "?�식",
                _ => "미정"
            };
            Color c = slot.type switch {
                FighterSlotType.Training => colorTraining,
                FighterSlotType.Work => colorPartTime,
                FighterSlotType.Rest => colorRest,
                _ => Color.gray
            };
            slotViews[i].SetDirect(label, c);
            slotViews[i].SetProgressVisual(state.fighter.slotProgress, i == selectedScheduleSlotIndex);
        }
    }

    void RefreshDaySummary(GameState state)
    {
        if (textSummary == null) return;
        
        string logSummary = "";
        if (state.dailyActivityLogs.Count > 0)
        {
            foreach (var log in state.dailyActivityLogs)
            {
                logSummary += $"- {log}\n";
            }
        }
        else
        {
            logSummary = "(?�동 기록 ?�음)\n";
        }

        textSummary.text = $"===== {state.DateString} 결산 =====\n\n" +
            $"[?�늘???�동]\n{logSummary}\n" +
            $"[?�재 ?�력�?\n" +
            $"?? {state.GetStat(TrainingStat.Strength)}  민첩: {state.GetStat(TrainingStat.Agility)}  ?�구: {state.GetStat(TrainingStat.Vitality)}\n" +
            $"지?? {state.GetStat(TrainingStat.Intelligence)}  근성: {state.GetStat(TrainingStat.Guts)}  감각: {state.GetStat(TrainingStat.Sensitivity)}\n" +
            $"골드: {state.player.gold}  명성: {state.player.reputation}";
    }
}
