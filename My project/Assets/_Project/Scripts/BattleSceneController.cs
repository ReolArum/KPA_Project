// ===== BattleSceneController.cs =====
// 전투 씬 통합 컨트롤러
// HP바, ATB 행동 서열바, 방침 버튼, 스킬 쿨타임 UI 포함
// ※ 모든 UI 오브젝트는 Unity 씬에서 미리 배치 후 Inspector에서 연결합니다.
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleSceneController : MonoBehaviour
{
    // ====================================================
    //  캐릭터 (씬 오브젝트)
    // ====================================================
    [Header("캐릭터")]
    [SerializeField] private Animator  playerAnimator;
    [SerializeField] private Animator  opponentAnimator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform opponentTransform;

    // ====================================================
    //  연출 설정
    // ====================================================
    [Header("연출 설정")]
    [SerializeField] private float attackMoveSpeed = 12f;
    [SerializeField] private float turnDelay       = 0.4f;
    [SerializeField] private float resultDelay     = 2f;
    [SerializeField] private int   maxTurns        = 100;

    // ====================================================
    //  UI - HP 바 & 이름 (플레이어)
    // ====================================================
    [Header("플레이어 HP")]
    [SerializeField] private Slider   sliderPlayerHP;
    [SerializeField] private TMP_Text textPlayerHP;      // "cur / max"
    [SerializeField] private TMP_Text textPlayerName;
    [SerializeField] private Image    fillPlayerHP;      // 색상 변경용 (선택)

    // ====================================================
    //  UI - HP 바 & 이름 (상대)
    // ====================================================
    [Header("상대 HP")]
    [SerializeField] private Slider   sliderOpponentHP;
    [SerializeField] private TMP_Text textOpponentHP;
    [SerializeField] private TMP_Text textOpponentName;
    [SerializeField] private Image    fillOpponentHP;

    // ====================================================
    //  UI - ATB 행동 서열바
    //  최대 6칸 표시: 각 슬롯 = 아이콘 + AV 수치
    // ====================================================
    [Header("ATB 행동 서열바")]
    [SerializeField] private Transform  timelineRoot;          // 슬롯을 담는 부모 Transform (Vertical Layout)
    [SerializeField] private GameObject timelineSlotPrefab;    // 슬롯 프리팹 (Image + TMP_Text)
    [SerializeField] private Sprite     iconPlayer;            // 플레이어 아이콘
    [SerializeField] private Sprite     iconOpponent;          // 상대 아이콘
    [SerializeField] private int        timelineDisplayCount = 5;

    // ====================================================
    //  UI - 방침 버튼
    //  밀어붙여(Aggressive) / 평소대로(Normal) / 버텨(Defensive) / 기술위주(Technical)
    // ====================================================
    [Header("방침 버튼")]
    [SerializeField] private Button   btnAggressive;
    [SerializeField] private Button   btnNormal;
    [SerializeField] private Button   btnDefensive;
    [SerializeField] private Button   btnTechnical;
    [SerializeField] private TMP_Text textDirectiveLabel;
    [SerializeField] private Button   btnPause;

    [SerializeField] private Color colorDirectiveSelected = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color colorDirectiveNormal   = Color.white;

    // ====================================================
    //  UI - 스킬 쿨타임 바
    // ====================================================
    [Header("스킬 쿨타임 바")]
    [SerializeField] private Transform  skillBarRoot;        // 스킬 슬롯 부모 (Horizontal Layout)
    [SerializeField] private GameObject skillBarSlotPrefab;  // 슬롯 프리팹

    // ====================================================
    //  UI - 데미지 팝업
    // ====================================================
    [Header("데미지 팝업")]
    [SerializeField] private Canvas     battleCanvas;
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private float popupRiseHeight = 80f;  // Canvas 픽셀 단위
    [SerializeField] private float popupDuration   = 0.9f;
    [SerializeField] private float popupFadeStart  = 0.5f;
    [SerializeField] private Color colorHit      = new Color(1f, 0.95f, 0.3f);
    [SerializeField] private Color colorCritical = new Color(1f, 0.25f, 0.1f);
    [SerializeField] private Color colorMiss     = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color colorEvade    = new Color(0.4f, 0.85f, 1f);

    // ====================================================
    //  UI - 전투 로그
    // ====================================================
    [Header("전투 로그")]
    [SerializeField] private TMP_Text  textBattleLog;
    [SerializeField] private ScrollRect scrollLog;

    // ====================================================
    //  UI - 결과 패널
    // ====================================================
    [Header("결과 패널")]
    [SerializeField] private GameObject panelResult;
    [SerializeField] private TMP_Text   textResult;
    [SerializeField] private Button     btnCloseResult;

    // ====================================================
    //  전투 시스템 (내부)
    // ====================================================
    private CombatUnit    playerUnit;
    private CombatUnit    opponentUnit;
    private BattleReport  report;
    private ATBTimeline   timeline;
    private BuffSystem    buffSystem;
    private SkillSelector skillSelector;
    private CombatResolver resolver;

    private BattleDirective currentDirective = BattleDirective.Normal;
    private bool            battleActive     = false;
    private bool            isPaused         = false;
    private int             turnCount        = 0;
    private string          logBuffer        = "";

    private Vector3 playerStartPos;
    private Vector3 opponentStartPos;

    private readonly List<GameObject>    timelineSlots = new();
    private readonly List<SkillBarSlot>  skillBarSlots = new();

    private Camera mainCam;

    // ====================================================
    //  내부 클래스: 스킬 바 슬롯 데이터
    // ====================================================
    private class SkillBarSlot
    {
        public SkillData  skill;
        public Image      icon;
        public TMP_Text   nameText;
        public TMP_Text   cooldownText;
        public Slider     cooldownSlider;
        public Image      overlay;
    }

    // ====================================================
    //  초기화
    // ====================================================
    void Start()
    {
        // ── Inspector 연결 누락 체크 ──────────────────────────
        bool uiMissing = false;
        if (battleCanvas      == null) { Debug.LogError("[BattleScene] battleCanvas 미연결!"); uiMissing = true; }
        if (sliderPlayerHP    == null) { Debug.LogError("[BattleScene] sliderPlayerHP 미연결!"); uiMissing = true; }
        if (sliderOpponentHP  == null) { Debug.LogError("[BattleScene] sliderOpponentHP 미연결!"); uiMissing = true; }
        if (textPlayerHP      == null) { Debug.LogError("[BattleScene] textPlayerHP 미연결!"); uiMissing = true; }
        if (textOpponentHP    == null) { Debug.LogError("[BattleScene] textOpponentHP 미연결!"); uiMissing = true; }
        if (timelineRoot      == null) { Debug.LogError("[BattleScene] timelineRoot 미연결!"); uiMissing = true; }
        if (timelineSlotPrefab == null){ Debug.LogError("[BattleScene] timelineSlotPrefab 미연결!"); uiMissing = true; }
        if (skillBarRoot      == null) { Debug.LogError("[BattleScene] skillBarRoot 미연결!"); uiMissing = true; }
        if (skillBarSlotPrefab == null){ Debug.LogError("[BattleScene] skillBarSlotPrefab 미연결!"); uiMissing = true; }
        if (panelResult       == null) { Debug.LogError("[BattleScene] panelResult 미연결!"); uiMissing = true; }
        if (textResult        == null) { Debug.LogError("[BattleScene] textResult 미연결!"); uiMissing = true; }

        if (uiMissing)
        {
            Debug.LogError("[BattleScene] Inspector 연결 누락 항목이 있습니다. 위 에러를 확인하고 씬에서 연결해주세요.");
            return;
        }

        // ── 전투 유닛 준비 ────────────────────────────────────
        if (BattleSceneData.playerUnit == null || BattleSceneData.opponentUnit == null)
        {
            Debug.LogWarning("[BattleScene] 전투 데이터 없음 → 더미 유닛으로 테스트 모드 시작");
            playerUnit   = MakeDummyPlayer();
            opponentUnit = CombatUnit.CreateOpponent(ArenaRank.Silver, 1);
        }
        else
        {
            playerUnit   = BattleSceneData.playerUnit;
            opponentUnit = BattleSceneData.opponentUnit;
        }

        if (playerUnit.equippedSkills  == null || playerUnit.equippedSkills.Count  == 0) AssignDefaultSkills(playerUnit);
        if (opponentUnit.equippedSkills == null || opponentUnit.equippedSkills.Count == 0) AssignDefaultSkills(opponentUnit);

        // ── 전투 시스템 초기화 ────────────────────────────────
        report        = new BattleReport();
        timeline      = new ATBTimeline();
        buffSystem    = new BuffSystem(timeline);
        skillSelector = new SkillSelector();
        resolver      = new CombatResolver();
        timeline.Initialize(playerUnit, opponentUnit);

        if (playerTransform)   playerStartPos   = playerTransform.position;
        if (opponentTransform) opponentStartPos = opponentTransform.position;
        if (playerAnimator)    playerAnimator.speed   = 0;
        if (opponentAnimator)  opponentAnimator.speed = 0;

        mainCam = Camera.main;

        // ── UI 초기화 ─────────────────────────────────────────
        SetupButtons();
        BuildTimelineSlots();
        BuildSkillBarSlots();

        panelResult.SetActive(false);
        RefreshHP();
        RefreshTimeline();
        RefreshSkillBar();
        UpdateDirectiveLabel();

        StartCoroutine(BattleLoop());
    }

    // ====================================================
    //  버튼 리스너 등록 (Inspector 연결 버튼용)
    // ====================================================
    void SetupButtons()
    {
        if (btnAggressive) btnAggressive.onClick.AddListener(() => SetDirective(BattleDirective.Aggressive));
        if (btnNormal)     btnNormal.onClick.AddListener(()     => SetDirective(BattleDirective.Normal));
        if (btnDefensive)  btnDefensive.onClick.AddListener(()  => SetDirective(BattleDirective.Defensive));
        if (btnTechnical)  btnTechnical.onClick.AddListener(()  => SetDirective(BattleDirective.Technical));
        if (btnPause)      btnPause.onClick.AddListener(TogglePause);

        if (btnCloseResult) btnCloseResult.onClick.AddListener(() =>
        {
            if (panelResult) panelResult.SetActive(false);
        });
    }

    void SetDirective(BattleDirective d)
    {
        currentDirective = d;
        UpdateDirectiveLabel();
        HighlightDirectiveButton();
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        if (btnPause)
        {
            var t = btnPause.GetComponentInChildren<TMP_Text>();
            if (t) t.text = isPaused ? "재개" : "일시정지";
        }
    }

    void UpdateDirectiveLabel()
    {
        if (textDirectiveLabel == null) return;
        textDirectiveLabel.text = currentDirective switch
        {
            BattleDirective.Aggressive => "◆ 밀어붙여",
            BattleDirective.Normal     => "◆ 평소대로",
            BattleDirective.Defensive  => "◆ 버텨",
            BattleDirective.Technical  => "◆ 기술위주",
            _                          => ""
        };
    }

    void HighlightDirectiveButton()
    {
        SetButtonColor(btnAggressive, currentDirective == BattleDirective.Aggressive);
        SetButtonColor(btnNormal,     currentDirective == BattleDirective.Normal);
        SetButtonColor(btnDefensive,  currentDirective == BattleDirective.Defensive);
        SetButtonColor(btnTechnical,  currentDirective == BattleDirective.Technical);
    }

    void SetButtonColor(Button btn, bool selected)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img) img.color = selected ? colorDirectiveSelected : colorDirectiveNormal;
    }

    // ====================================================
    //  ATB 서열바 - 슬롯 생성
    // ====================================================
    void BuildTimelineSlots()
    {
        if (timelineRoot == null || timelineSlotPrefab == null) return;

        foreach (var s in timelineSlots)
            if (s) Destroy(s);
        timelineSlots.Clear();

        for (int i = 0; i < timelineDisplayCount; i++)
        {
            var go = Instantiate(timelineSlotPrefab, timelineRoot);
            go.SetActive(false);
            timelineSlots.Add(go);
        }
    }

    // ====================================================
    //  ATB 서열바 - 갱신
    // ====================================================
    void RefreshTimeline()
    {
        if (timelineRoot == null || timeline == null) return;
        if (timelineSlots.Count == 0) return;

        var order = timeline.GetTimeline();
        int displayCount = Mathf.Min(timelineSlots.Count, order.Count);

        for (int i = 0; i < timelineSlots.Count; i++)
        {
            var slot = timelineSlots[i];
            if (slot == null) continue;

            if (i >= displayCount) { slot.SetActive(false); continue; }

            slot.SetActive(true);
            var (unit, av) = order[i];

            var iconImg = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                bool isPlayer = (unit == playerUnit);
                iconImg.sprite = isPlayer ? iconPlayer : iconOpponent;
                iconImg.color  = (i == 0) ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            var avText = slot.transform.Find("AVText")?.GetComponent<TMP_Text>();
            if (avText != null)
            {
                float maxAV = Mathf.Max(1f, playerUnit.derived.SPD > 0 ? 10000f / playerUnit.derived.SPD : 10000f);
                float ratio = Mathf.Clamp01(av / (maxAV * 1.5f));
                int displayVal = Mathf.RoundToInt(ratio * 100f);
                avText.text = i == 0 ? ">>" : displayVal.ToString();
            }

            var bg = slot.GetComponent<Image>();
            if (bg != null)
                bg.color = (i == 0)
                    ? new Color(1f, 0.9f, 0.2f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.25f, 0.85f);
        }
    }

    // ====================================================
    //  스킬 바 - 슬롯 생성
    // ====================================================
    void BuildSkillBarSlots()
    {
        if (skillBarRoot == null || skillBarSlotPrefab == null) return;

        foreach (var s in skillBarSlots)
            if (s?.icon != null) Destroy(s.icon.transform.parent?.gameObject);
        skillBarSlots.Clear();

        for (int i = skillBarRoot.childCount - 1; i >= 0; i--)
            Destroy(skillBarRoot.GetChild(i).gameObject);

        if (playerUnit == null) return;

        foreach (var skill in playerUnit.equippedSkills)
        {
            if (skill == null) continue;

            var go   = Instantiate(skillBarSlotPrefab, skillBarRoot);
            var slot = new SkillBarSlot { skill = skill };

            slot.icon           = go.transform.Find("Icon")?.GetComponent<Image>();
            slot.nameText       = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            slot.cooldownText   = go.transform.Find("CooldownText")?.GetComponent<TMP_Text>();
            slot.cooldownSlider = go.transform.Find("CooldownSlider")?.GetComponent<Slider>();
            slot.overlay        = go.transform.Find("Overlay")?.GetComponent<Image>();

            if (slot.icon     != null && skill.icon != null) slot.icon.sprite = skill.icon;
            if (slot.nameText != null) slot.nameText.text = skill.skillName;

            skillBarSlots.Add(slot);
        }
    }

    // ====================================================
    //  스킬 바 - 갱신
    // ====================================================
    void RefreshSkillBar()
    {
        if (playerUnit == null) return;

        foreach (var slot in skillBarSlots)
        {
            if (slot?.skill == null) continue;

            int  cd    = playerUnit.GetCooldown(slot.skill);
            bool ready = cd <= 0;

            if (slot.cooldownText   != null) slot.cooldownText.text = ready ? "준비" : $"{cd}턴";

            if (slot.cooldownSlider != null)
            {
                int maxCd = slot.skill.cooldownTurns;
                slot.cooldownSlider.value = maxCd > 0 ? 1f - (float)cd / maxCd : 1f;
            }

            if (slot.overlay != null)
            {
                slot.overlay.gameObject.SetActive(!ready);
                if (!ready) slot.overlay.color = new Color(0f, 0f, 0f, 0.55f);
            }
        }
    }

    // ====================================================
    //  전투 메인 루프
    // ====================================================
    IEnumerator BattleLoop()
    {
        battleActive = true;
        yield return new WaitForSeconds(0.8f);

        while (battleActive)
        {
            while (isPaused) yield return null;

            if (playerUnit.currentHP <= 0 || opponentUnit.currentHP <= 0)
            {
                report.playerWon = playerUnit.currentHP > opponentUnit.currentHP;
                yield return StartCoroutine(BattleEnd(report.playerWon));
                yield break;
            }

            turnCount++;

            CombatUnit attacker    = timeline.AdvanceAndGetNext();
            CombatUnit defender    = (attacker == playerUnit) ? opponentUnit : playerUnit;
            bool       isPlayerTurn = (attacker == playerUnit);

            Animator  attackerAnim   = isPlayerTurn ? playerAnimator   : opponentAnimator;
            Animator  defenderAnim   = isPlayerTurn ? opponentAnimator : playerAnimator;
            Transform attackerTr     = isPlayerTurn ? playerTransform   : opponentTransform;
            Transform defenderTr     = isPlayerTurn ? opponentTransform : playerTransform;
            Vector3   attackerStartP = isPlayerTurn ? playerStartPos    : opponentStartPos;

            attacker.TickCooldowns();
            buffSystem.OnUnitTurnStart(attacker);

            BattleDirective directive = isPlayerTurn ? currentDirective : BattleDirective.Normal;
            SkillData skill = skillSelector.SelectSkill(attacker, defender, directive);
            if (skill == null) { Debug.LogError("[BattleScene] 스킬 null"); yield break; }

            AppendLog($"<b>{attacker.unitName}</b>: {skill.skillName} 시전!");
            RefreshTimeline();

            yield return StartCoroutine(AttackSequence(
                attackerAnim, defenderAnim,
                attackerTr, defenderTr, attackerStartP,
                attacker, defender, skill, isPlayerTurn
            ));

            attacker.SetCooldown(skill);
            RefreshHP();
            RefreshTimeline();
            RefreshSkillBar();

            if (defender.currentHP <= 0)
            {
                report.playerWon = isPlayerTurn;
                yield return StartCoroutine(BattleEnd(isPlayerTurn));
                yield break;
            }

            if (turnCount >= maxTurns)
            {
                report.playerWon = playerUnit.currentHP >= opponentUnit.currentHP;
                yield return StartCoroutine(BattleEnd(report.playerWon));
                yield break;
            }

            yield return new WaitForSeconds(turnDelay);
        }
    }

    // ====================================================
    //  공격 연출 시퀀스
    // ====================================================
    IEnumerator AttackSequence(
        Animator attackerAnim, Animator defenderAnim,
        Transform attackerTr, Transform defenderTr,
        Vector3 startPos,
        CombatUnit attacker, CombatUnit defender,
        SkillData skill, bool isPlayerTurn)
    {
        bool has3D = attackerTr != null && defenderTr != null;

        if (attackerAnim) attackerAnim.speed = 1;
        if (has3D)
            yield return StartCoroutine(MoveToPosition(attackerTr,
                Vector3.Lerp(attackerTr.position, defenderTr.position, 0.75f), attackMoveSpeed));
        else
            yield return new WaitForSeconds(0.15f);

        // 스킬마다 지정된 트리거 사용 (기본값: "Attack")
        string atkTrigger = !string.IsNullOrEmpty(skill.animationTrigger) ? skill.animationTrigger : "Attack";
        string hitTrigger = !string.IsNullOrEmpty(skill.hitAnimationTrigger) ? skill.hitAnimationTrigger : "Hit";

        if (attackerAnim) attackerAnim.SetTrigger(atkTrigger);

        if (attackerAnim != null)
        {
            yield return null;
            float w = 0f;
            while (!attackerAnim.GetCurrentAnimatorStateInfo(0).IsName(atkTrigger) && w < 1f)
            { w += Time.deltaTime; yield return null; }

            w = 0f;
            while (attackerAnim.GetCurrentAnimatorStateInfo(0).IsName(atkTrigger) &&
                   attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.4f && w < 2f)
            { w += Time.deltaTime; yield return null; }
        }
        else yield return new WaitForSeconds(0.2f);

        // 데미지 판정
        DamageResult dmgResult = resolver.Resolve(attacker, defender, skill);
        report.LogAction(attacker, skill, dmgResult, turnCount, isPlayerTurn);

        // 피격자 Transform: AttackSequence 파라미터의 defenderTr 우선 사용
        // null이면 Inspector 연결된 Transform으로 fallback
        Transform defenderWorldTr = defenderTr ?? (isPlayerTurn ? opponentTransform : playerTransform);

        string logMsg;
        switch (dmgResult.outcome)
        {
            case HitOutcome.Hit:
            case HitOutcome.Critical:
                defender.currentHP = Mathf.Max(0, defender.currentHP - dmgResult.finalDamage);
                if (defenderAnim) { defenderAnim.speed = 1; defenderAnim.SetTrigger(hitTrigger); }
                if (skill.avAdvance > 0) timeline.AdvanceUnit(attacker, skill.avAdvance);
                if (skill.avDelay   > 0) timeline.DelayUnit(defender, skill.avDelay);
                if (skill.appliedBuff   != null) buffSystem.ApplyBuff(attacker, skill.appliedBuff);
                if (skill.appliedDebuff != null) buffSystem.ApplyBuff(defender, skill.appliedDebuff);

                bool isCrit = dmgResult.outcome == HitOutcome.Critical;
                SpawnDamagePopup(
                    defenderWorldTr,
                    isCrit ? $"<b>{dmgResult.finalDamage:F0}</b>" : $"{dmgResult.finalDamage:F0}",
                    isCrit ? colorCritical : colorHit,
                    isCrit ? 1.6f : 1.0f
                );
                logMsg = isCrit
                    ? $"  → <color=red>크리티컬!</color> {defender.unitName}에게 <color=red>{dmgResult.finalDamage:F0}</color> 데미지!"
                    : $"  → {defender.unitName}에게 <color=yellow>{dmgResult.finalDamage:F0}</color> 데미지!";
                break;
            case HitOutcome.Evaded:
                SpawnDamagePopup(defenderWorldTr, "EVADE", colorEvade, 0.85f);
                logMsg = $"  → {defender.unitName} 회피!";
                break;
            default:
                SpawnDamagePopup(defenderWorldTr, "MISS", colorMiss, 0.85f);
                logMsg = "  → 빗나감!";
                break;
        }
        AppendLog(logMsg);

        if (attackerAnim != null)
        {
            float w = 0f;
            while (attackerAnim.GetCurrentAnimatorStateInfo(0).IsName(atkTrigger) &&
                   attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.85f && w < 2f)
            { w += Time.deltaTime; yield return null; }
        }

        if (defenderAnim != null && defenderAnim.GetCurrentAnimatorStateInfo(0).IsName(hitTrigger))
        {
            float w = 0f;
            while (defenderAnim.GetCurrentAnimatorStateInfo(0).IsName(hitTrigger) &&
                   defenderAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.85f && w < 1f)
            { w += Time.deltaTime; yield return null; }
        }

        if (has3D)
            yield return StartCoroutine(MoveToPosition(attackerTr, startPos, attackMoveSpeed * 1.2f));
        if (attackerAnim) attackerAnim.speed = 0;
        if (defenderAnim) defenderAnim.speed = 0;

        yield return new WaitForSeconds(0.15f);
    }

    // ====================================================
    //  데미지 팝업
    // ====================================================
    void SpawnDamagePopup(Transform defenderTr, string text, Color color, float sizeScale = 1f)
    {
        if (battleCanvas == null) return;
        StartCoroutine(DamagePopupRoutine(defenderTr, text, color, sizeScale));
    }

    IEnumerator DamagePopupRoutine(Transform defenderTr, string text, Color color, float sizeScale)
    {
        GameObject popupGo;
        TMP_Text   tmp;

        if (damagePopupPrefab != null)
        {
            popupGo = Instantiate(damagePopupPrefab, battleCanvas.transform);
            tmp     = popupGo.GetComponentInChildren<TMP_Text>();
        }
        else
        {
            popupGo = new GameObject("DmgPopup");
            popupGo.transform.SetParent(battleCanvas.transform, false);
            tmp           = popupGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = 48f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            var shadow = popupGo.AddComponent<UnityEngine.UI.Outline>();
            shadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        if (tmp == null) { Destroy(popupGo); yield break; }

        tmp.text     = text;
        tmp.color    = color;
        tmp.fontSize = Mathf.RoundToInt(tmp.fontSize * sizeScale);

        RectTransform rt = popupGo.GetComponent<RectTransform>();
        if (rt == null) rt = popupGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 100f);

        Vector3 startAnchor;
        if (defenderTr != null)
        {
            Vector3 worldPos = defenderTr.position + Vector3.up * 1.8f;
            startAnchor = WorldToCanvasPos(worldPos);
        }
        else
        {
            // 3D Transform 없음: 플레이어(좌측 하단), 상대(우측 상단) 고정 위치
            bool defIsOpponent = (defenderTr == opponentTransform) || (opponentTransform == null && defenderTr != playerTransform);
            startAnchor = defIsOpponent
                ? new Vector3(320f,  180f, 0f)   // 상대: 우측 상단
                : new Vector3(-320f, -180f, 0f); // 플레이어: 좌측 하단
        }
        rt.anchoredPosition = startAnchor;

        float xJitter  = Random.Range(-60f, 60f);
        Vector3 endAnchor = startAnchor + new Vector3(xJitter, popupRiseHeight, 0f);

        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / popupDuration;
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            rt.anchoredPosition = Vector3.Lerp(startAnchor, endAnchor, eased);

            float alpha = t >= popupFadeStart
                ? 1f - (t - popupFadeStart) / (1f - popupFadeStart)
                : 1f;
            tmp.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        Destroy(popupGo);
    }

    Vector3 WorldToCanvasPos(Vector3 worldPos)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || battleCanvas == null) return Vector3.zero;

        Vector2 screenPos = mainCam.WorldToScreenPoint(worldPos);
        RectTransform canvasRect = battleCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return screenPos;

        if (battleCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPos);
            return localPos;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, battleCanvas.worldCamera, out Vector2 localPosCam);
        return localPosCam;
    }

    // ====================================================
    //  이동 코루틴
    // ====================================================
    IEnumerator MoveToPosition(Transform tr, Vector3 target, float speed)
    {
        if (tr == null) yield break;
        while (Vector3.Distance(tr.position, target) > 0.05f)
        {
            tr.position = Vector3.MoveTowards(tr.position, target, speed * Time.deltaTime);
            yield return null;
        }
        tr.position = target;
    }

    // ====================================================
    //  전투 종료
    // ====================================================
    IEnumerator BattleEnd(bool playerWon)
    {
        battleActive = false;

        if (playerAnimator)   playerAnimator.SetTrigger(playerWon ? "Victory" : "Defeat");
        if (opponentAnimator) opponentAnimator.SetTrigger(playerWon ? "Defeat" : "Victory");

        string resultColor = playerWon ? "<color=green>" : "<color=red>";
        AppendLog($"\n{resultColor}===== {(playerWon ? "승리!" : "패배...")} =====</color>");

        yield return new WaitForSeconds(resultDelay);

        if (panelResult != null && textResult != null)
        {
            textResult.text = report.ToReportString();
            panelResult.SetActive(true);

            bool closed = false;
            if (btnCloseResult)
            {
                btnCloseResult.onClick.RemoveAllListeners();
                btnCloseResult.onClick.AddListener(() => closed = true);
            }
            else closed = true;

            yield return new WaitUntil(() => closed);
        }

        BattleSceneData.CompleteBattle(report);
        SceneManager.LoadScene(BattleSceneData.SceneMain);
    }

    // ====================================================
    //  UI 갱신 - HP
    // ====================================================
    void RefreshHP()
    {
        if (playerUnit != null)
        {
            float maxHP = playerUnit.derived.MaxHP;
            float curHP = Mathf.Max(0, playerUnit.currentHP);
            float ratio = maxHP > 0 ? curHP / maxHP : 0f;

            if (sliderPlayerHP) sliderPlayerHP.value = ratio;
            if (textPlayerHP)   textPlayerHP.text    = $"{curHP:F0} / {maxHP:F0}";
            if (textPlayerName) textPlayerName.text  = playerUnit.unitName;
            if (fillPlayerHP)   fillPlayerHP.color   = GetHPColor(ratio);
        }

        if (opponentUnit != null)
        {
            float maxHP = opponentUnit.derived.MaxHP;
            float curHP = Mathf.Max(0, opponentUnit.currentHP);
            float ratio = maxHP > 0 ? curHP / maxHP : 0f;

            if (sliderOpponentHP) sliderOpponentHP.value = ratio;
            if (textOpponentHP)   textOpponentHP.text    = $"{curHP:F0} / {maxHP:F0}";
            if (textOpponentName) textOpponentName.text  = opponentUnit.unitName;
            if (fillOpponentHP)   fillOpponentHP.color   = GetHPColor(ratio);
        }
    }

    Color GetHPColor(float ratio)
    {
        if (ratio > 0.5f) return Color.Lerp(new Color(1f, 0.85f, 0f), new Color(0.2f, 0.85f, 0.2f), (ratio - 0.5f) * 2f);
        return Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(1f, 0.85f, 0f), ratio * 2f);
    }

    // ====================================================
    //  전투 로그
    // ====================================================
    void AppendLog(string line)
    {
        logBuffer += line + "\n";
        if (textBattleLog) textBattleLog.text = logBuffer;
        if (scrollLog != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollLog.verticalNormalizedPosition = 0f;
        }
    }

    // ====================================================
    //  더미 플레이어 유닛 (테스트 모드용)
    // ====================================================
    private static CombatUnit MakeDummyPlayer()
    {
        var u = new CombatUnit();
        u.unitName    = "내 클론";
        u.rawStats    = new CombatBaseStats { STR = 20, AGI = 15, VIT = 15, INT = 5, GUT = 5, SEN = 10 };
        u.schoolType  = SchoolType.Crusher;
        u.schoolLevel = 1;
        u.Recalculate();
        u.currentHP = u.derived.MaxHP;
        u.currentAV = u.derived.SPD > 0 ? 10000f / u.derived.SPD : 10000f;
        return u;
    }

    // ====================================================
    //  기본 스킬 부여 (스킬 DB 없는 경우 폴백)
    // ====================================================
    private static void AssignDefaultSkills(CombatUnit unit)
    {
        unit.equippedSkills.Clear();

        var basicAttack = ScriptableObject.CreateInstance<SkillData>();
        basicAttack.name                 = "일반공격";
        basicAttack.skillName            = "일반공격";
        basicAttack.category             = SkillCategory.Strike;
        basicAttack.weight               = 60;
        basicAttack.damageMultiplier     = 1.0f;
        basicAttack.cooldownTurns        = 0;
        basicAttack.animationTrigger     = "Attack";
        basicAttack.hitAnimationTrigger  = "Hit";

        var guardStance = ScriptableObject.CreateInstance<SkillData>();
        guardStance.name                 = "방어자세";
        guardStance.skillName            = "방어자세";
        guardStance.category             = SkillCategory.Defense;
        guardStance.weight               = 30;
        guardStance.damageMultiplier     = 0.5f;
        guardStance.cooldownTurns        = 2;
        guardStance.animationTrigger     = "Attack_Guard";
        guardStance.hitAnimationTrigger  = "Hit";

        var quickStep = ScriptableObject.CreateInstance<SkillData>();
        quickStep.name                   = "빠른발놀림";
        quickStep.skillName              = "빠른발놀림";
        quickStep.category               = SkillCategory.Mobility;
        quickStep.weight                 = 40;
        quickStep.damageMultiplier       = 0.8f;
        quickStep.avAdvance              = 500f;
        quickStep.cooldownTurns          = 3;
        quickStep.animationTrigger       = "Attack_Step";
        quickStep.hitAnimationTrigger    = "Hit";

        unit.equippedSkills.Add(basicAttack);
        unit.equippedSkills.Add(guardStance);
        unit.equippedSkills.Add(quickStep);
    }
}
