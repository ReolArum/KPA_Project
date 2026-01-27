using UnityEngine;

public static class SaveSystem
{
    const string KeyExists = "save_exists";
    const string KeyDay = "save_day";
    const string KeyGold = "save_gold";
    const string KeyBlock = "save_block";
    const string KeySchedule = "save_schedule"; // 문자열로 저장

    public static bool HasSave()
        => PlayerPrefs.GetInt(KeyExists, 0) == 1;

    public static void Save(GameState state)
    {
        PlayerPrefs.SetInt(KeyExists, 1);
        PlayerPrefs.SetInt(KeyDay, state.day);
        PlayerPrefs.SetInt(KeyGold, state.gold);
        PlayerPrefs.SetInt(KeyBlock, state.currentBlock);

        // schedule[26]을 "0,2,1,..." 같은 CSV로 저장
        // SlotType: Strength=0, Stamina=1, Rest=2 (Enums.cs 순서 기준)
        string[] arr = new string[GameManager.MaxBlocks];
        for (int i = 0; i < GameManager.MaxBlocks; i++)
            arr[i] = ((int)state.schedule[i]).ToString();

        PlayerPrefs.SetString(KeySchedule, string.Join(",", arr));

        PlayerPrefs.Save();
    }

    public static bool Load(GameState state)
    {
        if (!HasSave()) return false;

        state.day = PlayerPrefs.GetInt(KeyDay, 1);
        state.gold = PlayerPrefs.GetInt(KeyGold, 0);
        state.currentBlock = Mathf.Clamp(PlayerPrefs.GetInt(KeyBlock, 0), 0, GameManager.MaxBlocks);

        string csv = PlayerPrefs.GetString(KeySchedule, "");
        if (!string.IsNullOrEmpty(csv))
        {
            var parts = csv.Split(',');
            for (int i = 0; i < GameManager.MaxBlocks; i++)
            {
                int v = 2; // 기본 Rest
                if (i < parts.Length) int.TryParse(parts[i], out v);
                v = Mathf.Clamp(v, 0, 2);
                state.schedule[i] = (SlotType)v;
            }
        }
        else
        {
            // 스케줄 문자열이 없으면 기본 Rest
            for (int i = 0; i < GameManager.MaxBlocks; i++)
                state.schedule[i] = SlotType.Rest;
        }

        // 오늘 누적치(todayStrengthTrain 등)는 "하루 중간 저장"을 원할 때만 저장하는 게 보통이라,
        // 지금은 로드 시 0으로 둡니다.
        state.todayStrengthTrain = 0;
        state.todayStaminaTrain = 0;

        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KeyExists);
        PlayerPrefs.DeleteKey(KeyDay);
        PlayerPrefs.DeleteKey(KeyGold);
        PlayerPrefs.DeleteKey(KeyBlock);
        PlayerPrefs.DeleteKey(KeySchedule);
        PlayerPrefs.Save();
    }
}
