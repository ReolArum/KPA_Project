using UnityEngine;

/// <summary>
/// 탐사 이벤트 노드 마커.
/// 씬의 이벤트 오브젝트(함정, 상호작용, 탈출구 등)에 부착합니다.
/// nodeId를 ExplorationStageData의 ExplorationNodeData.nodeId와 동일하게 설정하면,
/// 런타임에 이 오브젝트의 위치가 해당 노드의 좌표로 자동 반영됩니다.
/// </summary>
public class ExplorationNodeMarker : MonoBehaviour {
    [Tooltip("ExplorationStageData의 nodeId와 정확히 일치해야 합니다.")]
    public string nodeId;

    [Header("씬 뷰 미리보기 (Gizmo)")]
    [Tooltip("단서 자동 획득 범위 (씬 뷰 표시용). 실제 값은 StageData 우선.")]
    public float clueRange = 2.0f;

    [Tooltip("상호작용 프롬프트 표시 범위 (씬 뷰 표시용). 실제 값은 StageData 우선.")]
    public float interactionRange = 1.5f;

    [Tooltip("이 노드의 이벤트 타입 (씬 뷰 색상 구분용).")]
    public ExplorationEventType eventType = ExplorationEventType.None;

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        // 타입별 색상
        Color nodeColor = GetGizmoColor();

        // 기본 구체
        Gizmos.color = nodeColor;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // 라벨
        string label = string.IsNullOrEmpty(nodeId) ? "(nodeId 미설정)" : nodeId;
        UnityEditor.Handles.color = nodeColor;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.0f,
            label,
            new GUIStyle {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = nodeColor },
                alignment = TextAnchor.MiddleCenter
            }
        );
    }

    private void OnDrawGizmosSelected() {
        // 선택 시 범위 원 표시
        Vector3 pos = transform.position;

        // 파란색: clueRange
        UnityEditor.Handles.color = new Color(0.3f, 0.6f, 1f, 0.3f);
        UnityEditor.Handles.DrawSolidDisc(pos, Vector3.up, clueRange);
        UnityEditor.Handles.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        UnityEditor.Handles.DrawWireDisc(pos, Vector3.up, clueRange);

        // 노란색: interactionRange
        UnityEditor.Handles.color = new Color(1f, 0.9f, 0.2f, 0.2f);
        UnityEditor.Handles.DrawSolidDisc(pos, Vector3.up, interactionRange);
        UnityEditor.Handles.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        UnityEditor.Handles.DrawWireDisc(pos, Vector3.up, interactionRange);
    }

    private Color GetGizmoColor() {
        switch (eventType) {
            case ExplorationEventType.Clue: return Color.magenta;
            case ExplorationEventType.Hazard: return Color.red;
            case ExplorationEventType.Exit: return Color.cyan;
            case ExplorationEventType.Reward: return Color.yellow;
            case ExplorationEventType.Interactive: return new Color(1f, 0.5f, 0f); // 주황
            default: return Color.white;
        }
    }
#endif
}
