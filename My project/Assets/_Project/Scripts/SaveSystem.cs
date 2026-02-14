using UnityEngine;

public static class SaveSystem
{
    private const string Key = "save_json";

    // ─── 내부 직렬화 클래스 ───

    [System.Serializable]
    private class FighterSlotSaveData
    {
        public int type;
        public int trainingStat;
    }

    [System.Serializable]
    private class StatSaveData
    {
        public int enumValue;
        public int amount;
    }

    [System.Serializable]
    private class ProfSaveData
    {
        public int enumValue;
        public int level;
        public int exp;
    }

    [System.Serializable]
    private class EndingSaveData
    {
        public int enumValue;
        public int amount;
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
    private class SaveData
    {
        public int day;
        public int gold;

        public int fighterSlotProgress;
        public FighterSlotSaveData[] fighterSchedule;

        public int playerActionsUsed;
        public int playerLocation;

        public int nightChoice;
        public bool nightCompleted;

        public StatSaveData[] stats;

        public int stress;
        public int fatigue;

        public ProfSaveData[] proficiencies;

        public EndingSaveData[] endingVars;

        public ArenaSaveData arena;
    }

    // ─── Public API ───

    public static bool HasSave()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));
    }

    public static void Save(GameState state)
    {
        var data = new SaveData
        {
            day = state.day,
            gold = state.gold,
            fighterSlotProgress = state.fighterSlotProgress,
            playerActionsUsed = state.playerActionsUsed,
            playerLocation = (int)state.playerLocation,
            nightChoice = (int)state.nightChoice,
            nightCompleted = state.nightCompleted,
            stress = state.stress,
            fatigue = state.fatigue
        };

        // 전투체 스케줄
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

        // 스탯 → 배열
        var statValues = System.Enum.GetValues(typeof(TrainingStat));
        data.stats = new StatSaveData[statValues.Length];
        int si = 0;
        foreach (TrainingStat s in statValues)
        {
            data.stats[si++] = new StatSaveData
            {
                enumValue = (int)s,
                amount = state.GetStat(s)
            };
        }

        // 숙련도 → 배열
        var profValues = System.Enum.GetValues(typeof(ProficiencyType));
        data.proficiencies = new ProfSaveData[profValues.Length];
        int pi = 0;
        foreach (ProficiencyType p in profValues)
        {
            var prof = state.GetProf(p);
            data.proficiencies[pi++] = new ProfSaveData
            {
                enumValue = (int)p,
                level = prof.level,
                exp = prof.exp
            };
        }

        // 엔딩 변수 → 배열
        var endValues = System.Enum.GetValues(typeof(EndingVar));
        data.endingVars = new EndingSaveData[endValues.Length];
        int ei = 0;
        foreach (EndingVar v in endValues)
        {
            data.endingVars[ei++] = new EndingSaveData
            {
                enumValue = (int)v,
                amount = state.endingVars.Get(v)
            };
        }

        // 아레나
        var ar = state.arena;
        data.arena = new ArenaSaveData
        {
            rank = (int)ar.currentRank,
            wins = ar.wins,
            losses = ar.losses,
            promotionWins = ar.promotionWins,
            promotionLosses = ar.promotionLosses
        };

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
        state.stress = data.stress;
        state.fatigue = data.fatigue;

        // 스탯 복원
        if (data.stats != null)
        {
            foreach (var sd in data.stats)
            {
                var key = (TrainingStat)sd.enumValue;
                if (state.stats.ContainsKey(key))
                    state.stats[key] = sd.amount;
            }
        }

        // 숙련도 복원
        if (data.proficiencies != null)
        {
            foreach (var pd in data.proficiencies)
            {
                var key = (ProficiencyType)pd.enumValue;
                if (state.proficiencies.ContainsKey(key))
                {
                    var prof = state.proficiencies[key];
                    prof.level = Mathf.Clamp(pd.level, 1, 5);
                    prof.exp = Mathf.Max(0, pd.exp);
                }
            }
        }

        // 엔딩 변수 복원
        if (data.endingVars != null)
        {
            foreach (var ed in data.endingVars)
            {
                var key = (EndingVar)ed.enumValue;
                if (state.endingVars.values.ContainsKey(key))
                    state.endingVars.values[key] = ed.amount;
            }
        }

        // 아레나 복원
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
                        (TrainingStat)Mathf.Clamp(data.fighterSchedule[i].trainingStat, 0,
                            System.Enum.GetValues(typeof(TrainingStat)).Length - 1);
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
}
