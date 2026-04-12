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

    public int GetStat(TrainingStat stat)
    {
        return GetModule<StatModule>()?.GetTotalStat(stat) ?? 0;
    }

    // [ADD] 외부에서 특정 애니메이션 트리거를 호출하기 위한 헬퍼
    public void PlayAnimation(string triggerName)
    {
        GetModule<AnimationModule>()?.PlayTrigger(triggerName);
    }
}
