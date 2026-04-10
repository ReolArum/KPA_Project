// ===== GameManager.cs =====
// 리팩토링: UI 직접 참조 제거 → GameEvents 이벤트 시스템 사용
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ====================================================
    //  참조
    // ====================================================
    [Header("Refs")]
    [SerializeField] private BattlePreparationUI battlePrepUI;

    public static GameManager Instance { get; private set; }
    public GameState  State { get; private set; } = new GameState();
    public GamePhase  Phase { get; private set; } = GamePhase.Title;

    // ===== 설정 상수 =====
    // 설정 상수는 이제 TrainingManager에서 관리합니다.

    private void Start()
    {
        // [REFRESH-ONLY] 아키텍처 리팩토링을 위해 최초 1회 세이브 초기화 제안
        // 테스트 완료 후 아래 라인은 삭제 권장
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

        // 일반 초기화
        if (QuestManager.Instance != null) QuestManager.Instance.GenerateDailyQuests(State.day);
        SaveSystem.Load(State);
        SetPhase(GamePhase.Title);
    }

    void SetPhase(GamePhase next)
    {
        Phase = next;
        GameEvents.RaisePhaseChanged(next);
        GameEvents.RaiseRefreshRequested(State, Phase);
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
        State.fighterSlotProgress = 0;
        State.playerActionsUsed   = 0;
        State.playerLocation      = MapLocation.None;
        SetPhase(GamePhase.DayMap);
    }

    // ====================================================
    //  Day Map
    // ====================================================
    public void OnClickMapLocation(int locationIndex)
    {
        if (Phase != GamePhase.DayMap) return;
        
        // [MOD] DayTimeManager를 통한 시간 소모 체크
        if (DayTimeManager.Instance.RemainingSlots <= 0) return;

        MapLocation target = (MapLocation)locationIndex;

        if (target == State.playerLocation)
        {
            SetPhase(GamePhase.DayPlaceAction);
            return;
        }

        State.playerLocation = target;
        DayTimeManager.Instance.ConsumeSlot(1); // [MOD] 슬롯 1개 소모
        
        // 전투체 슬롯 진행 (특정 조건에 따라 자동 진행)
        // ExecuteFighterSlot(); 

        SetPhase(GamePhase.DayPlaceAction);
    }

    public void OnClickBackToMap()
    {
        if (Phase != GamePhase.DayPlaceAction) return;

        // [변경] 지도 복귀 시도 시 행동권이 없으면 즉시 밤으로 전환
        if (DayTimeManager.Instance.RemainingSlots <= 0)
        {
            TransitionToNight();
            return;
        }

        State.playerLocation = MapLocation.None;
        SetPhase(GamePhase.DayMap);
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
                GrowthManager.Instance.ModifyEndingVar(EndingVar.Sync, 2);
                State.fighter.stress = Mathf.Max(0, State.fighter.stress - 1);
                GameEvents.RaiseActionResult("대화 완료. 동기화 +2");
                break;

            case PlaceActionType.AcceptQuest:
                GameEvents.RaiseActionResult("의뢰 게시판에서 수령 가능합니다.");
                break;

            case PlaceActionType.DeliverQuest:
                var completed = QuestManager.Instance.CheckDelivery(State.playerLocation);
                if (completed != null)
                {
                    QuestManager.Instance.CompleteQuest(completed);
                }
                else GameEvents.RaiseActionResult("배달할 의뢰가 없습니다.");
                break;

            case PlaceActionType.BuyItem:
                GameEvents.RaiseActionResult("상점에서 장비를 구매합니다.");
                break;

            case PlaceActionType.SellItem:
                GameEvents.RaiseActionResult("불필요한 장비를 판매합니다.");
                break;

            case PlaceActionType.Rest:
                if (State.playerLocation == MapLocation.Cafe)
                {
                    if (State.gold >= 30)
                    {
                        State.gold -= 30;
                        State.fatigue = Mathf.Max(0, State.fatigue - 15);
                        GameEvents.RaiseActionResult("카페 고급 휴식 (피로 -15, -30G)");
                    }
                    else GameEvents.RaiseActionResult("골드가 부족합니다.");
                }
                else
                {
                    State.fatigue = Mathf.Max(0, State.fatigue - 5);
                    GameEvents.RaiseActionResult("집에서 휴식 (피로 -5)");
                }
                break;

            case PlaceActionType.UpgradeFacility:
                int cost = (State.facilityUpgradeLevel + 1) * 100;
                if (State.gold >= cost)
                {
                    State.gold -= cost;
                    State.facilityUpgradeLevel++;
                    GameEvents.RaiseActionResult($"훈련 시설 업그레이드 완료! (Lv.{State.facilityUpgradeLevel})");
                }
                else GameEvents.RaiseActionResult($"골드가 부족합니다. (필요: {cost}G)");
                break;

            case PlaceActionType.SupportTraining:
                var currSlot = State.fighter.schedule[State.fighter.slotProgress];
                if (currSlot.type == FighterSlotType.Training)
                {
                    State.fighter.stress += 2;
                    State.AddStat(currSlot.trainingStat, 2);
                    GameEvents.RaiseActionResult($"훈련 보조 수행! ({GetCurrentStatName(currSlot.trainingStat)} +2)");
                }
                else GameEvents.RaiseActionResult("지금은 훈련 중이 아닙니다.");
                break;

            case PlaceActionType.BuyFood:
                if (State.gold >= 50)
                {
                    State.gold -= 50;
                    State.trainingEfficiency = 1.5f;
                    State.fatigue = Mathf.Max(0, State.fatigue - 5);
                    GameEvents.RaiseActionResult("특수 음식 섭취! (훈련 효율 증가, 피로 -5)");
                }
                else GameEvents.RaiseActionResult("골드가 부족합니다.");
                break;

            case PlaceActionType.RerollQuests:
                if (QuestManager.Instance != null && !QuestManager.Instance.IsRerollUsed)
                {
                    QuestManager.Instance.SetRerollUsed(true);
                    State.quests.GenerateDailyQuests(State.day);
                    GameEvents.RaiseActionResult("의뢰 게시판 리롤 완료!");
                }
                else GameEvents.RaiseActionResult("오늘은 더 이상 리롤할 수 없습니다.");
                break;
        }
    }

    // ====================================================
    //  Schedule Actions
    // ====================================================
    public void SetFighterSchedule(int index, FighterSlotType type, TrainingStat stat)
    {
        if (index < 0 || index >= State.fighter.schedule.Length) return;
        State.fighter.schedule[index].type = type;
        State.fighter.schedule[index].trainingStat = stat;
        MainGameUIController.Instance.RefreshUI(State);
    }

    public void CopyYesterdaySchedule()
    {
        for (int i = 0; i < State.fighter.schedule.Length; i++)
        {
            State.fighter.schedule[i].type = State.fighter.yesterdaySchedule[i].type;
            State.fighter.schedule[i].trainingStat = State.fighter.yesterdaySchedule[i].trainingStat;
        }
        MainGameUIController.Instance.RefreshUI(State);
    }

    public void ResetFighterSchedule()
    {
        for (int i = 0; i < State.fighter.schedule.Length; i++)
        {
            State.fighter.schedule[i].type = FighterSlotType.Rest;
            State.fighter.schedule[i].trainingStat = TrainingStat.Strength; // None 대신 Strength로 기본 설정
        }
        MainGameUIController.Instance.RefreshUI(State);
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
    void ExecuteFighterSlot()
    {
        if (State.fighter.slotProgress >= GameState.DaySlotCount) return;

        FighterSlot slot = State.fighter.schedule[State.fighter.slotProgress];
        
        if (TrainingManager.Instance != null)
        {
            TrainingManager.Instance.ExecuteSlot(slot);
            State.fighter.slotProgress++;
        }
    }

    // ====================================================
    //  Night
    // ====================================================
    void TransitionToNight()
    {
        // [MOD] 남은 스케줄 모두 실행 전 조기 종료 보너스 등이 적용된 결과 처리
        FighterScheduleManager.Instance.ProcessResults();

        State.nightCompleted = false;
        SetPhase(GamePhase.NightAction);
    }

    public void OnClickNightChoice(int choiceIndex)
    {
        if (Phase != GamePhase.NightChoice) return;

        NightActionType choice = (NightActionType)choiceIndex;

        if (choice == NightActionType.Arena && !State.IsArenaOpen)
        { GameEvents.RaiseArenaClosedWarning(); return; }

        if (choice != NightActionType.Rest && State.stress >= 80)
        { GameEvents.RaiseStressWarning(); return; }

        State.nightChoice = choice;
        SetPhase(GamePhase.NightAction);
        ExecuteNightAction(choice);
    }

    void ExecuteNightAction(NightActionType action)
    {
        switch (action)
        {
            case NightActionType.Exploration:
                State.stress  += 5;
                State.fatigue += 3;
                State.endingVars.Modify(EndingVar.Reputation, 1);
                // 숙련도 제외 요청에 따라 Exploration 숙련도 추가 삭제 (필요 시)
                
                // 탐사 씬으로 전환
                SceneManager.LoadScene("ExplorationScene");
                return;

            case NightActionType.Arena:
                State.stress  += 3;
                State.fatigue += 5;
                StartArenaBattle();
                return;

            case NightActionType.Rest:
                State.stress  = Mathf.Max(0, State.stress  - 5);
                State.fatigue = Mathf.Max(0, State.fatigue - 5);
                State.nightCompleted = true;
                SetPhase(GamePhase.DaySummary);
                break;
        }
    }

    // ====================================================
    //  아레나 전투
    // ====================================================
    void StartArenaBattle()
    {
        Phase = GamePhase.BattlePreparation;
        GameEvents.RaisePhaseChanged(GamePhase.BattlePreparation);

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
                    State.stress  = Mathf.Max(0, State.stress  - 3);
                    State.fatigue = Mathf.Max(0, State.fatigue - 5);
                    State.nightCompleted = false;
                    SetPhase(GamePhase.NightChoice);
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

    // 전투 씬에서 복귀한 후 결과 처리
    void OnBattleFinished(BattleReport report)
    {
        State.lastBattleReport = report.ToReportString();

        // [MOD] 아레나 판정 로직을 ArenaManager로 이관
        var arenaResult = ArenaManager.Instance.ProcessMatchResult(
            report.playerWon, 
            State.IsPromotionDay ? 80 : 20, // 기본 보상
            State.IsPromotionDay ? 10 : 3    // 기본 명성
        );

        GameEvents.RaiseBattleResult(arenaResult);
        State.nightCompleted = true;
        SetPhase(GamePhase.DaySummary);
    }

    // ====================================================
    //  Day Summary
    // ====================================================
    public void OnClickNextDay()
    {
        if (Phase != GamePhase.DaySummary) return;
        State.ResetForNewDay();
        if (QuestManager.Instance != null) QuestManager.Instance.GenerateDailyQuests(State.day);
        SaveSystem.Save(State);
        SetPhase(GamePhase.ScheduleSetting);
    }

    // ====================================================
    //  Quest
    // ====================================================
    public void OnClickAcceptQuest(int questId)
    {
        if (State.quests.AcceptQuest(questId))
            GameEvents.RaiseActionResult("의뢰 수령 완료!");
        else
            GameEvents.RaiseActionResult("의뢰를 수령할 수 없습니다.");

        GameEvents.RaiseRefreshRequested(State, Phase);
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
        GamePhase.NightChoice or GamePhase.NightAction                 => "20:00",
        GamePhase.DaySummary                                           => "22:00",
        _                                                              => $"{8 + state.playerActionsUsed * 3:00}:00"
    };

    public string GetCurrentStatName(TrainingStat stat) => GetStatName(stat);

    public static string GetStatName(TrainingStat stat) => stat switch
    {
        TrainingStat.Strength  => "힘",
        TrainingStat.Agility   => "민첩",
        TrainingStat.Dexterity => "재주",
        TrainingStat.Endurance => "지구력",
        _                      => stat.ToString()
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
        State.gold += 10;
        GameEvents.RaiseRefreshRequested(State, Phase);
    }

    public void DebugReduceStress()
    {
        State.stress = Mathf.Max(0, State.stress - 20);
        GameEvents.RaiseRefreshRequested(State, Phase);
    }

    public void DebugForceDaySummary()
    {
        State.playerActionsUsed = GameState.MaxPlayerActions;
        State.nightCompleted    = true;
        SetPhase(GamePhase.DaySummary);
    }

    public void DebugClearSave() => SaveSystem.Clear();

    public void DebugAddProfExp()
    {
        foreach (ProficiencyType p in System.Enum.GetValues(typeof(ProficiencyType)))
            State.GetProf(p).AddExp(10);
        GameEvents.RaiseRefreshRequested(State, Phase);
    }

    public void DebugSkipToPromotion()
    {
        State.day = CalendarSystem.NextPromotionDay(State.day) - 1;
        GameEvents.RaiseRefreshRequested(State, Phase);
    }

    public void DebugForceBattle() => StartArenaBattle();
}
