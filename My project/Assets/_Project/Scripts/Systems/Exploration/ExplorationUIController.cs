using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ExplorationUIController : MonoBehaviour
{
    [Header("HUD Layer")]
    [SerializeField] private TMP_Text textTime;
    [SerializeField] private TMP_Text textTickets; // textChoices에서 변경
    [SerializeField] private TMP_Text textGold;
    [SerializeField] private TMP_Text textPredictedTime; 
    [SerializeField] private TMP_Text textActionResult;  
    [SerializeField] private Button btnConfirmPath; 
    [SerializeField] private float actionResultDuration = 2.5f; 

    [Header("Node Visualization")]
    [SerializeField] private Image nodeIconPrefab; 
    [SerializeField] private Transform nodeIconContainer; // nodeContainer에서 변경
    [SerializeField] private Color interactiveHighlightColor = Color.yellow;

    [Header("Overlay VN Layer")]
    [SerializeField] private GameObject vnLayer; // panelVN에서 변경
    [SerializeField] private TMP_Text textName; // textVNName에서 변경
    [SerializeField] private TMP_Text textDialogue; // textVNDialogue에서 변경
    [SerializeField] private Image portLeft; // imgVNLeft에서 변경
    [SerializeField] private Image portRight; // imgVNRight에서 변경
    [SerializeField] private Image imgVNBG; // imgVNBackground에서 변경
    [SerializeField] private Button dialogueBox; // btnVNDialogueBox에서 변경
    [SerializeField] private Transform choiceButtonRoot;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Result Layer")]
    [SerializeField] private GameObject resultLayer; // panelResult에서 변경
    [SerializeField] private TMP_Text textStatus; // textResultStatus에서 변경
    [SerializeField] private TMP_Text textSummary; // textResultSummary에서 변경
    [SerializeField] private Button btnExit;

    [Header("Scene Setup (Technical)")]
    [SerializeField] private Camera camTop;
    [SerializeField] private Camera camQuarter;
    [SerializeField] private LineRenderer pathRenderer;
    [SerializeField] private GameObject mapVisualRoot; // 실제 고퀄리티 맵
    [SerializeField] private GameObject mapTechnicalRoot; // 기술 지도 (NavMesh)
    [SerializeField] private bool showPathDuringMoving = true;

    [Header("Optional Modules")]
    [SerializeField] private GameObject findingsLayer; // panelEnvObjectList에서 변경
    [SerializeField] private Transform  findingsContent; // envObjectListContent에서 변경
    [SerializeField] private GameObject findingsItemPrefab; // envObjectItemPrefab에서 변경
    [SerializeField] private Button     btnToggleFindings; // btnToggleEnvObjectList에서 변경
    
    private List<DialogueStep>  currentVNSteps;
    private int                 currentVNIndex;
    private System.Action<ExplorationChoiceType> onVNComplete;
    private DialogueNodeData    curEventNode; 
    private List<DialogueChoiceData> curChoices;
    private Dictionary<string, List<Renderer>> nodeRendererCache = new Dictionary<string, List<Renderer>>();
    private Image playerIconInstance; // Planning 단계에서 캐릭터 대신 표시될 아이콘

    void OnEnable()
    {
        GameEvents.OnExplorationStarted += HandleExplorationStarted;
        GameEvents.OnExplorationUpdated += HandleExplorationUpdated;
        GameEvents.OnExplorationEnvObjectFound += HandleEnvObjectFound;
        GameEvents.OnExplorationVNStarted += HandleVNStarted;
        GameEvents.OnExplorationPhaseChanged += HandlePhaseChanged;
        GameEvents.OnActionResult += HandleActionResult; 
    }

    void OnDisable()
    {
        GameEvents.OnExplorationStarted -= HandleExplorationStarted;
        GameEvents.OnExplorationUpdated -= HandleExplorationUpdated;
        GameEvents.OnExplorationEnvObjectFound -= HandleEnvObjectFound;
        GameEvents.OnExplorationVNStarted -= HandleVNStarted;
        GameEvents.OnExplorationPhaseChanged -= HandlePhaseChanged;
        GameEvents.OnActionResult -= HandleActionResult;
    }

    private void Start()
    {
        if (resultLayer) resultLayer.SetActive(false);
        if (vnLayer)     vnLayer.SetActive(false);
        if (textActionResult) textActionResult.gameObject.SetActive(false);
        
        if (btnExit) btnExit.onClick.AddListener(() => ExplorationManager.Instance.ExitExploration());
        
        if (btnConfirmPath) 
        {
            btnConfirmPath.onClick.AddListener(() => {
                ExplorationManager.Instance.ConfirmPath();
                btnConfirmPath.gameObject.SetActive(false);
            });
        }

        if (btnToggleFindings && findingsLayer)
        {
            btnToggleFindings.onClick.AddListener(() => findingsLayer.SetActive(!findingsLayer.activeSelf));
        }

        if (findingsLayer) findingsLayer.SetActive(false);

        // [ADD] 대화창 클릭 시 다음 대사로 넘어가도록 자동 연결
        if (dialogueBox)
        {
            dialogueBox.onClick.RemoveAllListeners(); // 중복 방지
            dialogueBox.onClick.AddListener(OnVNClick);
        }

        if (camTop) camTop.enabled = true;
        if (camQuarter) camQuarter.enabled = false;
    }

    private void HandleExplorationUpdated(ExplorationState state)
    {
        UpdateHUD(state);
        UpdatePathVisuals(state);
    }

    private void UpdateHUD(ExplorationState state)
    {
        if (textTime)
        {
            int min = (int)state.remainingTime / 60;
            int sec = (int)state.remainingTime % 60;
            textTime.text = $"{min:00}:{sec:00}";
        }
        
        if (textTickets)
        {
            string count = state.remainingEnemyTickets == -1 ? "무제한" : state.remainingEnemyTickets.ToString();
            textTickets.text = $"적 전용 선택권: {count}";
        }

        if (textGold) textGold.text = $"{state.collectedGold} G";

        if (textPredictedTime)
        {
            int pMin = (int)state.predictedTime / 60;
            int pSec = (int)state.predictedTime % 60;
            textPredictedTime.text = $"예상: {pMin:00}:{pSec:00}";
            textPredictedTime.gameObject.SetActive(state.phase == ExplorationPhase.Planning);
        }
    }

    private void HandleVNStarted(List<DialogueStep> steps, DialogueNodeData node, System.Action<ExplorationChoiceType> onComplete)
    {
        currentVNSteps = steps;
        currentVNIndex = 0;
        this.onVNComplete = onComplete;
        this.curEventNode = node;
        
        if (vnLayer)
        {
            vnLayer.SetActive(true);
            if (dialogueBox != null) dialogueBox.interactable = true;
            
            // [FIX] 이전 노드의 이미지 잔상 제거 (배경은 step에서 관리)
            if (portLeft) { portLeft.sprite = null; portLeft.gameObject.SetActive(false); }
            if (portRight) { portRight.sprite = null; portRight.gameObject.SetActive(false); }
            
            ShowVNStep(0);
        }
        else
        {
            onComplete?.Invoke(ExplorationChoiceType.None);
        }
    }

    private void ShowVNStep(int index)
    {
        if (index < 0 || index >= currentVNSteps.Count) return;

        var step = currentVNSteps[index];
        if (textName) textName.text = step.characterName;
        if (textDialogue) textDialogue.text = step.dialogueText;
        
        if (portLeft) 
        {
            portLeft.sprite = step.leftSprite;
            portLeft.gameObject.SetActive(step.leftSprite != null);
        }
        if (portRight) 
        {
            portRight.sprite = step.rightSprite;
            portRight.gameObject.SetActive(step.rightSprite != null);
        }
        if (imgVNBG)
        {
            if (step.backgroundOverride != null)
            {
                imgVNBG.sprite = step.backgroundOverride;
                imgVNBG.gameObject.SetActive(true);
            }
            else
            {
                imgVNBG.sprite = null;
                imgVNBG.gameObject.SetActive(false);
            }
        }
    }

    public void OnVNClick() 
    {
        currentVNIndex++;
        if (currentVNIndex >= currentVNSteps.Count)
        {
            bool hasChoices = curEventNode != null && curEventNode.choices.Count > 0;

            if (hasChoices)
            {
                ProcessAndShowChoices();
            }
            else
            {
                vnLayer.SetActive(false);
                onVNComplete?.Invoke(ExplorationChoiceType.None); // [MOD] 일반 종료 시 None 전달
                curEventNode = null;
            }
        }
        else
        {
            ShowVNStep(currentVNIndex);
        }
    }

    private void ProcessAndShowChoices()
    {
        if (curEventNode == null || ExplorationEventProcessor.Instance == null) return;

        curChoices = ExplorationEventProcessor.Instance.FilterChoices(curEventNode.choices);
        Debug.Log($"[Choice Debug] Found {curEventNode.choices.Count} raw choices, {curChoices.Count} filtered choices.");
        
        foreach (Transform child in choiceButtonRoot)
            Destroy(child.gameObject);

        foreach (var choice in curChoices)
        {
            var btn = Instantiate(choiceButtonPrefab, choiceButtonRoot);
            var textComp = btn.GetComponentInChildren<TMP_Text>();
            if (textComp != null)
            {
                textComp.text = $"{choice.label}";
                textComp.color = Color.black; // [FIX] 글자가 보이지 않는 문제 해결을 위해 검은색 강제 적용
            }
            
            btn.onClick.AddListener(() => {
                // [FIX] 선택 즉시 모든 버튼 제거
                foreach (Transform child in choiceButtonRoot)
                    Destroy(child.gameObject);

                ExplorationEventProcessor.Instance.ApplyChoiceEffect(choice);
                vnLayer.SetActive(false); 
                onVNComplete?.Invoke(choice.type); // [MOD] 선택한 타입 전달
                curEventNode = null;
            });
        }
        
        // [ADD] 레이아웃 즉시 강제 갱신 (직사각형 형태가 즉시 잡히도록 함)
        Canvas.ForceUpdateCanvases();
        if (choiceButtonRoot is RectTransform rect)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
        
        if (dialogueBox != null) dialogueBox.interactable = false;
    }

    private Coroutine actionResultCoroutine;
    private void HandleActionResult(string msg)
    {
        if (textActionResult == null) return;
        if (actionResultCoroutine != null) StopCoroutine(actionResultCoroutine);
        actionResultCoroutine = StartCoroutine(ShowActionResultRoutine(msg));
    }

    private System.Collections.IEnumerator ShowActionResultRoutine(string msg)
    {
        textActionResult.text = msg;
        textActionResult.gameObject.SetActive(true);
        yield return new WaitForSeconds(actionResultDuration);
        textActionResult.gameObject.SetActive(false);
        actionResultCoroutine = null;
    }

    private void HandleEnvObjectFound(string objId)
    {
        GameEvents.RaiseActionResult($"환경 오브젝트 발견: {objId}");
        RefreshEnvObjectList();
    }

    private void RefreshEnvObjectList()
    {
        if (findingsContent == null || findingsItemPrefab == null) return;

        foreach (Transform child in findingsContent)
            Destroy(child.gameObject);

        var foundIds = ExplorationManager.Instance.currentState.foundEnvObjectIds;
        foreach (var id in foundIds)
        {
            var item = Instantiate(findingsItemPrefab, findingsContent);
            var label = item.GetComponentInChildren<TMP_Text>();
            if (label) label.text = id;
        }
    }

    private void UpdatePathVisuals(ExplorationState state)
    {
        if (pathRenderer == null) return;
        pathRenderer.useWorldSpace = true;

        List<Vector3> allPoints = new List<Vector3>();
        foreach (var segment in state.pathSegments)
        {
            allPoints.AddRange(segment);
        }

        pathRenderer.positionCount = allPoints.Count;
        pathRenderer.SetPositions(allPoints.ToArray());
    }

    private void HandleExplorationStarted(ExplorationStageData data, ExplorationState state)
    {
        if (resultLayer) resultLayer.SetActive(false);
        UpdateHUD(state);
        SpawnNodeIcons(data); 
        CacheNodeRenderers(data); 
        HighlightInStageObjects(data); 
    }

    private void CacheNodeRenderers(ExplorationStageData data)
    {
        nodeRendererCache.Clear();
        foreach (var node in data.nodes)
        {
            List<Renderer> renderers = new List<Renderer>();
            Collider[] cols = Physics.OverlapSphere(node.worldPosition, 1.2f); 
            foreach (var col in cols)
            {
                var rend = col.GetComponent<Renderer>();
                if (rend != null) renderers.Add(rend);
            }
            nodeRendererCache[node.nodeId] = renderers;
        }
    }

    private void HighlightInStageObjects(ExplorationStageData data)
    {
        foreach (var node in data.nodes)
        {
            if (nodeRendererCache.TryGetValue(node.nodeId, out var renderers))
            {
                foreach (var rend in renderers)
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", interactiveHighlightColor * 0.5f);
                }
            }
        }
    }

    private void SpawnNodeIcons(ExplorationStageData data)
    {
        if (nodeIconPrefab == null || nodeIconContainer == null) return;
        currentStageData = data;

        foreach (Transform child in nodeIconContainer)
            Destroy(child.gameObject);
        
        nodeIconMap.Clear();

        foreach (var node in data.nodes)
        {
            if (node.isInfoRequired && GameManager.Instance != null)
            {
                if (!GameManager.Instance.State.revealedStageNodeIds.Contains(node.nodeId))
                    continue;
            }

            var icon = Instantiate(nodeIconPrefab, nodeIconContainer);
            var label = icon.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = node.nodeName ?? node.nodeId;
            
            icon.color = node.eventType == ExplorationEventType.Exit ? Color.green : Color.white;
            
            nodeIconMap[node] = icon; // 실시간 갱신을 위해 등록
        }
        
        // 생성 직후 첫 위치 갱신
        UpdateNodeIconPositions();
    }

    private Dictionary<DialogueNodeData, Image> nodeIconMap = new Dictionary<DialogueNodeData, Image>();
    private ExplorationStageData currentStageData;

    private void Update()
    {
        // [ADD] Planning 단계에서만 노드 아이콘들의 위치를 실시간으로 업데이트
        if (ExplorationManager.Instance != null && 
            ExplorationManager.Instance.currentState.phase == ExplorationPhase.Planning &&
            currentStageData != null)
        {
            UpdateNodeIconPositions();
        }
    }

    private void UpdateNodeIconPositions()
    {
        var cam = camTop != null && camTop.enabled ? camTop : (camQuarter != null ? camQuarter : Camera.main);
        if (cam == null) return;

        foreach (var pair in nodeIconMap)
        {
            var node = pair.Key;
            var icon = pair.Value;
            
            // ExplorationManager를 통해 보정된(마커 위치) 좌표 가져오기
            Vector3 worldPos = ExplorationManager.Instance.GetNodePosition(node);
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                icon.gameObject.SetActive(false);
            }
            else
            {
                icon.gameObject.SetActive(true);
                icon.transform.position = screenPos;
            }
        }
    }

    private void HandlePhaseChanged(ExplorationPhase phase)
    {
        // [ADD] 페어즈에 따른 아이콘 컨테이너 가시성 제어
        if (nodeIconContainer != null)
        {
            nodeIconContainer.gameObject.SetActive(phase == ExplorationPhase.Planning);
        }

        if (phase == ExplorationPhase.Planning)
        {
            if (btnConfirmPath) btnConfirmPath.gameObject.SetActive(true);
            StartCoroutine(TransitionCamera(true)); 
            
            // [FIX] Planning 진입 시 선 렌더러 항상 활성화 (가시성 확보)
            if (pathRenderer) pathRenderer.enabled = true;
            
            // [MOD] 렌더러만 숨겨서 물리(Raycast)는 작동하게 함
            if (mapVisualRoot) SetRenderersActive(mapVisualRoot, false);
            if (mapTechnicalRoot) SetRenderersActive(mapTechnicalRoot, true);
            
            var fighter = ExplorationManager.Instance.CurrentFighter;
            if (fighter) SetRenderersActive(fighter.gameObject, false);
            
            UpdatePlayerIcon(true);
        }
        else if (phase == ExplorationPhase.Moving)
        {
            StartCoroutine(TransitionCamera(false)); 
            
            // [MOD] 렌더러 복구
            if (mapVisualRoot) SetRenderersActive(mapVisualRoot, true);
            if (mapTechnicalRoot) SetRenderersActive(mapTechnicalRoot, false);
            
            var fighter = ExplorationManager.Instance.CurrentFighter;
            if (fighter) SetRenderersActive(fighter.gameObject, true);
            
            if (pathRenderer) pathRenderer.enabled = showPathDuringMoving;
            
            UpdatePlayerIcon(false);
        }
        else if (phase == ExplorationPhase.Result)
        {
            ShowResult(ExplorationManager.Instance.currentState);
        }
    }

    // [ADD] 오브젝트의 모든 렌더러만 토글하는 헬퍼 메서드
    private void SetRenderersActive(GameObject root, bool active)
    {
        if (root == null) return;
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = active;
        }
    }

    private void UpdatePlayerIcon(bool show)
    {
        if (!show)
        {
            if (playerIconInstance) playerIconInstance.gameObject.SetActive(false);
            return;
        }

        if (nodeIconPrefab == null || nodeIconContainer == null) return;
        
        if (playerIconInstance == null)
            playerIconInstance = Instantiate(nodeIconPrefab, nodeIconContainer);
        
        playerIconInstance.gameObject.SetActive(true);
        playerIconInstance.color = Color.cyan; // 플레이어는 하늘색으로 구분
        
        var fighter = ExplorationManager.Instance.CurrentFighter;
        var cam = camTop != null && camTop.enabled ? camTop : (camQuarter != null ? camQuarter : Camera.main);
        
        if (fighter && cam)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(fighter.transform.position);
            playerIconInstance.transform.position = screenPos;
            
            var label = playerIconInstance.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "현재 위치";
        }
    }

    private System.Collections.IEnumerator TransitionCamera(bool toTop)
    {
        if (!camTop || !camQuarter) yield break;
        camTop.enabled = toTop;
        camQuarter.enabled = !toTop;

        // [ADD] 쿼터뷰 카메라 추적 토글: 이동 페이즈(Moving)일 때만 활성화
        var camCtrl = camQuarter.GetComponent<ExplorationCameraController>();
        if (camCtrl != null)
        {
            camCtrl.SetFollowing(!toTop); 
            if (!toTop) camCtrl.WarpToTarget(); // 이동 시작 시 카메라 위치 초기화
        }

        yield return null;
    }

    private void ShowResult(ExplorationState state)
    {
        if (resultLayer == null) return;

        bool success = state.remainingTime > 0;
        textStatus.text = success ? "<color=green>탐사 성공!</color>" : "<color=red>탐사 실패</color>";
        
        int min = (int)state.remainingTime / 60;
        int sec = (int)state.remainingTime % 60;
        string timeStr = success ? $"\n남은 시간: {min:00}:{sec:00}" : "";

        textSummary.text = $"획득 골드: {state.collectedGold}G{timeStr}\n\n[수집한 항목]";
        
        if (state.foundEnvObjectIds.Count > 0)
        {
            foreach (var id in state.foundEnvObjectIds)
                textSummary.text += $"\n- {id}";
        }
        else
        {
            textSummary.text += "\n없음";
        }
        
        resultLayer.SetActive(true);
    }
}
