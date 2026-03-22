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

    [Header("Path Visuals (Prototype)")]
    [SerializeField] private LineRenderer pathRenderer;

    [Header("Node Visualization")]
    [SerializeField] private Image nodeIconPrefab; // [ADD] 노드 아이콘 프리팹 (UI용)
    [SerializeField] private Transform nodeContainer;  // [ADD] 아이콘들이 담길 부모 오브젝트

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
        if (btnConfirmPath) btnConfirmPath.onClick.AddListener(() => {
            ExplorationManager.Instance.ConfirmPath();
            btnConfirmPath.gameObject.SetActive(false);
        });
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

        List<Vector3> points = new List<Vector3>();
        points.Add(state.currentPosition);
        points.AddRange(state.plannedPath);

        pathRenderer.positionCount = points.Count;
        pathRenderer.SetPositions(points.ToArray());
    }

    private void HandleExplorationStarted(ExplorationStageData data, ExplorationState state)
    {
        if (panelResult) panelResult.SetActive(false);
        UpdateHUD(state);
        SpawnNodeIcons(data); // [ADD] 노드 아이콘 생성
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
        }
        else if (phase == ExplorationPhase.Result)
        {
            ShowResult(ExplorationManager.Instance.currentState);
        }
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
