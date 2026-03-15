// ===== GameManager.cs =====
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ====================================================
    //  씬 이름 상수
    // ====================================================
    private const string SceneBattle = "BattleScene";
    private const string SceneMain   = "MainScene";

    // ====================================================
    //  참조
    // ====================================================
    [Header("Refs")]
    [SerializeField] private UIController       ui;
    [SerializeField] private BattlePreparationUI battlePrepUI;

    public GameState  State { get; private set; } = new GameState();
    public GamePhase  Phase { get; private set; } = GamePhase.Title;

    // ====================================================
    //  상수
    // ====================================================
    public const int DaySlotCount     = GameState.DaySlotCount;
    public const int MaxPlayerActions = GameState.MaxPlayerActions;

    const int BaseTrainAmount = 2;
    const int TrainFatigue    = 3;
    const int TrainStress     = 1;

    // ====================================================
    //  초기화
    // ====================================================
    void Awake()
    {
        if (ui == null)           ui           = FindFirstObjectByType<UIController>();
        if (battlePrepUI == null) battlePrepUI = FindFirstObjectByType<BattlePreparationUI>();

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
        for (int i = 0; i < DaySlotCount; i++)
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
        ui.ShowPhase(next);
        ui.RefreshAll(State, Phase);
    }

    // ====================================================
    //  Title
    // ====================================================
    public void OnClickStart() => SetPhase(GamePhase.ScheduleSetting);

    // ====================================================
    //  Schedule Setting
    // ====================================================
    public void OnClickConfirmSchedule()
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

        if (State.IsDayOver) { TransitionToNight(); return; }

        SetPhase(GamePhase.DayPlaceAction);
    }

    public void OnClickBackToMap()
    {
        if (Phase != GamePhase.DayPlaceAction) return;
        // 지도로 돌아갈 때 위치를 None으로 초기화
        // → 어떤 장소를 다시 클릭해도 "재입장"으로 인식해 행동권 소모
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

        // 행동권은 소모하지 않음 → 같은 장소에서 여러 번 행동 가능
        // 행동권 소모는 장소 이동 시에만 발생 (OnClickMapLocation)
        SetPhase(GamePhase.DayPlaceAction);
    }

    void ExecutePlaceAction(PlaceActionType action)
    {
        switch (action)
        {
            case PlaceActionType.Talk:
                State.endingVars.Modify(EndingVar.Sync, 2);
                State.stress = Mathf.Max(0, State.stress - 1);
                ui.ShowActionResult("대화 완료. 동기화 +2");
                break;

            case PlaceActionType.Investigate:
                State.stress += 2;
                State.endingVars.Modify(EndingVar.Reputation, 1);
                var profInv = State.GetProf(ProficiencyType.Investigation);
                if (profInv.AddExp(3)) ui.ShowLevelUpNotice(ProficiencyType.Investigation, profInv.level);
                ui.ShowActionResult("조사 수행. 평판 +1");
                break;

            case PlaceActionType.AcceptQuest:
                // 행동권 소모 없음 - 목록 열람만
                ui.ShowActionResult("의뢰를 확인하세요.");
                break;

            case PlaceActionType.DeliverQuest:
                var quest = State.quests.CheckDelivery(State.playerLocation);
                if (quest != null)
                {
                    State.quests.CompleteQuest(quest);
                    State.gold           += quest.goldReward;
                    State.todayGoldEarned += quest.goldReward;
                    ui.ShowActionResult($"의뢰 완료! +{quest.goldReward} Gold");
                }
                else ui.ShowActionResult("배달할 의뢰가 없습니다.");
                break;

            case PlaceActionType.BuyItem:
                ui.ShowActionResult("상점 이용. (준비 중)");
                break;

            case PlaceActionType.Rest:
                State.fatigue = Mathf.Max(0, State.fatigue - 2);
                State.stress  = Mathf.Max(0, State.stress  - 1);
                ui.ShowActionResult("휴식. 피로 -2, 스트레스 -1");
                break;
        }
    }

    // ====================================================
    //  Fighter Schedule
    // ====================================================
    void ExecuteFighterSlot()
    {
        if (State.fighterSlotProgress >= DaySlotCount) return;

        FighterSlot slot      = State.fighterSchedule[State.fighterSlotProgress];
        var         profTrain = State.GetProf(ProficiencyType.Training);
        var         profPart  = State.GetProf(ProficiencyType.PartTime);

        switch (slot.type)
        {
            case FighterSlotType.Training:
                State.AddStat(slot.trainingStat, BaseTrainAmount);
                State.todayTrainingCount++;
                State.fatigue += Mathf.Max(0, TrainFatigue - profTrain.TrainingFatigueReduction);
                State.stress  += TrainStress;
                State.endingVars.Modify(EndingVar.Sync, 1);
                profTrain.AddExp(3);
                ui.ShowFighterSlotResult($"전투체: {GetStatName(slot.trainingStat)} 훈련 완료 (+{BaseTrainAmount})");
                break;

            case FighterSlotType.PartTime:
                bool big    = Random.value < (0.1f + profPart.PartTimeBigSuccessBonus);
                int  reward = big ? 20 : 10;
                State.gold           += reward;
                State.todayGoldEarned += reward;
                State.fatigue        += 1;
                profPart.AddExp(2);
                ui.ShowFighterSlotResult($"전투체: 알바 {(big ? "대성공" : "완료")} (+{reward}G)");
                break;

            case FighterSlotType.Rest:
                State.fatigue = Mathf.Max(0, State.fatigue - 3);
                State.stress  = Mathf.Max(0, State.stress  - 2);
                ui.ShowFighterSlotResult("전투체: 휴식 (피로 -3, 스트레스 -2)");
                break;
        }

        State.fighterSlotProgress++;
    }

    // ====================================================
    //  Night
    // ====================================================
    void TransitionToNight()
    {
        while (State.fighterSlotProgress < DaySlotCount)
            ExecuteFighterSlot();

        State.nightCompleted = false;
        SetPhase(GamePhase.NightChoice);
    }

    public void OnClickNightChoice(int choiceIndex)
    {
        if (Phase != GamePhase.NightChoice) return;

        NightActionType choice = (NightActionType)choiceIndex;

        if (choice == NightActionType.Arena && !State.IsArenaOpen)
        { ui.ShowArenaClosedWarning(); return; }

        if (choice != NightActionType.Rest && State.stress >= 80)
        { ui.ShowStressWarning(); return; }

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
                State.gold    += 5;
                State.endingVars.Modify(EndingVar.Reputation, 1);
                var profExp = State.GetProf(ProficiencyType.Exploration);
                if (profExp.AddExp(4)) ui.ShowLevelUpNotice(ProficiencyType.Exploration, profExp.level);
                State.nightCompleted = true;
                SetPhase(GamePhase.DaySummary);
                break;

            case NightActionType.Arena:
                State.stress  += 3;
                State.fatigue += 5;
                StartArenaBattle();
                return;  // 전투 준비 화면으로 넘어가므로 여기서 return

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
        ui.ShowPhase(GamePhase.BattlePreparation);

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
                    // 아레나 선택 시 소모한 스탯 복구
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
        SceneManager.LoadScene(SceneBattle);
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
                    goldReward       = 80,  // 버그 수정: 50(기본) + 30(승급 보너스) 통합 → 이중 적용 방지
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

        ui.ShowBattleResult(arenaResult);
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
        // 버그 수정: 매일 새 의뢰 생성 (ResetForNewDay에서 day++가 먼저 실행됨)
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
            ui.ShowActionResult("의뢰 수령 완료!");
        else
            ui.ShowActionResult("의뢰를 수령할 수 없습니다.");

        ui.RefreshAll(State, Phase);
    }

    // ====================================================
    //  Calendar
    // ====================================================
    public void OnClickOpenCalendar()  => ui.ShowCalendar(State);
    public void OnClickCloseCalendar() => ui.HideCalendar();

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
        ui.RefreshAll(State, Phase);
    }

    public void DebugReduceStress()
    {
        State.stress = Mathf.Max(0, State.stress - 20);
        ui.RefreshAll(State, Phase);
    }

    public void DebugForceDaySummary()
    {
        State.playerActionsUsed = MaxPlayerActions;
        State.nightCompleted    = true;
        SetPhase(GamePhase.DaySummary);
    }

    public void DebugClearSave() => SaveSystem.Clear();

    public void DebugAddProfExp()
    {
        foreach (ProficiencyType p in System.Enum.GetValues(typeof(ProficiencyType)))
            State.GetProf(p).AddExp(10);
        ui.RefreshAll(State, Phase);
    }

    public void DebugSkipToPromotion()
    {
        State.day = CalendarSystem.NextPromotionDay(State.day) - 1;
        ui.RefreshAll(State, Phase);
    }

    public void DebugForceBattle() => StartArenaBattle();
}
