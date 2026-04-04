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

        // 1. 시작 지점 (START_)
        if (name.StartsWith("START_"))
        {
            SetupStartMarker(t);
        }
        // 2. 단서 아이템 (CLUE_)
        else if (name.StartsWith("CLUE_"))
        {
            SetupNodeMarker(t, ExplorationEventType.Clue);
        }
        // 3. 함정 (TRAP_)
        else if (name.StartsWith("TRAP_"))
        {
            SetupNodeMarker(t, ExplorationEventType.Hazard);
        }
        // 3. 상호작용 오브젝트 (OBJ_)
        else if (name.StartsWith("OBJ_"))
        {
            SetupNodeMarker(t, ExplorationEventType.Interactive);
        }
        // 4. 보상박스 (RW_ 또는 REWARD_)
        else if (name.StartsWith("RW_") || name.StartsWith("REWARD_"))
        {
            SetupNodeMarker(t, ExplorationEventType.Reward);
        }
        // 5. 탈출구 (EXIT_)
        else if (name.StartsWith("EXIT_"))
        {
            SetupNodeMarker(t, ExplorationEventType.Exit);
        }
        // 6. 경로 가이드 (PATH_)
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

    private void SetupStartMarker(Transform t)
    {
        // ExplorationStartMarker 자동 부착
        if (t.GetComponent<ExplorationStartMarker>() == null)
        {
            t.gameObject.AddComponent<ExplorationStartMarker>();
        }
        Debug.Log($"[ExplorationSetup] 시작 마커 설정: {t.name}");
    }

    private void SetupNodeMarker(Transform t, ExplorationEventType type)
    {
        // ExplorationNodeMarker 자동 부착
        var marker = t.GetComponent<ExplorationNodeMarker>();
        if (marker == null)
        {
            marker = t.gameObject.AddComponent<ExplorationNodeMarker>();
        }
        marker.nodeId = t.name;  // 오브젝트 이름 = nodeId
        marker.eventType = type;

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

        Debug.Log($"[ExplorationSetup] 노드 마커 설정: {t.name} → {type}");
    }
}

