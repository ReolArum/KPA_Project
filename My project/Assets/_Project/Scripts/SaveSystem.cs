using UnityEngine;

public static class SaveSystem
{
    const string Key = "save_json";

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
    }

    public static bool HasSave()
        => !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));

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
            fatigue = state.fatigue
        };

        data.daySchedule = new int[GameState.DaySlotCount];
        for (int i = 0; i < GameState.DaySlotCount; i++)
            data.daySchedule[i] = (int)state.daySchedule[i];

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key, json);
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
