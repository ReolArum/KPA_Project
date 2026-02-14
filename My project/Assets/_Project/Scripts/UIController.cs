using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    // ====================================================
    //  Panels
    // ====================================================
    [Header("Panels")]
    [SerializeField] private GameObject panelTitle;
    [SerializeField] private GameObject panelSchedule;
    [SerializeField] private GameObject panelDayProgress;
    [SerializeField] private GameObject panelNightChoice;
    [SerializeField] private GameObject panelNightAction;
    [SerializeField] private GameObject panelDaySummary;

    // ====================================================
    //  Top Bar
    // ====================================================
    [Header("TopBar")]
    [SerializeField] private TMP_Text textDay;
    [SerializeField] private TMP_Text textTime;
    [SerializeField] private TMP_Text textGold;
    [SerializeField] private TMP_Text textStress;
    [SerializeField] private TMP_Text textFatigue;

    // ====================================================
    //  Schedule Grid (낮 4슬롯)
    // ====================================================
    [Header("Schedule Grid")]
    [SerializeField] private Transform scheduleGridRoot;
    [SerializeField] private ScheduleSlotView slotPrefab;

    // ====================================================
    //  Day Progress
    // ====================================================
    [Header("Day Progress")]
    [SerializeField] private TMP_Text textCurrentSlotInfo;
    [SerializeField] private TMP_Text textActionResult;      // 행동 결과 텍스트
    [SerializeField] private Button btnExecuteSlot;

    // ====================================================
    //  Training Choice Popup
    // ====================================================
    [Header("Training Choice Popup")]
    [SerializeField] private GameObject popupTrainingChoice;
    [SerializeField] private Button btnTrainStrength;
    [SerializeField] private Button btnTrainAgility;
    [SerializeField] private Button btnTrainDexterity;
    [SerializeField] private Button btnTrainEndurance;
    [SerializeField] private TMP_Text textTrainingInfo;      // 현재 스탯 + 숙련도 표시

    // ====================================================
    //  Night Choice
    // ====================================================
    [Header("Night Choice")]
    [SerializeField] private Button btnNightExplore;
    [SerializeField] private Button btnNightArena;
    [SerializeField] private Button btnNightRest;
    [SerializeField] private TMP_Text textArenaStatus;
    [SerializeField] private TMP_Text textNightWarning;

    // ====================================================
    //  Night Action Result
    // ====================================================
    [Header("Night Action")]
    [SerializeField] private TMP_Text textNightResult;

    // ====================================================
    //  Day Summary
    // ====================================================
    [Header("Day Summary")]
    [SerializeField] private TMP_Text textSummary;

    // ====================================================
    //  Slot Selection Popup
    // ====================================================
    [Header("Slot Selection Popup")]
    [SerializeField] private GameObject popupSlotDropdown;
    [SerializeField] private Button btnSlotTraining;
    [SerializeField] private Button btnSlotPartTime;
    [SerializeField] private Button btnSlotShop;
    [SerializeField] private Button btnSlotInvestigation;
    [SerializeField] private Button btnSlotRelationship;
    [SerializeField] private Button btnSlotRest;
    [SerializeField] private Button btnSlotClosePopup;

    // ====================================================
    //  Level Up Notice
    // ====================================================
    [Header("Level Up Notice")]
    [SerializeField] private GameObject panelLevelUpNotice;
    [SerializeField] private TMP_Text textLevelUpNotice;
    [SerializeField] private Button btnCloseLevelUp;

    // ====================================================
    //  Colors
    // ====================================================
    [Header("Slot Colors")]
    [SerializeField] private Color colorTraining = new Color(0.85f, 0.35f, 0.35f);
    [SerializeField] private Color colorPartTime = new Color(0.35f, 0.75f, 0.35f);
    [SerializeField] private Color colorShop = new Color(0.90f, 0.80f, 0.30f);
    [SerializeField] private Color colorInvestigation = new Color(0.60f, 0.40f, 0.80f);
    [SerializeField] private Color colorRelationship = new Color(0.90f, 0.55f, 0.70f);
    [SerializeField] private Color colorRest = new Color(0.55f, 0.55f, 0.55f);

    // ====================================================
    //  Internal
    // ====================================================
    readonly List<ScheduleSlotView> slotViews = new();
    GameManager gm;
    int popupTargetIndex = -1;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();

        BuildScheduleGrid();
        SetupSlotPopupButtons();
        SetupNightButtons();
        SetupTrainingButtons();
        SetupLevelUpNotice();

        CloseSlotPopup();
        ShowTrainingChoicePopup(false);
        CloseLevelUpNotice();
    }

    // ====================================================
    //  Build / Setup
    // ====================================================

    void BuildScheduleGrid()
    {
        if (scheduleGridRoot == null || slotPrefab == null) return;
        if (slotViews.Count == GameState.DaySlotCount) return;

        for (int i = scheduleGridRoot.childCount - 1; i >= 0; i--)
            Destroy(scheduleGridRoot.GetChild(i).gameObject);

        slotViews.Clear();

        for (int i = 0; i < GameState.DaySlotCount; i++)
        {
            var v = Instantiate(slotPrefab, scheduleGridRoot);
            v.Init(this, i);
            v.SetTimeLabel($"{GameManager.DaySlotToTimeLabel(i)}~{GameManager.DaySlotToEndTimeLabel(i)}");
            slotViews.Add(v);
        }
    }

    void SetupSlotPopupButtons()
    {
        if (btnSlotTraining) btnSlotTraining.onClick.AddListener(() => SelectSlotType(DaySlotType.Training));
        if (btnSlotPartTime) btnSlotPartTime.onClick.AddListener(() => SelectSlotType(DaySlotType.PartTime));
        if (btnSlotShop) btnSlotShop.onClick.AddListener(() => SelectSlotType(DaySlotType.Shop));
        if (btnSlotInvestigation) btnSlotInvestigation.onClick.AddListener(() => SelectSlotType(DaySlotType.Investigation));
        if (btnSlotRelationship) btnSlotRelationship.onClick.AddListener(() => SelectSlotType(DaySlotType.Relationship));
        if (btnSlotRest) btnSlotRest.onClick.AddListener(() => SelectSlotType(DaySlotType.Rest));
        if (btnSlotClosePopup) btnSlotClosePopup.onClick.AddListener(CloseSlotPopup);
    }

    void SetupNightButtons()
    {
        if (btnNightExplore) btnNightExplore.onClick.AddListener(() => gm.OnClickNightChoice((int)NightActionType.Exploration));
        if (btnNightArena) btnNightArena.onClick.AddListener(() => gm.OnClickNightChoice((int)NightActionType.Arena));
        if (btnNightRest) btnNightRest.onClick.AddListener(() => gm.OnClickNightChoice((int)NightActionType.Rest));
    }

    void SetupTrainingButtons()
    {
        if (btnTrainStrength) btnTrainStrength.onClick.AddListener(() => gm.OnTrainingStatChosen(TrainingStat.Strength));
        if (btnTrainAgility) btnTrainAgility.onClick.AddListener(() => gm.OnTrainingStatChosen(TrainingStat.Agility));
        if (btnTrainDexterity) btnTrainDexterity.onClick.AddListener(() => gm.OnTrainingStatChosen(TrainingStat.Dexterity));
        if (btnTrainEndurance) btnTrainEndurance.onClick.AddListener(() => gm.OnTrainingStatChosen(TrainingStat.Endurance));
    }

    void SetupLevelUpNotice()
    {
        if (btnCloseLevelUp) btnCloseLevelUp.onClick.AddListener(CloseLevelUpNotice);
    }

    // ====================================================
    //  Panel Switching
    // ====================================================

    public void ShowPhase(GamePhase phase)
    {
        if (panelTitle) panelTitle.SetActive(phase == GamePhase.Title);
        if (panelSchedule) panelSchedule.SetActive(phase == GamePhase.ScheduleSetting);
        if (panelDayProgress) panelDayProgress.SetActive(phase == GamePhase.DayProgress);
        if (panelNightChoice) panelNightChoice.SetActive(phase == GamePhase.NightChoice);
        if (panelNightAction) panelNightAction.SetActive(phase == GamePhase.NightAction);
        if (panelDaySummary) panelDaySummary.SetActive(phase == GamePhase.DaySummary);

        if (phase != GamePhase.ScheduleSetting) CloseSlotPopup();
        if (phase != GamePhase.DayProgress) ShowTrainingChoicePopup(false);
    }

    // ====================================================
    //  Refresh
    // ====================================================

    public void RefreshAll(GameState state, GamePhase phase)
    {
        RefreshTopBar(state, phase);
        RefreshScheduleGrid(state);

        if (phase == GamePhase.DayProgress)
            RefreshDayProgress(state);
        if (phase == GamePhase.NightChoice)
            RefreshNightChoice(state);
        if (phase == GamePhase.NightAction)
            RefreshNightAction(state);
        if (phase == GamePhase.DaySummary)
            RefreshDaySummary(state);
    }

    void RefreshTopBar(GameState state, GamePhase phase)
    {
        if (textDay) textDay.text = $"Day {state.day}";
        if (textGold) textGold.text = $"Gold: {state.gold}";
        if (textTime) textTime.text = GameManager.GetCurrentTimeLabel(state, phase);
        if (textStress) textStress.text = $"스트레스: {state.stress}";
        if (textFatigue) textFatigue.text = $"피로: {state.fatigue}";
    }

    void RefreshScheduleGrid(GameState state)
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].SetType(state.daySchedule[i],
                colorTraining, colorPartTime, colorShop,
                colorInvestigation, colorRelationship, colorRest);
            slotViews[i].SetProgressVisual(state.currentDaySlot);
        }
    }

    void RefreshDayProgress(GameState state)
    {
        if (textCurrentSlotInfo == null) return;

        if (state.currentDaySlot >= GameState.DaySlotCount)
        {
            textCurrentSlotInfo.text = "낮 일과 완료!";
            return;
        }

        string time = GameManager.DaySlotToTimeLabel(state.currentDaySlot);
        string endTime = GameManager.DaySlotToEndTimeLabel(state.currentDaySlot);
        DaySlotType action = state.daySchedule[state.currentDaySlot];
        string actionName = GetDaySlotName(action);

        textCurrentSlotInfo.text =
            $"[{time}~{endTime}] {actionName}\n" +
            $"슬롯 {state.currentDaySlot + 1} / {GameState.DaySlotCount}";

        // 훈련 대기 중이면 팝업 표시
        if (state.waitingForTrainingChoice)
            RefreshTrainingPopup(state);
    }

    void RefreshNightChoice(GameState state)
    {
        if (textArenaStatus)
        {
            textArenaStatus.text = state.IsArenaOpen
                ? "아레나: 오픈 (참가 가능)"
                : $"아레나: 휴무 (다음 오픈: Day {NextArenaDay(state.day)})";
        }

        bool stressLocked = state.stress >= 80;
        if (btnNightExplore) btnNightExplore.interactable = !stressLocked;
        if (btnNightArena) btnNightArena.interactable = state.IsArenaOpen && !stressLocked;

        if (textNightWarning)
        {
            if (stressLocked) textNightWarning.text = "스트레스가 너무 높습니다! 휴식만 가능합니다.";
            else if (state.stress >= 60) textNightWarning.text = "스트레스가 높습니다. 주의하세요.";
            else textNightWarning.text = "";
        }
    }

    void RefreshNightAction(GameState state)
    {
        if (textNightResult == null) return;

        string actionName = state.nightChoice switch
        {
            NightActionType.Exploration => "탐사",
            NightActionType.Arena => "아레나",
            NightActionType.Rest => "휴식",
            _ => "?"
        };

        textNightResult.text = $"밤 행동: {actionName} 완료!";
    }

    void RefreshDaySummary(GameState state)
    {
        if (textSummary == null) return;

        var pt = state.profTraining;
        var pi = state.profInvestigation;
        var pe = state.profExploration;
        var pp = state.profPartTime;

        textSummary.text =
            $"===== Day {state.day} 결산 =====\n\n" +

            $"[전투 스탯]\n" +
            $"  힘: {state.statStrength}  민첩: {state.statAgility}\n" +
            $"  재주: {state.statDexterity}  지구력: {state.statEndurance}\n\n" +

            $"[숙련도]\n" +
            $"  훈련  Lv.{pt.level} ({pt.exp}/{(pt.IsMaxLevel ? "MAX" : pt.ExpToNext + pt.exp + "")})\n" +
            $"  조사  Lv.{pi.level} ({pi.exp}/{(pi.IsMaxLevel ? "MAX" : pi.ExpToNext + pi.exp + "")})\n" +
            $"  탐사  Lv.{pe.level} ({pe.exp}/{(pe.IsMaxLevel ? "MAX" : pe.ExpToNext + pe.exp + "")})\n" +
            $"  알바  Lv.{pp.level} ({pp.exp}/{(pp.IsMaxLevel ? "MAX" : pp.ExpToNext + pp.exp + "")})\n\n" +

            $"[오늘]\n" +
            $"  훈련: {state.todayTrainingCount}회  |  획득 골드: {state.todayGoldEarned}\n" +
            $"  총 골드: {state.gold}\n" +
            $"  스트레스: {state.stress}  |  피로: {state.fatigue}\n\n" +

            (state.IsArenaOpen ? "오늘은 아레나가 열린 날이었습니다.\n" : "");
    }

    // ====================================================
    //  Training Choice Popup
    // ====================================================

    public void ShowTrainingChoicePopup(bool show)
    {
        if (popupTrainingChoice) popupTrainingChoice.SetActive(show);

        if (show && gm != null)
            RefreshTrainingPopup(gm.State);
    }

    void RefreshTrainingPopup(GameState state)
    {
        if (textTrainingInfo == null) return;

        var p = state.profTraining;
        string lvlInfo = p.IsMaxLevel ? "MAX" : $"Lv.{p.level} (EXP: {p.exp})";
        string bonus = p.TrainingStatMultiplier > 1f ? $"  보너스: +{(p.TrainingStatMultiplier - 1f) * 100:0}%" : "";
        string fatigueNote = p.TrainingFatigueReduction > 0 ? $"  피로 경감: -{p.TrainingFatigueReduction}" : "";
        string advNote = p.AdvancedTrainingUnlocked ? "\n  ★ 상급 훈련 해금!" : "";

        textTrainingInfo.text =
            $"훈련할 스탯을 선택하세요.\n\n" +
            $"  힘: {state.statStrength}  |  민첩: {state.statAgility}\n" +
            $"  재주: {state.statDexterity}  |  지구력: {state.statEndurance}\n\n" +
            $"훈련 숙련도: {lvlInfo}{bonus}{fatigueNote}{advNote}";
    }

    // ====================================================
    //  Action Result (간단 토스트)
    // ====================================================

    public void ShowActionResult(string msg)
    {
        if (textActionResult) textActionResult.text = msg;
    }

    // ====================================================
    //  Level Up Notice
    // ====================================================

    public void ShowLevelUpNotice(ProficiencyType type, int newLevel)
    {
        if (panelLevelUpNotice == null) return;

        string typeName = type switch
        {
            ProficiencyType.Training => "훈련",
            ProficiencyType.Investigation => "조사",
            ProficiencyType.Exploration => "탐사",
            ProficiencyType.PartTime => "아르바이트",
            _ => "?"
        };

        if (textLevelUpNotice)
            textLevelUpNotice.text = $"{typeName} 숙련도가 Lv.{newLevel}로 상승했습니다!";

        panelLevelUpNotice.SetActive(true);
    }

    void CloseLevelUpNotice()
    {
        if (panelLevelUpNotice) panelLevelUpNotice.SetActive(false);
    }

    // ====================================================
    //  Warnings
    // ====================================================

    public void ShowArenaClosedWarning()
    {
        if (textNightWarning) textNightWarning.text = "아레나는 오늘 열리지 않습니다!";
    }

    public void ShowStressWarning()
    {
        if (textNightWarning) textNightWarning.text = "스트레스가 너무 높아 휴식만 가능합니다!";
    }

    // ====================================================
    //  Schedule Slot Popup
    // ====================================================

    public void OnClickScheduleSlot(int index)
    {
        if (gm != null && gm.Phase != GamePhase.ScheduleSetting) return;
        popupTargetIndex = index;
        if (popupSlotDropdown) popupSlotDropdown.SetActive(true);
    }

    void CloseSlotPopup()
    {
        popupTargetIndex = -1;
        if (popupSlotDropdown) popupSlotDropdown.SetActive(false);
    }

    void SelectSlotType(DaySlotType t)
    {
        if (gm == null || gm.Phase != GamePhase.ScheduleSetting) { CloseSlotPopup(); return; }
        if (popupTargetIndex < 0 || popupTargetIndex >= GameState.DaySlotCount) { CloseSlotPopup(); return; }

        gm.State.daySchedule[popupTargetIndex] = t;
        RefreshScheduleGrid(gm.State);
        CloseSlotPopup();
    }

    // ====================================================
    //  Utility
    // ====================================================

    public static string GetDaySlotName(DaySlotType t) => t switch
    {
        DaySlotType.Training => "훈련",
        DaySlotType.PartTime => "아르바이트",
        DaySlotType.Shop => "상점",
        DaySlotType.Investigation => "조사",
        DaySlotType.Relationship => "호감도",
        DaySlotType.Rest => "휴식",
        _ => "?"
    };

    public static Color GetSlotColor(DaySlotType t,
        Color training, Color partTime, Color shop,
        Color investigation, Color relationship, Color rest) => t switch
    {
        DaySlotType.Training => training,
        DaySlotType.PartTime => partTime,
        DaySlotType.Shop => shop,
        DaySlotType.Investigation => investigation,
        DaySlotType.Relationship => relationship,
        DaySlotType.Rest => rest,
        _ => rest
    };

    static int NextArenaDay(int currentDay)
    {
        for (int d = currentDay + 1; d <= currentDay + 3; d++)
            if (d % 3 == 0) return d;
        return currentDay + 3;
    }
}
