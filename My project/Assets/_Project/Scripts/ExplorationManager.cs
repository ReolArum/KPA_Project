using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance { get; private set; }

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float timeScale = 1.0f; // 시간 소모 배율

    [Header("Current Session")]
    public ExplorationStageData stageData;
    public ExplorationState currentState = new ExplorationState();

    private Coroutine movementCoroutine;

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
#if ENABLE_INPUT_SYSTEM
        // New Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            // 2D 평면(Z=0)과의 교점 계산
            float distance = -Camera.main.transform.position.z / ray.direction.z;
            Vector3 worldPos = ray.GetPoint(distance);
            worldPos.z = 0;

            AddWaypoint(worldPos);
            GameEvents.RaiseExplorationUpdated(currentState);
        }
#else
        // Legacy Input (혹은 Both 설정인 경우 대비)
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; 
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;

            AddWaypoint(worldPos);
            GameEvents.RaiseExplorationUpdated(currentState);
        }
#endif
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
        currentState.plannedPath.Clear();
    }

    public void ConfirmPath()
    {
        if (currentState.phase != ExplorationPhase.Planning) return;
        if (currentState.plannedPath.Count == 0) return;

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
    }

    public void OnExplorationFailed(string reason)
    {
        SetPhase(ExplorationPhase.Result);
        GameEvents.RaiseActionResult($"탐사 실패: {reason}");
    }

    public void ExitExploration()
    {
        // 메인 씬으로 복귀 로직
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}
