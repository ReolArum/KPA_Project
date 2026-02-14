using UnityEngine;

public static class SaveSystem
{
    private const string Key = "save_json";

    // ─── 내부 직렬화 클래스 ───

    [System.Serializable]
    private class FighterSlotSaveData
    {
        public int type;          // FighterSlotType
        public int trainingStat;  // TrainingStat
    }

    [System.Serializable]
    private class ProfSaveData
    {
        public int level;
        public int exp;
    }

    [System.Serializable]
    private class ArenaSaveData
    {
        public int rank;
        public int wins;
        public int losses;
        public int promotionWins;
        public int promotionLosses;
    }

    [System.Serializable]
    private class EndingSaveData
    {
        public int reputation;
        public int corpA;
        public int corpB;
        public int sync;
        public int ethics;
    }

    [System.Serializable]
    private class SaveData
    {
        // 기본
        public int day;
        public int gold;

        // 전투체 스케줄
        public int fighterSlotProgress;
        public FighterSlotSaveData[] fighterSchedule;

        // 플레이어
        public int playerActionsUsed;
        public int playerLocation;

        // 밤
        public int nightChoice;
        public bool nightCompleted;

        // 전투 스탯
        public int statStrength;
        public int statAgility;
        public int statDexterity;
        public int statEndurance;

        // 컨디션
        public int stress;
        public int fatigue;

        // 숙련도
        public ProfSaveData profTraining;
        public ProfSaveData profInvestigation;
        public ProfSaveData profExploration;
        public ProfSaveData profPartTime;

        // 엔딩 변수
        public EndingSaveData endingVars;

        // 아레나
        public ArenaSaveData arena;
    }

    // ─── Public API ───

    public static bool HasSave()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));
    }

    public static void Save(GameState state)
    {
        var ev = state.endingVars;
        var ar = state.arena;

        var data = new SaveData
        {
            day = state.day,
            gold = state.gold,
            fighterSlotProgress = state.fighterSlotProgress,
            playerActionsUsed = state.playerActionsUsed,
            playerLocation = (int)state.playerLocation,
            nightChoice = (int)state.nightChoice,
            nightCompleted = state.nightCompleted,

            statStrength = state.statStrength,
            statAgility = state.statAgility,
            statDexterity = state.statDexterity,
            statEndurance = state.statEndurance,

            stress = state.stress,
            fatigue = state.fatigue,

            profTraining = ToSaveData(state.profTraining),
            profInvestigation = ToSaveData(state.profInvestigation),
            profExploration = ToSaveData(state.profExploration),
            profPartTime = ToSaveData(state.profPartTime),

            endingVars = new EndingSaveData
            {
                reputation = ev.reputation,
                corpA = ev.corpARelation,
                corpB = ev.corpBRelation,
                sync = ev.synchronization,
                ethics = ev.ethicsEfficiency
            },

            arena = new ArenaSaveData
            {
                rank = (int)ar.currentRank,
                wins = ar.wins,
                losses = ar.losses,
                promotionWins = ar.promotionWins,
                promotionLosses = ar.promotionLosses
            }
        };

        // 전투체 스케줄 저장 (타입 + 훈련 스탯)
        data.fighterSchedule = new FighterSlotSaveData[GameState.DaySlotCount];
        for (int i = 0; i < GameState.DaySlotCount; i++)
        {
            var slot = state.fighterSchedule[i];
            data.fighterSchedule[i] = new FighterSlotSaveData
            {
                type = (int)slot.type,
                trainingStat = (int)slot.trainingStat
            };
        }

        string json = JsonUtility.ToJson(data, false);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    public static bool Load(GameState state)
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return false;

        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return false;

        // 기본
        state.day = data.day;
        state.gold = data.gold;
        state.fighterSlotProgress = data.fighterSlotProgress;
        state.playerActionsUsed = data.playerActionsUsed;
        state.playerLocation = (MapLocation)data.playerLocation;
        state.nightChoice = (NightActionType)data.nightChoice;
        state.nightCompleted = data.nightCompleted;

        // 전투 스탯
        state.statStrength = data.statStrength;
        state.statAgility = data.statAgility;
        state.statDexterity = data.statDexterity;
        state.statEndurance = data.statEndurance;

        // 컨디션
        state.stress = data.stress;
        state.fatigue = data.fatigue;

        // 숙련도
        FromSaveData(data.profTraining, state.profTraining);
        FromSaveData(data.profInvestigation, state.profInvestigation);
        FromSaveData(data.profExploration, state.profExploration);
        FromSaveData(data.profPartTime, state.profPartTime);

        // 엔딩 변수
        if (data.endingVars != null)
        {
            state.endingVars.reputation = data.endingVars.reputation;
            state.endingVars.corpARelation = data.endingVars.corpA;
            state.endingVars.corpBRelation = data.endingVars.corpB;
            state.endingVars.synchronization = data.endingVars.sync;
            state.endingVars.ethicsEfficiency = data.endingVars.ethics;
        }

        // 아레나
        if (data.arena != null)
        {
            state.arena.currentRank = (ArenaRank)data.arena.rank;
            state.arena.wins = data.arena.wins;
            state.arena.losses = data.arena.losses;
            state.arena.promotionWins = data.arena.promotionWins;
            state.arena.promotionLosses = data.arena.promotionLosses;
        }

        // 전투체 스케줄 복원
        if (data.fighterSchedule != null)
        {
            for (int i = 0; i < GameState.DaySlotCount; i++)
            {
                if (i < data.fighterSchedule.Length && data.fighterSchedule[i] != null)
                {
                    state.fighterSchedule[i].type =
                        (FighterSlotType)Mathf.Clamp(data.fighterSchedule[i].type, 0, 2);
                    state.fighterSchedule[i].trainingStat =
                        (TrainingStat)Mathf.Clamp(data.fighterSchedule[i].trainingStat, 0, 3);
                }
                else
                {
                    state.fighterSchedule[i].type = FighterSlotType.Rest;
                    state.fighterSchedule[i].trainingStat = TrainingStat.Strength;
                }
            }
        }

        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    // ─── Helper ───

    private static ProfSaveData ToSaveData(Proficiency p)
    {
        return new ProfSaveData
        {
            level = p.level,
            exp = p.exp
        };
    }

    private static void FromSaveData(ProfSaveData data, Proficiency p)
    {
        if (data == null) return;
        p.level = Mathf.Clamp(data.level, 1, 5);
        p.exp = Mathf.Max(0, data.exp);
    }
}
