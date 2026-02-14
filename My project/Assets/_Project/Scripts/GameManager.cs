using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIController ui;

    public GameState State { get; private set; } = new GameState();
    public GamePhase Phase { get; private set; } = GamePhase.Title;

    // ===== 상수 =====
    public const int DaySlotCount = GameState.DaySlotCount;
    public const int MaxPlayerActions = GameState.MaxPlayerActions;

    const int BaseTrainAmount = 2;
    const int TrainFatigue = 3;
    const int TrainStress = 1;

    void Awake()
    {
        if (ui == null) ui = FindFirstObjectByType<UIController>();

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
    //  Schedule Setting (전투체 스케줄)
    // ====================================================
    public void OnClickConfirmSchedule()
    {
        State.fighterSlotProgress = 0;
        State.playerActionsUsed = 0;
        State.playerLocation = MapLocation.Home;
        SetPhase(GamePhase.DayMap);
    }

    // ====================================================
    //  Day Map (플레이어 지도 이동)
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
    //  Place Actions (장소 내 행동)
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
    //  Fighter Schedule (전투체 자동 진행)
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
                break;

            case NightActionType.Arena:
                State.stress += 3;
                State.fatigue += 5;

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
                break;

            case NightActionType.Rest:
                State.stress = Mathf.Max(0, State.stress - 5);
                State.fatigue = Mathf.Max(0, State.fatigue - 5);
                break;
        }

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
    //  Time Utility
    // ====================================================

    public static string GetCurrentTimeLabel(GameState state, GamePhase phase)
    {
        if (phase == GamePhase.NightChoice || phase == GamePhase.NightAction)
            return "20:00";
        if (phase == GamePhase.DaySummary)
            return "22:00";

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
}
