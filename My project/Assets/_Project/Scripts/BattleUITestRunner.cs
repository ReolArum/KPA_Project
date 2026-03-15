// ===== BattleUITestRunner.cs =====
// ★ 실험용 단독 전투 테스트 스크립트 ★
// 기존 스크립트를 전혀 건드리지 않습니다.
//
// [사용 방법]
// 1. 빈 씬(또는 BattleScene)에 빈 GameObject를 하나 만든다.
// 2. 이 스크립트를 그 GameObject에 붙인다.
// 3. Play → 화면에 전투 UI가 자동으로 생성되어 바로 전투가 시작됩니다.
//    (BattleSceneData / 외부 씬 전환 불필요)
//
// [테스트 더미 유닛]
//   플레이어: STR20 / AGI15 / VIT15 / INT5 / GUT5 / SEN10
//   상대    : ArenaRank.Silver 기준 랜덤 생성
//
// ──────────────────────────────────────────────────────────

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUITestRunner : MonoBehaviour
{
    // =====================================================
    //  Inspector - 연출 설정 (선택, 기본값으로 바로 실행 가능)
    // =====================================================
    [Header("연출 (선택)")]
    [SerializeField] float turnDelay    = 0.6f;
    [SerializeField] float resultDelay  = 1.5f;
    [SerializeField] int   maxTurns     = 60;

    [Header("팝업 설정")]
    [SerializeField] float popupRiseHeight = 80f;   // Canvas px 단위
    [SerializeField] float popupDuration   = 0.9f;
    [SerializeField] float popupFadeStart  = 0.5f;

    // =====================================================
    //  전투 시스템 (기존 코드 그대로 사용)
    // =====================================================
    CombatUnit     playerUnit;
    CombatUnit     opponentUnit;
    BattleReport   report;
    ATBTimeline    timeline;
    BuffSystem     buffSystem;
    SkillSelector  skillSelector;
    CombatResolver resolver;

    BattleDirective directive = BattleDirective.Normal;
    bool battleActive = false;
    bool isPaused     = false;
    int  turnCount    = 0;
    string logBuffer  = "";

    // =====================================================
    //  UI 참조 (코드로 자동 생성)
    // =====================================================
    Canvas      cvs;

    // HP
    Slider      sliderPlayerHP,  sliderOpponentHP;
    Image       fillPlayerHP,    fillOpponentHP;
    TMP_Text    textPlayerHP,    textOpponentHP;
    TMP_Text    textPlayerName,  textOpponentName;

    // ATB 서열바
    List<(Image icon, TMP_Text avTxt, Image bg)> tlSlots = new();
    Sprite      sprPlayer,  sprOpponent;   // 아이콘 (단색 텍스처)

    // 방침 버튼
    Button      btnAggressive, btnNormal, btnDefensive, btnTechnical, btnPause;
    TMP_Text    textDirective;
    readonly Color colSelected = new Color(1f, 0.85f, 0.1f);
    readonly Color colNormal   = new Color(0.22f, 0.22f, 0.28f);

    // 스킬 바
    class SkillSlot { public SkillData skill; public TMP_Text nmTxt, cdTxt; public Slider cdSlider; public Image overlay; }
    List<SkillSlot> skillSlots = new();

    // 로그
    TMP_Text    textLog;
    ScrollRect  scrollLog;

    // 결과
    GameObject  panelResult;
    TMP_Text    textResult;
    Button      btnClose;

    // =====================================================
    //  색상 상수
    // =====================================================
    static readonly Color C_HIT      = new Color(1f,   0.95f, 0.3f);
    static readonly Color C_CRIT     = new Color(1f,   0.25f, 0.1f);
    static readonly Color C_MISS     = new Color(0.6f, 0.6f,  0.6f);
    static readonly Color C_EVADE    = new Color(0.4f, 0.85f, 1f);
    static readonly Color C_PANEL_BG = new Color(0.1f, 0.1f,  0.15f, 0.92f);
    static readonly Color C_DARK     = new Color(0.13f,0.13f, 0.18f, 1f);
    static readonly Color C_GREEN    = new Color(0.2f, 0.85f, 0.3f);
    static readonly Color C_YELLOW   = new Color(1f,   0.85f, 0.15f);
    static readonly Color C_RED      = new Color(0.9f, 0.15f, 0.15f);

    // =====================================================
    //  Start
    // =====================================================
    void Start()
    {
        // 1. 유닛 생성 (더미 데이터)
        playerUnit = MakePlayerUnit();
        opponentUnit = CombatUnit.CreateOpponent(ArenaRank.Silver, 3);

        // 스킬 부여
        AssignDefaultSkills(playerUnit);
        AssignDefaultSkills(opponentUnit);

        // 2. 전투 시스템
        report       = new BattleReport();
        timeline     = new ATBTimeline();
        buffSystem   = new BuffSystem(timeline);
        skillSelector = new SkillSelector();
        resolver     = new CombatResolver();
        timeline.Initialize(playerUnit, opponentUnit);

        // 3. UI 빌드
        BuildUI();

        // 4. 초기 UI 갱신
        RefreshHP();
        RefreshTimeline();
        RefreshSkillBar();
        HighlightDirective();

        // 5. 전투 시작
        StartCoroutine(BattleLoop());
    }

    // =====================================================
    //  유닛 생성 헬퍼
    // =====================================================
    CombatUnit MakePlayerUnit()
    {
        var u = new CombatUnit();
        u.unitName = "내 클론";
        u.rawStats = new CombatBaseStats { STR=20, AGI=15, VIT=15, INT=5, GUT=5, SEN=10 };
        u.schoolType  = SchoolType.Crusher;
        u.schoolLevel = 1;
        u.Recalculate();
        u.currentHP = u.derived.MaxHP;
        u.currentAV = u.derived.SPD > 0 ? 10000f / u.derived.SPD : 10000f;
        return u;
    }

    static void AssignDefaultSkills(CombatUnit unit)
    {
        unit.equippedSkills.Clear();

        SkillData Mk(string name, SkillCategory cat, float w, float dmg, int cd = 0, float avAdv = 0)
        {
            var s = ScriptableObject.CreateInstance<SkillData>();
            s.skillName = name; s.category = cat;
            s.weight = w; s.damageMultiplier = dmg;
            s.cooldownTurns = cd; s.avAdvance = avAdv;
            return s;
        }

        unit.equippedSkills.Add(Mk("일반공격",   SkillCategory.Strike,   60, 1.0f, 0));
        unit.equippedSkills.Add(Mk("방어자세",   SkillCategory.Defense,  30, 0.5f, 2));
        unit.equippedSkills.Add(Mk("빠른발놀림", SkillCategory.Mobility, 40, 0.8f, 3, 500f));
    }

    // =====================================================
    //  전투 루프
    // =====================================================
    IEnumerator BattleLoop()
    {
        battleActive = true;
        yield return new WaitForSeconds(0.5f);

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

            CombatUnit attacker = timeline.AdvanceAndGetNext();
            CombatUnit defender = attacker == playerUnit ? opponentUnit : playerUnit;
            bool isPlayer = attacker == playerUnit;

            attacker.TickCooldowns();
            buffSystem.OnUnitTurnStart(attacker);

            BattleDirective dir = isPlayer ? directive : BattleDirective.Normal;
            SkillData skill = skillSelector.SelectSkill(attacker, defender, dir);
            if (skill == null) yield break;

            Log($"<b>{attacker.unitName}</b>: <color=#aef>{skill.skillName}</color> 시전!");
            RefreshTimeline();

            // 데미지 판정
            yield return new WaitForSeconds(0.15f);

            DamageResult dmg = resolver.Resolve(attacker, defender, skill);
            report.LogAction(attacker, skill, dmg, turnCount, isPlayer);

            // 피격 처리
            Transform defTr = isPlayer ? null : null; // 3D 없음
            RectTransform defHpRect = isPlayer ? GetHPRect(false) : GetHPRect(true);

            switch (dmg.outcome)
            {
                case HitOutcome.Hit:
                case HitOutcome.Critical:
                    defender.currentHP = Mathf.Max(0, defender.currentHP - dmg.finalDamage);
                    if (skill.avAdvance > 0) timeline.AdvanceUnit(attacker, skill.avAdvance);
                    if (skill.avDelay   > 0) timeline.DelayUnit(defender, skill.avDelay);
                    bool crit = dmg.outcome == HitOutcome.Critical;
                    SpawnPopup(defHpRect,
                        crit ? $"<b>{dmg.finalDamage:F0}</b>" : $"{dmg.finalDamage:F0}",
                        crit ? C_CRIT : C_HIT, crit ? 1.6f : 1f);
                    Log(crit
                        ? $"  → <color=#f44>크리티컬!</color> {defender.unitName}에게 <color=#f44>{dmg.finalDamage:F0}</color> 데미지!"
                        : $"  → {defender.unitName}에게 <color=#ff4>{dmg.finalDamage:F0}</color> 데미지!");
                    break;
                case HitOutcome.Evaded:
                    SpawnPopup(defHpRect, "EVADE", C_EVADE, 0.85f);
                    Log($"  → {defender.unitName} 회피!");
                    break;
                default:
                    SpawnPopup(defHpRect, "MISS", C_MISS, 0.85f);
                    Log("  → 빗나감!");
                    break;
            }

            attacker.SetCooldown(skill);
            RefreshHP();
            RefreshTimeline();
            RefreshSkillBar();

            if (defender.currentHP <= 0)
            { report.playerWon = isPlayer; yield return StartCoroutine(BattleEnd(isPlayer)); yield break; }
            if (turnCount >= maxTurns)
            { report.playerWon = playerUnit.currentHP >= opponentUnit.currentHP;
              yield return StartCoroutine(BattleEnd(report.playerWon)); yield break; }

            yield return new WaitForSeconds(turnDelay);
        }
    }

    IEnumerator BattleEnd(bool won)
    {
        battleActive = false;
        string col = won ? "<color=#4f4>" : "<color=#f44>";
        Log($"\n{col}===== {(won ? "승리!" : "패배...")} =====</color>");

        yield return new WaitForSeconds(resultDelay);

        if (panelResult != null)
        {
            textResult.text = report.ToReportString();
            panelResult.SetActive(true);
        }
    }

    // =====================================================
    //  HP 슬라이더 위치 참조 (팝업 좌표용)
    // =====================================================
    RectTransform GetHPRect(bool isPlayer)
        => isPlayer ? sliderPlayerHP?.GetComponent<RectTransform>()
                    : sliderOpponentHP?.GetComponent<RectTransform>();

    // =====================================================
    //  데미지 팝업
    // =====================================================
    void SpawnPopup(RectTransform anchor, string text, Color color, float scale)
    {
        if (cvs == null || anchor == null) return;
        StartCoroutine(PopupRoutine(anchor, text, color, scale));
    }

    IEnumerator PopupRoutine(RectTransform anchor, string text, Color color, float scale)
    {
        // 팝업 오브젝트 생성
        var go  = new GameObject("DmgPopup");
        go.transform.SetParent(cvs.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = Mathf.RoundToInt(52 * scale);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;

        // Outline (그림자)
        var outline = go.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor    = new Color(0, 0, 0, 0.85f);
        outline.effectDistance = new Vector2(2, -2);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 100);

        // 앵커 좌표 기준 시작 위치 계산
        Vector2 anchorScreenPos = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cvs.GetComponent<RectTransform>(), anchorScreenPos, null, out Vector2 localPos);
        localPos += new Vector2(Random.Range(-50f, 50f), 20f);   // 살짝 위 + 가로 흔들림

        Vector2 startPos = localPos;
        Vector2 endPos   = localPos + new Vector2(0, popupRiseHeight);

        // 애니메이션
        float t = 0;
        while (t < popupDuration)
        {
            t += Time.deltaTime;
            float n = t / popupDuration;
            float eased = 1 - Mathf.Pow(1 - n, 2f);   // easeOut

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

            float alpha = n >= popupFadeStart
                ? 1f - (n - popupFadeStart) / (1f - popupFadeStart)
                : 1f;
            tmp.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }
        Destroy(go);
    }

    // =====================================================
    //  UI 갱신
    // =====================================================
    void RefreshHP()
    {
        void ApplyHP(Slider sl, Image fill, TMP_Text txt, CombatUnit u)
        {
            if (u == null || sl == null) return;
            float ratio = u.derived.MaxHP > 0 ? u.currentHP / u.derived.MaxHP : 0;
            sl.value  = Mathf.Clamp01(ratio);
            if (txt)  txt.text = $"{Mathf.Max(0,u.currentHP):F0} / {u.derived.MaxHP:F0}";
            if (fill) fill.color = HPColor(ratio);
        }
        ApplyHP(sliderPlayerHP,   fillPlayerHP,   textPlayerHP,   playerUnit);
        ApplyHP(sliderOpponentHP, fillOpponentHP, textOpponentHP, opponentUnit);
    }

    Color HPColor(float r) =>
        r > 0.5f ? Color.Lerp(C_YELLOW, C_GREEN, (r - 0.5f) * 2f)
                 : Color.Lerp(C_RED,    C_YELLOW, r * 2f);

    void RefreshTimeline()
    {
        if (timeline == null) return;
        var order = timeline.GetTimeline();
        float maxAV = playerUnit.derived.SPD > 0 ? 10000f / playerUnit.derived.SPD : 1000f;

        for (int i = 0; i < tlSlots.Count; i++)
        {
            var (ico, avTxt, bg) = tlSlots[i];
            if (i >= order.Count) { bg.gameObject.SetActive(false); continue; }
            bg.gameObject.SetActive(true);

            var (unit, av) = order[i];
            bool isP = unit == playerUnit;

            ico.color  = i == 0 ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            // 색: 플레이어=파란색, 상대=빨간색 (단색 아이콘)
            ico.color  *= isP ? new Color(0.5f,0.7f,1f) : new Color(1f,0.5f,0.5f);
            avTxt.text = i == 0 ? "▶" : Mathf.RoundToInt(Mathf.Clamp01(av / (maxAV * 1.5f)) * 100).ToString();
            bg.color   = i == 0 ? new Color(1f,0.9f,0.2f,0.9f) : new Color(0.15f,0.15f,0.25f,0.85f);
        }
    }

    void RefreshSkillBar()
    {
        foreach (var s in skillSlots)
        {
            if (s?.skill == null) continue;
            int cd    = playerUnit.GetCooldown(s.skill);
            bool rdy  = cd <= 0;
            if (s.cdTxt)    s.cdTxt.text = rdy ? "준비" : $"{cd}턴";
            if (s.cdSlider) s.cdSlider.value = s.skill.cooldownTurns > 0
                ? 1f - (float)cd / s.skill.cooldownTurns : 1f;
            if (s.overlay)  s.overlay.gameObject.SetActive(!rdy);
        }
    }

    void HighlightDirective()
    {
        void Set(Button b, BattleDirective d)
        {
            if (b == null) return;
            b.GetComponent<Image>().color = directive == d ? colSelected : colNormal;
        }
        Set(btnAggressive, BattleDirective.Aggressive);
        Set(btnNormal,     BattleDirective.Normal);
        Set(btnDefensive,  BattleDirective.Defensive);
        Set(btnTechnical,  BattleDirective.Technical);

        if (textDirective) textDirective.text = directive switch
        {
            BattleDirective.Aggressive => "◆ 밀어붙여",
            BattleDirective.Normal     => "◆ 평소대로",
            BattleDirective.Defensive  => "◆ 버텨",
            BattleDirective.Technical  => "◆ 기술위주",
            _ => ""
        };
    }

    void Log(string line)
    {
        logBuffer += line + "\n";
        if (textLog) textLog.text = logBuffer;
        if (scrollLog != null)
        { Canvas.ForceUpdateCanvases(); scrollLog.verticalNormalizedPosition = 0f; }
    }

    // =====================================================
    //  UI 자동 빌드 (Canvas ~ 버튼 전부 코드 생성)
    // =====================================================
    void BuildUI()
    {
        // ── Canvas ──────────────────────────────────────
        var cvsGo = new GameObject("BattleTestCanvas");
        cvs = cvsGo.AddComponent<Canvas>();
        cvs.renderMode = RenderMode.ScreenSpaceOverlay;
        cvs.sortingOrder = 100;
        cvsGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cvsGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        cvsGo.AddComponent<GraphicRaycaster>();

        var cRect = cvsGo.GetComponent<RectTransform>();

        // ── 공통 헬퍼 ───────────────────────────────────
        RectTransform Anchor(GameObject go, Vector2 aMin, Vector2 aMax,
                             Vector2 off0, Vector2 off1)
        {
            var r = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.offsetMin = off0; r.offsetMax = off1;
            return r;
        }

        GameObject Panel(string name, Transform parent, Color c,
                         Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = c;
            Anchor(go, aMin, aMax, o0, o1);
            return go;
        }

        TMP_Text Txt(string name, Transform parent, string text,
                     float size, Color col, TextAlignmentOptions align,
                     Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
            Anchor(go, aMin, aMax, o0, o1);
            return t;
        }

        Button Btn(string label, Transform parent, Color bg,
                   Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1,
                   System.Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            Anchor(go, aMin, aMax, o0, o1);
            Txt(label, go.transform, label, 22, Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        Slider MakeSlider(string name, Transform parent, Color fillColor,
                          Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Anchor(go, aMin, aMax, o0, o1);

            // Background
            var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.1f,0.1f,0.1f);
            Anchor(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Fill Area
            var fa = new GameObject("FillArea"); fa.transform.SetParent(go.transform, false);
            Anchor(fa, Vector2.zero, Vector2.one, new Vector2(5,0), new Vector2(-5,0));

            var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = fillColor;
            Anchor(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var sl = go.AddComponent<Slider>();
            sl.fillRect = fill.GetComponent<RectTransform>();
            sl.targetGraphic = fillImg;
            sl.direction = Slider.Direction.LeftToRight;
            sl.value = 1f;

            return sl;
        }

        // ════════════════════════════════════════════════
        //  1. 상대 HP (상단)
        // ════════════════════════════════════════════════
        var oppPanel = Panel("OppHP", cvsGo.transform, C_PANEL_BG,
            new Vector2(0.3f,0), new Vector2(0.7f,1),
            new Vector2(0,10), new Vector2(0,-950));
        // 이름
        textOpponentName = Txt("OppName", oppPanel.transform,
            opponentUnit.unitName, 30, Color.white, TextAlignmentOptions.Left,
            Vector2.zero, Vector2.one, new Vector2(10,-5), new Vector2(-10,-45));
        // HP 슬라이더
        sliderOpponentHP = MakeSlider("OppSlider", oppPanel.transform, C_GREEN,
            new Vector2(0,0), new Vector2(1,1),
            new Vector2(10,10), new Vector2(-10,-50));
        fillOpponentHP = sliderOpponentHP.fillRect.GetComponent<Image>();
        // HP 수치
        textOpponentHP = Txt("OppHPTxt", oppPanel.transform,
            "", 22, Color.white, TextAlignmentOptions.Right,
            Vector2.zero, Vector2.one, new Vector2(0,10), new Vector2(-10,-50));

        // ════════════════════════════════════════════════
        //  2. 플레이어 HP (하단)
        // ════════════════════════════════════════════════
        var plyPanel = Panel("PlyHP", cvsGo.transform, C_PANEL_BG,
            new Vector2(0.3f,0), new Vector2(0.7f,1),
            new Vector2(0,870), new Vector2(0,-10));
        textPlayerName = Txt("PlyName", plyPanel.transform,
            playerUnit.unitName, 30, Color.white, TextAlignmentOptions.Left,
            Vector2.zero, Vector2.one, new Vector2(10,-5), new Vector2(-10,-45));
        sliderPlayerHP = MakeSlider("PlySlider", plyPanel.transform, C_GREEN,
            new Vector2(0,0), new Vector2(1,1),
            new Vector2(10,10), new Vector2(-10,-50));
        fillPlayerHP = sliderPlayerHP.fillRect.GetComponent<Image>();
        textPlayerHP = Txt("PlyHPTxt", plyPanel.transform,
            "", 22, Color.white, TextAlignmentOptions.Right,
            Vector2.zero, Vector2.one, new Vector2(0,10), new Vector2(-10,-50));

        // ════════════════════════════════════════════════
        //  3. ATB 행동 서열바 (좌측 세로)
        // ════════════════════════════════════════════════
        var tlPanel = Panel("Timeline", cvsGo.transform, new Color(0.08f,0.08f,0.12f,0.95f),
            Vector2.zero, Vector2.one,
            new Vector2(10,200), new Vector2(-1760,-200));
        var tlLayout = tlPanel.AddComponent<VerticalLayoutGroup>();
        tlLayout.spacing = 4; tlLayout.childForceExpandHeight = false;
        tlLayout.childAlignment = TextAnchor.UpperCenter;
        tlLayout.padding = new RectOffset(4,4,4,4);
        tlPanel.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        int tlCount = 6;
        tlSlots.Clear();
        for (int i = 0; i < tlCount; i++)
        {
            var slot = new GameObject($"TL_{i}");
            slot.transform.SetParent(tlPanel.transform, false);
            var slotImg = slot.AddComponent<Image>();
            slotImg.color = C_DARK;
            var slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(120, 80);
            var slotLayout = slot.AddComponent<HorizontalLayoutGroup>();
            slotLayout.childForceExpandWidth = false;
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.spacing = 4;
            slotLayout.padding = new RectOffset(6,6,6,6);

            // 아이콘 (단색 원형 느낌)
            var icoGo = new GameObject("Icon");
            icoGo.transform.SetParent(slot.transform, false);
            var icoImg = icoGo.AddComponent<Image>();
            icoImg.color = new Color(0.4f,0.6f,1f);
            var icoRect = icoGo.GetComponent<RectTransform>();
            icoRect.sizeDelta = new Vector2(52, 52);
            var icoLE = icoGo.AddComponent<LayoutElement>();
            icoLE.preferredWidth = 52; icoLE.preferredHeight = 52;

            // AV 수치
            var avGo = new GameObject("AVText");
            avGo.transform.SetParent(slot.transform, false);
            var avTmp = avGo.AddComponent<TextMeshProUGUI>();
            avTmp.text = "--"; avTmp.fontSize = 24;
            avTmp.color = Color.white;
            avTmp.alignment = TextAlignmentOptions.Center;
            var avLE = avGo.AddComponent<LayoutElement>();
            avLE.preferredWidth = 44; avLE.preferredHeight = 52;

            tlSlots.Add((icoImg, avTmp, slotImg));
        }

        // ════════════════════════════════════════════════
        //  4. 방침 버튼 (우측)
        // ════════════════════════════════════════════════
        var dirPanel = Panel("Directive", cvsGo.transform, new Color(0.08f,0.08f,0.12f,0.95f),
            Vector2.one, Vector2.one,
            new Vector2(-190,220), new Vector2(-10,-200));
        var dirLayout = dirPanel.AddComponent<VerticalLayoutGroup>();
        dirLayout.spacing = 6; dirLayout.childForceExpandHeight = false;
        dirLayout.padding = new RectOffset(8,8,8,8);
        dirLayout.childAlignment = TextAnchor.UpperCenter;
        dirPanel.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // 방침 레이블
        textDirective = Txt("DirLabel", dirPanel.transform,
            "◆ 평소대로", 20, C_YELLOW, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var dirLabelLE = textDirective.gameObject.AddComponent<LayoutElement>();
        dirLabelLE.preferredHeight = 30;

        // 방침 버튼 4개
        (string label, BattleDirective dir)[] dirDefs =
        {
            ("밀어붙여",  BattleDirective.Aggressive),
            ("평소대로",  BattleDirective.Normal),
            ("버텨",      BattleDirective.Defensive),
            ("기술위주",  BattleDirective.Technical),
        };
        btnAggressive = btnNormal = btnDefensive = btnTechnical = null;
        foreach (var (lbl, d) in dirDefs)
        {
            var go = new GameObject("DirBtn_" + lbl);
            go.transform.SetParent(dirPanel.transform, false);
            var img = go.AddComponent<Image>(); img.color = colNormal;
            var btn = go.AddComponent<Button>();
            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44; le.preferredWidth = 160;
            BattleDirective captured = d;
            btn.onClick.AddListener(() => { directive = captured; HighlightDirective(); });
            Txt(lbl, go.transform, lbl, 22, Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            if (d == BattleDirective.Aggressive) btnAggressive = btn;
            else if (d == BattleDirective.Normal) btnNormal = btn;
            else if (d == BattleDirective.Defensive) btnDefensive = btn;
            else btnTechnical = btn;
        }

        // 일시정지 버튼
        btnPause = Btn("일시정지", dirPanel.transform, new Color(0.3f,0.3f,0.4f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            () => { isPaused = !isPaused;
                    if (btnPause != null)
                    { var t2 = btnPause.GetComponentInChildren<TMP_Text>();
                      if (t2) t2.text = isPaused ? "재개" : "일시정지"; } });
        btnPause.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

        // ════════════════════════════════════════════════
        //  5. 스킬 바 (하단 중앙)
        // ════════════════════════════════════════════════
        var skillPanel = Panel("SkillBar", cvsGo.transform, new Color(0.08f,0.08f,0.12f,0.95f),
            new Vector2(0.2f,0), new Vector2(0.8f,0),
            new Vector2(0,5), new Vector2(0,115));
        var skillLayout = skillPanel.AddComponent<HorizontalLayoutGroup>();
        skillLayout.spacing = 8; skillLayout.childForceExpandWidth = false;
        skillLayout.padding = new RectOffset(10,10,8,8);
        skillLayout.childAlignment = TextAnchor.MiddleCenter;

        skillSlots.Clear();
        foreach (var skill in playerUnit.equippedSkills)
        {
            if (skill == null) continue;
            var go = new GameObject("Skill_" + skill.skillName);
            go.transform.SetParent(skillPanel.transform, false);
            go.AddComponent<Image>().color = C_DARK;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 130; le.preferredHeight = 100;

            // 스킬 이름
            var nmTxt = Txt("Name", go.transform, skill.skillName, 18,
                Color.white, TextAlignmentOptions.Center,
                new Vector2(0,0.55f), Vector2.one,
                new Vector2(4,-4), new Vector2(-4,-4));

            // 쿨타임 텍스트
            var cdTxt = Txt("CD", go.transform, "준비", 16,
                C_GREEN, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(1,0.55f),
                new Vector2(4,30), new Vector2(-4,-4));

            // 쿨타임 슬라이더
            var cdSl = MakeSlider("CDSlider", go.transform, C_YELLOW,
                Vector2.zero, new Vector2(1,0),
                new Vector2(6,6), new Vector2(-6,20));
            cdSl.value = 1f;

            // 어두운 오버레이 (쿨타임 중)
            var ovGo = new GameObject("Overlay");
            ovGo.transform.SetParent(go.transform, false);
            var ovImg = ovGo.AddComponent<Image>();
            ovImg.color = new Color(0,0,0,0.55f);
            Anchor(ovGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ovGo.SetActive(false);

            skillSlots.Add(new SkillSlot
            {
                skill = skill, nmTxt = nmTxt,
                cdTxt = cdTxt, cdSlider = cdSl, overlay = ovImg
            });
        }

        // ════════════════════════════════════════════════
        //  6. 전투 로그 (중앙 하단)
        // ════════════════════════════════════════════════
        var logPanel = Panel("Log", cvsGo.transform, new Color(0,0,0,0.7f),
            new Vector2(0.15f,0), new Vector2(0.85f,0),
            new Vector2(0,120), new Vector2(0,370));

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(logPanel.transform, false);
        viewport.AddComponent<Image>().color = new Color(0,0,0,0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Anchor(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        textLog = content.AddComponent<TextMeshProUGUI>();
        textLog.fontSize = 18; textLog.color = Color.white;
        textLog.alignment = TextAlignmentOptions.BottomLeft;
        textLog.textWrappingMode = TextWrappingModes.Normal;
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0,0); contentRect.anchorMax = new Vector2(1,1);
        contentRect.offsetMin = new Vector2(8,4); contentRect.offsetMax = new Vector2(-8,-4);
        content.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scrollLog = logPanel.AddComponent<ScrollRect>();
        scrollLog.viewport  = viewport.GetComponent<RectTransform>();
        scrollLog.content   = contentRect;
        scrollLog.horizontal = false;

        // ════════════════════════════════════════════════
        //  7. 결과 패널
        // ════════════════════════════════════════════════
        panelResult = Panel("Result", cvsGo.transform, new Color(0,0,0,0.88f),
            new Vector2(0.2f,0.1f), new Vector2(0.8f,0.9f),
            Vector2.zero, Vector2.zero);
        panelResult.SetActive(false);

        textResult = Txt("ResultTxt", panelResult.transform,
            "", 20, Color.white, TextAlignmentOptions.TopLeft,
            Vector2.zero, Vector2.one, new Vector2(20,60), new Vector2(-20,-20));
        textResult.textWrappingMode = TextWrappingModes.Normal;

        btnClose = Btn("닫기", panelResult.transform, C_RED,
            new Vector2(0.35f,0), new Vector2(0.65f,0),
            new Vector2(0,10), new Vector2(0,55),
            () => panelResult.SetActive(false));
    }
}
