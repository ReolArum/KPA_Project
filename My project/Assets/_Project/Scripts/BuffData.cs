// ===== BuffData.cs =====

using UnityEngine;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Combat/Buff")]
public class BuffData : ScriptableObject
{
    public string buffName;
    public string id;  // 갱신 판정용 고유 ID

    [Header("Stat Modification")]
    public int modSTR;
    public int modAGI;
    public int modVIT;
    public int modINT;
    public int modGUT;
    public int modSEN;

    [Header("Duration")]
    public int duration = 3;  // 해당 캐릭터 행동 턴 기준

    public bool AffectsSPD => modAGI != 0 || modSEN != 0;
}

// ===== BuffInstance.cs =====

[System.Serializable]
public class BuffInstance
{
    public BuffData data;
    public int remainingTurns;
}
