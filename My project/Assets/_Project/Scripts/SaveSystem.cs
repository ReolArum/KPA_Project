using UnityEngine;

public static class SaveSystem
{
    const string Key = "save_json";

    [System.Serializable]
    private class ProfSaveData
    {
        public int level;
        public int exp;
    }

    [System.Serializable]
    private class SaveData
    {
        public int day;
        public int gold;
        public int currentDaySlot;
        public int[] daySchedule;
        public int nightChoice;
        public bool nightCompleted;

        public int statStrength;
        public int statAgility;
        public int statDexterity;
        public int statEndurance;

        public int stress;
        public int fatigue;

        // 숙련도
        public ProfSaveData profTraining;
        public ProfSaveData profInvestigation;
        public ProfSaveData profExploration;
        public ProfSaveData profPartTime;
    }

    public static bool HasSave()
        => !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));

    static ProfSaveData ToSaveData(Proficiency p) => new()
    {
        level = p.level,
        exp = p.exp
    };

    static void FromSaveData(ProfSaveData data, Proficiency p)
    {
        if (data == null) return;
        p.level = Mathf.Clamp(data.level, 1, 5);
        p.exp = Mathf.Max(0, data.exp);
    }

    public static void Save(GameState state)
    {
        var data = new SaveData
        {
            day = state.day,
            gold = state.gold,
            currentDaySlot = state.currentDaySlot,
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
            profPartTime = ToSaveData(state.profPartTime)
        };

        data.daySchedule = new int[GameState.DaySlotCount];
        for (int i = 0; i < GameState.DaySlotCount; i++)
            data.daySchedule[i] = (int)state.daySchedule[i];

        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool Load(GameState state)
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return false;

        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return false;

        state.day = data.day;
        state.gold = data.gold;
        state.currentDaySlot = data.currentDaySlot;
        state.nightChoice = (NightActionType)data.nightChoice;
        state.nightCompleted = data.nightCompleted;

        state.statStrength = data.statStrength;
        state.statAgility = data.statAgility;
        state.statDexterity = data.statDexterity;
        state.statEndurance = data.statEndurance;

        state.stress = data.stress;
        state.fatigue = data.fatigue;

        FromSaveData(data.profTraining, state.profTraining);
        FromSaveData(data.profInvestigation, state.profInvestigation);
        FromSaveData(data.profExploration, state.profExploration);
        FromSaveData(data.profPartTime, state.profPartTime);

        if (data.daySchedule != null)
        {
            for (int i = 0; i < GameState.DaySlotCount; i++)
            {
                if (i < data.daySchedule.Length)
                    state.daySchedule[i] = (DaySlotType)Mathf.Clamp(data.daySchedule[i], 0, 5);
                else
                    state.daySchedule[i] = DaySlotType.Rest;
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
