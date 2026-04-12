using UnityEngine;

/// <summary>
/// 이동형 적 순찰 마커.
/// 씬에 배치하여 적의 시작 위치와 순찰 경로(웨이포인트)를 시각적으로 설정합니다.
/// linkedNodeId를 ExplorationStageData의 DialogueNodeData.nodeId와 동일하게 설정하면,
/// 해당 노드의 이벤트가 적 감지 시 발동합니다.
/// </summary>
public class ExplorationEnemyMarker : MonoBehaviour
{
    [Tooltip("ExplorationStageData의 nodes 중 일치하는 nodeId")]
    public string linkedNodeId;

    [Header("순찰 설정")]
    [Tooltip("순찰 경로점 (Transform 리스트). 비어있으면 제자리에서 대기합니다.")]
    public Transform[] waypoints;

    [Tooltip("순찰 이동 속도")]
    public float patrolSpeed = 2f;

    [Tooltip("플레이어 감지 반경")]
    public float detectRange = 3f;

    [Tooltip("경로점 도착 후 대기 시간 (초)")]
    public float waitTimeAtPoint = 1f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 적 위치: 빨간 큐브로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);

        // 라벨
        string label = string.IsNullOrEmpty(linkedNodeId) ? "(nodeId 미설정)" : $"적: {linkedNodeId}";
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.2f,
            label,
            new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red },
                alignment = TextAnchor.MiddleCenter
            }
        );

        // 순찰 경로 선 그리기
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Vector3 prev = transform.position;
            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                Gizmos.DrawLine(prev, wp.position);
                Gizmos.DrawSphere(wp.position, 0.2f);
                prev = wp.position;
            }
            // 마지막 → 처음으로 돌아오는 루프 표시
            if (waypoints[0] != null)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
                Gizmos.DrawLine(prev, waypoints[0].position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 선택 시 감지 범위 원 표시
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.15f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, detectRange);
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.8f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, detectRange);
    }
#endif
}
