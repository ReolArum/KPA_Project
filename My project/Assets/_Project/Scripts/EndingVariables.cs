using System;
using UnityEngine;

[Serializable]
public class EndingVariables
{
    // -100 ~ +100 범위로 운영
    public int reputation = 0;        // 평판 (아레나/대중)
    public int corpARelation = 0;     // 기업 A 관계도
    public int corpBRelation = 0;     // 기업 B 관계도
    public int synchronization = 0;   // 동기화 (주인공-훈련생)
    public int ethicsEfficiency = 0;  // 윤리/효율 성향 (+:윤리, -:효율)

    public void Modify(EndingVar variable, int amount)
    {
        switch (variable)
        {
            case EndingVar.Reputation:
                reputation = Mathf.Clamp(reputation + amount, -100, 100);
                break;
            case EndingVar.CorpA:
                corpARelation = Mathf.Clamp(corpARelation + amount, -100, 100);
                break;
            case EndingVar.CorpB:
                corpBRelation = Mathf.Clamp(corpBRelation + amount, -100, 100);
                break;
            case EndingVar.Sync:
                synchronization = Mathf.Clamp(synchronization + amount, -100, 100);
                break;
            case EndingVar.Ethics:
                ethicsEfficiency = Mathf.Clamp(ethicsEfficiency + amount, -100, 100);
                break;
        }
    }

    public int Get(EndingVar variable) => variable switch
    {
        EndingVar.Reputation => reputation,
        EndingVar.CorpA => corpARelation,
        EndingVar.CorpB => corpBRelation,
        EndingVar.Sync => synchronization,
        EndingVar.Ethics => ethicsEfficiency,
        _ => 0
    };

    public string GetLabel(EndingVar variable) => variable switch
    {
        EndingVar.Reputation => "평판",
        EndingVar.CorpA => "기업A 관계",
        EndingVar.CorpB => "기업B 관계",
        EndingVar.Sync => "동기화",
        EndingVar.Ethics => ethicsEfficiency >= 0 ? "윤리 성향" : "효율 성향",
        _ => "?"
    };
}

public enum EndingVar
{
    Reputation,
    CorpA,
    CorpB,
    Sync,
    Ethics
}
