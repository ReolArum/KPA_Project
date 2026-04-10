using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExplorationMapGenerator : MonoBehaviour
{
    [Header("Settings")]
    public Material mapMaterial;
    public float yOffset = 0.05f; // 지형과의 Z-Fighting 방지용
    public string mapGameObjectName = "Exploration_Blueprint_Map";

    [ContextMenu("Generate Map from NavMesh")]
    public GameObject GenerateMap()
    {
        // 1. 기존 생성된 맵이 있다면 제거
        Transform existingMap = transform.Find(mapGameObjectName);
        if (existingMap != null)
        {
            if (Application.isPlaying) Destroy(existingMap.gameObject);
            else DestroyImmediate(existingMap.gameObject);
        }

        // 2. NavMesh 데이터 추출 (Bake된 데이터 기준)
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices.Length == 0)
        {
            Debug.LogError("[ExplorationMapGenerator] NavMesh data not found! Please Bake NavMesh first.");
            return null;
        }

        // 3. 메시 생성
        Mesh mesh = new Mesh();
        mesh.name = "NavMesh_Map_Mesh";
        
        // 정점 위치를 아주 약간 위로 올림
        Vector3[] vertices = new Vector3[triangulation.vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = triangulation.vertices[i] + Vector3.up * yOffset;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangulation.indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 4. 오브젝트 생성 및 컴포넌트 설정
        GameObject mapObj = new GameObject(mapGameObjectName);
        mapObj.transform.SetParent(this.transform);
        mapObj.transform.localPosition = Vector3.zero;
        mapObj.transform.localRotation = Quaternion.identity;

        MeshFilter mf = mapObj.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = mapObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mapMaterial != null ? mapMaterial : CreateDefaultMaterial();

        // 5. 충돌체 추가 (Raycast용)
        mapObj.AddComponent<MeshCollider>().sharedMesh = mesh;

        // 6. 레이어 설정
        mapObj.layer = gameObject.layer; // 부모와 동일한 레이어 사용

        Debug.Log($"[ExplorationMapGenerator] Map Generated: {vertices.Length} vertices, {triangulation.indices.Length / 3} triangles.");
        return mapObj;
    }

    private Material CreateDefaultMaterial()
    {
        // 기본 머티리얼 (반투명 푸른색)
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 0.5f, 1.0f, 0.5f);
        
        // 투명 설정
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        return mat;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ExplorationMapGenerator))]
public class ExplorationMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ExplorationMapGenerator generator = (ExplorationMapGenerator)target;
        if (GUILayout.Button("Generate Blueprint Map"))
        {
            generator.GenerateMap();
        }
    }
}
#endif
