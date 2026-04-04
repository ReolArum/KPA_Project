using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ExplorationNodeMarker))]
public class ExplorationNodeMarkerEditor : Editor {
    private void OnSceneGUI() {
        var marker = (ExplorationNodeMarker)target;
        Vector3 pos = marker.transform.position;

        // clueRange 핸들 (파란)
        Handles.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        float newClueRange = Handles.RadiusHandle(
            Quaternion.identity, pos, marker.clueRange
        );
        if (newClueRange != marker.clueRange) {
            Undo.RecordObject(marker, "Change Clue Range");
            marker.clueRange = newClueRange;
        }

        // interactionRange 핸들 (노란)
        Handles.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        float newInteractRange = Handles.RadiusHandle(
            Quaternion.identity, pos, marker.interactionRange
        );
        if (newInteractRange != marker.interactionRange) {
            Undo.RecordObject(marker, "Change Interaction Range");
            marker.interactionRange = newInteractRange;
        }

        // 라벨
        Handles.Label(pos + Vector3.up * 1.5f,
            $"[{marker.eventType}] {marker.nodeId}\n" +
            $"Clue: {marker.clueRange:F1}m | Interact: {marker.interactionRange:F1}m",
            new GUIStyle {
                fontSize = 11,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
}
