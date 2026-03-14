// ===== GameManager.cs =====
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIController ui;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleUIController battleUI;
    [SerializeField] private BattlePreparationUI battlePrepUI;  // ★ 추가

    public GameState State { get; private set; } = new GameState();
    public GamePhase Phase { get; private set; } = GamePhase.Title;

    public const int DaySlotCount = GameState.DaySlotCount;
    public const int MaxPlayerActions = GameState.MaxPlayerActions;

    const int BaseTrainAmount = 2;
    const int TrainFatigue = 3;
    const int TrainStress = 1;

void Awake()
{
    // ===== 전투 씬에서 돌아왔는지 확인 ★ =====
    if (BattleSceneData.battleCompleted)
    {
        if (ui == null) ui = FindFirstObjectByType<UIController>();

        State = BattleSceneData.gameState;
        var report = BattleSceneData.battleReport;
        BattleSceneData.Clear();

        SetPhase(GamePhase.NightAction);
        OnBattleFinished(report);
        return;
    }

    // ===== 기존 초기화 코드 =====
    if (ui == null) ui = FindFirstObjectByType<UIController>();
    if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();
    if (battleUI == null) battleUI = FindFirstObjectByType<BattleUIController>();
    if (battlePrepUI == null) battlePrepUI = FindFirstObjectByType<BattlePreparationUI>();

    for (int i = 0; i < DaySlotCount; i++)
    {
        State.fighterSchedule[i].type = FighterSlotType.Rest;
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
    public void OnClickStart()
    {
        SetPhase(GamePhase.ScheduleSetting);
    }

    // ====================================================
    //  Schedule Setting
    // ====================================================
    public void OnClickConfirmSchedule()
    {
        State.fighterSlotProgress = 0;
        State.playerActionsUsed = 0;
        State.playerLocation = MapLocation.Home;
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
            SetPhase(GamePhase.DayPlaceAction);
            return;
        }

        State.playerLocation = target;
        State.playerActionsUsed++;

        ExecuteFighterSlot();

        if (State.IsDayOver)
        {
            TransitionToNight();
            return;
        }

        SetPhase(GamePhase.DayPlaceAction);
    }

    public void OnClickBackToMap()
    {
        if (Phase != GamePhase.DayPlaceAction) return;
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

        if (State.IsDayOver)
            TransitionToNight();
        else
            SetPhase(GamePhase.DayMap);
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
                bool ilvl = profInv.AddExp(3);
                if (ilvl) ui.ShowLevelUpNotice(ProficiencyType.Investigation, profInv.level);
                ui.ShowActionResult("조사 수행. 평판 +1");
                break;

            case PlaceActionType.AcceptQuest:
                ui.ShowActionResult("의뢰를 확인하세요.");
                break;

            case PlaceActionType.DeliverQuest:
                var quest = State.quests.CheckDelivery(State.playerLocation);
                if (quest != null)
                {
                    State.quests.CompleteQuest(quest);
                    State.gold += quest.goldReward;
                    State.todayGoldEarned += quest.goldReward;
                    ui.ShowActionResult($"의뢰 완료! +{quest.goldReward} Gold");
                }
                else
                {
                    ui.ShowActionResult("배달할 의뢰가 없습니다.");
                }
                break;

            case PlaceActionType.BuyItem:
                ui.ShowActionResult("상점 이용. (준비 중)");
                break;

            case PlaceActionType.Rest:
                State.fatigue = Mathf.Max(0, State.fatigue - 2);
                State.stress = Mathf.Max(0, State.stress - 1);
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

        FighterSlot slot = State.fighterSchedule[State.fighterSlotProgress];
        var profTrain = State.GetProf(ProficiencyType.Training);
        var profPart = State.GetProf(ProficiencyType.PartTime);

        switch (slot.type)
        {
            case FighterSlotType.Training:
                State.AddStat(slot.trainingStat, BaseTrainAmount);
                State.todayTrainingCount++;
                State.fatigue += Mathf.Max(0, TrainFatigue - profTrain.TrainingFatigueReduction);
                State.stress += TrainStress;
                State.endingVars.Modify(EndingVar.Sync, 1);
                profTrain.AddExp(3);

                string statName = GetStatName(slot.trainingStat);
                ui.ShowFighterSlotResult($"전투체: {statName} 훈련 완료 (+{BaseTrainAmount})");
                break;

            case FighterSlotType.PartTime:
                float bigChance = 0.1f + profPart.PartTimeBigSuccessBonus;
                bool big = Random.value < bigChance;
                int reward = big ? 20 : 10;
                State.gold += reward;
                State.todayGoldEarned += reward;
                State.fatigue += 1;
                profPart.AddExp(2);
                ui.ShowFighterSlotResult($"전투체: 알바 {(big ? "대성공" : "완료")} (+{reward}G)");
                break;

            case FighterSlotType.Rest:
                State.fatigue = Mathf.Max(0, State.fatigue - 3);
                State.stress = Mathf.Max(0, State.stress - 2);
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
        {
            ui.ShowArenaClosedWarning();
            return;
        }

        if (choice != NightActionType.Rest && State.stress >= 80)
        {
            ui.ShowStressWarning();
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
                State.stress += 5;
                State.fatigue += 3;
                State.gold += 5;
                State.endingVars.Modify(EndingVar.Reputation, 1);
                var profExp = State.GetProf(ProficiencyType.Exploration);
                bool elvl = profExp.AddExp(4);
                if (elvl) ui.ShowLevelUpNotice(ProficiencyType.Exploration, profExp.level);
                State.nightCompleted = true;
                SetPhase(GamePhase.DaySummary);
                break;

            case NightActionType.Arena:
                State.stress += 3;
                State.fatigue += 5;
                StartArenaBattle();
                return;

            case NightActionType.Rest:
                State.stress = Mathf.Max(0, State.stress - 5);
                State.fatigue = Mathf.Max(0, State.fatigue - 5);
                State.nightCompleted = true;
                SetPhase(GamePhase.DaySummary);
                break;
        }
    }

    // ====================================================
    //  ★ 아레나 전투 (전투 준비 → 전투)
    // ====================================================
    void StartArenaBattle()
    {
        // 전투 준비 화면으로 이동
        Phase = GamePhase.BattlePreparation;
        ui.ShowPhase(GamePhase.BattlePreparation);

        if (battlePrepUI != null)
        {
            battlePrepUI.Open(State,
                onStart: () =>
                {
                    // 전투 시작 버튼 클릭 시
                    battlePrepUI.Close();
                    BeginBattle();
                },
                onCancel: () =>
                {
                    // 돌아가기 버튼 클릭 시
                    battlePrepUI.Close();
                    State.stress -= 3;  // 아레나 선택 시 추가된 스트레스 복구
                    State.fatigue -= 5; // 아레나 선택 시 추가된 피로 복구
                    State.nightCompleted = false;
                    SetPhase(GamePhase.NightChoice);
                }
            );
        }
        else
        {
            // 전투 준비 UI 없으면 바로 전투
            BeginBattle();
        }
    }

   void BeginBattle()
{
    // 전투 데이터 설정
    BattleSceneData.SetupBattle(State);

    // 전투 씬으로 이동
    UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
}


    void OnBattleFinished(BattleReport report)
    {
        if (battleManager != null)
            battleManager.OnBattleEnd -= OnBattleFinished;

        if (battleUI != null)
            battleUI.gameObject.SetActive(false);

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
                    won = true,
                    goldReward = 50,
                    reputationChange = 10,
                    isPromotion = true,
                    oldRank = oldRank,
                    newRank = State.arena.currentRank,
                    message = $"승급전 승리! {oldRank} → {State.arena.currentRank}"
                };
                ApplyPromotionReward();
            }
            else
            {
                State.arena.promotionLosses++;
                arenaResult = new ArenaBattleResult
                {
                    won = false,
                    goldReward = 10,
                    reputationChange = -3,
                    isPromotion = true,
                    oldRank = State.arena.currentRank,
                    newRank = State.arena.currentRank,
                    message = "승급전 패배... 다음 기회를 노리세요."
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
                    won = true,
                    goldReward = 20,
                    reputationChange = 3,
                    message = "아레나 승리!"
                };
            }
            else
            {
                State.arena.losses++;
                arenaResult = new ArenaBattleResult
                {
                    won = false,
                    goldReward = 5,
                    reputationChange = -1,
                    message = "아레나 패배..."
                };
            }
        }

        State.gold += arenaResult.goldReward;
        State.todayGoldEarned += arenaResult.goldReward;
        State.endingVars.Modify(EndingVar.Reputation, arenaResult.reputationChange);

        ui.ShowBattleResult(arenaResult);

        State.nightCompleted = true;
        SetPhase(GamePhase.DaySummary);
    }

    void FallbackArenaBattle()
    {
        ArenaBattleResult result;

        if (State.IsPromotionDay)
        {
            result = State.arena.ProcessPromotionBattle(State);
            if (result.won) ApplyPromotionReward();
        }
        else
        {
            result = State.arena.ProcessNormalBattle(State);
        }

        State.gold += result.goldReward;
        State.todayGoldEarned += result.goldReward;
        State.endingVars.Modify(EndingVar.Reputation, result.reputationChange);
        ui.ShowBattleResult(result);

        State.nightCompleted = true;
        SetPhase(GamePhase.DaySummary);
    }

    void ApplyPromotionReward()
    {
        State.gold += 30;
    }

    // ====================================================
    //  Day Summary
    // ====================================================
    public void OnClickNextDay()
    {
        if (Phase != GamePhase.DaySummary) return;

        State.ResetForNewDay();
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
    public void OnClickOpenCalendar() => ui.ShowCalendar(State);
    public void OnClickCloseCalendar() => ui.HideCalendar();

    // ====================================================
    //  Utility
    // ====================================================
    public static string GetCurrentTimeLabel(GameState state, GamePhase phase)
    {
        if (phase == GamePhase.BattlePreparation) return "20:30";  // ★ 추가
        if (phase == GamePhase.Battle) return "21:00";
        if (phase == GamePhase.NightChoice || phase == GamePhase.NightAction) return "20:00";
        if (phase == GamePhase.DaySummary) return "22:00";

        int hour = 8 + state.playerActionsUsed * 3;
        return $"{hour:00}:00";
    }

    public static string GetStatName(TrainingStat stat) => stat switch
    {
        TrainingStat.Strength => "힘",
        TrainingStat.Agility => "민첩",
        TrainingStat.Dexterity => "재주",
        TrainingStat.Endurance => "지구력",
        _ => stat.ToString()
    };

    public static string GetProfName(ProficiencyType type) => type switch
    {
        ProficiencyType.Training => "훈련",
        ProficiencyType.Investigation => "조사",
        ProficiencyType.Exploration => "탐사",
        ProficiencyType.PartTime => "알바",
        _ => type.ToString()
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
        State.nightCompleted = true;
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
        int next = CalendarSystem.NextPromotionDay(State.day);
        State.day = next - 1;
        ui.RefreshAll(State, Phase);
    }

    public void DebugForceBattle()
    {
        StartArenaBattle();
    }
}
