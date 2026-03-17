using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExplorationState
{
    public float remainingTime;       // 남은 시간 (초)
    public int   remainingChoices;    // 남은 선택 횟수 (-1이면 무제한)
    public int   collectedGold;       // 이번 탐사에서 얻은 골드
    
    // 획득한 특수 오브젝트/단서 ID 리스트
    public List<string> foundObjectIds = new List<string>();

    // 현재 플레이어의 위치 (노드 인덱스 또는 좌표)
    public Vector3 currentPosition;
    public int     currentNodeIndex = 0;

    // 경로 계획 리스트 (플레이어가 드래그한 좌표들)
    public List<Vector3> plannedPath = new List<Vector3>();

    public ExplorationPhase phase = ExplorationPhase.Planning;

    public void Reset(float startTime, int maxChoices)
    {
        remainingTime    = startTime;
        remainingChoices = maxChoices;
        collectedGold    = 0;
        foundObjectIds.Clear();
        plannedPath.Clear();
        currentNodeIndex = 0;
        phase = ExplorationPhase.Planning;
    }
}
