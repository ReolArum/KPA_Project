// ===== GameManager.cs =====
// 리팩토링: UI 직접 참조 제거 → GameEvents 이벤트 시스템 사용
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // ====================================================
    //  참조
    // ====================================================
    [Header("Refs")]
    [SerializeField] private BattlePreparationUI battlePrepUI;
    [SerializeField] private List<LocationThemeData> locationThemes; // [ADD] 장소 데이터 마스터 리스트
    private Dictionary<MapLocation, LocationThemeData> themeDict = new();

    public static GameManager Instance { get; private set; }
    public GameState  State { get; private set; } = new GameState();
    public GamePhase  Phase { get; private set; } = GamePhase.Title;

    // ===== 설정 상수 =====
    // 설정 상수는 이제 TrainingManager에서 관리합니다.

    private void Start()
    {
        // [REFRESH-ONLY] 아키텍처 리팩토링을 위해 최초 1회 세이브 초기화 제안
        // SaveSystem.Clear(); 
        RefreshUI();
    }

    public void ToggleCharacterUI()
    {
        if (CharacterUnifiedUI.Instance != null)
            CharacterUnifiedUI.Instance.Toggle();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 전투 씬에서 복귀한 경우
        if (BattleSceneData.battleCompleted)
        {
            State = BattleSceneData.gameState;
            var report = BattleSceneData.battleReport;
            BattleSceneData.Clear();

            SetPhase(GamePhase.NightAction);
            OnBattleFinished(report);
            return;
        }

        // 탐사 씬에서 복귀한 경우 [NEW]
        if (ExplorationSceneData.explorationCompleted)
        {
            State = ExplorationSceneData.gameState;
            ExplorationSceneData.Clear();

            SetPhase(GamePhase.NightAction);
            OnExplorationFinished();
            return;
        }

        // 데이터 사전 구축
        themeDict.Clear();
        if (locationThemes != null)
        {
            foreach (var t in locationThemes) themeDict[t.location] = t;
        }

        // 일반 초기화
        SaveSystem.Load(State);
        if (QuestManager.Instance != null) QuestManager.Instance.GenerateDailyQuests(State.player.day);
        SetPhase(GamePhase.Title);
    }

    void SetPhase(GamePhase next)
    {
        Phase = next;
        GameEvents.RaisePhaseChanged(next);
        GameEvents.RaiseRefreshRequested(State, Phase);
        RefreshUI();
    }

    void RefreshUI()
    {
        if (MainGameUIController.Instance != null)
            MainGameUIController.Instance.RefreshUI(State);
    }

    // ====================================================
    //  Title
    // ====================================================
    public void OnClickStart() => SetPhase(GamePhase.MorningSchedule);

    // ====================================================
    //  Schedule Setting
    // ====================================================
    public void OnClickStartDay()
    {
        State.fighter.slotProgress = 0;
        State.player.actionsUsed   = 0;
        State.player.location      = MapLocation.None;
        SetPhase(GamePhase.DayMap);
    }

    // ====================================================
    //  Day Map
    // ====================================================
    public void OnClickMapLocation(int locationIndex)
    {
        if (Phase != GamePhase.DayMap) return;
        
        // [MOD] GameState의 행동 슬롯 소모 체크
        if (State.RemainingActions <= 0) return;

        MapLocation target = (MapLocation)locationIndex;

        if (target == State.player.location)
        {
            SetPhase(GamePhase.DayPlaceAction);
            return;
        }

        // 1. 시간 소모 (이동 및 전투체 스케줄 실행 통합)
        if (ConsumeTime(1))
        {
            State.player.location = target;
            
            // [MOD] 장소 진입 시 VN 시스템 트리거 (entryNode 우선)
            if (themeDict.TryGetValue(target, out var theme) && theme.entryNode != null)
            {
                DialogueManager.Instance.StartDialogue(theme.entryNode);
            }
            else
            {
                // 데이터가 없는 경우 기존 페이즈 전환 (폴백)
                SetPhase(GamePhase.DayPlaceAction);
            }
        }
    }

    /// <summary>
    /// 플레이어의 시간을 소모하고 동시에 전투체의 스케줄을 하나 처리합니다. (실시간 동기화 트랜잭션)
    /// </summary>
    /// <returns>소모 성공 여부</returns>
    public bool ConsumeTime(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (State.RemainingActions <= 0) return false;
            
            // 행동권 소모 시도
            if (State.ConsumeActionSlot(1))
            {
                // 전투체 스케줄 수행
                if (State.fighter.slotProgress < GameState.DaySlotCount)
                {
                    if (TrainingManager.Instance != null)
                    {
                        string log = TrainingManager.Instance.ExecuteSlot(State.fighter.schedule[State.fighter.slotProgress]);
                        State.dailyActivityLogs.Add(log); // 일일 로그 기록
                        State.fighter.slotProgress++;
                    }
                }
            }
            else return false;
        }
        return true;
    }

    public void OnClickBackToMap()
    {
        if (Phase != GamePhase.DayPlaceAction) return;

        // [변경] 지도 복귀 시도 시 행동권이 없으면 즉시 일과를 정산하고 밤으로 전환
        if (State.RemainingActions <= 0)
        {
            FinishDay();
            return;
        }

        State.player.location = MapLocation.None;
        SetPhase(GamePhase.DayMap);
    }

    /// <summary>
    /// 하루 일과를 종료하고 정산한 뒤 밤으로 전환합니다 (자연 종료/조기 종료 공용)
    /// </summary>
    public void FinishDay()
    {
        // 1. [MOD] 아직 수행되지 않은 잔여 스케줄 모두 실행
        int remaining = GameState.DaySlotCount - State.fighter.slotProgress;
        if (remaining > 0)
        {
            ConsumeTime(remaining);
        }

        string fullLog = "오늘의 일과 정산:\n";
        foreach (var log in State.dailyActivityLogs)
        {
            fullLog += $"- {log}\n";
        }

        // 2. 중간 저장 (성장 수치 등 보존)
        SaveSystem.Save(State);

        // 3. 밤으로 전환
        TransitionToNight(fullLog);
    }

    // ====================================================
    //  Place Actions
    // ====================================================
    public void OnClickPlaceAction(int actionIndex)
    {
        if (Phase != GamePhase.DayPlaceAction) return;

        PlaceActionType action = (PlaceActionType)actionIndex;
        ExecutePlaceAction(action);
        SetPhase(GamePhase.DayPlaceAction);
    }

    void ExecutePlaceAction(PlaceActionType action)
    {
        switch (action)
        {
            case PlaceActionType.Talk:
                State.ModifyEndingVar(EndingVar.Sync, 2);
                State.fighter.stress = Mathf.Max(0, State.fighter.stress - 1);
                GameEvents.RaiseActionResult("대화 완료. 동기화 +2");
                break;

            case PlaceActionType.AcceptQuest:
                GameEvents.RaiseActionResult("의뢰 게시판에서 수령 가능합니다.");
                break;

            case PlaceActionType.DeliverQuest:
                if (QuestManager.Instance != null)
                {
                    var completed = QuestManager.Instance.CheckDelivery(State.player.location);
                    if (completed != null)
                    {
                        QuestManager.Instance.CompleteQuest(completed);
                    }
                    else GameEvents.RaiseActionResult("배달할 의뢰가 없습니다.");
                }
                break;

            case PlaceActionType.BuyItem:
                GameEvents.RaiseActionResult("상점에서 장비를 구매합니다.");
                break;

            case PlaceActionType.SellItem:
                GameEvents.RaiseActionResult("불필요한 장비를 판매합니다.");
                break;

            case PlaceActionType.Rest:
                if (State.player.location == MapLocation.Cafe)
                {
                    if (State.player.gold >= 30)
                    {
                        State.AddGold(-30);
                        State.fighter.fatigue = Mathf.Max(0, State.fighter.fatigue - 15);
                        GameEvents.RaiseActionResult("카페 고급 휴식 (피로 -15, -30G)");
                    }
                    else GameEvents.RaiseActionResult("골드가 부족합니다.");
                }
                else
                {
                    State.fighter.fatigue = Mathf.Max(0, State.fighter.fatigue - 5);
                    GameEvents.RaiseActionResult("집에서 휴식 (피로 -5)");
                }
                break;

            case PlaceActionType.UpgradeFacility:
                int cost = (State.facilityUpgradeLevel + 1) * 100;
                if (State.player.gold >= cost)
                {
                    State.AddGold(-cost);
                    State.facilityUpgradeLevel++;
                    GameEvents.RaiseActionResult($"훈련 시설 업그레이드 완료! (Lv.{State.facilityUpgradeLevel})");
                }
                else GameEvents.RaiseActionResult($"골드가 부족합니다. (필요: {cost}G)");
                break;

            case PlaceActionType.SupportTraining:
                // [MOD] 런타임 인덱스 에러 방지 체크
                if (State.fighter.slotProgress < GameState.DaySlotCount)
                {
                    var currSlot = State.fighter.schedule[State.fighter.slotProgress];
                    if (currSlot.type == FighterSlotType.Training)
                    {
                        State.fighter.stress += 2;
                        State.AddStat(currSlot.trainingStat, 2);
                        string supportMsg = $"훈련 보조 수행! ({GetStatName(currSlot.trainingStat)} +2)";
                        GameEvents.RaiseActionResult(supportMsg);
                        State.dailyActivityLogs.Add($"[보조] {supportMsg}");
                    }
                    else GameEvents.RaiseActionResult("지금은 훈련 중이 아닙니다.");
                }
                else GameEvents.RaiseActionResult("오늘의 모든 스케줄이 완료되어 보조할 수 없습니다.");
                break;

            case PlaceActionType.BuyFood:
                if (State.player.gold >= 50)
                {
                    State.AddGold(-50);
                    State.trainingEfficiency = 1.5f;
                    State.fighter.fatigue = Mathf.Max(0, State.fighter.fatigue - 5);
                    GameEvents.RaiseActionResult("특수 음식 섭취! (훈련 효율 증가, 피로 -5)");
                }
                else GameEvents.RaiseActionResult("골드가 부족합니다.");
                break;

            case PlaceActionType.RerollQuests:
                if (QuestManager.Instance != null && !QuestManager.Instance.IsRerollUsed)
                {
                    QuestManager.Instance.SetRerollUsed(true);
                    QuestManager.Instance.GenerateDailyQuests(State.player.day);
                    GameEvents.RaiseActionResult("의뢰 게시판 리롤 완료!");
                }
                else GameEvents.RaiseActionResult("오늘은 더 이상 리롤할 수 없습니다.");
                break;
        }
    }

    /// <summary>
    /// VN 선택지에서 전달된 메인 게임용 액션을 처리합니다.
    /// </summary>
    public void ApplyChoiceAction(DialogueChoiceData choice)
    {
        Debug.Log($"[GameManager] Executing Choice Action: {choice.type}");

        switch (choice.type)
        {
            case ExplorationChoiceType.Shop:
                // TODO: 상점 UI 직접 호출 또는 페이즈 전환
                // 현재는 기존 장소 UI를 통해 기능을 이용하므로 페이즈 전환으로 연결
                SetPhase(GamePhase.DayPlaceAction); 
                break;
            case ExplorationChoiceType.QuestBoard:
                SetPhase(GamePhase.DayPlaceAction);
                // 추가로 의뢰창을 바로 띄우는 로직 연동 가능
                break;
            case ExplorationChoiceType.Talk:
                // NPC와 대화 액션 처리 (LocationThemeData의 talkNode 등 활용)
                if (themeDict.TryGetValue(State.player.location, out var theme) && theme.talkNode != null)
                {
                    DialogueManager.Instance.StartDialogue(theme.talkNode);
                }
                break;
            case ExplorationChoiceType.MapReturn:
                OnClickBackToMap();
                break;
            case ExplorationChoiceType.Interact:
                // 일반 상호작용은 DialogueManager의 기본 효과(ApplyEffectList)로 처리됨
                break;
        }
    }

    // ====================================================
    //  Schedule Actions
    // ====================================================
    public void SetScheduleSlot(int index, FighterSlotType type, TrainingStat stat)
    {
        if (index < 0 || index >= State.fighter.schedule.Length) return;
        State.fighter.schedule[index].type = type;
        State.fighter.schedule[index].trainingStat = stat;
        RefreshUI();
    }

    public void CopyYesterdaySchedule()
    {
        for (int i = 0; i < State.fighter.schedule.Length; i++)
        {
            State.fighter.schedule[i].type = State.fighter.yesterdaySchedule[i].type;
            State.fighter.schedule[i].trainingStat = State.fighter.yesterdaySchedule[i].trainingStat;
        }
        RefreshUI();
    }

    public void ResetFighterSchedule()
    {
        for (int i = 0; i < State.fighter.schedule.Length; i++)
        {
            State.fighter.schedule[i].type = FighterSlotType.Rest;
            State.fighter.schedule[i].trainingStat = TrainingStat.Strength;
        }
        RefreshUI();
    }

    public string GetTotalPredictedOutcome()
    {
        if (TrainingManager.Instance == null) return "매니저를 찾을 수 없습니다.";
        
        return TrainingManager.Instance.GetPredictedOutcome(
            State.fighter.schedule, 
            State.facilityUpgradeLevel, 
            State.trainingEfficiency, 
            State.fighter.fatigue
        );
    }

    // ====================================================
    //  Fighter Schedule
    // ====================================================
    // ====================================================
    //  Night
    // ====================================================
    public void TransitionToNight(string summaryLog = "")
    {
        if (!string.IsNullOrEmpty(summaryLog))
        {
            Debug.Log(summaryLog);
        }

        State.nightCompleted = false;
        SetPhase(GamePhase.NightTransition);
        GameEvents.RaiseRefreshRequested(State, Phase);
    }

    public void OnClickTransitionToNight(int actionIndex)
    {
        NightActionType choice = (NightActionType)actionIndex;
        if (choice != NightActionType.Rest && State.fighter.stress >= 80)
        {
            GameEvents.RaiseActionResult("스트레스가 너무 높아 휴식 외의 행동을 할 수 없습니다!");
            GameEvents.RaiseStressWarning();
            return;
        }

        State.nightChoice = choice;
        SetPhase(GamePhase.NightAction);
        ExecuteNightAction(choice);
    }

    void ExecuteNightAction(NightActionType action)
    {
        switch (action)
        {
            case NightActionType.Exploration:
                State.fighter.stress += 5;
                State.fighter.fatigue += 3;
                State.player.reputation += 1;
                SceneManager.LoadScene("Scene_Exploration");
                return;

            case NightActionType.Arena:
                State.fighter.stress += 3;
                State.fighter.fatigue += 5;
                StartArenaBattle();
                return;

            case NightActionType.Rest:
                State.fighter.stress = Mathf.Max(0, State.fighter.stress - 5);
                State.fighter.fatigue = Mathf.Max(0, State.fighter.fatigue - 5);
                State.nightCompleted = true;
                SetPhase(GamePhase.LateNightReport);
                break;
        }
    }

    // ====================================================
    //  아레나 전투
    // ====================================================
    void StartArenaBattle()
    {
        SetPhase(GamePhase.BattlePreparation);

        if (battlePrepUI != null)
        {
            battlePrepUI.Open(State,
                onStart: () =>
                {
                    battlePrepUI.Close();
                    BeginBattle();
                },
                onCancel: () =>
                {
                    battlePrepUI.Close();
                    State.fighter.stress = Mathf.Max(0, State.fighter.stress - 3);
                    State.fighter.fatigue = Mathf.Max(0, State.fighter.fatigue - 5);
                    State.nightCompleted = false;
                    SetPhase(GamePhase.NightTransition);
                }
            );
        }
        else
        {
            BeginBattle();
        }
    }

    void BeginBattle()
    {
        BattleSceneData.SetupBattle(State);
        SceneManager.LoadScene(BattleSceneData.SceneBattle);
    }

    void OnBattleFinished(BattleReport report)
    {
        State.lastBattleReport = report.ToReportString();

        if (ArenaManager.Instance != null)
        {
            var arenaResult = ArenaManager.Instance.ProcessMatchResult(
                report.playerWon, 
                State.IsPromotionDay ? 80 : 20, 
                State.IsPromotionDay ? 10 : 3
            );
            GameEvents.RaiseBattleResult(arenaResult);
        }

        State.nightCompleted = true;
        SetPhase(GamePhase.LateNightReport);
    }

    void OnExplorationFinished()
    {
        // 탐사 종료 후 추가 처리 (필요 시)
        State.nightCompleted = true;
        SetPhase(GamePhase.LateNightReport);
    }

    // ====================================================
    //  Day Summary
    // ====================================================
    public void OnClickNextDay()
    {
        if (Phase != GamePhase.LateNightReport) return;
        
        State.ResetForNewDay();
        // [MOD] 데이터 클래스에서 이식된 매니저 호출 로직
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetRerollUsed(false);
            QuestManager.Instance.GenerateDailyQuests(State.player.day);
        }
        
        SaveSystem.Save(State);
        SetPhase(GamePhase.MorningSchedule);
    }

    /// <summary>조기 종료 보상 횟수 계산 (DayTimeManager에서 이식)</summary>
    public static int GetDayBonusCount(GameState state)
    {
        return Mathf.FloorToInt(state.RemainingActions / 2.0f);
    }

    // ====================================================
    //  Quest
    // ====================================================
    public void OnClickAcceptQuest(int questId)
    {
        if (QuestManager.Instance != null)
        {
            var quest = State.quests.availableQuests.Find(q => q.id == questId);
            if (quest != null)
            {
                QuestManager.Instance.AcceptQuest(quest);
            }
            else GameEvents.RaiseActionResult("의뢰를 찾을 수 없습니다.");
        }
        RefreshUI();
    }

    // ====================================================
    //  Calendar
    // ====================================================
    public void OnClickOpenCalendar()  => GameEvents.RaiseShowCalendar(State);
    public void OnClickCloseCalendar() => GameEvents.RaiseHideCalendar();

    // ====================================================
    //  유틸리티
    // ====================================================
    public static string GetCurrentTimeLabel(GameState state, GamePhase phase) => phase switch
    {
        GamePhase.BattlePreparation                                    => "20:30",
        GamePhase.Battle                                               => "21:00",
        GamePhase.NightTransition or GamePhase.NightAction             => "20:00",
        GamePhase.LateNightReport                                      => "22:00",
        _                                                              => $"{8 + state.player.actionsUsed * 3:00}:00"
    };

    public string GetCurrentStatName(TrainingStat stat) => GetStatName(stat);

    public static string GetStatName(TrainingStat stat) => stat switch
    {
        TrainingStat.Strength => "힘",
        TrainingStat.Agility => "민첩",
        TrainingStat.Intelligence => "지능",
        TrainingStat.Vitality => "내구",
        TrainingStat.Guts => "근성",
        TrainingStat.Sensitivity => "감각",
        _ => stat.ToString()
    };

    public static string GetProfName(ProficiencyType type) => type switch
    {
        ProficiencyType.Training     => "훈련",
        ProficiencyType.Investigation => "조사",
        ProficiencyType.Exploration  => "탐사",
        ProficiencyType.PartTime     => "알바",
        _                            => type.ToString()
    };

    // ====================================================
    //  Debug
    // ====================================================
    public void DebugAddGold10()
    {
        State.AddGold(10);
    }

    public void DebugReduceStress()
    {
        State.fighter.stress = Mathf.Max(0, State.fighter.stress - 20);
        RefreshUI();
    }

    public void DebugForceDaySummary()
    {
        State.player.actionsUsed = GameState.MaxPlayerActions;
        State.nightCompleted    = true;
        SetPhase(GamePhase.LateNightReport);
    }

    public void DebugClearSave() => SaveSystem.Clear();

    public void DebugAddProfExp()
    {
        foreach (ProficiencyType p in System.Enum.GetValues(typeof(ProficiencyType)))
            State.GetProf(p).AddExp(10);
        RefreshUI();
    }

    public void DebugSkipToPromotion()
    {
        State.player.day = CalendarSystem.NextPromotionDay(State.player.day) - 1;
        RefreshUI();
    }

    public void DebugForceBattle() => StartArenaBattle();
}
