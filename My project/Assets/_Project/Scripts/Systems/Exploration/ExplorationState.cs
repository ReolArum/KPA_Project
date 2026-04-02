using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExplorationState
{
    public float remainingTime;       // 남은 시간 (초)
    public int   remainingChoices;    // 남은 선택 횟수 (-1이면 무제한)
    public int   collectedGold;       // 이번 탐사에서 얻은 골드
    public Vector3 currentPosition;   // [ADD] 현재 플레이어 위치
    
    // 획득한 특수 오브젝트/단서 ID 리스트
    public List<string> foundObjectIds = new List<string>();

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
        plannedPath.Clear();
        pathSegments.Clear(); // [ADD] 드래그 세그먼트 데이터 초기화
        phase = ExplorationPhase.Planning;
    }
}
