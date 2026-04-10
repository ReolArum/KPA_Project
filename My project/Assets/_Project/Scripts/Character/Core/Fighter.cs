using KPA.Character;
using UnityEngine;

public class Fighter : CharacterBase
{
    [SerializeField] private string fighterId;
    [SerializeField] private GameObject modelPrefab; // VisualModule에서 사용할 프리팹

    public string FighterId => fighterId;

    protected override void InitializeModules()
    {
        // 핵심 모듈들 조립
        AddModule(new StatModule());
        AddModule(new VisualModule(modelPrefab));
        AddModule(new AnimationModule());
    }

    // [ADD] 탐사 시스템 등에서 스탯을 편하게 가져오기 위한 헬퍼
    public int GetStat(TrainingStat stat)
    {
        return GetModule<StatModule>()?.GetTotalStat(stat) ?? 0;
    }
}
