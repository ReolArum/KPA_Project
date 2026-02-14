using System;
using System.Collections.Generic;
using UnityEngine;

public enum EndingVar
{
    Reputation,
    CorpA,
    CorpB,
    Sync,
    Ethics
}

[Serializable]
public class EndingVariables
{
    public Dictionary<EndingVar, int> values = new();

    public EndingVariables()
    {
        foreach (EndingVar v in Enum.GetValues(typeof(EndingVar)))
            values[v] = 0;
    }

    public int Get(EndingVar v) => values.ContainsKey(v) ? values[v] : 0;

    public void Modify(EndingVar v, int amount)
    {
        if (!values.ContainsKey(v)) values[v] = 0;
        values[v] = Mathf.Clamp(values[v] + amount, -100, 100);
    }

    public string GetLabel(EndingVar v) => v switch
    {
        EndingVar.Reputation => "평판",
        EndingVar.CorpA => "기업A 관계",
        EndingVar.CorpB => "기업B 관계",
        EndingVar.Sync => "동기화",
        EndingVar.Ethics => values.ContainsKey(v) && values[v] >= 0 ? "윤리 성향" : "효율 성향",
        _ => "?"
    };

    // 하위 호환용 프로퍼티
    public int reputation { get => Get(EndingVar.Reputation); set => values[EndingVar.Reputation] = value; }
    public int corpARelation { get => Get(EndingVar.CorpA); set => values[EndingVar.CorpA] = value; }
    public int corpBRelation { get => Get(EndingVar.CorpB); set => values[EndingVar.CorpB] = value; }
    public int synchronization { get => Get(EndingVar.Sync); set => values[EndingVar.Sync] = value; }
    public int ethicsEfficiency { get => Get(EndingVar.Ethics); set => values[EndingVar.Ethics] = value; }
}
