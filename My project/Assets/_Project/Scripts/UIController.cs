// ===== UIController.cs =====
// 리팩토링: GameEvents 이벤트 구독 방식으로 전환
// GameManager 직접 참조 최소화 (버튼 콜백용으로만 유지)
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Module Controllers")]
    [SerializeField] private GlobalHUDController globalHUD;
    [SerializeField] private PlaceActionUIController placeActionUI;

    [Header("Panels")]
    [SerializeField] private GameObject panelTitle;
    [SerializeField] private GameObject panelSchedule;
    [SerializeField] private GameObject panelDayMap;
    [SerializeField] private GameObject panelDayPlaceAction;
    [SerializeField] private GameObject panelNightChoice;
    [SerializeField] private GameObject panelNightAction;
    [SerializeField] private GameObject panelDaySummary;
    [SerializeField] private GameObject panelBattle;

    // ====================================================
    //  Fighter Schedule Grid
    // ====================================================
    [Header("Fighter Schedule")]
    [SerializeField] private Transform scheduleGridRoot;
    [SerializeField] private ScheduleSlotView slotPrefab;

    [Header("Day Map Buttons")]
    [SerializeField] private Button btnMapHome;
    [SerializeField] private Button btnMapShop;
    [SerializeField] private Button btnMapTrainingGround;
    [SerializeField] private Button btnMapCafe;
    [SerializeField] private Button btnMapQuestBoard;
    [SerializeField] private TMP_Text textPlayerLocation;
    [SerializeField] private TMP_Text textFighterProgress;

    [Header("Place Action Buttons")]
    [SerializeField] private Button btnActionTalk;
    [SerializeField] private Button btnActionAcceptQuest;
    [SerializeField] private Button btnActionDeliverQuest;
    [SerializeField] private Button btnActionBuyItem;
    [SerializeField] private Button btnActionSellItem;
    [SerializeField] private Button btnActionRest;
    [SerializeField] private Button btnActionUpgrade;
    [SerializeField] private Button btnActionSupport;
    [SerializeField] private Button btnActionFood;
    [SerializeField] private Button btnActionReroll;
    [SerializeField] private Button btnBackToMap;

    // ====================================================
    //  Quest
    // ====================================================
    [Header("Quest")]
    [SerializeField] private TMP_Text textQuestList;

    // ====================================================
    //  Night
    // ====================================================
    [Header("Night Choice")]
    [SerializeField] private Button btnNightExplore;
    [SerializeField] private Button btnNightArena;
    [SerializeField] private Button btnNightRest;
    [SerializeField] private TMP_Text textArenaStatus;
    [SerializeField] private TMP_Text textNightWarning;

    [Header("Night Action")]
    [SerializeField] private TMP_Text textNightResult;

    [Header("Battle Result")]
    [SerializeField] private GameObject panelBattleResult;
    [SerializeField] private TMP_Text textBattleResult;
    [SerializeField] private Button btnCloseBattleResult;

    // ====================================================
    //  Day Summary
    // ====================================================
    [Header("Day Summary")]
    [SerializeField] private TMP_Text textSummary;

    // ====================================================
    //  Calendar
    // ====================================================
    [Header("Calendar")]
    [SerializeField] private GameObject panelCalendar;
    [SerializeField] private TMP_Text textCalendarTitle;
    [SerializeField] private TMP_Text textCalendarContent;
    [SerializeField] private Button btnCalendarPrev;
    [SerializeField] private Button btnCalendarNext;
    [SerializeField] private Button btnCalendarClose;

    // ====================================================
    //  Fighter Slot Popup (1단계: 행동 종류)
    // ====================================================
    [Header("Fighter Slot Popup")]
    [SerializeField] private GameObject popupSlotDropdown;
    [SerializeField] private Button btnSlotTraining;
    [SerializeField] private Button btnSlotPartTime;
    [SerializeField] private Button btnSlotRest;
    [SerializeField] private Button btnSlotClosePopup;

    // ====================================================
    //  Training Stat Popup (2단계: 훈련 스탯 선택)
    // ====================================================
    [Header("Training Stat Popup")]
    [SerializeField] private GameObject popupTrainingStat;
    [SerializeField] private Button btnStatStrength;
    [SerializeField] private Button btnStatAgility;
    [SerializeField] private Button btnStatDexterity;
    [SerializeField] private Button btnStatEndurance;
    [SerializeField] private Button btnStatCancel;

    // ====================================================
    //  Level Up
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
    [SerializeField] private Color colorRest = new Color(0.55f, 0.55f, 0.55f);

    // ====================================================
    //  Internal
    // ====================================================
    readonly List<ScheduleSlotView> slotViews = new();
    GameManager gm;
    int popupTargetIndex = -1;
    int calendarViewYear = 1;
    int calendarViewMonth = 1;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();

        BuildScheduleGrid();
        SetupFighterSlotPopup();
        SetupTrainingStatPopup();
        SetupMapButtons();
        SetupPlaceActionButtons();
        SetupNightButtons();
        SetupCalendarButtons();
        SetupBattleResultButton();
        SetupLevelUpNotice();

        CloseSlotPopup();
        CloseTrainingStatPopup();
        CloseLevelUpNotice();
        HideCalendar();
        CloseBattleResult();
    }

    // ====================================================
    //  이벤트 구독 / 해제
    // ====================================================
    void OnEnable()
    {
        GameEvents.OnPhaseChanged       += HandlePhaseChanged;
        GameEvents.OnRefreshRequested   += HandleRefreshRequested;
        GameEvents.OnActionResult       += HandleActionResult;
        GameEvents.OnFighterSlotResult  += HandleFighterSlotResult;
        GameEvents.OnBattleResult       += HandleBattleResult;
        GameEvents.OnProficiencyLevelUp += HandleProficiencyLevelUp;
        GameEvents.OnArenaClosedWarning += HandleArenaClosedWarning;
        GameEvents.OnStressWarning      += HandleStressWarning;
        GameEvents.OnShowCalendar       += HandleShowCalendar;
        GameEvents.OnHideCalendar       += HandleHideCalendar;
    }

    void OnDisable()
    {
        GameEvents.OnPhaseChanged       -= HandlePhaseChanged;
        GameEvents.OnRefreshRequested   -= HandleRefreshRequested;
        GameEvents.OnActionResult       -= HandleActionResult;
        GameEvents.OnFighterSlotResult  -= HandleFighterSlotResult;
        GameEvents.OnBattleResult       -= HandleBattleResult;
        GameEvents.OnProficiencyLevelUp -= HandleProficiencyLevelUp;
        GameEvents.OnArenaClosedWarning -= HandleArenaClosedWarning;
        GameEvents.OnStressWarning      -= HandleStressWarning;
        GameEvents.OnShowCalendar       -= HandleShowCalendar;
        GameEvents.OnHideCalendar       -= HandleHideCalendar;
    }

    // ====================================================
    //  이벤트 핸들러
    // ====================================================
    void HandlePhaseChanged(GamePhase phase) => ShowPhase(phase);
    void HandleRefreshRequested(GameState state, GamePhase phase) => RefreshAll(state, phase);
    void HandleActionResult(string msg) => ShowActionResult(msg);
    void HandleFighterSlotResult(string msg) => ShowFighterSlotResult(msg);
    void HandleBattleResult(ArenaBattleResult result) => ShowBattleResult(result);
    void HandleProficiencyLevelUp(ProficiencyType type, int level) => ShowLevelUpNotice(type, level);
    void HandleArenaClosedWarning() => ShowArenaClosedWarning();
    void HandleStressWarning() => ShowStressWarning();
    void HandleShowCalendar(GameState state) => ShowCalendar(state);
    void HandleHideCalendar() => HideCalendar();

    // ====================================================
    //  Setup
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
            v.SetTimeLabel($"블록 {i + 1}");
            slotViews.Add(v);
        }
    }

    void SetupFighterSlotPopup()
    {
        if (btnSlotTraining) btnSlotTraining.onClick.AddListener(() => OnSlotTypeChosen(FighterSlotType.Training));
        if (btnSlotPartTime) btnSlotPartTime.onClick.AddListener(() => OnSlotTypeChosen(FighterSlotType.PartTime));
        if (btnSlotRest) btnSlotRest.onClick.AddListener(() => OnSlotTypeChosen(FighterSlotType.Rest));
        if (btnSlotClosePopup) btnSlotClosePopup.onClick.AddListener(CloseSlotPopup);
    }

    void SetupTrainingStatPopup()
    {
        if (btnStatStrength) btnStatStrength.onClick.AddListener(() => OnTrainingStatChosen(TrainingStat.Strength));
        if (btnStatAgility) btnStatAgility.onClick.AddListener(() => OnTrainingStatChosen(TrainingStat.Agility));
        if (btnStatDexterity) btnStatDexterity.onClick.AddListener(() => OnTrainingStatChosen(TrainingStat.Dexterity));
        if (btnStatEndurance) btnStatEndurance.onClick.AddListener(() => OnTrainingStatChosen(TrainingStat.Endurance));
        if (btnStatCancel) btnStatCancel.onClick.AddListener(CloseTrainingStatPopup);
    }

    void SetupMapButtons()
    {
        if (btnMapHome) btnMapHome.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Home));
        if (btnMapShop) btnMapShop.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Shop));
        if (btnMapTrainingGround) btnMapTrainingGround.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.TrainingGround));
        if (btnMapCafe) btnMapCafe.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.Cafe));
        if (btnMapQuestBoard) btnMapQuestBoard.onClick.AddListener(() => gm.OnClickMapLocation((int)MapLocation.QuestBoard));
    }

    void SetupPlaceActionButtons()
    {
        if (btnActionTalk) btnActionTalk.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.Talk));
        if (btnActionAcceptQuest) btnActionAcceptQuest.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.AcceptQuest));
        if (btnActionDeliverQuest) btnActionDeliverQuest.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.DeliverQuest));
        if (btnActionBuyItem) btnActionBuyItem.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.BuyItem));
        if (btnActionSellItem) btnActionSellItem.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.SellItem));
        if (btnActionRest) btnActionRest.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.Rest));
        if (btnActionUpgrade) btnActionUpgrade.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.UpgradeFacility));
        if (btnActionSupport) btnActionSupport.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.SupportTraining));
        if (btnActionFood) btnActionFood.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.BuyFood));
        if (btnActionReroll) btnActionReroll.onClick.AddListener(() => gm.OnClickPlaceAction((int)PlaceActionType.RerollQuests));
        if (btnBackToMap) btnBackToMap.onClick.AddListener(() => gm.OnClickBackToMap());
    }

    void SetupNightButtons()
    {
        if (btnNightExplore) btnNightExplore.onClick.AddListener(() => gm.OnClickNightChoice((int)NightActionType.Exploration));
        if (btnNightArena) btnNightArena.onClick.AddListener(() => gm.OnClickNightChoice((int)NightActionType.Arena));
        if (btnNightRest) btnNightRest.onClick.AddListener(() => gm.OnClickNightChoice((int)NightActionType.Rest));
    }

    void SetupCalendarButtons()
    {
        if (btnOpenCalendar) btnOpenCalendar.onClick.AddListener(() => gm.OnClickOpenCalendar());
        if (btnCalendarClose) btnCalendarClose.onClick.AddListener(() => gm.OnClickCloseCalendar());
        if (btnCalendarPrev) btnCalendarPrev.onClick.AddListener(CalendarPrevMonth);
        if (btnCalendarNext) btnCalendarNext.onClick.AddListener(CalendarNextMonth);
    }

    void SetupBattleResultButton()
    {
        if (btnCloseBattleResult) btnCloseBattleResult.onClick.AddListener(CloseBattleResult);
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
        if (panelDayMap) panelDayMap.SetActive(phase == GamePhase.DayMap);
        if (panelDayPlaceAction) panelDayPlaceAction.SetActive(phase == GamePhase.DayPlaceAction);
        if (panelNightChoice) panelNightChoice.SetActive(phase == GamePhase.NightChoice);
        if (panelNightAction) panelNightAction.SetActive(
            phase == GamePhase.NightAction ||
            phase == GamePhase.BattlePreparation);
        if (panelDaySummary) panelDaySummary.SetActive(phase == GamePhase.DaySummary);
        if (panelBattle) panelBattle.SetActive(phase == GamePhase.Battle);

        if (phase != GamePhase.ScheduleSetting)
        {
            CloseSlotPopup();
            CloseTrainingStatPopup();
        }
    }

    // ====================================================
    //  Refresh
    // ====================================================

    public void RefreshAll(GameState state, GamePhase phase)
    {
        if (globalHUD) globalHUD.Refresh(state, phase);
        RefreshScheduleGrid(state);

        if (phase == GamePhase.DayMap) RefreshDayMap(state);
        if (phase == GamePhase.DayPlaceAction) RefreshPlaceAction(state);
        if (phase == GamePhase.NightChoice) RefreshNightChoice(state);
        if (phase == GamePhase.NightAction || phase == GamePhase.BattlePreparation) RefreshNightAction(state);
        if (phase == GamePhase.DaySummary) RefreshDaySummary(state);
    }

    void RefreshTopBar(GameState state, GamePhase phase)
    {
        // Replaced by GlobalHUDController
    }

    void RefreshScheduleGrid(GameState state)
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            var slot = state.fighterSchedule[i];
            string label;
            Color c;

            if (slot.type == FighterSlotType.Training)
            {
                string statName = GameManager.GetStatName(slot.trainingStat);
                label = $"훈련({statName})";
                c = colorTraining;
            }
            else if (slot.type == FighterSlotType.PartTime)
            {
                label = "알바";
                c = colorPartTime;
            }
            else
            {
                label = "휴식";
                c = colorRest;
            }

            slotViews[i].SetDirect(label, c);
            slotViews[i].SetProgressVisual(state.fighterSlotProgress);
        }
    }

    void RefreshDayMap(GameState state)
    {
        bool hasActions = !state.IsDayOver;
        if (btnMapHome) btnMapHome.interactable = hasActions;
        if (btnMapShop) btnMapShop.interactable = hasActions;
        if (btnMapTrainingGround) btnMapTrainingGround.interactable = hasActions;
        if (btnMapCafe) btnMapCafe.interactable = hasActions;
        if (btnMapQuestBoard) btnMapQuestBoard.interactable = hasActions;

        RefreshQuestList(state);
    }

    void RefreshPlaceAction(GameState state)
    {
        if (placeActionUI) placeActionUI.Refresh(state);
        RefreshQuestList(state);
    }

    void RefreshQuestList(GameState state)
    {
        if (textQuestList == null) return;

        string text = "[진행 중 의뢰]\n";
        if (state.quests.activeQuests.Count == 0)
            text += "  없음\n";
        else
            foreach (var q in state.quests.activeQuests)
                text += $"  {q.title}: {q.description} ({q.goldReward}G)\n";

        if (state.playerLocation == MapLocation.QuestBoard)
        {
            text += "\n[수령 가능 의뢰]\n";
            if (state.quests.availableQuests.Count == 0)
                text += "  없음\n";
            else
                foreach (var q in state.quests.availableQuests)
                    text += $"  {q.title}: {q.description} ({q.goldReward}G)\n";
        }

        textQuestList.text = text;
    }

    void RefreshNightChoice(GameState state)
    {
        if (textArenaStatus)
        {
            if (state.IsPromotionDay) textArenaStatus.text = "[승급전] (참가 가능)";
            else if (state.IsArenaOpen) textArenaStatus.text = "아레나: 오픈";
            else textArenaStatus.text = $"아레나: 휴무 (다음: Day {CalendarSystem.NextArenaDay(state.day)})";
        }

        bool locked = state.stress >= 80;
        if (btnNightExplore) btnNightExplore.interactable = !locked;
        if (btnNightArena) btnNightArena.interactable = state.IsArenaOpen && !locked;

        if (textNightWarning)
        {
            if (locked) textNightWarning.text = "스트레스 과다! 휴식만 가능합니다.";
            else if (state.stress >= 60) textNightWarning.text = "스트레스가 높습니다.";
            else if (state.IsPromotionDay) textNightWarning.text = "오늘은 승급전입니다!";
            else textNightWarning.text = "";
        }
    }

    void RefreshNightAction(GameState state)
    {
        if (textNightResult == null) return;
        textNightResult.text = $"밤 행동: {state.nightChoice switch { NightActionType.Exploration => "탐사", NightActionType.Arena => "아레나", _ => "휴식" }} 완료!";
    }

    void RefreshDaySummary(GameState state)
    {
        if (textSummary == null) return;

        var ar = state.arena;

        // 스탯
        string statText = "";
        foreach (TrainingStat s in System.Enum.GetValues(typeof(TrainingStat)))
        {
            if (statText.Length > 0) statText += "  ";
            statText += $"{GameManager.GetStatName(s)}: {state.GetStat(s)}";
        }

        // 숙련도
        string profText = "";
        foreach (ProficiencyType p in System.Enum.GetValues(typeof(ProficiencyType)))
        {
            if (profText.Length > 0) profText += "  ";
            profText += $"{GameManager.GetProfName(p)} Lv.{state.GetProf(p).level}";
        }

        // 엔딩 변수
        string endText = "";
        foreach (EndingVar v in System.Enum.GetValues(typeof(EndingVar)))
        {
            if (endText.Length > 0) endText += "  ";
            endText += $"{state.endingVars.GetLabel(v)}: {state.endingVars.Get(v)}";
        }

        // 전투 리포트
        string battleText = "";
        if (!string.IsNullOrEmpty(state.lastBattleReport))
        {
            battleText = $"\n\n[전투 리포트]\n{state.lastBattleReport}";
        }

        textSummary.text =
            $"===== {state.DateString} 결산 =====\n\n" +
            $"[전투 스탯]\n  {statText}\n\n" +
            $"[아레나] 등급: {ar.GetRankName()}  전적: {ar.wins}승 {ar.losses}패\n\n" +
            $"[숙련도] {profText}\n\n" +
            $"[엔딩 변수]\n  {endText}\n\n" +
            $"[오늘] 훈련: {state.todayTrainingCount}회  획득: {state.todayGoldEarned}G\n" +
            $"총 Gold: {state.gold}  스트레스: {state.stress}  피로: {state.fatigue}" +
            battleText;
    }

    // ====================================================
    //  Fighter Schedule Popup (2단계 선택)
    // ====================================================

    public void OnClickScheduleSlot(int index)
    {
        if (gm != null && gm.Phase != GamePhase.ScheduleSetting) return;
        popupTargetIndex = index;
        if (popupSlotDropdown) popupSlotDropdown.SetActive(true);
    }

    void OnSlotTypeChosen(FighterSlotType type)
    {
        if (popupTargetIndex < 0 || popupTargetIndex >= GameState.DaySlotCount) { CloseSlotPopup(); return; }

        if (type == FighterSlotType.Training)
        {
            gm.State.fighterSchedule[popupTargetIndex].type = FighterSlotType.Training;
            CloseSlotPopup();
            OpenTrainingStatPopup();
        }
        else
        {
            gm.State.fighterSchedule[popupTargetIndex].type = type;
            gm.State.fighterSchedule[popupTargetIndex].trainingStat = TrainingStat.Strength;
            CloseSlotPopup();
            RefreshScheduleGrid(gm.State);
        }
    }

    void OnTrainingStatChosen(TrainingStat stat)
    {
        if (popupTargetIndex < 0 || popupTargetIndex >= GameState.DaySlotCount) { CloseTrainingStatPopup(); return; }

        gm.State.fighterSchedule[popupTargetIndex].trainingStat = stat;
        CloseTrainingStatPopup();
        RefreshScheduleGrid(gm.State);
    }

    void OpenTrainingStatPopup()
    {
        if (popupTrainingStat) popupTrainingStat.SetActive(true);
    }

    void CloseTrainingStatPopup()
    {
        if (popupTrainingStat) popupTrainingStat.SetActive(false);
    }

    void CloseSlotPopup()
    {
        if (popupSlotDropdown) popupSlotDropdown.SetActive(false);
    }

    // ====================================================
    //  Results / Warnings / Popups
    // ====================================================

    public void ShowFighterSlotResult(string msg)
    {
        if (textFighterSlotResult) textFighterSlotResult.text = msg;
    }

    public void ShowActionResult(string msg)
    {
        if (textPlaceActionResult) textPlaceActionResult.text = msg;
    }

    public void ShowLevelUpNotice(ProficiencyType type, int newLevel)
    {
        if (panelLevelUpNotice == null) return;
        if (textLevelUpNotice) textLevelUpNotice.text = $"{GameManager.GetProfName(type)} 숙련도가 Lv.{newLevel}로 상승했습니다!";
        panelLevelUpNotice.SetActive(true);
    }

    void CloseLevelUpNotice() { if (panelLevelUpNotice) panelLevelUpNotice.SetActive(false); }

    public void ShowBattleResult(ArenaBattleResult result)
    {
        if (panelBattleResult == null) return;
        if (textBattleResult)
        {
            string text = result.message + "\n\n" +
                $"보상: {result.goldReward} Gold\n" +
                $"평판: {(result.reputationChange >= 0 ? "+" : "")}{result.reputationChange}";
            if (result.isPromotion && result.won)
                text += $"\n\n승급! {result.oldRank} -> {result.newRank}";
            textBattleResult.text = text;
        }
        panelBattleResult.SetActive(true);
    }

    void CloseBattleResult() { if (panelBattleResult) panelBattleResult.SetActive(false); }
    public void ShowArenaClosedWarning() { if (textNightWarning) textNightWarning.text = "아레나 휴무입니다!"; }
    public void ShowStressWarning() { if (textNightWarning) textNightWarning.text = "스트레스 과다! 휴식만 가능합니다!"; }

    // ====================================================
    //  Calendar
    // ====================================================

    public void ShowCalendar(GameState state)
    {
        if (panelCalendar == null) return;
        calendarViewYear = CalendarSystem.GetYear(state.day);
        calendarViewMonth = CalendarSystem.GetMonth(state.day);
        RefreshCalendar(state);
        panelCalendar.SetActive(true);
    }

    public void HideCalendar() { if (panelCalendar) panelCalendar.SetActive(false); }

    void CalendarPrevMonth()
    {
        calendarViewMonth--;
        if (calendarViewMonth < 1) { calendarViewMonth = 12; calendarViewYear = Mathf.Max(1, calendarViewYear - 1); }
        if (gm != null) RefreshCalendar(gm.State);
    }

    void CalendarNextMonth()
    {
        calendarViewMonth++;
        if (calendarViewMonth > 12) { calendarViewMonth = 1; calendarViewYear = Mathf.Min(3, calendarViewYear + 1); }
        if (gm != null) RefreshCalendar(gm.State);
    }

    void RefreshCalendar(GameState state)
    {
        if (textCalendarTitle) textCalendarTitle.text = $"{calendarViewYear}년 {calendarViewMonth}월";
        if (textCalendarContent == null) return;

        var events = CalendarSystem.GetMonthEvents(calendarViewYear, calendarViewMonth);
        int monthStart = ((calendarViewYear - 1) * 12 + (calendarViewMonth - 1)) * 30 + 1;
        string content = "";

        for (int d = 0; d < CalendarSystem.DaysPerMonth; d++)
        {
            int dayNum = monthStart + d;
            bool isToday = dayNum == state.day;
            string eventText = "";

            foreach (var evt in events)
                if (evt.day == dayNum)
                    eventText += evt.type == CalendarEventType.PromotionMatch ? " [승급전]" : " [아레나]";

            if (!string.IsNullOrEmpty(eventText) || isToday)
                content += $"{d + 1}일{eventText}{(isToday ? " << 오늘" : "")}\n";
        }

        textCalendarContent.text = string.IsNullOrEmpty(content) ? "특별한 일정 없음" : content;
    }
}
