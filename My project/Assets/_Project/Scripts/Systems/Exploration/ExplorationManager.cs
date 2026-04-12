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
    public float timeScale = 1.0f; 

    [Header("Character Setup")]
    public Fighter fighterPrefab; 
    public Fighter CurrentFighter { get; private set; } // [MOD] 외부에서 접근 가능하게 변경
    private Transform playerTransform;

    [Header("Current Session")]
    public ExplorationStageData stageData;
    public ExplorationState currentState = new ExplorationState();

    [Header("Drawing Settings")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public float drawThreshold = 0.5f; // 점 사이의 최소 거리

#if ENABLE_INPUT_SYSTEM
    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;
    private InputAction clickAction;
    private InputAction rightClickAction;
    private InputAction pointAction;
#endif

    private NavMeshAgent agent; // [ADD] 물리 기반 이동을 위한 에이전트
    private Coroutine movementCoroutine;
    private bool isDrawing = false;
    private Vector3 lastAddedPoint;
    private bool _isPointerOverUI = false; // [FIX] Input System 콜백에서 IsPointerOverGameObject 사용 불가 대응

    // [ADD] 이동형 적 관리
    private List<ExplorationEnemyController> activeEnemies = new List<ExplorationEnemyController>();
    private List<ExplorationEnemyMarker> enemyMarkers = new List<ExplorationEnemyMarker>();

    // [ADD] 씬 마커 기반 위치 오버라이드 (ScriptableObject 원본 보호)
    private Dictionary<string, Vector3> nodePositionOverrides = new Dictionary<string, Vector3>();
    private Vector3? startPositionOverride;
    private Vector3 lastScanPosition; // [ADD] 최적화: 마지막으로 노드 스캔을 수행한 위치

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

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) return;

        clickAction = playerInput.actions["Attack"]; // Player Map의 Attack (Left Click)
        rightClickAction = playerInput.actions["RightClick"]; // UI Map의 RightClick
        pointAction = playerInput.actions["Point"]; // UI Map의 Point (Mouse Position)

        // 클릭 이벤트 구독
        clickAction.started += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
        
        // 우클릭(Undo) 이벤트 구독
        rightClickAction.performed += OnRightClickPerformed;
    }

    private void OnDisable()
    {
        if (clickAction != null)
        {
            clickAction.started -= OnClickStarted;
            clickAction.canceled -= OnClickCanceled;
        }
        if (rightClickAction != null) rightClickAction.performed -= OnRightClickPerformed;
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (currentState.phase == ExplorationPhase.Planning) StartDrawing();
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        isDrawing = false;
    }

    private void OnRightClickPerformed(InputAction.CallbackContext context)
    {
        if (currentState.phase == ExplorationPhase.Planning) UndoLastSegment();
    }

