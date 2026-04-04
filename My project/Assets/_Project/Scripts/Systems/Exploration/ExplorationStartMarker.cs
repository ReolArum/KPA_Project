using UnityEngine;

/// <summary>
/// 탐사 시작 지점 마커.
/// 씬에 빈 GameObject를 만들고 이 컴포넌트를 붙이면,
/// ExplorationManager가 런타임에 이 위치를 캐릭터 스폰 지점으로 사용합니다.
/// 씬 당 1개만 배치하세요.
/// </summary>
public class ExplorationStartMarker : MonoBehaviour {
#if UNITY_EDITOR
    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.8f);

        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.2f,
            "▶ START",
            new GUIStyle {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green },
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
#endif
}
