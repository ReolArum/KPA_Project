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
    private const int BaseTrainAmount = 10;
    private const int TrainFatigue = 15;
    private const int TrainStress = 10;
    private const int RestFatigueRecovery = 20;
    private const int RestStressRecovery = 15;

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
        for (int i = 0; i < GameState.DaySlotCount; i++)
        {
            State.fighterSchedule[i].type         = FighterSlotType.Rest;
            State.fighterSchedule[i].trainingStat = TrainingStat.Strength;
        }

        State.quests.GenerateDailyQuests(State.day);
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
    public void OnClickStart() => SetPhase(GamePhase.ScheduleSetting);

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
        if (State.IsDayOver) return;

        MapLocation target = (MapLocation)locationIndex;

        if (target == State.playerLocation)
        {
            // 행동권이 남아있을 때만 같은 장소 행동 패널로 진입
            if (State.IsDayOver) return;
            SetPhase(GamePhase.DayPlaceAction);
            return;
        }

        State.playerLocation = target;
        State.playerActionsUsed++;
        ExecuteFighterSlot();

        // [변경] 행동권 소진 시에도 일단 장소 UI로 진입하여 마지막 행동을 보장함
        SetPhase(GamePhase.DayPlaceAction);
    }

    public void OnClickBackToMap()
    {
        if (Phase != GamePhase.DayPlaceAction) return;

        // [변경] 지도 복귀 시도 시 행동권이 없으면 즉시 밤으로 전환
        if (State.IsDayOver)
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
                State.endingVars.Modify(EndingVar.Sync, 2);
                State.stress = Mathf.Max(0, State.stress - 1);
                GameEvents.RaiseActionResult("대화 완료. 동기화 +2");
                break;

            case PlaceActionType.AcceptQuest:
                GameEvents.RaiseActionResult("의뢰 게시판에서 수령 가능합니다.");
                break;

            case PlaceActionType.DeliverQuest:
                var completed = State.quests.CheckDelivery(State.playerLocation);
                if (completed != null)
                {
                    State.gold += completed.goldReward;
                    State.todayGoldEarned += completed.goldReward;
                    State.quests.CompleteQuest(completed);
                    GameEvents.RaiseActionResult($"의뢰 보상 수령! (+{completed.goldReward}G)");
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
                var currSlot = State.fighterSchedule[State.fighterSlotProgress];
                if (currSlot.type == FighterSlotType.Training)
                {
                    State.stress += 2;
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
                if (!State.dailyRerollUsed)
                {
                    State.dailyRerollUsed = true;
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
    public void SetScheduleSlot(int index, FighterSlotType type, TrainingStat stat)
    {
        if (index < 0 || index >= State.fighterSchedule.Length) return;
        State.fighterSchedule[index].type = type;
        State.fighterSchedule[index].trainingStat = stat;
        GameEvents.RaiseGameStateChanged(State);
    }

    public void OnClickApplyYesterdaySchedule()
    {
        GameEvents.RaiseActionResult("어제와 동일한 스케줄을 적용했습니다.");
        GameEvents.RaiseGameStateChanged(State);
    }

    public void OnClickResetSchedule()
    {
        for (int i = 0; i < State.fighterSchedule.Length; i++)
        {
            State.fighterSchedule[i].type = FighterSlotType.Rest;
            State.fighterSchedule[i].trainingStat = TrainingStat.None;
        }
        GameEvents.RaiseActionResult("스케줄을 초기화했습니다.");
        GameEvents.RaiseGameStateChanged(State);
    }

    public string GetTotalPredictedOutcome()
    {
        int tStr = 0, tAgi = 0, tDex = 0, tEnd = 0, tFat = 0, tStrss = 0;
        foreach (var s in State.fighterSchedule)
        {
            if (s.type == FighterSlotType.Training)
            {
                int val = Mathf.RoundToInt((BaseTrainAmount + State.facilityUpgradeLevel) * State.trainingEfficiency);
                if (s.trainingStat == TrainingStat.Strength) tStr += val;
                else if (s.trainingStat == TrainingStat.Agility) tAgi += val;
                else if (s.trainingStat == TrainingStat.Dexterity) tDex += val;
                else if (s.trainingStat == TrainingStat.Endurance) tEnd += val;
                tFat += TrainFatigue; tStrss += TrainStress;
            }
            else if (s.type == FighterSlotType.Rest)
            {
                tFat -= RestFatigueRecovery; tStrss -= RestStressRecovery;
            }
        }
        tFat = Mathf.Max(-State.fatigue, tFat);
        return $"[일간 예상 수치]\n힘: +{tStr}, 민: +{tAgi}, 기: +{tDex}, 체: +{tEnd}\n피로: {(tFat >= 0 ? "+" : "")}{tFat}, 스트레스: {(tStrss >= 0 ? "+" : "")}{tStrss}";
    }

    // ====================================================
    //  Fighter Schedule
    // ====================================================
    void ExecuteFighterSlot()
    {
        if (State.fighterSlotProgress >= GameState.DaySlotCount) return;

        FighterSlot slot      = State.fighterSchedule[State.fighterSlotProgress];
        var         profTrain = State.GetProf(ProficiencyType.Training);
        var         profPart  = State.GetProf(ProficiencyType.PartTime);

        switch (slot.type)
        {
            case FighterSlotType.Training:
                int amount = BaseTrainAmount + State.facilityUpgradeLevel; // 시설 레벨당 +1
                amount = Mathf.RoundToInt(amount * State.trainingEfficiency); // 음식 효율 곱셈

                State.AddStat(slot.trainingStat, amount);
                State.fatigue += TrainFatigue;
                State.stress  += TrainStress;
                State.todayTrainingCount++;
                
                var pTrain = State.GetProf(ProficiencyType.Training);
                if (pTrain.AddExp(amount)) GameEvents.RaiseProficiencyLevelUp(ProficiencyType.Training, pTrain.level);

                GameEvents.RaiseFighterSlotResult($"전투체: 훈련({GetCurrentStatName(slot.trainingStat)}) 완료 (+{amount})");
                break;

            case FighterSlotType.PartTime:
                bool big    = Random.value < (0.1f + profPart.PartTimeBigSuccessBonus);
                int  reward = big ? 20 : 10;
                State.gold           += reward;
                State.todayGoldEarned += reward;
                State.fatigue        += 1;
                profPart.AddExp(2);
                GameEvents.RaiseFighterSlotResult($"전투체: 알바 {(big ? "대성공" : "완료")} (+{reward}G)");
                break;

            case FighterSlotType.Rest:
                State.fatigue = Mathf.Max(0, State.fatigue - RestFatigueRecovery);
                State.stress  = Mathf.Max(0, State.stress  - RestStressRecovery);
                GameEvents.RaiseFighterSlotResult($"전투체: 휴식 (피로 -{RestFatigueRecovery}, 스트레스 -{RestStressRecovery})");
                break;
        }

        State.fighterSlotProgress++;
    }

    // ====================================================
    //  Night
    // ====================================================
    void TransitionToNight()
    {
        while (State.fighterSlotProgress < GameState.DaySlotCount)
            ExecuteFighterSlot();

        State.nightCompleted = false;
        SetPhase(GamePhase.NightChoice);
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

        ArenaBattleResult arenaResult;

        if (State.IsPromotionDay)
        {
            if (report.playerWon)
            {
                State.arena.promotionWins++;
                ArenaRank oldRank = State.arena.currentRank;
                if (State.arena.currentRank < ArenaRank.Champion)
                    State.arena.currentRank++;

                arenaResult = new ArenaBattleResult
                {
                    won              = true,
                    goldReward       = 80,
                    reputationChange = 10,
                    isPromotion      = true,
                    oldRank          = oldRank,
                    newRank          = State.arena.currentRank,
                    message          = $"승급전 승리! {oldRank} → {State.arena.currentRank}"
                };
            }
            else
            {
                State.arena.promotionLosses++;
                arenaResult = new ArenaBattleResult
                {
                    won              = false,
                    goldReward       = 10,
                    reputationChange = -3,
                    isPromotion      = true,
                    oldRank          = State.arena.currentRank,
                    newRank          = State.arena.currentRank,
                    message          = "승급전 패배... 다음 기회를 노리세요."
                };
            }
        }
        else
        {
            if (report.playerWon)
            {
                State.arena.wins++;
                arenaResult = new ArenaBattleResult
                {
                    won              = true,
                    goldReward       = 20,
                    reputationChange = 3,
                    message          = "아레나 승리!"
                };
            }
            else
            {
                State.arena.losses++;
                arenaResult = new ArenaBattleResult
                {
                    won              = false,
                    goldReward       = 5,
                    reputationChange = -1,
                    message          = "아레나 패배..."
                };
            }
        }

        State.gold            += arenaResult.goldReward;
        State.todayGoldEarned += arenaResult.goldReward;
        State.endingVars.Modify(EndingVar.Reputation, arenaResult.reputationChange);

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
        State.quests.GenerateDailyQuests(State.day);
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
