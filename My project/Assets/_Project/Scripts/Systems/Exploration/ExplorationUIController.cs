using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ExplorationUIController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text textTime;
    [SerializeField] private TMP_Text textChoices;
    [SerializeField] private TMP_Text textGold;
    [SerializeField] private Button btnConfirmPath; // [ADD] 경로 확정 버튼

    [Header("Event Popup")]
    [SerializeField] private GameObject panelEvent;
    [SerializeField] private TMP_Text textEventTitle;
    [SerializeField] private TMP_Text textEventDesc;
    [SerializeField] private Transform choiceButtonRoot;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Result Screen")]
    [SerializeField] private GameObject panelResult;
    [SerializeField] private TMP_Text textResultStatus;
    [SerializeField] private TMP_Text textResultSummary;
    [SerializeField] private Button btnExit;

    [Header("Cameras")]
    [SerializeField] private Camera camTop;
    [SerializeField] private Camera camQuarter;
    [SerializeField] private float camTransitionDuration = 1.0f;

    [Header("Path Visuals (Prototype)")]
    [SerializeField] private LineRenderer pathRenderer;

    [Header("Node Visualization")]
    [SerializeField] private Image nodeIconPrefab; // [ADD] 노드 아이콘 프리팹 (UI용)
    [SerializeField] private Transform nodeContainer;  // [ADD] 아이콘들이 담길 부모 오브젝트
    [SerializeField] private Color interactiveHighlightColor = Color.yellow;

    void OnEnable()
    {
        GameEvents.OnExplorationStarted += HandleExplorationStarted;
        GameEvents.OnExplorationUpdated += HandleExplorationUpdated;
        GameEvents.OnExplorationEventTriggered += HandleEventTriggered;
        GameEvents.OnExplorationPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        GameEvents.OnExplorationStarted -= HandleExplorationStarted;
        GameEvents.OnExplorationUpdated -= HandleExplorationUpdated;
        GameEvents.OnExplorationEventTriggered -= HandleEventTriggered;
        GameEvents.OnExplorationPhaseChanged -= HandlePhaseChanged;
    }

    private void Start()
    {
        if (panelEvent) panelEvent.SetActive(false);
        if (panelResult) panelResult.SetActive(false);
        if (btnExit) btnExit.onClick.AddListener(() => ExplorationManager.Instance.ExitExploration());
        
        if (btnConfirmPath) 
        {
            btnConfirmPath.onClick.AddListener(() => {
                ExplorationManager.Instance.ConfirmPath();
                btnConfirmPath.gameObject.SetActive(false);
            });
        }

        // 초기 카메라 설정 (Planning 단계이므로 Top-view)
        if (camTop && camQuarter)
        {
            camTop.enabled = true;
            camQuarter.enabled = false;
        }
    }

    void Update()
    {
        // 실시간 업데이트 (UI 프리팹이 붙어있다면 매 프레임 ExplorationManager 데이터를 읽음)
        if (ExplorationManager.Instance != null && ExplorationManager.Instance.currentState.phase != ExplorationPhase.Result)
        {
            UpdateHUD(ExplorationManager.Instance.currentState);
            UpdatePathVisuals(ExplorationManager.Instance.currentState);
        }
    }

    private void UpdateHUD(ExplorationState state)
    {
        if (textTime)
        {
            int min = (int)state.remainingTime / 60;
            int sec = (int)state.remainingTime % 60;
            textTime.text = $"{min:00}:{sec:00}";
        }
        
        if (textChoices)
        {
            string count = state.remainingChoices == -1 ? "무제한" : state.remainingChoices.ToString();
            textChoices.text = $"선택권: {count}";
        }

        if (textGold) textGold.text = $"{state.collectedGold} G";
    }

    private void UpdatePathVisuals(ExplorationState state)
    {
        if (pathRenderer == null) return;

        List<Vector3> allPoints = new List<Vector3>();
        allPoints.Add(state.currentPosition);

        // 모든 세그먼트를 순서대로 합쳐서 시각화
        foreach (var segment in state.pathSegments)
        {
            allPoints.AddRange(segment);
        }

        // 드로잉 중인 현재 실시간 경로도 포함 (필요 시)
        // 위 로직은 이미 ExplorationManager에서 실시간으로 세그먼트에 점을 추가하므로 연동됨

        pathRenderer.positionCount = allPoints.Count;
        pathRenderer.SetPositions(allPoints.ToArray());
    }

    private void HandleExplorationStarted(ExplorationStageData data, ExplorationState state)
    {
        if (panelResult) panelResult.SetActive(false);
        UpdateHUD(state);
        SpawnNodeIcons(data); // [ADD] 노드 아이콘 생성
        HighlightInStageObjects(data); // [ADD] 3D 오브젝트 강조
    }

    private void HighlightInStageObjects(ExplorationStageData data)
    {
        // 프로토타입용: 월드상의 노드 위치 근처에 있는 기즈모나 머티리얼 강조
        // 실제로는 노드 아이디별로 매핑된 게임 오브젝트를 찾아야 함
        foreach (var node in data.nodes)
        {
            // 월드 좌표 기준 반경 1.0m 내의 Renderer들을 찾아 강조 색상 적용
            Collider[] cols = Physics.OverlapSphere(node.worldPosition, 1.0f);
            foreach (var col in cols)
            {
                var rend = col.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", interactiveHighlightColor * 0.5f);
                }
            }
        }
    }

    private void SpawnNodeIcons(ExplorationStageData data)
    {
        if (nodeIconPrefab == null || nodeContainer == null) return;

        // 기존 아이콘 제거
        foreach (Transform child in nodeContainer)
            Destroy(child.gameObject);

        foreach (var node in data.nodes)
        {
            var icon = Instantiate(nodeIconPrefab, nodeContainer);
            
            // 월드 좌표를 스크린/UI 좌표로 변환 (간단화를 위해 가정한 방식)
            // 실제 구현에서는 맵의 앵커와 피벗에 따라 보정이 필요할 수 있습니다.
            icon.transform.localPosition = node.worldPosition; 
            
            // 툴팁이나 이름을 아이콘에 표시할 수도 있습니다.
            var label = icon.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = node.nodeId;
            
            // 이벤트 타입에 따라 색상 변경 등
            icon.color = node.eventType == ExplorationEventType.Exit ? Color.green : Color.white;
        }
    }

    private void HandleExplorationUpdated(ExplorationState state)
    {
        UpdateHUD(state);
    }

    private void HandleEventTriggered(ExplorationNodeData node, List<ExplorationChoiceData> visibleChoices)
    {
        if (panelEvent == null) return;

        textEventTitle.text = node.eventType.ToString();
        textEventDesc.text = $"[위험 조우] {node.nodeId}에 도착했습니다. 어떻게 하시겠습니까?";

        // 기존 버튼 제거
        foreach (Transform child in choiceButtonRoot)
            Destroy(child.gameObject);

        // 새 버튼 생성
        foreach (var choice in visibleChoices)
        {
            var btn = Instantiate(choiceButtonPrefab, choiceButtonRoot);
            btn.GetComponentInChildren<TMP_Text>().text = $"{choice.label} ({choice.timePenalty}s)";
            btn.onClick.AddListener(() => {
                ExplorationEventProcessor.Instance.ApplyChoiceEffect(choice);
                panelEvent.SetActive(false);
            });
        }

        panelEvent.SetActive(true);
    }

    private void HandlePhaseChanged(ExplorationPhase phase)
    {
        if (phase == ExplorationPhase.Planning)
        {
            if (btnConfirmPath) btnConfirmPath.gameObject.SetActive(true);
            StartCoroutine(TransitionCamera(true)); // Top View
        }
        else if (phase == ExplorationPhase.Moving)
        {
            StartCoroutine(TransitionCamera(false)); // Quarter View
        }
        else if (phase == ExplorationPhase.Result)
        {
            ShowResult(ExplorationManager.Instance.currentState);
        }
    }

    private System.Collections.IEnumerator TransitionCamera(bool toTop)
    {
        if (!camTop || !camQuarter) yield break;

        Camera from = toTop ? camQuarter : camTop;
        Camera to = toTop ? camTop : camQuarter;

        // "전환 시작" 시 두 카메라의 파라미터를 보간하기 위해 
        // 하나의 메인 카메라 시점을 옮기는 방식이 아닌, 두 카메라의 활성화를 제어함.
        // 부드러운 전환을 위해 'to' 카메라의 상태를 'from' 카메라의 현재 상태에서 시작하게 함 (혹은 전용 블렌딩 카메라 사용)
        
        // 여기서는 간단하게 toTop 여부에 따라 카메라 활성화 전환만 우선 수행하고,
        // 실제 줌/회전 느낌을 주기 위해 FOV나 거리를 조절하는 연출을 추가할 수 있습니다.
        
        float elapsed = 0;
        Vector3 startPos = from.transform.position;
        Quaternion startRot = from.transform.rotation;
        float startFOV = from.fieldOfView;

        // [연출용] 실제 한 카메라를 부드럽게 옮기는 로직 (메인 카메라가 camTop 혹은 camQuarter를 따라감)
        while (elapsed < camTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / camTransitionDuration;
            t = Mathf.SmoothStep(0, 1, t);

            // 메인 카메라가 있다면 해당 카메라를 움직이거나, 두 카메라의 가중치로 연출
            // 이 예시에서는 to 카메라를 활성화하고 시점을 부드럽게 전환하는 시뮬레이션을 수행
            elapsed += Time.deltaTime;
            yield return null;
        }

        camTop.enabled = toTop;
        camQuarter.enabled = !toTop;
    }

    private void ShowResult(ExplorationState state)
    {
        if (panelResult == null) return;

        bool success = state.remainingTime > 0;
        textResultStatus.text = success ? "탐사 성공!" : "탐사 실패 (시간 초과)";
        textResultSummary.text = $"획득 골드: {state.collectedGold}G\n발견한 오브젝트: {state.foundObjectIds.Count}개";
        
        panelResult.SetActive(true);
    }
}
