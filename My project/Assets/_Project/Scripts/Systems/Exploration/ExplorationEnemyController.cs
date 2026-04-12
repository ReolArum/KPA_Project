using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 이동형 적의 런타임 순찰 동작을 제어합니다.
/// ExplorationManager가 EnemyMarker 정보를 바탕으로 이 컴포넌트를 가진 오브젝트를 스폰합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ExplorationEnemyController : MonoBehaviour
{
    /// <summary>연결된 StageData 노드 ID</summary>
    [HideInInspector] public string linkedNodeId;

    private NavMeshAgent agent;
    private Vector3[] waypoints;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float waitDuration = 1f;
    private float detectRange = 3f;
    private bool isPaused = true;
    private bool isDetected = false; // 이미 감지 이벤트를 발생시켰는지 여부

    /// <summary>
    /// ExplorationManager에서 스폰 시 호출하여 순찰 정보를 주입합니다.
    /// </summary>
    public void Initialize(Vector3[] patrolWaypoints, float speed, float detection, float waitTime)
    {
        agent = GetComponent<NavMeshAgent>();
        waypoints = patrolWaypoints;
        detectRange = detection;
        waitDuration = waitTime;

        agent.speed = speed;
        agent.acceleration = 40f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.3f;
        agent.autoBraking = true;

        // [FIX] NavMesh 위에 강제 배치
        agent.Warp(transform.position);

        // 시작 시 정지 상태 (Planning 단계)
        SetPaused(true);
    }

    /// <summary>
    /// 순찰 일시정지/재개. ExplorationManager에서 페이즈 전환 시 호출합니다.
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = paused;
        }
    }

    /// <summary>
    /// 감지 플래그 초기화. Escape 후 적이 다시 플레이어를 감지할 수 있게 합니다.
    /// </summary>
    public void ResetDetection()
    {
        isDetected = false;
        SetPaused(false); // [FIX] 감지 리셋 시 순찰 재개
    }

    private void Update()
    {
        if (isPaused || agent == null || waypoints == null || waypoints.Length == 0) return;

        // --- 플레이어 감지 ---
        if (!isDetected && ExplorationManager.Instance != null)
        {
            Vector3 playerPos = ExplorationManager.Instance.currentState.currentPosition;
            float distSqr = (transform.position - playerPos).sqrMagnitude;

            if (distSqr <= detectRange * detectRange)
            {
                isDetected = true;
                SetPaused(true);
                ExplorationManager.Instance.OnEnemyDetectedPlayer(linkedNodeId);
                return;
            }
        }

        // --- 순찰 로직 ---
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // 경로점 도착 → 잠시 대기
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                waitTimer = 0f;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypointIndex]);
            }
        }
    }

    /// <summary>
    /// 첫 순찰 목적지를 설정합니다. Initialize 후 순찰 시작 시 호출합니다.
    /// </summary>
    public void StartPatrol()
    {
        // [FIX] 1프레임 대기 후 순찰 시작 (NavMeshAgent가 NavMesh에 안착하기까지 대기)
        StartCoroutine(StartPatrolDelayed());
    }

    private System.Collections.IEnumerator StartPatrolDelayed()
    {
        yield return null; // 1프레임 대기
        if (agent != null && agent.isOnNavMesh && waypoints != null && waypoints.Length > 0)
        {
            currentWaypointIndex = 0;
            agent.SetDestination(waypoints[0]);
            SetPaused(false);
            Debug.Log($"[Enemy] 순찰 시작: {linkedNodeId}, 웨이포인트 {waypoints.Length}개");
        }
        else
        {
            Debug.LogWarning($"[Enemy] 순찰 시작 실패: {linkedNodeId}, isOnNavMesh={agent?.isOnNavMesh}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 런타임 감지 범위 표시
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.2f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, detectRange);
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, detectRange);
    }
#endif
}
