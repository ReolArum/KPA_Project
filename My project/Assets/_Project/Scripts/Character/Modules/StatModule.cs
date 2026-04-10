using System.Collections.Generic;
using UnityEngine;
using KPA.Character;

public class StatModule : ICharacterModule
{
    private CharacterBase _owner;
    
    // 기본 스탯 (TrainingStat Enum 활용)
    private Dictionary<TrainingStat, int> _baseStats = new Dictionary<TrainingStat, int>();
    
    // 추가 스탯 (장비, 버프 등)
    private Dictionary<TrainingStat, int> _bonusStats = new Dictionary<TrainingStat, int>();

    public void Initialize(CharacterBase owner)
    {
        _owner = owner;
    }

    public float GetCritical()
    {
        // [MOD] '재주' 스탯이 삭제됨에 따라 '감각(SEN)' 수치로 일원화 (0.2 + 0.1 = 0.3배율 적용)
        float val = 5f + GetTotalStat(TrainingStat.Sensitivity) * 0.3f;
        return Mathf.Min(val, 100f); // 상한 100%
    }

    public void SetBaseStat(TrainingStat stat, int value)
    {
        _baseStats[stat] = value;
    }

    public int GetTotalStat(TrainingStat stat)
    {
        int baseVal = _baseStats.ContainsKey(stat) ? _baseStats[stat] : 0;
        int bonusVal = _bonusStats.ContainsKey(stat) ? _bonusStats[stat] : 0;
        return baseVal + bonusVal;
    }

    public void OnUpdate() { }
    public void OnFixedUpdate() { }
}
