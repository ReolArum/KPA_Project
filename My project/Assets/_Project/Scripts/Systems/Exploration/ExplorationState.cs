using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExplorationState
{
    public float remainingTime;       // 남은 시간 (초)
    public int   remainingChoices;    // 남은 선택 횟수 (-1이면 무제한)
    public int   collectedGold;       // 이번 탐사에서 얻은 골드
    public Vector3 currentPosition;   // [ADD] 현재 플레이어 위치
    public float predictedTime;       // [ADD] 계획된 경로의 예상 소요 시간 (초)
    
    // 획득한 특수 오브젝트/단서 ID 리스트
    public List<string> foundObjectIds = new List<string>();

    // [ADD] 이미 처리된 노드 ID (단서 수집 완료 + 이벤트 발동 완료 모두 포함)
    public HashSet<string> triggeredNodeIds = new HashSet<string>();

    // 경로 계획 리스트 (세그먼트 단위: 드래그 시 생성된 점들의 묶음들)
    public List<List<Vector3>> pathSegments = new List<List<Vector3>>();
    // 실제 이동을 위해 직렬화된 전체 경로
    public List<Vector3> plannedPath = new List<Vector3>();

    public ExplorationPhase phase = ExplorationPhase.Planning;

    public void Reset(float startTime, int maxChoices, Vector3 startPos)
    {
        remainingTime    = startTime;
        remainingChoices = maxChoices;
        collectedGold    = 0;
        currentPosition  = startPos; // [ADD] 시작 위치 초기화
        foundObjectIds.Clear();
        triggeredNodeIds.Clear(); // [ADD] 처리 노드 초기화
        plannedPath.Clear();
        pathSegments.Clear(); // [ADD] 드래그 세그먼트 데이터 초기화
        phase = ExplorationPhase.Planning;
    }
}
