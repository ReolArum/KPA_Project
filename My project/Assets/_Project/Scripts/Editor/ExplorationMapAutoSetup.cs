using UnityEngine;
using UnityEditor;

public class ExplorationMapAutoSetup : AssetPostprocessor
{
    // 모델 임포트 후 호출되는 훅
    void OnPostprocessModel(GameObject g)
    {
        // 이름에 따라 태그/레이어/컴포넌트 자동 설정
        ProcessObjectRecursive(g.transform);
    }

    private void ProcessObjectRecursive(Transform t)
    {
        string name = t.name.ToUpper();

        // 1. 함정 (TRAP_)
        if (name.StartsWith("TRAP_"))
        {
            SetupNode(t, ExplorationEventType.Hazard);
        }
        // 2. 상호작용 오브젝트 (OBJ_)
        else if (name.StartsWith("OBJ_"))
        {
            SetupNode(t, ExplorationEventType.Interactive);
        }
        // 3. 보상박스 (RW_ 또는 REWARD_)
        else if (name.StartsWith("RW_") || name.StartsWith("REWARD_"))
        {
            SetupNode(t, ExplorationEventType.Reward);
        }
        // 4. 탈출구 (EXIT_)
        else if (name.StartsWith("EXIT_"))
        {
            SetupNode(t, ExplorationEventType.Exit);
        }
        // 5. 경로 가이드 (PATH_)
        else if (name.StartsWith("PATH_"))
        {
            // 경로 노드는 시각적 요소일 가능성이 높으므로 별도 컴포넌트 없이 위치만 활용할 수 있음
        }

        // 자식 오브젝트도 순회
        foreach (Transform child in t)
        {
            ProcessObjectRecursive(child);
        }
    }

    private void SetupNode(Transform t, ExplorationEventType type)
    {
        // 런타임에 쓰일 수 있는 식별용 태그나 레이어 설정 (필요 시)
        // t.gameObject.layer = LayerMask.NameToLayer("ExplorationNode");

        // 콜라이더가 없으면 자동 생성 (메시 기반)
        if (t.GetComponent<Collider>() == null)
        {
            if (t.GetComponent<MeshFilter>() != null)
                t.gameObject.AddComponent<MeshCollider>().convex = true;
            else
                t.gameObject.AddComponent<BoxCollider>();
        }

        // 트리거로 설정 (자동 이동 중 접촉 감지용)
        var col = t.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // 나중에 ExplorationManager에서 찾기 편하게 특정 컴포넌트나 태그 부여
        Debug.Log($"[ExplorationSetup] Configured {t.name} as {type}");
    }
}
