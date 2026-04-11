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
    [SerializeField] private TMP_Text textPredictedTime; 
    [SerializeField] private TMP_Text textActionResult;  
    [SerializeField] private Button btnConfirmPath; 

    [Header("Event Popup (Legacy)")]
    [SerializeField] private GameObject panelEvent;
    [SerializeField] private TMP_Text textEventTitle;
    [SerializeField] private TMP_Text textEventDesc;
    [SerializeField] private float actionResultDuration = 2.5f; 
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

    [Header("Path Visuals")]
    [SerializeField] private LineRenderer pathRenderer;

    [Header("Node Visualization")]
    [SerializeField] private Image nodeIconPrefab; 
    [SerializeField] private Transform nodeContainer;  
    [SerializeField] private Color interactiveHighlightColor = Color.yellow;

    [Header("Visual Novel (VN) Mode")]
    [SerializeField] private GameObject  panelVN;
    [SerializeField] private TMP_Text    textVNName;
    [SerializeField] private TMP_Text    textVNDialogue;
    [SerializeField] private Image       imgVNLeft;
    [SerializeField] private Image       imgVNRight;
    [SerializeField] private Image       imgVNBackground;
    [SerializeField] private Button      btnVNDialogueBox; 
    
    private List<DialogueStep>  currentVNSteps;
    private int                 currentVNIndex;
    private System.Action       onVNComplete;
    private DialogueNodeData    curEventNode; 
    private List<DialogueChoiceData> curChoices;

    [Header("Interaction & Environment Objects")]
    [SerializeField] private GameObject panelInteractPrompt;
    [SerializeField] private TMP_Text   textInteractPrompt;
    [SerializeField] private GameObject panelEnvObjectList;
    [SerializeField] private Transform  envObjectListContent;
    [SerializeField] private GameObject envObjectItemPrefab;
    [SerializeField] private Button     btnToggleEnvObjectList;
    
    private Dictionary<string, List<Renderer>> nodeRendererCache = new Dictionary<string, List<Renderer>>();

    void OnEnable()
    {
        GameEvents.OnExplorationStarted += HandleExplorationStarted;
        GameEvents.OnExplorationUpdated += HandleExplorationUpdated;
        GameEvents.OnExplorationEventTriggered += HandleEventTriggered;
        GameEvents.OnExplorationPhaseChanged += HandlePhaseChanged;
        GameEvents.OnExplorationEnvObjectFound += HandleEnvObjectFound;
        GameEvents.OnExplorationVNStarted += HandleVNStarted;
        GameEvents.OnExplorationInteractionPrompt += HandleInteractionPrompt;
        GameEvents.OnActionResult += HandleActionResult; 
    }

    void OnDisable()
    {
        GameEvents.OnExplorationStarted -= HandleExplorationStarted;
        GameEvents.OnExplorationUpdated -= HandleExplorationUpdated;
        GameEvents.OnExplorationEventTriggered -= HandleEventTriggered;
        GameEvents.OnExplorationPhaseChanged -= HandlePhaseChanged;
        GameEvents.OnExplorationEnvObjectFound -= HandleEnvObjectFound;
        GameEvents.OnExplorationVNStarted -= HandleVNStarted;
        GameEvents.OnExplorationInteractionPrompt -= HandleInteractionPrompt;
        GameEvents.OnActionResult -= HandleActionResult;
    }

    private void Start()
    {
        if (panelEvent) panelEvent.SetActive(false);
        if (panelResult) panelResult.SetActive(false);
        if (panelVN)     panelVN.SetActive(false);
        if (textActionResult) textActionResult.gameObject.SetActive(false);
        
        if (btnExit) btnExit.onClick.AddListener(() => ExplorationManager.Instance.ExitExploration());
        
        if (btnConfirmPath) 
        {
            btnConfirmPath.onClick.AddListener(() => {
                ExplorationManager.Instance.ConfirmPath();
                btnConfirmPath.gameObject.SetActive(false);
            });
        }

        if (btnToggleEnvObjectList && panelEnvObjectList)
        {
            btnToggleEnvObjectList.onClick.AddListener(() => panelEnvObjectList.SetActive(!panelEnvObjectList.activeSelf));
        }

        if (panelResult) panelResult.SetActive(false);
        if (panelEnvObjectList) panelEnvObjectList.SetActive(false);

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
        
        if (textChoices)
        {
            string count = state.remainingEnemyTickets == -1 ? "무제한" : state.remainingEnemyTickets.ToString();
            textChoices.text = $"적 전용 선택권: {count}";
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

    private void HandleVNStarted(List<DialogueStep> steps, System.Action onComplete)
    {
        currentVNSteps = steps;
        currentVNIndex = 0;
        this.onVNComplete = onComplete;
        
        if (panelVN)
        {
            panelVN.SetActive(true);
            ShowVNStep(0);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void ShowVNStep(int index)
    {
        if (index < 0 || index >= currentVNSteps.Count) return;

        var step = currentVNSteps[index];
        if (textVNName) textVNName.text = step.characterName;
        if (textVNDialogue) textVNDialogue.text = step.dialogueText;
        
        if (imgVNLeft) 
        {
            imgVNLeft.sprite = step.leftSprite;
            imgVNLeft.gameObject.SetActive(step.leftSprite != null);
        }
        if (imgVNRight) 
        {
            imgVNRight.sprite = step.rightSprite;
            imgVNRight.gameObject.SetActive(step.rightSprite != null);
        }
        if (imgVNBackground && step.backgroundOverride != null) 
        {
            imgVNBackground.sprite = step.backgroundOverride;
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
                panelVN.SetActive(false);
                onVNComplete?.Invoke();
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
        
        foreach (Transform child in choiceButtonRoot)
            Destroy(child.gameObject);

        foreach (var choice in curChoices)
        {
            var btn = Instantiate(choiceButtonPrefab, choiceButtonRoot);
            btn.GetComponentInChildren<TMP_Text>().text = $"{choice.label}";
            
            btn.onClick.AddListener(() => {
                ExplorationEventProcessor.Instance.ApplyChoiceEffect(choice);
                panelVN.SetActive(false); 
                onVNComplete?.Invoke();
                curEventNode = null;
            });
        }
        
        if (btnVNDialogueBox != null) btnVNDialogueBox.interactable = false;
    }

    private void HandleEventTriggered(DialogueNodeData node, List<DialogueChoiceData> visibleChoices)
    {
        curEventNode = node;
        curChoices = visibleChoices;
        if (btnVNDialogueBox != null) btnVNDialogueBox.interactable = true;
    }

    private void HandleInteractionPrompt(string prompt, bool show)
    {
        if (panelInteractPrompt)
        {
            panelInteractPrompt.SetActive(show);
            if (show && textInteractPrompt)
            {
                string keyName = ExplorationManager.Instance != null ? ExplorationManager.Instance.interactKey.ToString() : "E";
                textInteractPrompt.text = $"[{keyName}] {prompt}";
            }
        }
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
        if (envObjectListContent == null || envObjectItemPrefab == null) return;

        foreach (Transform child in envObjectListContent)
            Destroy(child.gameObject);

        var foundIds = ExplorationManager.Instance.currentState.foundEnvObjectIds;
        foreach (var id in foundIds)
        {
            var item = Instantiate(envObjectItemPrefab, envObjectListContent);
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
        if (panelResult) panelResult.SetActive(false);
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
        if (nodeIconPrefab == null || nodeContainer == null) return;

        foreach (Transform child in nodeContainer)
            Destroy(child.gameObject);

        var cam = camTop != null && camTop.enabled ? camTop : (camQuarter != null ? camQuarter : Camera.main);
        if (cam == null) return;

        foreach (var node in data.nodes)
        {
            var icon = Instantiate(nodeIconPrefab, nodeContainer);
            Vector3 screenPos = cam.WorldToScreenPoint(node.worldPosition);
            
            if (screenPos.z < 0) 
            {
                icon.gameObject.SetActive(false);
            }
            else
            {
                icon.transform.position = screenPos;
            }
            
            var label = icon.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = node.nodeName ?? node.nodeId;
            
            icon.color = node.eventType == ExplorationEventType.Exit ? Color.green : Color.white;
        }
    }

    private void HandlePhaseChanged(ExplorationPhase phase)
    {
        if (phase == ExplorationPhase.Planning)
        {
            if (btnConfirmPath) btnConfirmPath.gameObject.SetActive(true);
            StartCoroutine(TransitionCamera(true)); 
        }
        else if (phase == ExplorationPhase.Moving)
        {
            StartCoroutine(TransitionCamera(false)); 
        }
        else if (phase == ExplorationPhase.Result)
        {
            ShowResult(ExplorationManager.Instance.currentState);
        }
    }

    private System.Collections.IEnumerator TransitionCamera(bool toTop)
    {
        if (!camTop || !camQuarter) yield break;
        camTop.enabled = toTop;
        camQuarter.enabled = !toTop;
        yield return null;
    }

    private void ShowResult(ExplorationState state)
    {
        if (panelResult == null) return;

        bool success = state.remainingTime > 0;
        textResultStatus.text = success ? "<color=green>탐사 성공!</color>" : "<color=red>탐사 실패</color>";
        
        int min = (int)state.remainingTime / 60;
        int sec = (int)state.remainingTime % 60;
        string timeStr = success ? $"\n남은 시간: {min:00}:{sec:00}" : "";

        textResultSummary.text = $"획득 골드: {state.collectedGold}G{timeStr}\n\n[수집한 항목]";
        
        if (state.foundEnvObjectIds.Count > 0)
        {
            foreach (var id in state.foundEnvObjectIds)
                textResultSummary.text += $"\n- {id}";
        }
        else
        {
            textResultSummary.text += "\n없음";
        }
        
        panelResult.SetActive(true);
    }
}