#endif

    public void TriggerEvent(DialogueNodeData node)
    {
        if (node.eventType == ExplorationEventType.None) return;
        
        SetPhase(ExplorationPhase.EventProcessing);

        // [MOD] 모든 이벤트는 VN UI를 통해 보여짐 (범용 DialogueStep 사용)
        List<DialogueStep> steps = node.vnSequence;

        // 대화 데이터가 아예 없는 경우, 기본 안내용 대화 1단계 생성
        if (steps == null || steps.Count == 0)
        {
            steps = new List<DialogueStep>
            {
                new DialogueStep 
                { 
                    characterName = "시스템", 
                    dialogueText = $"{node.nodeName ?? node.nodeId}에 도착했습니다." 
                }
            };
        }

        GameEvents.RaiseExplorationVNStarted(steps, node, (ExplorationChoiceType choiceType) => {
            // [MOD] 선택지 타입에 따른 노드 완료 처리
            // Escape가 아닌 선택(Combat, Interactive 등): 무조건 노드 삭제
            // Escape 또는 선택지 없음(None): isOneTime일 때만 삭제
            if (choiceType != ExplorationChoiceType.Escape)
            {
                currentState.triggeredNodeIds.Add(node.nodeId);
                RemoveEnemy(node.nodeId); // [ADD] 처리된 적 오브젝트 제거
            }
            else if (node.isOneTime)
            {
                currentState.triggeredNodeIds.Add(node.nodeId);
                RemoveEnemy(node.nodeId);
            }
            else
            {
                // [ADD] Escape 시 적의 감지 플래그 리셋 (다시 순찰 시작)
                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && enemy.linkedNodeId == node.nodeId)
                        enemy.ResetDetection();
                }
            }

            // 탈출(Exit)의 경우 VN 종료 후 결과창 진입
            if (node.eventType == ExplorationEventType.Exit)
            {
                OnExplorationSucceeded();
                return;
            }

            // VN 종료 혹은 수집 완료 후 선택지 처리 로직
            if (node.eventType == ExplorationEventType.EnvObject && (node.choices == null || node.choices.Count == 0))
            {
                if (!currentState.foundEnvObjectIds.Contains(node.nodeId))
                {
                    currentState.foundEnvObjectIds.Add(node.nodeId);
                    GameEvents.RaiseExplorationEnvObjectFound(node.nodeName ?? node.nodeId);
                }
            }
        });
    }
    private string lastTriggeredNodeId; // [ADD] 현재 혹은 방금 상호작용한 노드 ID 수집용

    void Update()
    {
        // [OPTIMIZE] Planning 페이즈에서만 UI 호버 체크 수행
        if (currentState.phase == ExplorationPhase.Planning)
        {
            _isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (isDrawing) UpdateDrawing();
        }
    }


    private void StartDrawing()
    {
        // [FIX] 캐싱된 UI 호버 상태 사용 (Input System 콜백 호환)
        if (_isPointerOverUI) return;

        Vector3 hitPos;
        if (TryGetMouseWorldPosition(out hitPos))
        {
            isDrawing = true;
            
            // 새 세그먼트 시작
            List<Vector3> newSegment = new List<Vector3>();
            
            // [FIX] 이전 지점에서 현재 클릭 지점까지 NavMesh 최단 경로 계산
            Vector3 startPoint = GetLastPathPoint();
            
                // [OPTIMIZE] sqrMagnitude 사용으로 거리 체크 최적화
                float distSqr = (startPoint - hitPos).sqrMagnitude;
                if (distSqr > 0.01f) // 0.1m의 제곱
                {
                    var navPath = CalculateNavMeshPath(startPoint, hitPos);
                    float pathTime = CalculatePathTime(navPath, startPoint);
                    if (currentState.predictedTime + pathTime <= currentState.remainingTime)
                    {
                        newSegment.AddRange(navPath);
                        currentState.pathSegments.Add(newSegment);
                        lastAddedPoint = hitPos;
                        UpdatePredictedTime();
                        GameEvents.RaiseExplorationUpdated(currentState);
                    }
                    else
                    {
                        isDrawing = false;
                        Debug.Log("Cannot start path: Exceeds remaining time!");
                    }
                }
            else
            {
                newSegment.Add(hitPos);
                currentState.pathSegments.Add(newSegment);
                lastAddedPoint = hitPos;
                UpdatePredictedTime();
                GameEvents.RaiseExplorationUpdated(currentState);
            }
        }
    }

    private float CalculatePathTime(List<Vector3> points, Vector3 start)
    {
        float dist = 0;
        Vector3 curr = start;
        foreach (var p in points)
        {
            dist += Vector3.Distance(curr, p);
            curr = p;
        }
        return dist / moveSpeed;
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

            // [OPTIMIZE] sqrMagnitude 사용으로 드로잉 거리 체크 최적화
            float distSqr = (lastAddedPoint - hitPos).sqrMagnitude;
            float thresholdSqr = drawThreshold * drawThreshold;

            if (distSqr > thresholdSqr)
            {
                if (currentState.pathSegments.Count > 0)
                {
                    float addedDist = Mathf.Sqrt(distSqr); // 시간 계산에는 실제 거리 필요
                    float addedTime = addedDist / moveSpeed;

                    if (currentState.predictedTime + addedTime <= currentState.remainingTime)
                    {
                        currentState.pathSegments[currentState.pathSegments.Count - 1].Add(hitPos);
                        lastAddedPoint = hitPos;
                        UpdatePredictedTime(); 
                        GameEvents.RaiseExplorationUpdated(currentState);
                    }
                    else
                    {
                        isDrawing = false;
                    }
                }
            }
        }
    }

    private void UndoLastSegment()
    {
        if (currentState.pathSegments.Count > 0)
        {
            currentState.pathSegments.RemoveAt(currentState.pathSegments.Count - 1);
            UpdatePredictedTime(); // [ADD] 예상 시간 갱신
            GameEvents.RaiseExplorationUpdated(currentState);
            GameEvents.RaiseActionResult("마지막 경로 구간을 취소했습니다.");
        }
    }

    private bool TryGetMouseWorldPosition(out Vector3 position)
    {
        position = Vector3.zero;
        
        Vector2 mousePos;
        if (pointAction == null) return false;
        mousePos = pointAction.ReadValue<Vector2>();

        var cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            // [FIX] NavMesh 표면에 스냅 — 걸을 수 있는 영역 위에만 그려짐
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 2.0f, NavMesh.AllAreas))
            {
                position = navHit.position;
                // position.y += 0.15f; // [REMOVE] 이동 데이터는 바닥에 붙여야 덝그덕거림이 사라짐
                return true;
            }
            // NavMesh 범위 밖이면 그리기 거부
            return false;
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

    private void UpdatePredictedTime()
    {
        float totalDist = 0;
        Vector3 current = currentState.currentPosition;

        foreach (var segment in currentState.pathSegments)
        {
            totalDist += CalculatePathTime(segment, current) * moveSpeed;
            if (segment.Count > 0) current = segment[segment.Count - 1];
        }

        // 공식: 시간 = 거리 / 속도
        currentState.predictedTime = totalDist / moveSpeed;
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
                p.y += 0.15f; // [FIX] 최단 경로 점들도 높게 띄움
                points.Add(p);
            }
        }
        else
        {
            // 경로를 못 찾으면 직선으로 (스냅된 위치 사용)
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
        
        // [FIX] GameManager가 없을 때(씬 단독 테스트 등) 발생하던 NullReferenceException 방어
        if (GameManager.Instance != null)
        {
            ExplorationSceneData.SetupExploration(GameManager.Instance.State);
        }
        else
        {
            Debug.LogWarning("[Exploration] GameManager not found. Running in standalone mode (Data persistence disabled).");
        }

        // [ADD] 씬 마커 스캔 — 기획자가 배치한 오브젝트 위치를 자동으로 읽어옴
        ScanSceneMarkers();

        Vector3 spawnPos = startPositionOverride ?? data.startPosition;
        currentState.Reset(data.limitTime, data.maxEnemyTickets, spawnPos);
        lastScanPosition = spawnPos; // 스캔 위치 초기화
        
        SyncVisualPosition();
        
        // [FIX] 캐릭터 스폰 및 입력 맵 명시적 활성화 (UI 맵에 잠겨있는 경우 대비)
        SpawnFighter(spawnPos);
        if (playerInput != null) playerInput.SwitchCurrentActionMap("Player");

        GameEvents.RaiseExplorationStarted(data, currentState); // [ADD] UI 시작 이벤트
        SetPhase(ExplorationPhase.Planning);
    }

    private void SpawnFighter(Vector3 position)
    {
        if (fighterPrefab == null)
        {
            Debug.LogError("Fighter Prefab is not assigned in ExplorationManager!");
            return;
        }

        if (CurrentFighter != null) Destroy(CurrentFighter.gameObject);

        CurrentFighter = Instantiate(fighterPrefab, position, Quaternion.identity);
        playerTransform = CurrentFighter.transform;

        // [FIX] 전역 변수 agent에 할당 (지역 변수 var 삭제)
        agent = CurrentFighter.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(position); 
            agent.speed = moveSpeed;
            agent.acceleration = 60f;      // [FIX] 가속도를 대폭 높여 덝그덕거림 방지
            agent.angularSpeed = 720f;     // [FIX] 회전을 시원시원하게 변경
            agent.stoppingDistance = 0.1f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // [FIX] 명칭 수정
        }

        // [ADD] 카메라 컨트롤러 연결: 소환된 캐릭터를 추적 대상으로 지정
        var camCtrl = Object.FindAnyObjectByType<ExplorationCameraController>();
        if (camCtrl != null)
        {
            camCtrl.SetTarget(playerTransform);
            camCtrl.WarpToTarget(); // 소환된 위치로 카메라 즉시 이동
        }
    }

    /// <summary>
    /// 씬에 배치된 ExplorationStartMarker / ExplorationNodeMarker를 탐색하여
    /// 위치 정보를 딕셔너리에 캐싱합니다. ScriptableObject 원본은 수정하지 않습니다.
    /// </summary>
    private void ScanSceneMarkers()
    {
        // 시작 마커
        var startMarker = Object.FindAnyObjectByType<ExplorationStartMarker>();
        startPositionOverride = (startMarker != null) ? (Vector3?)startMarker.transform.position : null;
        if (startMarker != null)
            Debug.Log($"[MarkerScan] 시작 마커 감지: {startMarker.transform.position}");

        // 노드 마커
        nodePositionOverrides.Clear();
        var nodeMarkers = Object.FindObjectsByType<ExplorationNodeMarker>(FindObjectsInactive.Exclude);
        foreach (var marker in nodeMarkers)
        {
            if (string.IsNullOrEmpty(marker.nodeId))
            {
                Debug.LogWarning($"[MarkerScan] nodeId가 비어있는 마커: {marker.gameObject.name}");
                continue;
            }
            nodePositionOverrides[marker.nodeId] = marker.transform.position;
            Debug.Log($"[MarkerScan] 노드 마커 감지: {marker.nodeId} → {marker.transform.position}");
        }
        Debug.Log($"[MarkerScan] 총 {nodePositionOverrides.Count}개 노드 마커, 시작 마커 {(startMarker != null ? "있음" : "없음 (fallback 사용)")}");

        // [ADD] 적 마커 스캔
        enemyMarkers.Clear();
        var scannedEnemyMarkers = Object.FindObjectsByType<ExplorationEnemyMarker>(FindObjectsInactive.Exclude);
        enemyMarkers.AddRange(scannedEnemyMarkers);
        Debug.Log($"[MarkerScan] 적 마커 {enemyMarkers.Count}개 감지");
    }

    /// <summary>
    /// 노드의 실제 사용 위치를 반환합니다.
    /// 씬 마커가 있으면 마커 위치, 없으면 ScriptableObject의 worldPosition을 사용합니다.
    /// </summary>
    public Vector3 GetNodePosition(DialogueNodeData node)
    {
        if (nodePositionOverrides.TryGetValue(node.nodeId, out var pos))
            return pos;
        return node.worldPosition;
    }

    private void SetPhase(ExplorationPhase nextPhase)
    {
        currentState.phase = nextPhase;
        GameEvents.RaiseExplorationPhaseChanged(nextPhase);
        Debug.Log($"Exploration Phase Changed: {nextPhase}");

        // [ADD] 이동형 적 페이즈 연동
        bool shouldPause = (nextPhase != ExplorationPhase.Moving);
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.SetPaused(shouldPause);
        }
    }

    // ====================================================
    //  Phase: Planning (경로 설정)
    // ====================================================
    
    public void AddWaypoint(Vector3 worldPos)
    {
        if (currentState.phase != ExplorationPhase.Planning) return;
        
        currentState.plannedPath.Add(worldPos);
    }

    public void ClearPath()
    {
        if (currentState.phase != ExplorationPhase.Planning) return;
        currentState.pathSegments.Clear();
        currentState.plannedPath.Clear();
        currentState.predictedTime = 0;
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

        // [FIX] 확정된 경로는 plannedPath로 넘어갔으므로 드래그 세그먼트는 비움
        // 이렇게 해야 다음 드로잉 시 이전 경로가 섞이지 않음
        currentState.pathSegments.Clear();

        SetPhase(ExplorationPhase.Moving);
        SpawnEnemies(); // [ADD] 이동 시작 시 적 스폰
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MovementRoutine());
    }

    // ====================================================
    //  이동형 적 관리
    // ====================================================

    /// <summary>
    /// 씨에 배치된 EnemyMarker를 기반으로 적 오브젝트를 스폰합니다.
    /// </summary>
    private void SpawnEnemies()
    {
        // 기존 적 제거
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();

        foreach (var marker in enemyMarkers)
        {
            if (marker == null) continue;
            // 이미 처리된 적은 스폰하지 않음
            if (currentState.triggeredNodeIds.Contains(marker.linkedNodeId)) continue;

            // 새 게임오브젝트 생성 (NavMeshAgent 필요)
            var enemyGO = new GameObject($"Enemy_{marker.linkedNodeId}");
            enemyGO.transform.position = marker.transform.position;

            // [ADD] 임시 시각 표시용 빨간 큐브 (나중에 적 프리팹으로 교체)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(enemyGO.transform);
            visual.transform.localPosition = Vector3.up * 0.5f;
            visual.transform.localScale = Vector3.one * 0.8f;
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.red;
            }
            // 큐브의 콜라이더 제거 (NavMeshAgent 이동 방해 방지)
            var col = visual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // NavMeshAgent 추가
            var navAgent = enemyGO.AddComponent<NavMeshAgent>();
            navAgent.height = 1.8f;
            navAgent.radius = 0.3f;

            // EnemyController 추가 및 초기화
            var controller = enemyGO.AddComponent<ExplorationEnemyController>();
            controller.linkedNodeId = marker.linkedNodeId;

            // 웨이포인트 좌표 추출
            Vector3[] wpPositions;
            if (marker.waypoints != null && marker.waypoints.Length > 0)
            {
                var posList = new List<Vector3>();
                foreach (var wp in marker.waypoints)
                {
                    if (wp != null) posList.Add(wp.position);
                }
                wpPositions = posList.Count > 0 ? posList.ToArray() : new Vector3[] { marker.transform.position };
            }
            else
            {
                wpPositions = new Vector3[] { marker.transform.position };
            }

            controller.Initialize(wpPositions, marker.patrolSpeed, marker.detectRange, marker.waitTimeAtPoint);
            controller.StartPatrol();
            activeEnemies.Add(controller);

            Debug.Log($"[Enemy] 스폰: {marker.linkedNodeId} at {marker.transform.position}");
        }
    }

    /// <summary>
    /// 이동형 적이 플레이어를 감지했을 때 호출됩니다.
    /// </summary>
    public void OnEnemyDetectedPlayer(string nodeId)
    {
        // StageData에서 해당 노드 찾기
        DialogueNodeData targetNode = null;
        foreach (var node in stageData.nodes)
        {
            if (node.nodeId == nodeId)
            {
                targetNode = node;
                break;
            }
        }

        if (targetNode != null)
        {
            // 이동 중단 + 이벤트 발동
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            Debug.Log($"[Enemy] 플레이어 감지! {nodeId}");
            TriggerEvent(targetNode);
        }
        else
        {
            Debug.LogWarning($"[Enemy] StageData에 nodeId '{nodeId}'가 없습니다.");
        }
    }

    /// <summary>
    /// Combat으로 처리된 적을 맵에서 제거합니다.
    /// </summary>
    public void RemoveEnemy(string nodeId)
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null && activeEnemies[i].linkedNodeId == nodeId)
            {
                Destroy(activeEnemies[i].gameObject);
                activeEnemies.RemoveAt(i);
                Debug.Log($"[Enemy] 제거: {nodeId}");
            }
        }
    }

    // ====================================================
    //  Phase: Moving (자동 이동)
    // ====================================================

    private IEnumerator MovementRoutine()
    {
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent not found on player!");
            yield break;
        }

        while (currentState.plannedPath.Count > 0)
        {
            Vector3 target = currentState.plannedPath[0];
            agent.SetDestination(target);
            agent.isStopped = false;

            // 목적지에 도달할 때까지 대기
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                // 이벤트 처리 중이면 이동/시간소모 일시정지
                if (currentState.phase != ExplorationPhase.Moving)
                {
                    agent.isStopped = true;
                    yield return new WaitUntil(() => currentState.phase == ExplorationPhase.Moving || currentState.phase == ExplorationPhase.Result);
                    if (currentState.phase == ExplorationPhase.Result) 
                    {
                        agent.isStopped = true;
                        yield break;
                    }
                    agent.isStopped = false;
                    agent.SetDestination(target); // 목적지 재설정 (혹시 몰라서)
                }

                // 이동 시 시간 소모 (Agent의 실시간 속도 반영 가능하지만 여기선 단순화)
                ConsumeTime(Time.deltaTime * timeScale);
                
                // 현재 위치 갱신 (에이전트가 이동시킨 좌표 반영)
                currentState.currentPosition = playerTransform.position;
                GameEvents.RaiseExplorationUpdated(currentState);

                // [FIX] 비주얼 동기화 및 조건부 노드 스캔 (최적화)
                SyncVisualPosition();

                if (currentState.remainingTime <= 0)
                {
                    agent.isStopped = true;
                    OnExplorationFailed("시간 초과!");
                    yield break;
                }

                yield return null;
            }

            // 노드 도달 (경로 점 도달 시)
            currentState.plannedPath.RemoveAt(0);
        }

        agent.isStopped = true;

        // 모든 경로 점을 소모한 후
        if (currentState.phase != ExplorationPhase.Result)
        {
            // [FIX] 이동 종료 시 잔여 드래그 데이터가 남아있지 않도록 정리
            currentState.pathSegments.Clear();
            SetPhase(ExplorationPhase.Planning);
        }
    }

    private void SyncVisualPosition()
    {
        // [MOD] 이제 NavMeshAgent가 위치를 직접 제어하므로,
        // playerTransform -> currentState.currentPosition 방향으로 좌표를 역동기화합니다.
        if (playerTransform != null)
        {
            currentState.currentPosition = playerTransform.position;
        }

        // [OPTIMIZE] 마지막 스캔 위치로부터 일정 거리(0.2m) 이상 이동했을 때만 노드 스캔 수행
        // [OPTIMIZE] 마지막 스캔 위치로부터 일정 거리(0.2m) 이상 이동했을 때만 노드 스캔 수행
        float scanThresholdSqr = 0.2f * 0.2f;
        if ((lastScanPosition - currentState.currentPosition).sqrMagnitude > scanThresholdSqr)
        {
            lastScanPosition = currentState.currentPosition;
            ScanNearbyNodes();
        }
    }

    private void ScanNearbyNodes()
    {
        DialogueNodeData currentTriggerNode = null;

        foreach (var node in stageData.nodes)
        {
            // 1. 이미 처리된 노드는 스킵 (선택지 결과에 따라 기록됨)
            if (currentState.triggeredNodeIds.Contains(node.nodeId)) continue;

            Vector3 nodePos = GetNodePosition(node);
            float distSqr = (currentState.currentPosition - nodePos).sqrMagnitude;
            
            // [ADD] 진입 조건(Requirements) 체크: 미충족 시 무반응 통과
            if (!ExplorationEventProcessor.Instance.CheckRequirementsExternal(node.requirements))
            {
                continue;
            }
            
            float interactionRangeSqr = node.interactionRange * node.interactionRange;
            float envObjectRangeSqr = node.envObjectRange * node.envObjectRange;

            // 2. 현재 어떤 노드 범위 안에 있는지 체크
            bool inInteraction = (node.eventType != ExplorationEventType.None && node.eventType != ExplorationEventType.EnvObject && distSqr <= interactionRangeSqr);
            bool inEnvObject = (node.eventType == ExplorationEventType.EnvObject && distSqr <= envObjectRangeSqr);

            if (inInteraction || inEnvObject)
            {
                // 방금 발동했던 노드 안에 아직 있다면 스킵 (재발동 방어)
                if (node.nodeId == lastTriggeredNodeId) 
                {
                    currentTriggerNode = node; // 여전히 이 노드 범위 내임
                    continue; 
                }

                // 3. 발동! (노드 완료 처리는 VN 종료 콜백에서 선택지 타입에 따라 수행)
                lastTriggeredNodeId = node.nodeId;
                
                if (inEnvObject)
                {
                    // [MOD] 환경 오브젝트 예외 처리: VN 데이터가 있을 때만 연출 발생
                    if (node.vnSequence != null && node.vnSequence.Count > 0)
                    {
                        Debug.Log($"[Exploration] 환경 오브젝트 발견 (연출 포함): {node.nodeId}");
                        TriggerEvent(node);
                        return; // 이동 일시 정지
                    }
                    else
                    {
                        // VN 데이터가 없으면 기존처럼 자동 수집 + 즉시 완료 처리
                        currentState.triggeredNodeIds.Add(node.nodeId); // [FIX] 반복 수집 방지
                        currentState.foundEnvObjectIds.Add(node.nodeId);
                        GameEvents.RaiseExplorationEnvObjectFound(node.nodeName ?? node.nodeId);
                        Debug.Log($"[Exploration] 환경 오브젝트 자동 수집: {node.nodeId}");
                    }
                }
                else if (node.eventType == ExplorationEventType.Exit)
                {
                    // [ADD] 탈출 시 전용 대사가 있다면 VN 호출, 없다면 즉시 성공 처리
                    Debug.Log($"[Exploration] 탈출 지점 도달: {node.nodeId}");
                    TriggerEvent(node);
                    return;
                }
                else
                {
                    Debug.Log($"[Exploration] 이벤트 발동: {node.nodeId} ({node.eventType})");
                    TriggerEvent(node);
                    return; // 이벤트 발동 시 즉시 스캔 중단
                }
            }
        }

        // 4. 추적 중인 노드 범위를 완전히 벗어났다면 ID 초기화 (재발동 가능하게 함)
        if (currentTriggerNode == null)
        {
            lastTriggeredNodeId = null;
        }
    }

    private void ConsumeTime(float amount)
    {
        currentState.remainingTime = Mathf.Max(0, currentState.remainingTime - amount);
        GameEvents.RaiseExplorationUpdated(currentState); // [FIX] HUD에 시간 갱신 알림
    }

    // ====================================================
    //  Phase: Event (위험 조우)
    // ====================================================


    public void ResumeMovement(bool shouldRedraw = false)
    {
        if (currentState.phase == ExplorationPhase.EventProcessing)
        {
            if (shouldRedraw)
            {
                // [FIX] 경로 재작성 시 기존 이동 코루틴을 확실히 중지
                if (movementCoroutine != null)
                {
                    StopCoroutine(movementCoroutine);
                    movementCoroutine = null;
                }

                currentState.plannedPath.Clear();
                currentState.pathSegments.Clear();
                SetPhase(ExplorationPhase.Planning);
                GameEvents.RaiseExplorationUpdated(currentState);
            }
            else
            {
                // [FIX] 기존 경로 유지: 페이지만 Moving으로 변경하면 MovementRoutine이 이어서 진행됨
                SetPhase(ExplorationPhase.Moving);
                if (movementCoroutine == null && currentState.plannedPath.Count > 0)
                {
                    movementCoroutine = StartCoroutine(MovementRoutine());
                }
            }
        }
    }

    // ====================================================
    //  Termination
    // ====================================================

    public void OnExplorationSucceeded()
    {
        SetPhase(ExplorationPhase.Result);
        
        // [FIX] GameManager가 빌드 런타임 외 씬 단독 테스트 시 없을 수 있음
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager instance not found. Exploration results will not be saved.");
            return;
        }

        // GameState에 결과 반영
        var state = GameManager.Instance.State;
        state.AddGold(currentState.collectedGold);
        
        foreach (var id in currentState.foundEnvObjectIds)
        {
            if (!state.explorationFoundKeys.Contains(id))
                state.explorationFoundKeys.Add(id);
        }

        GameEvents.RaiseActionResult($"탐사 성공! {currentState.collectedGold}G 획득");
        state.lastExplorationStatus = "Success";
        ExplorationSceneData.CompleteExploration();
        SaveSystem.Save(state);
    }

    public void OnExplorationFailed(string reason)
    {
        SetPhase(ExplorationPhase.Result);
        GameEvents.RaiseActionResult($"탐사 실패: {reason}");
        ExplorationSceneData.CompleteExploration();
        
        if (GameManager.Instance != null)
        {
            // [FIX] 실패 시 데이터는 초기화하되, '실패했다는 기록'은 남김
            ExplorationSceneData.RestoreBackup();
            
            var state = GameManager.Instance.State;
            state.lastExplorationStatus = "Failed";
            state.nightCompleted = true; // 밤 활동 종료 처리
            
            SaveSystem.Save(state);
        }
    }

    public void ExitExploration()
    {
        // 메인 씬으로 복귀 로직
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_MainGame");
    }
}
