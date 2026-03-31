using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.AI;

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance { get; private set; }

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float timeScale = 1.0f; // 시간 소모 배율

    [Header("Current Session")]
    public ExplorationStageData stageData;
    public ExplorationState currentState = new ExplorationState();

    [Header("Drawing Settings")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public float drawThreshold = 0.5f; // 점 사이의 최소 거리

    private Coroutine movementCoroutine;
    private bool isDrawing = false;
    private Vector3 lastAddedPoint;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 씬 진입 시 자동으로 탐사 시작 (테스트용 혹은 GameManager 연동용)
        if (stageData != null)
        {
            StartExploration(stageData);
        }
    }

    void Update()
    {
        if (currentState.phase == ExplorationPhase.Planning)
        {
            HandlePlanningInput();
        }
    }

    private void HandlePlanningInput()
    {
        // 1. 좌클릭: 그리기 시작 및 드래그
        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            UpdateDrawing();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
        }

        // 2. 우클릭: 실행 취소 (마지막 세그먼트 삭제)
        if (Input.GetMouseButtonDown(1))
        {
            UndoLastSegment();
        }
    }

    private void StartDrawing()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 hitPos;
        if (TryGetMouseWorldPosition(out hitPos))
        {
            isDrawing = true;
            
            // 새 세그먼트 시작
            List<Vector3> newSegment = new List<Vector3>();
            
            // 이전 마지막 지점이 있다면 최단 경로로 연결
            Vector3 startPoint = GetLastPathPoint();
            if (Vector3.Distance(startPoint, hitPos) > 0.1f)
            {
                var navPath = CalculateNavMeshPath(startPoint, hitPos);
                newSegment.AddRange(navPath);
            }
            else
            {
                newSegment.Add(hitPos);
            }

            currentState.pathSegments.Add(newSegment);
            lastAddedPoint = hitPos;
            GameEvents.RaiseExplorationUpdated(currentState);
        }
    }

    private void UpdateDrawing()
    {
        Vector3 hitPos;
        if (TryGetMouseWorldPosition(out hitPos))
        {
            // 벽(Obstacle) 충돌 체크
            if (IsPathBlocked(lastAddedPoint, hitPos))
            {
                isDrawing = false; // 벽에 닿으면 그리기 중단
                Debug.Log("Path blocked by obstacle!");
                return;
            }

            // 일정 거리 이상 움직였을 때만 점 추가
            if (Vector3.Distance(lastAddedPoint, hitPos) > drawThreshold)
            {
                if (currentState.pathSegments.Count > 0)
                {
                    currentState.pathSegments[currentState.pathSegments.Count - 1].Add(hitPos);
                    lastAddedPoint = hitPos;
                    GameEvents.RaiseExplorationUpdated(currentState);
                }
            }
        }
    }

    private void UndoLastSegment()
    {
        if (currentState.pathSegments.Count > 0)
        {
            currentState.pathSegments.RemoveAt(currentState.pathSegments.Count - 1);
            GameEvents.RaiseExplorationUpdated(currentState);
            GameEvents.RaiseActionResult("마지막 경로 구간을 취소했습니다.");
        }
    }

    private bool TryGetMouseWorldPosition(out Vector3 position)
    {
        position = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            position = hit.point;
            position.y = 0; // 평면 유지
            return true;
        }
        return false;
    }

    private Vector3 GetLastPathPoint()
    {
        if (currentState.pathSegments.Count > 0)
        {
            var lastSegment = currentState.pathSegments[currentState.pathSegments.Count - 1];
            if (lastSegment.Count > 0) return lastSegment[lastSegment.Count - 1];
        }
        return currentState.currentPosition;
    }

    private List<Vector3> CalculateNavMeshPath(Vector3 start, Vector3 end)
    {
        NavMeshPath path = new NavMeshPath();
        List<Vector3> points = new List<Vector3>();
        if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
        {
            foreach (var corner in path.corners)
            {
                Vector3 p = corner;
                p.y = 0;
                points.Add(p);
            }
        }
        else
        {
            // 경로를 못 찾으면 직선으로 (혹은 에러 처리)
            points.Add(end);
        }
        return points;
    }

    private bool IsPathBlocked(Vector3 start, Vector3 end)
    {
        // 간단한 레이캐스트로 장애물 확인
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        return Physics.Raycast(start + Vector3.up * 0.5f, direction, distance, obstacleLayer);
    }

    public void StartExploration(ExplorationStageData data)
    {
        stageData = data;
        currentState.Reset(data.limitTime, data.maxChoices);
        
        GameEvents.RaiseActionResult($"탐사 시작: {data.stageName}");
        GameEvents.RaiseExplorationStarted(data, currentState); // [ADD] UI 시작 이벤트
        SetPhase(ExplorationPhase.Planning);
    }

    private void SetPhase(ExplorationPhase nextPhase)
    {
        currentState.phase = nextPhase;
        GameEvents.RaiseExplorationPhaseChanged(nextPhase); // [ADD] 페이즈 변경 이벤트
        Debug.Log($"Exploration Phase Changed: {nextPhase}");
    }

    // ====================================================
    //  Phase: Planning (경로 설정)
    // ====================================================
    
    public void AddWaypoint(Vector3 worldPos)
    {
        if (currentState.phase != ExplorationPhase.Planning) return;
        
        currentState.plannedPath.Add(worldPos);
        // 예상 소모 시간 계산 UI 갱신 로직 등이 들어갈 자리
    }

    public void ClearPath()
    {
        if (currentState.phase != ExplorationPhase.Planning) return;
        currentState.pathSegments.Clear();
        currentState.plannedPath.Clear();
        GameEvents.RaiseExplorationUpdated(currentState);
    }

    public void ConfirmPath()
    {
        if (currentState.phase != ExplorationPhase.Planning) return;
        if (currentState.pathSegments.Count == 0) return;

        // 세그먼트들을 하나의 연속된 path로 병합
        currentState.plannedPath.Clear();
        foreach (var segment in currentState.pathSegments)
        {
            currentState.plannedPath.AddRange(segment);
        }

        SetPhase(ExplorationPhase.Moving);
        movementCoroutine = StartCoroutine(MovementRoutine());
    }

    // ====================================================
    //  Phase: Moving (자동 이동)
    // ====================================================

    private IEnumerator MovementRoutine()
    {
        while (currentState.plannedPath.Count > 0)
        {
            Vector3 target = currentState.plannedPath[0];
            
            while (Vector3.Distance(currentState.currentPosition, target) > 0.1f)
            {
                if (currentState.phase != ExplorationPhase.Moving) yield return null;

                float step = moveSpeed * Time.deltaTime;
                currentState.currentPosition = Vector3.MoveTowards(currentState.currentPosition, target, step);
                
                // 시간 소모
                ConsumeTime(Time.deltaTime * timeScale);
                GameEvents.RaiseExplorationUpdated(currentState); // [ADD] 이동 중 UI 갱신 (시간/위치 등)

                if (currentState.remainingTime <= 0)
                {
                    OnExplorationFailed("시간 초과!");
                    yield break;
                }

                yield return null;
            }

            // 노드 도달
            currentState.plannedPath.RemoveAt(0);
            CheckForEventAtCurrentPosition();

            // 이벤트가 발생했다면 코루틴 일시 정지 (SetPhase에서 제어)
            if (currentState.phase == ExplorationPhase.EventProcessing)
            {
                yield return new WaitUntil(() => currentState.phase == ExplorationPhase.Moving || currentState.phase == ExplorationPhase.Result);
            }
        }

        // 경로 끝 도달 시 다시 계획 단계로 (기획서에 따라 다를 수 있음)
        if (currentState.phase != ExplorationPhase.Result)
        {
            SetPhase(ExplorationPhase.Planning);
        }
    }

    private void ConsumeTime(float amount)
    {
        currentState.remainingTime = Mathf.Max(0, currentState.remainingTime - amount);
    }

    private void CheckForEventAtCurrentPosition()
    {
        // 1. 트리거 콜라이더 기반 혹은 거리 기반으로 이벤트 체크
        // 여기선 단순화를 위해 stageData의 노드 중 가장 가까운 노드 체크
        foreach (var node in stageData.nodes)
        {
            if (Vector3.Distance(currentState.currentPosition, node.worldPosition) < 0.5f)
            {
                TriggerEvent(node);
                break;
            }
        }
    }

    // ====================================================
    //  Phase: Event (위험 조우)
    // ====================================================

    public void TriggerEvent(ExplorationNodeData node)
    {
        if (node.eventType == ExplorationEventType.None) return;
        
        SetPhase(ExplorationPhase.EventProcessing);
        ExplorationEventProcessor.Instance.ProcessEvent(node);
    }

    public void ResumeMovement()
    {
        if (currentState.phase == ExplorationPhase.EventProcessing)
        {
            SetPhase(ExplorationPhase.Moving);
        }
    }

    // ====================================================
    //  Termination
    // ====================================================

    public void OnExplorationSucceeded()
    {
        SetPhase(ExplorationPhase.Result);
        
        // GameState에 결과 반영
        var state = GameManager.Instance.State;
        state.gold += currentState.collectedGold;
        state.todayGoldEarned += currentState.collectedGold;
        
        foreach (var id in currentState.foundObjectIds)
        {
            if (!state.explorationFoundKeys.Contains(id))
                state.explorationFoundKeys.Add(id);
        }

        GameEvents.RaiseActionResult($"탐사 성공! {currentState.collectedGold}G 획득");
        SaveSystem.Save(state);
    }

    public void OnExplorationFailed(string reason)
    {
        SetPhase(ExplorationPhase.Result);
        GameEvents.RaiseActionResult($"탐사 실패: {reason}");
        SaveSystem.Save(GameManager.Instance.State);
    }

    public void ExitExploration()
    {
        // 메인 씬으로 복귀 로직
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}
