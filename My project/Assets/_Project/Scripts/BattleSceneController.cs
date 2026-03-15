// ===== BattleSceneController.cs =====
// HP바, ATB 슬롯, 방침 버튼, 스킬 쿨타임 바, 데미지 팝업, 전투 로그 포함
// 모든 UI 오브젝트는 씬에서 Inspector로 연결
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleSceneController : MonoBehaviour
{
    // ====================================================
    //  캐릭터
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
    //  UI - HP 바 & 이름
    // ====================================================
    [Header("플레이어 HP")]
    [SerializeField] private Slider   sliderPlayerHP;
    [SerializeField] private TMP_Text textPlayerHP;
    [SerializeField] private TMP_Text textPlayerName;
    [SerializeField] private Image    fillPlayerHP;

    [Header("상대 HP")]
    [SerializeField] private Slider   sliderOpponentHP;
    [SerializeField] private TMP_Text textOpponentHP;
    [SerializeField] private TMP_Text textOpponentName;
    [SerializeField] private Image    fillOpponentHP;

    // ====================================================
    //  UI - ATB 행동 서열 슬롯
    // ====================================================
    [Header("ATB 행동 서열바")]
    [SerializeField] private Transform  timelineRoot;
    [SerializeField] private GameObject timelineSlotPrefab;
    [SerializeField] private Sprite     iconPlayer;
    [SerializeField] private Sprite     iconOpponent;
    [SerializeField] private int        timelineDisplayCount = 5;

    // ====================================================
    //  UI - 방침 버튼
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
    [SerializeField] private Transform  skillBarRoot;
    [SerializeField] private GameObject skillBarSlotPrefab;

    // ====================================================
    //  UI - 데미지 팝업
    // ====================================================
    [Header("데미지 팝업")]
    [SerializeField] private Canvas     battleCanvas;
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private float popupRiseHeight = 80f;
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
    //  내부 데이터
    // ====================================================
    private CombatUnit    playerUnit;
    private CombatUnit    opponentUnit;
    private BattleReport  report;
    private ATBTimeline   timeline;
    private BuffSystem    buffSystem;
    private SkillSelector skillSelector;
    private CombatResolver resolver;

    private BattleDirective currentDirective = BattleDirective.Normal;
    private bool battleActive = false;
    private bool isPaused     = false;
    private int  turnCount    = 0;
    private string logBuffer  = "";

    private Vector3 playerStartPos;
    private Vector3 opponentStartPos;

    private readonly List<GameObject>   timelineSlots = new();
    private readonly List<SkillBarSlot> skillBarSlots = new();
    private Camera mainCam;

    private class SkillBarSlot
    {
        public SkillData  skill;
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
        // 전투 데이터 없으면 더미 유닛으로 테스트
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

        SetupButtons();
        BuildTimelineSlots();
        BuildSkillBarSlots();

        if (panelResult) panelResult.SetActive(false);
        RefreshHP();
        RefreshTimeline();
        RefreshSkillBar();
        UpdateDirectiveLabel();

        StartCoroutine(BattleLoop());
    }

    // ====================================================
    //  버튼 리스너
    // ====================================================
    void SetupButtons()
    {
        if (btnAggressive) btnAggressive.onClick.AddListener(() => SetDirective(BattleDirective.Aggressive));
        if (btnNormal)     btnNormal    .onClick.AddListener(() => SetDirective(BattleDirective.Normal));
        if (btnDefensive)  btnDefensive .onClick.AddListener(() => SetDirective(BattleDirective.Defensive));
        if (btnTechnical)  btnTechnical .onClick.AddListener(() => SetDirective(BattleDirective.Technical));
        if (btnPause)      btnPause     .onClick.AddListener(TogglePause);
        if (btnCloseResult) btnCloseResult.onClick.AddListener(() => { if (panelResult) panelResult.SetActive(false); });
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
        SetBtnColor(btnAggressive, currentDirective == BattleDirective.Aggressive);
        SetBtnColor(btnNormal,     currentDirective == BattleDirective.Normal);
        SetBtnColor(btnDefensive,  currentDirective == BattleDirective.Defensive);
        SetBtnColor(btnTechnical,  currentDirective == BattleDirective.Technical);
    }

    void SetBtnColor(Button btn, bool selected)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img) img.color = selected ? colorDirectiveSelected : colorDirectiveNormal;
    }

    // ====================================================
    //  ATB 슬롯 생성
    // ====================================================
    void BuildTimelineSlots()
    {
        if (timelineRoot == null || timelineSlotPrefab == null) return;
        foreach (var s in timelineSlots) if (s) Destroy(s);
        timelineSlots.Clear();

        for (int i = 0; i < timelineDisplayCount; i++)
        {
            var go = Instantiate(timelineSlotPrefab, timelineRoot);
            go.SetActive(false);
            timelineSlots.Add(go);
        }
    }

    // ====================================================
    //  ATB 슬롯 갱신
    // ====================================================
    void RefreshTimeline()
    {
        if (timeline == null || timelineSlots.Count == 0) return;

        var order = timeline.GetTimeline(timelineSlots.Count);

        for (int i = 0; i < timelineSlots.Count; i++)
        {
            var slot = timelineSlots[i];
            if (slot == null) continue;

            if (i >= order.Count) { slot.SetActive(false); continue; }
            slot.SetActive(true);

            var (unit, av) = order[i];

            var iconImg = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                bool isPlayer  = (unit == playerUnit);
                iconImg.sprite = isPlayer ? iconPlayer : iconOpponent;
                iconImg.color  = (i == 0) ? Color.white : new Color(0.75f, 0.75f, 0.75f);
            }

            var avText = slot.transform.Find("AVText")?.GetComponent<TMP_Text>();
            if (avText != null)
                avText.text = i == 0 ? ">>" : "";

            var bg = slot.GetComponent<Image>();
            if (bg != null)
                bg.color = i == 0
                    ? new Color(1f, 0.9f, 0.2f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.25f, 0.85f);
        }
    }

    // ====================================================
    //  스킬 바 슬롯 생성
    // ====================================================
    void BuildSkillBarSlots()
    {
        if (skillBarRoot == null || skillBarSlotPrefab == null) return;

        for (int i = skillBarRoot.childCount - 1; i >= 0; i--)
            Destroy(skillBarRoot.GetChild(i).gameObject);
        skillBarSlots.Clear();

        if (playerUnit == null) return;

        foreach (var skill in playerUnit.equippedSkills)
        {
            if (skill == null) continue;
            var go   = Instantiate(skillBarSlotPrefab, skillBarRoot);
            var slot = new SkillBarSlot { skill = skill };

            slot.nameText       = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            slot.cooldownText   = go.transform.Find("CooldownText")?.GetComponent<TMP_Text>();
            slot.cooldownSlider = go.transform.Find("CooldownSlider")?.GetComponent<Slider>();
            slot.overlay        = go.transform.Find("Overlay")?.GetComponent<Image>();

            if (slot.nameText != null) slot.nameText.text = skill.skillName;
            skillBarSlots.Add(slot);
        }
    }

    // ====================================================
    //  스킬 바 갱신
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
            if (slot.overlay != null) slot.overlay.gameObject.SetActive(!ready);
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

            CombatUnit attacker     = timeline.AdvanceAndGetNext();
            CombatUnit defender     = (attacker == playerUnit) ? opponentUnit : playerUnit;
            bool       isPlayerTurn = (attacker == playerUnit);

            Animator  attackerAnim  = isPlayerTurn ? playerAnimator   : opponentAnimator;
            Animator  defenderAnim  = isPlayerTurn ? opponentAnimator : playerAnimator;
            Transform attackerTr    = isPlayerTurn ? playerTransform   : opponentTransform;
            Transform defenderTr    = isPlayerTurn ? opponentTransform : playerTransform;
            Vector3   atkStartPos   = isPlayerTurn ? playerStartPos    : opponentStartPos;

            attacker.TickCooldowns();
            buffSystem.OnUnitTurnStart(attacker);

            BattleDirective directive = isPlayerTurn ? currentDirective : BattleDirective.Normal;
            SkillData skill = skillSelector.SelectSkill(attacker, defender, directive);
            if (skill == null) { Debug.LogError("[BattleScene] 스킬 null"); yield break; }

            AppendLog($"<b>{attacker.unitName}</b>: {skill.skillName} 시전!");
            RefreshTimeline();

            yield return StartCoroutine(AttackSequence(
                attackerAnim, defenderAnim,
                attackerTr, defenderTr, atkStartPos,
                attacker, defender, skill, isPlayerTurn));

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
        string atkTrigger = !string.IsNullOrEmpty(skill.animationTrigger)    ? skill.animationTrigger    : "Attack";
        string hitTrigger = !string.IsNullOrEmpty(skill.hitAnimationTrigger) ? skill.hitAnimationTrigger : "Hit";

        // 1. 돌진
        if (attackerAnim) attackerAnim.speed = 1;
        bool has3D = attackerTr != null && defenderTr != null;
        if (has3D)
        {
            Vector3 targetPos = Vector3.Lerp(startPos, defenderTr.position, 0.75f);
            yield return StartCoroutine(MoveToPosition(attackerTr, targetPos, attackMoveSpeed));
        }
        else
        {
            yield return new WaitForSeconds(0.15f);
        }

        // 2. 공격 트리거
        if (attackerAnim) attackerAnim.SetTrigger(atkTrigger);

        // 3. 애니메이션 진입 대기 (타임아웃 1초)
        if (attackerAnim != null)
        {
            float w = 0f;
            yield return null;
            while (!attackerAnim.GetCurrentAnimatorStateInfo(0).IsName(atkTrigger) && w < 1f)
            { w += Time.deltaTime; yield return null; }

            // 4. 타격 타이밍 40% (타임아웃 2초)
            w = 0f;
            while (attackerAnim.GetCurrentAnimatorStateInfo(0).IsName(atkTrigger) &&
                   attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.4f && w < 2f)
            { w += Time.deltaTime; yield return null; }
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        // 5. 데미지 판정
        DamageResult dmgResult = resolver.Resolve(attacker, defender, skill);
        report.LogAction(attacker, skill, dmgResult, turnCount, isPlayerTurn);

        Transform defWorldTr = defenderTr ?? (isPlayerTurn ? opponentTransform : playerTransform);

        // 6. 결과 처리
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
                SpawnDamagePopup(defWorldTr,
                    isCrit ? $"<b>{dmgResult.finalDamage:F0}</b>" : $"{dmgResult.finalDamage:F0}",
                    isCrit ? colorCritical : colorHit, isCrit ? 1.6f : 1f);
                logMsg = isCrit
                    ? $"  → <color=red>크리티컬!</color> {defender.unitName}에게 <color=red>{dmgResult.finalDamage:F0}</color> 데미지!"
                    : $"  → {defender.unitName}에게 <color=yellow>{dmgResult.finalDamage:F0}</color> 데미지!";
                break;

            case HitOutcome.Evaded:
                SpawnDamagePopup(defWorldTr, "EVADE", colorEvade, 0.85f);
                logMsg = $"  → {defender.unitName} 회피!";
                break;

            default:
                SpawnDamagePopup(defWorldTr, "MISS", colorMiss, 0.85f);
                logMsg = "  → 빗나감!";
                break;
        }
        AppendLog(logMsg);

        // 7. 공격 종료 대기 (타임아웃 2초)
        if (attackerAnim != null)
        {
            float w = 0f;
            while (attackerAnim.GetCurrentAnimatorStateInfo(0).IsName(atkTrigger) &&
                   attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.85f && w < 2f)
            { w += Time.deltaTime; yield return null; }
        }

        // 8. 피격 종료 대기 (타임아웃 1.5초)
        if (defenderAnim != null && defenderAnim.GetCurrentAnimatorStateInfo(0).IsName(hitTrigger))
        {
            float w = 0f;
            while (defenderAnim.GetCurrentAnimatorStateInfo(0).IsName(hitTrigger) &&
                   defenderAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.85f && w < 1.5f)
            { w += Time.deltaTime; yield return null; }
        }

        // 9. 복귀
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
            var outline = popupGo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor    = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        if (tmp == null) { Destroy(popupGo); yield break; }

        tmp.text     = text;
        tmp.color    = color;
        tmp.fontSize = Mathf.RoundToInt(tmp.fontSize * sizeScale);

        RectTransform rt = popupGo.GetComponent<RectTransform>() ?? popupGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 100f);

        // 팝업 위치: 3D Transform 있으면 월드→캔버스 변환, 없으면 고정 위치
        Vector3 startAnchor;
        if (defenderTr != null)
            startAnchor = WorldToCanvasPos(defenderTr.position + Vector3.up * 1.8f);
        else
            startAnchor = (defenderTr == opponentTransform || opponentTransform == null)
                ? new Vector3(320f,  180f, 0f)
                : new Vector3(-320f, -180f, 0f);

        rt.anchoredPosition = startAnchor;
        Vector3 endAnchor = startAnchor + new Vector3(Random.Range(-60f, 60f), popupRiseHeight, 0f);

        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / popupDuration;
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            rt.anchoredPosition = Vector3.Lerp(startAnchor, endAnchor, eased);
            float alpha = t >= popupFadeStart ? 1f - (t - popupFadeStart) / (1f - popupFadeStart) : 1f;
            tmp.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        Destroy(popupGo);
    }

    Vector3 WorldToCanvasPos(Vector3 worldPos)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || battleCanvas == null) return Vector3.zero;

        Vector2 screenPos  = mainCam.WorldToScreenPoint(worldPos);
        RectTransform crt  = battleCanvas.GetComponent<RectTransform>();
        if (crt == null) return screenPos;

        if (battleCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(crt, screenPos, null, out Vector2 local);
            return local;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(crt, screenPos, battleCanvas.worldCamera, out Vector2 localCam);
        return localCam;
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
    //  HP 갱신
    // ====================================================
    void RefreshHP()
    {
        if (playerUnit != null)
        {
            float max = playerUnit.derived.MaxHP;
            float cur = Mathf.Max(0, playerUnit.currentHP);
            float ratio = max > 0 ? cur / max : 0f;
            if (sliderPlayerHP)  sliderPlayerHP.value  = ratio;
            if (textPlayerHP)    textPlayerHP.text      = $"{cur:F0} / {max:F0}";
            if (textPlayerName)  textPlayerName.text    = playerUnit.unitName;
            if (fillPlayerHP)    fillPlayerHP.color     = GetHPColor(ratio);
        }
        if (opponentUnit != null)
        {
            float max = opponentUnit.derived.MaxHP;
            float cur = Mathf.Max(0, opponentUnit.currentHP);
            float ratio = max > 0 ? cur / max : 0f;
            if (sliderOpponentHP)  sliderOpponentHP.value  = ratio;
            if (textOpponentHP)    textOpponentHP.text      = $"{cur:F0} / {max:F0}";
            if (textOpponentName)  textOpponentName.text    = opponentUnit.unitName;
            if (fillOpponentHP)    fillOpponentHP.color     = GetHPColor(ratio);
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
    //  더미 유닛 (테스트용)
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
    //  기본 스킬 부여
    // ====================================================
    private static void AssignDefaultSkills(CombatUnit unit)
    {
        unit.equippedSkills.Clear();

        var s1 = ScriptableObject.CreateInstance<SkillData>();
        s1.skillName = "일반공격"; s1.category = SkillCategory.Strike;
        s1.weight = 60; s1.damageMultiplier = 1.0f;
        s1.animationTrigger = "Attack"; s1.hitAnimationTrigger = "Hit";

        var s2 = ScriptableObject.CreateInstance<SkillData>();
        s2.skillName = "방어자세"; s2.category = SkillCategory.Defense;
        s2.weight = 30; s2.damageMultiplier = 0.5f; s2.cooldownTurns = 3;
        s2.animationTrigger = "Attack_Guard"; s2.hitAnimationTrigger = "Hit";

        var s3 = ScriptableObject.CreateInstance<SkillData>();
        s3.skillName = "빠른발놀림"; s3.category = SkillCategory.Mobility;
        s3.weight = 40; s3.damageMultiplier = 0.8f; s3.avAdvance = 500f; s3.cooldownTurns = 2;
        s3.animationTrigger = "Attack_Step"; s3.hitAnimationTrigger = "Hit";

        unit.equippedSkills.Add(s1);
        unit.equippedSkills.Add(s2);
        unit.equippedSkills.Add(s3);
    }
}
