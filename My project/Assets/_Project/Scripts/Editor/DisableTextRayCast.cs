using TMPro;
using UnityEditor;
using UnityEngine;

public class DisableTextRaycast
{
    [MenuItem("Tools/TMP Raycast Target 전체 해제")]
    static void DisableAll()
    {
        var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var t in texts)
        {
            if (t.raycastTarget)
            {
                t.raycastTarget = false;
                EditorUtility.SetDirty(t);
                count++;
            }
        }

        Debug.Log($"Raycast Target 해제 완료: {count}개");
    }
}
