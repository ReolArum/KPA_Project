// ===== BattleSceneController.cs =====
// 전투 씬 통합 컨트롤러
// HP바, ATB 행동 서열바, 방침 버튼, 스킬 쿨타임 UI 포함
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
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator opponentAnimator;
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
    [SerializeField] private Slider    sliderPlayerHP;
    [SerializeField] private TMP_Text  textPlayerHP;      // "cur / max"
    [SerializeField] private TMP_Text  textPlayerName;
    [SerializeField] private Image     fillPlayerHP;      // 색상 변경용 (선택)

    // ====================================================
    //  UI - HP 바 & 이름 (상대)
    // ====================================================
    [Header("상대 HP")]
    [SerializeField] private Slider    sliderOpponentHP;
    [SerializeField] private TMP_Text  textOpponentHP;
    [SerializeField] private TMP_Text  textOpponentName;
    [SerializeField] private Image     fillOpponentHP;

    // ====================================================
    //  UI - ATB 행동 서열바 (세로 리스트, 이미지 참고)
    //  최대 6칸 표시: 각 슬롯 = 아이콘 + AV 수치
    // ====================================================
    [Header("ATB 행동 서열바")]
    [SerializeField] private Transform  timelineRoot;     // 슬롯을 담는 부모 Transform (Vertical Layout)
    [SerializeField] private GameObject timelineSlotPrefab; // 슬롯 프리팹 (Image + TMP_Text)
    [SerializeField] private Sprite     iconPlayer;       // 플레이어 아이콘
    [SerializeField] private Sprite     iconOpponent;     // 상대 아이콘
    [SerializeField] private int        timelineDisplayCount = 6; // 표시할 슬롯 수

    // ====================================================
    //  UI - 방침 버튼
    //  밀어붙여(Aggressive) / 평소대로(Normal) / 버텨(Defensive) / 기술위주(Technical)
    // ====================================================
    [Header("방침 버튼")]
    [SerializeField] private Button    btnAggressive;
    [SerializeField] private Button    btnNormal;
    [SerializeField] private Button    btnDefensive;
    [SerializeField] private Button    btnTechnical;
    [SerializeField] private TMP_Text  textDirectiveLabel;
    [SerializeField] private Button    btnPause;

    // 선택된 방침 버튼 강조색
    [SerializeField] private Color colorDirectiveSelected   = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color colorDirectiveNormal     = Color.white;

    // ====================================================
    //  UI - 스킬 쿨타임 바
    //  플레이어 장착 스킬마다 슬롯 자동 생성
    // ====================================================
    [Header("스킬 쿨타임 바")]
    [SerializeField] private Transform  skillBarRoot;         // 스킬 슬롯 부모 (Horizontal Layout)
    [SerializeField] private GameObject skillBarSlotPrefab;   // 슬롯 프리팹 (Image아이콘 + 이름 + 쿨타임 텍스트 + Slider)

    // ====================================================
    //  UI - 데미지 팝업 텍스트
    //  피격 캐릭터 위치에서 위로 떠오르며 페이드 아웃
    // ====================================================
    [Header("데미지 팝업")]
    [SerializeField] private Canvas     battleCanvas;         // 팝업을 올릴 Canvas (Screen Space - Camera 또는 Overlay)
    [SerializeField] private GameObject damagePopupPrefab;   // 프리팹: TMP_Text 하나짜리 오브젝트
    // 팝업 연출 설정
    [SerializeField] private float popupRiseHeight  = 1.5f;  // 위로 이동 거리 (World 단위)
    [SerializeField] private float popupDuration    = 0.9f;  // 전체 지속 시간(초)
    [SerializeField] private float popupFadeStart   = 0.5f;  // 이 비율 이후부터 페이드 아웃 (0~1)
    // 데미지 종류별 색상
    [SerializeField] private Color colorHit         = new Color(1f, 0.95f, 0.3f);   // 일반 피해: 노란색
    [SerializeField] private Color colorCritical    = new Color(1f, 0.25f, 0.1f);   // 크리티컬: 빨간색
    [SerializeField] private Color colorMiss        = new Color(0.7f, 0.7f, 0.7f);  // 빗나감: 회색
    [SerializeField] private Color colorEvade       = new Color(0.4f, 0.85f, 1f);   // 회피: 하늘색

    // ====================================================
    //  UI - 전투 로그
    // ====================================================
    [Header("전투 로그")]
    [SerializeField] private TMP_Text   textBattleLog;
    [SerializeField] private ScrollRect scrollLog;

    // ====================================================
    //  UI - 결과 패널
    // ====================================================
    [Header("결과 패널")]
    [SerializeField] private GameObject panelResult;
    [SerializeField] private TMP_Text   textResult;
    [SerializeField] private Button     btnCloseResult;

    // ====================================================
    //  전투 시스템
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
    private bool            autoUIBuilt      = false;  // AutoBuildUI 실행 여부

    private Vector3 playerStartPos;
    private Vector3 opponentStartPos;

    // 런타임 생성된 ATB 슬롯들
    private readonly List<GameObject> timelineSlots = new();
    // 런타임 생성된 스킬 슬롯들 (스킬 인덱스 → SkillBarSlot)
    private readonly List<SkillBarSlot> skillBarSlots = new();

    // World → Screen 변환용 카메라 캐시
    private Camera mainCam;

    // ====================================================
    //  내부 클래스: 스킬 바 슬롯 데이터
    // ====================================================
    private class SkillBarSlot
    {
        public SkillData  skill;
        public Image      icon;
        public TMP_Text   nameText;
        public TMP_Text   cooldownText;  // "사용 가능" or "N턴 후"
        public Slider     cooldownSlider; // 0=쿨중, 1=사용가능
        public Image      overlay;        // 반투명 어둠 오버레이 (쿨타임 중)
    }

    // ====================================================
    //  초기화
    // ====================================================
    void Start()
    {
        // BattleSceneData가 없으면 (씬 직접 실행 등) 더미 유닛으로 테스트 모드 진입
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

        // 전투 시스템 초기화
        report       = new BattleReport();
        timeline     = new ATBTimeline();
        buffSystem   = new BuffSystem(timeline);
        skillSelector = new SkillSelector();
        resolver     = new CombatResolver();
        timeline.Initialize(playerUnit, opponentUnit);

        if (playerTransform)   playerStartPos   = playerTransform.position;
        if (opponentTransform) opponentStartPos = opponentTransform.position;

        if (playerAnimator)   playerAnimator.speed   = 0;
        if (opponentAnimator) opponentAnimator.speed = 0;

        mainCam = Camera.main;

        // Inspector에 Canvas가 연결되지 않은 경우 자동 생성
        if (battleCanvas == null) { AutoBuildUI(); autoUIBuilt = true; }

        // UI 구성
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
    //  UI 자동 생성 (Inspector 미연결 시 폴백)
    // ====================================================
    void AutoBuildUI()
    {
        // ── Canvas ──────────────────────────────────────────────
        var cvsGo = new GameObject("BattleCanvas_Auto");
        var cvs   = cvsGo.AddComponent<Canvas>();
        cvs.renderMode   = RenderMode.ScreenSpaceOverlay;
        cvs.sortingOrder = 50;
        var scaler = cvsGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        cvsGo.AddComponent<GraphicRaycaster>();
        battleCanvas = cvs;

        var cRect = cvsGo.GetComponent<RectTransform>();

        // ── 공통 헬퍼 ───────────────────────────────────────────
        RectTransform Anc(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1)
        {
            var r = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.offsetMin = o0;   r.offsetMax = o1;
            return r;
        }
        GameObject Pnl(string nm, Transform p, Color c, Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1)
        {
            var go = new GameObject(nm); go.transform.SetParent(p, false);
            go.AddComponent<Image>().color = c;
            Anc(go, aMin, aMax, o0, o1); return go;
        }
        TMP_Text Txt(string nm, Transform p, string txt, float sz, Color col, TextAlignmentOptions al,
                     Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1)
        {
            var go = new GameObject(nm); go.transform.SetParent(p, false);
            var t  = go.AddComponent<TextMeshProUGUI>();
            t.text = txt; t.fontSize = sz; t.color = col; t.alignment = al;
            Anc(go, aMin, aMax, o0, o1); return t;
        }
        Button Btn(string lbl, Transform p, Color bg, Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1, System.Action cb)
        {
            var go = new GameObject("Btn_" + lbl); go.transform.SetParent(p, false);
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>(); btn.onClick.AddListener(() => cb());
            Anc(go, aMin, aMax, o0, o1);
            Txt(lbl, go.transform, lbl, 22, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }
        Slider MkSlider(string nm, Transform p, Color fillCol, Vector2 aMin, Vector2 aMax, Vector2 o0, Vector2 o1)
        {
            var go = new GameObject(nm); go.transform.SetParent(p, false);
            Anc(go, aMin, aMax, o0, o1);
            var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f);
            Anc(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fa = new GameObject("FillArea"); fa.transform.SetParent(go.transform, false);
            Anc(fa, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));
            var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = fillCol;
            Anc(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sl = go.AddComponent<Slider>();
            sl.fillRect = fill.GetComponent<RectTransform>();
            sl.direction = Slider.Direction.LeftToRight; sl.value = 1f;
            return sl;
        }

        Color BG   = new Color(0.10f, 0.10f, 0.15f, 0.92f);
        Color DARK = new Color(0.13f, 0.13f, 0.18f, 1f);
        Color GRN  = new Color(0.20f, 0.85f, 0.30f);
        Color YLW  = new Color(1.00f, 0.85f, 0.15f);
        Color RED  = new Color(0.90f, 0.15f, 0.15f);

        // ════════════════════════════════════════════
        //  상대 HP (상단)
        // ════════════════════════════════════════════
        var oppHpPnl = Pnl("OppHP", cvsGo.transform, BG,
            new Vector2(0.25f, 1f), new Vector2(0.75f, 1f),
            new Vector2(0, -110), new Vector2(0, -4));
        textOpponentName = Txt("OppName", oppHpPnl.transform,
            opponentUnit?.unitName ?? "상대", 28, Color.white, TextAlignmentOptions.Left,
            Vector2.zero, Vector2.one, new Vector2(10, -4), new Vector2(-10, -44));
        sliderOpponentHP = MkSlider("OppSlider", oppHpPnl.transform, GRN,
            new Vector2(0,0), new Vector2(1,1), new Vector2(8,8), new Vector2(-8,-50));
        fillOpponentHP = sliderOpponentHP.fillRect.GetComponent<Image>();
        textOpponentHP = Txt("OppHPTxt", oppHpPnl.transform,
            "", 20, Color.white, TextAlignmentOptions.Right,
            Vector2.zero, Vector2.one, new Vector2(0,6), new Vector2(-10,-50));

        // ════════════════════════════════════════════
        //  플레이어 HP (하단)
        // ════════════════════════════════════════════
        var plyHpPnl = Pnl("PlyHP", cvsGo.transform, BG,
            new Vector2(0.25f, 0f), new Vector2(0.75f, 0f),
            new Vector2(0, 4), new Vector2(0, 110));
        textPlayerName = Txt("PlyName", plyHpPnl.transform,
            playerUnit?.unitName ?? "플레이어", 28, Color.white, TextAlignmentOptions.Left,
            Vector2.zero, Vector2.one, new Vector2(10, -4), new Vector2(-10, -44));
        sliderPlayerHP = MkSlider("PlySlider", plyHpPnl.transform, GRN,
            new Vector2(0,0), new Vector2(1,1), new Vector2(8,8), new Vector2(-8,-50));
        fillPlayerHP = sliderPlayerHP.fillRect.GetComponent<Image>();
        textPlayerHP = Txt("PlyHPTxt", plyHpPnl.transform,
            "", 20, Color.white, TextAlignmentOptions.Right,
            Vector2.zero, Vector2.one, new Vector2(0,6), new Vector2(-10,-50));

        // ════════════════════════════════════════════
        //  ATB 행동 서열바 (좌측 세로)
        // ════════════════════════════════════════════
        var tlRoot = new GameObject("TimelineRoot"); tlRoot.transform.SetParent(cvsGo.transform, false);
        Anc(tlRoot, Vector2.zero, new Vector2(0,1), new Vector2(4,120), new Vector2(144,-120));
        var tlLayout = tlRoot.AddComponent<VerticalLayoutGroup>();
        tlLayout.spacing = 4; tlLayout.childForceExpandHeight = false; tlLayout.childForceExpandWidth = true;
        tlLayout.childAlignment = TextAnchor.UpperCenter;
        tlLayout.padding = new RectOffset(4,4,4,4);
        tlRoot.AddComponent<Image>().color = new Color(0.08f,0.08f,0.12f,0.95f);
        timelineRoot = tlRoot.transform;

        // 슬롯 프리팹 대신 인라인 생성용 임시 프리팹
        var tlPrefab = new GameObject("TL_SlotPrefab");
        tlPrefab.SetActive(false);
        var tlSlotImg = tlPrefab.AddComponent<Image>(); tlSlotImg.color = DARK;
        var tlSlotRect = tlPrefab.GetComponent<RectTransform>();
        tlSlotRect.sizeDelta = new Vector2(130, 76);
        var tlSlotLayout = tlPrefab.AddComponent<HorizontalLayoutGroup>();
        tlSlotLayout.childForceExpandWidth = false; tlSlotLayout.childAlignment = TextAnchor.MiddleCenter;
        tlSlotLayout.spacing = 4; tlSlotLayout.padding = new RectOffset(6,6,6,6);

        var tlIco = new GameObject("Icon"); tlIco.transform.SetParent(tlPrefab.transform, false);
        var tlIcoImg = tlIco.AddComponent<Image>(); tlIcoImg.color = new Color(0.5f,0.7f,1f);
        tlIco.GetComponent<RectTransform>().sizeDelta = new Vector2(50,50);
        tlIco.AddComponent<LayoutElement>().preferredWidth = 50;

        var tlAV = new GameObject("AVText"); tlAV.transform.SetParent(tlPrefab.transform, false);
        var tlAVtmp = tlAV.AddComponent<TextMeshProUGUI>();
        tlAVtmp.text = "--"; tlAVtmp.fontSize = 22; tlAVtmp.color = Color.white;
        tlAVtmp.alignment = TextAlignmentOptions.Center;
        tlAV.AddComponent<LayoutElement>().preferredWidth = 44;

        timelineSlotPrefab = tlPrefab;

        // ════════════════════════════════════════════
        //  방침 버튼 (우측)
        // ════════════════════════════════════════════
        var dirPnl = Pnl("DirectivePanel", cvsGo.transform, new Color(0.08f,0.08f,0.12f,0.95f),
            new Vector2(1,0), Vector2.one,
            new Vector2(-180, 130), new Vector2(-4, -130));
        var dirLayout = dirPnl.AddComponent<VerticalLayoutGroup>();
        dirLayout.spacing = 6; dirLayout.childForceExpandHeight = false;
        dirLayout.childAlignment = TextAnchor.UpperCenter;
        dirLayout.padding = new RectOffset(8,8,8,8);
        dirPnl.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        textDirectiveLabel = Txt("DirLabel", dirPnl.transform,
            "◆ 평소대로", 20, YLW, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        textDirectiveLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

        Button MkDirBtn(string lbl, BattleDirective d)
        {
            var go = new GameObject("DirBtn_" + lbl); go.transform.SetParent(dirPnl.transform, false);
            go.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.28f);
            var btn = go.AddComponent<Button>();
            var le  = go.AddComponent<LayoutElement>(); le.preferredHeight = 44; le.preferredWidth = 160;
            BattleDirective captured = d;
            btn.onClick.AddListener(() => SetDirective(captured));
            Txt(lbl, go.transform, lbl, 22, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }
        btnAggressive = MkDirBtn("밀어붙여", BattleDirective.Aggressive);
        btnNormal     = MkDirBtn("평소대로", BattleDirective.Normal);
        btnDefensive  = MkDirBtn("버텨",     BattleDirective.Defensive);
        btnTechnical  = MkDirBtn("기술위주", BattleDirective.Technical);

        btnPause = Btn("일시정지", dirPnl.transform, new Color(0.3f,0.3f,0.4f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TogglePause);
        btnPause.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

        // ════════════════════════════════════════════
        //  스킬 쿨타임 바 (하단 중앙)
        // ════════════════════════════════════════════
        var skillPnl = Pnl("SkillBarRoot", cvsGo.transform, new Color(0.08f,0.08f,0.12f,0.95f),
            new Vector2(0.15f,0), new Vector2(0.85f,0),
            new Vector2(0,4), new Vector2(0,114));
        var skillLayout = skillPnl.AddComponent<HorizontalLayoutGroup>();
        skillLayout.spacing = 8; skillLayout.childForceExpandWidth = false;
        skillLayout.childAlignment = TextAnchor.MiddleCenter;
        skillLayout.padding = new RectOffset(10,10,8,8);
        skillBarRoot = skillPnl.transform;

        // 스킬 슬롯 프리팹
        var skillPrefab = new GameObject("SkillSlot_Prefab"); skillPrefab.SetActive(false);
        skillPrefab.AddComponent<Image>().color = DARK;
        skillPrefab.AddComponent<LayoutElement>().preferredWidth  = 130;
        skillPrefab.GetComponent<LayoutElement>().preferredHeight = 96;

        var skIco = new GameObject("Icon"); skIco.transform.SetParent(skillPrefab.transform, false);
        skIco.AddComponent<Image>().color = new Color(0.3f,0.5f,0.8f);
        Anc(skIco, new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(6,-22), new Vector2(50,22));

        var skNm = new GameObject("NameText"); skNm.transform.SetParent(skillPrefab.transform, false);
        var skNmTmp = skNm.AddComponent<TextMeshProUGUI>();
        skNmTmp.text = "스킬"; skNmTmp.fontSize = 17; skNmTmp.color = Color.white;
        skNmTmp.alignment = TextAlignmentOptions.Center;
        Anc(skNm, new Vector2(0,0.5f), Vector2.one, new Vector2(4,4), new Vector2(-4,-4));

        var skCd = new GameObject("CooldownText"); skCd.transform.SetParent(skillPrefab.transform, false);
        var skCdTmp = skCd.AddComponent<TextMeshProUGUI>();
        skCdTmp.text = "준비"; skCdTmp.fontSize = 15; skCdTmp.color = GRN;
        skCdTmp.alignment = TextAlignmentOptions.Center;
        Anc(skCd, Vector2.zero, new Vector2(1,0.4f), new Vector2(4,4), new Vector2(-4,-2));

        var skSl = new GameObject("CooldownSlider"); skSl.transform.SetParent(skillPrefab.transform, false);
        Anc(skSl, Vector2.zero, new Vector2(1,0), new Vector2(6,4), new Vector2(-6,18));
        var skSlBg = new GameObject("BG"); skSlBg.transform.SetParent(skSl.transform, false);
        skSlBg.AddComponent<Image>().color = new Color(0.1f,0.1f,0.1f);
        Anc(skSlBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var skSlFa = new GameObject("FillArea"); skSlFa.transform.SetParent(skSl.transform, false);
        Anc(skSlFa, Vector2.zero, Vector2.one, new Vector2(2,0), new Vector2(-2,0));
        var skSlFill = new GameObject("Fill"); skSlFill.transform.SetParent(skSlFa.transform, false);
        var skSlFillImg = skSlFill.AddComponent<Image>(); skSlFillImg.color = YLW;
        Anc(skSlFill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var skSlComp = skSl.AddComponent<Slider>();
        skSlComp.fillRect = skSlFill.GetComponent<RectTransform>();
        skSlComp.direction = Slider.Direction.LeftToRight; skSlComp.value = 1f;

        var skOv = new GameObject("Overlay"); skOv.transform.SetParent(skillPrefab.transform, false);
        skOv.AddComponent<Image>().color = new Color(0,0,0,0.55f);
        Anc(skOv, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        skOv.SetActive(false);

        skillBarSlotPrefab = skillPrefab;

        // ════════════════════════════════════════════
        //  전투 로그 (화면 하단 중앙)
        // ════════════════════════════════════════════
        var logPnl = Pnl("BattleLog", cvsGo.transform, new Color(0,0,0,0.70f),
            new Vector2(0.15f,0), new Vector2(0.85f,0),
            new Vector2(0,118), new Vector2(0,340));

        var viewport = new GameObject("Viewport"); viewport.transform.SetParent(logPnl.transform, false);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Anc(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var logContent = new GameObject("Content"); logContent.transform.SetParent(viewport.transform, false);
        textBattleLog = logContent.AddComponent<TextMeshProUGUI>();
        textBattleLog.fontSize = 17; textBattleLog.color = Color.white;
        textBattleLog.alignment = TextAlignmentOptions.BottomLeft;
        textBattleLog.textWrappingMode = TextWrappingModes.Normal;
        var logContentRect = logContent.GetComponent<RectTransform>();
        logContentRect.anchorMin = Vector2.zero; logContentRect.anchorMax = Vector2.one;
        logContentRect.offsetMin = new Vector2(8,4); logContentRect.offsetMax = new Vector2(-8,-4);
        logContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollLog = logPnl.AddComponent<ScrollRect>();
        scrollLog.viewport  = viewport.GetComponent<RectTransform>();
        scrollLog.content   = logContentRect;
        scrollLog.horizontal = false;

        // ════════════════════════════════════════════
        //  결과 패널
        // ════════════════════════════════════════════
        panelResult = Pnl("ResultPanel", cvsGo.transform, new Color(0,0,0,0.88f),
            new Vector2(0.2f,0.1f), new Vector2(0.8f,0.9f),
            Vector2.zero, Vector2.zero);
        panelResult.SetActive(false);

        textResult = Txt("ResultTxt", panelResult.transform,
            "", 19, Color.white, TextAlignmentOptions.TopLeft,
            Vector2.zero, Vector2.one, new Vector2(20,60), new Vector2(-20,-20));
        textResult.textWrappingMode = TextWrappingModes.Normal;

        btnCloseResult = Btn("닫기", panelResult.transform, RED,
            new Vector2(0.35f,0), new Vector2(0.65f,0),
            new Vector2(0,10), new Vector2(0,52),
            () => panelResult.SetActive(false));

        Debug.Log("[BattleScene] UI 자동 생성 완료");
    }

    // ====================================================
    //  버튼 세팅 (AutoBuildUI를 사용하지 않고 Inspector에서 연결한 경우에만 실행)
    // ====================================================
    void SetupButtons()
    {
        // AutoBuildUI가 실행된 경우 버튼 리스너는 이미 등록되어 있으므로 건너뜀
        if (autoUIBuilt) return;

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
        // 선택된 버튼만 강조색, 나머지는 원래 색
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
    //  timelineSlotPrefab: Image(아이콘) + TMP_Text(AV 수치)
    // ====================================================
    void BuildTimelineSlots()
    {
        if (timelineRoot == null || timelineSlotPrefab == null) return;

        // 기존 슬롯 제거
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
    //  슬롯마다 아이콘(플레이어/상대)과 AV 수치 표시
    //  현재 행동할 유닛(AV 최소)에는 강조 표시
    // ====================================================
    void RefreshTimeline()
    {
        if (timelineRoot == null || timeline == null) return;

        var order = timeline.GetTimeline(); // (CombatUnit, av) 오름차순

        // 슬롯이 없으면 텍스트 폴백
        if (timelineSlots.Count == 0)
        {
            // 슬롯 프리팹 미연결 시 기존 텍스트 방식
            return;
        }

        int displayCount = Mathf.Min(timelineSlots.Count, order.Count);

        // 반복할 순서는 AV 기준 정렬 (작은 것부터 = 곧 행동)
        // GetTimeline()이 이미 오름차순 정렬을 해줌
        for (int i = 0; i < timelineSlots.Count; i++)
        {
            var slot = timelineSlots[i];
            if (slot == null) continue;

            if (i >= displayCount)
            {
                slot.SetActive(false);
                continue;
            }

            slot.SetActive(true);
            var (unit, av) = order[i];

            // 아이콘 설정
            var iconImg = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                bool isPlayer = (unit == playerUnit);
                iconImg.sprite = isPlayer ? iconPlayer : iconOpponent;
                // 첫 번째(곧 행동) 슬롯 강조: 살짝 크게 + 밝게
                iconImg.color = (i == 0) ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            // AV 수치 텍스트
            var avText = slot.transform.Find("AVText")?.GetComponent<TMP_Text>();
            if (avText != null)
            {
                // av 값을 0~100 범위로 정규화하여 "게이지" 느낌으로 표시
                // av 최솟값=0(바로 행동), 최댓값은 기준 AV(약 10000/SPD)
                float maxAV = Mathf.Max(1f, playerUnit.derived.SPD > 0 ? 10000f / playerUnit.derived.SPD : 10000f);
                float ratio = Mathf.Clamp01(av / (maxAV * 1.5f));
                int displayVal = Mathf.RoundToInt(ratio * 100f);
                avText.text = i == 0 ? "▶" : displayVal.ToString();
            }

            // 슬롯 배경 강조 (i==0 = 현재 행동자)
            var bg = slot.GetComponent<Image>();
            if (bg != null)
                bg.color = (i == 0)
                    ? new Color(1f, 0.9f, 0.2f, 0.9f)   // 노란색 강조
                    : new Color(0.15f, 0.15f, 0.25f, 0.85f);
        }
    }

    // ====================================================
    //  스킬 바 - 슬롯 생성
    //  플레이어 장착 스킬마다 슬롯 1개 생성
    //  skillBarSlotPrefab: Icon(Image) + NameText(TMP) + CooldownText(TMP) + CooldownSlider(Slider) + Overlay(Image)
    // ====================================================
    void BuildSkillBarSlots()
    {
        if (skillBarRoot == null || skillBarSlotPrefab == null) return;

        // 기존 슬롯 제거
        foreach (var s in skillBarSlots)
            if (s?.icon != null) Destroy(s.icon.transform.parent?.gameObject);
        skillBarSlots.Clear();

        for (int i = skillBarRoot.childCount - 1; i >= 0; i--)
            Destroy(skillBarRoot.GetChild(i).gameObject);

        if (playerUnit == null) return;

        foreach (var skill in playerUnit.equippedSkills)
        {
            if (skill == null) continue;

            var go = Instantiate(skillBarSlotPrefab, skillBarRoot);

            var slot = new SkillBarSlot { skill = skill };
            slot.icon          = go.transform.Find("Icon")?.GetComponent<Image>();
            slot.nameText      = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            slot.cooldownText  = go.transform.Find("CooldownText")?.GetComponent<TMP_Text>();
            slot.cooldownSlider = go.transform.Find("CooldownSlider")?.GetComponent<Slider>();
            slot.overlay       = go.transform.Find("Overlay")?.GetComponent<Image>();

            // 아이콘/이름 초기 설정
            if (slot.icon     != null && skill.icon != null) slot.icon.sprite = skill.icon;
            if (slot.nameText != null) slot.nameText.text = skill.skillName;

            skillBarSlots.Add(slot);
        }
    }

    // ====================================================
    //  스킬 바 - 갱신
    //  쿨타임 남은 경우: 오버레이 + 텍스트 "N턴 후"
    //  사용 가능: 오버레이 숨김 + 텍스트 "사용 가능"
    // ====================================================
    void RefreshSkillBar()
    {
        if (playerUnit == null) return;

        foreach (var slot in skillBarSlots)
        {
            if (slot?.skill == null) continue;

            int cd = playerUnit.GetCooldown(slot.skill);
            bool ready = cd <= 0;

            if (slot.cooldownText != null)
                slot.cooldownText.text = ready ? "준비" : $"{cd}턴";

            // CooldownSlider: 0=쿨타임 최대, 1=사용 가능
            if (slot.cooldownSlider != null)
            {
                int maxCd = slot.skill.cooldownTurns;
                slot.cooldownSlider.value = maxCd > 0 ? 1f - (float)cd / maxCd : 1f;
            }

            // 쿨타임 중이면 오버레이 표시 (반투명 어두운 이미지)
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

            // HP 0 이하 선제 체크
            if (playerUnit.currentHP <= 0 || opponentUnit.currentHP <= 0)
            {
                report.playerWon = playerUnit.currentHP > opponentUnit.currentHP;
                yield return StartCoroutine(BattleEnd(report.playerWon));
                yield break;
            }

            turnCount++;

            // 1. ATB 행동순서 결정
            CombatUnit attacker = timeline.AdvanceAndGetNext();
            CombatUnit defender = (attacker == playerUnit) ? opponentUnit : playerUnit;
            bool isPlayerTurn   = (attacker == playerUnit);

            Animator  attackerAnim    = isPlayerTurn ? playerAnimator   : opponentAnimator;
            Animator  defenderAnim    = isPlayerTurn ? opponentAnimator : playerAnimator;
            Transform attackerTr      = isPlayerTurn ? playerTransform   : opponentTransform;
            Transform defenderTr      = isPlayerTurn ? opponentTransform : playerTransform;
            Vector3   attackerStartP  = isPlayerTurn ? playerStartPos    : opponentStartPos;

            // 2. 쿨타임 틱 (턴 시작)
            attacker.TickCooldowns();

            // 3. 버프 턴 감소
            buffSystem.OnUnitTurnStart(attacker);

            // 4. 스킬 선택 (쿨타임 고려)
            BattleDirective directive = isPlayerTurn ? currentDirective : BattleDirective.Normal;
            SkillData skill = skillSelector.SelectSkill(attacker, defender, directive);
            if (skill == null) { Debug.LogError("[BattleScene] 스킬 null"); yield break; }

            AppendLog($"<b>{attacker.unitName}</b>: {skill.skillName} 시전!");

            // ATB 서열바 갱신
            RefreshTimeline();

            // 5. 공격 연출 + 판정
            yield return StartCoroutine(AttackSequence(
                attackerAnim, defenderAnim,
                attackerTr, defenderTr, attackerStartP,
                attacker, defender, skill, isPlayerTurn
            ));

            // 6. 스킬 쿨타임 등록
            attacker.SetCooldown(skill);

            RefreshHP();
            RefreshTimeline();
            RefreshSkillBar();

            // 7. 승패 판정
            if (defender.currentHP <= 0)
            {
                report.playerWon = isPlayerTurn;
                yield return StartCoroutine(BattleEnd(isPlayerTurn));
                yield break;
            }

            // 8. 턴 한계 판정
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
        // 3D Transform이 없으면 이동 연출 건너뜀 (테스트 모드)
        bool has3D = attackerTr != null && defenderTr != null;

        if (attackerAnim) attackerAnim.speed = 1;
        if (has3D)
        {
            Vector3 targetPos = Vector3.Lerp(attackerTr.position, defenderTr.position, 0.75f);
            yield return StartCoroutine(MoveToPosition(attackerTr, targetPos, attackMoveSpeed));
        }
        else
            yield return new WaitForSeconds(0.15f);

        if (attackerAnim) attackerAnim.SetTrigger("Attack");

        // Attack 상태 진입 대기 (타임아웃 1초)
        if (attackerAnim != null)
        {
            yield return null;
            float w = 0f;
            while (!attackerAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack") && w < 1f)
            { w += Time.deltaTime; yield return null; }

            // 타격 타이밍 (40%), 타임아웃 2초
            w = 0f;
            while (attackerAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack") &&
                   attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.4f && w < 2f)
            { w += Time.deltaTime; yield return null; }
        }
        else yield return new WaitForSeconds(0.2f);

        // 데미지 판정
        DamageResult dmgResult = resolver.Resolve(attacker, defender, skill);
        report.LogAction(attacker, skill, dmgResult, turnCount, isPlayerTurn);

        // 피격 유닛 Transform (팝업 위치 계산용)
        Transform defenderWorldTr = isPlayerTurn ? opponentTransform : playerTransform;

        string logMsg;
        switch (dmgResult.outcome)
        {
            case HitOutcome.Hit:
            case HitOutcome.Critical:
                defender.currentHP = Mathf.Max(0, defender.currentHP - dmgResult.finalDamage);
                if (defenderAnim) { defenderAnim.speed = 1; defenderAnim.SetTrigger("Hit"); }
                if (skill.avAdvance > 0) timeline.AdvanceUnit(attacker, skill.avAdvance);
                if (skill.avDelay   > 0) timeline.DelayUnit(defender, skill.avDelay);
                if (skill.appliedBuff   != null) buffSystem.ApplyBuff(attacker, skill.appliedBuff);
                if (skill.appliedDebuff != null) buffSystem.ApplyBuff(defender, skill.appliedDebuff);

                // 데미지 팝업
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

        // 공격 애니메이션 종료 대기 (타임아웃 2초)
        if (attackerAnim != null)
        {
            float w = 0f;
            while (attackerAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack") &&
                   attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.85f && w < 2f)
            { w += Time.deltaTime; yield return null; }
        }

        // 피격 애니메이션 종료 대기 (타임아웃 1초)
        if (defenderAnim != null && defenderAnim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
        {
            float w = 0f;
            while (defenderAnim.GetCurrentAnimatorStateInfo(0).IsName("Hit") &&
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
    //  데미지 팝업 생성
    // ====================================================

    /// <summary>
    /// 피격 유닛 위치에서 위로 떠오르며 페이드 아웃되는 데미지 텍스트를 생성한다.
    /// damagePopupPrefab이 없으면 코드로 직접 TMP_Text를 생성하는 폴백을 사용한다.
    /// </summary>
    void SpawnDamagePopup(Transform defenderTr, string text, Color color, float sizeScale = 1f)
    {
        if (battleCanvas == null) return;
        StartCoroutine(DamagePopupRoutine(defenderTr, text, color, sizeScale));
    }

    IEnumerator DamagePopupRoutine(Transform defenderTr, string text, Color color, float sizeScale)
    {
        // ── 팝업 오브젝트 생성 ──────────────────────────────
        GameObject popupGo;
        TMP_Text   tmp;

        if (damagePopupPrefab != null)
        {
            popupGo = Instantiate(damagePopupPrefab, battleCanvas.transform);
            tmp     = popupGo.GetComponentInChildren<TMP_Text>();
        }
        else
        {
            // 폴백: 동적 생성
            popupGo = new GameObject("DmgPopup");
            popupGo.transform.SetParent(battleCanvas.transform, false);
            tmp = popupGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = 48f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            // 그림자 효과
            var shadow = popupGo.AddComponent<UnityEngine.UI.Outline>();
            shadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        if (tmp == null) { Destroy(popupGo); yield break; }

        tmp.text     = text;
        tmp.color    = color;
        tmp.fontSize = Mathf.RoundToInt(tmp.fontSize * sizeScale);

        // ── 초기 위치: 피격 유닛 머리 근처(월드 → 스크린 → Canvas 좌표) ──
        // 3D Transform 없으면 Canvas 중앙 상단/하단으로 fallback
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
            // 3D 없음: 피격 측 HP바 근처 고정 위치 사용
            bool defIsPlayer = (defenderTr == (opponentTransform != null ? opponentTransform : null));
            // opponentTransform=null인 경우 isPlayer 판별 불가 → Canvas 중앙 위/아래 사용
            startAnchor = new Vector3(Random.Range(-80f, 80f), defIsPlayer ? -260f : 260f, 0f);
        }
        rt.anchoredPosition = startAnchor;

        // ── 랜덤 가로 흔들림 (같은 위치에 여러 개 뜰 때 겹침 방지) ──
        float xJitter = Random.Range(-60f, 60f);
        Vector3 endAnchor = startAnchor + new Vector3(xJitter, popupRiseHeight * 120f, 0f);

        // ── 애니메이션 루프 ──────────────────────────────────
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            // 이징: easeOut (처음 빠르게, 나중에 느리게)
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            rt.anchoredPosition = Vector3.Lerp(startAnchor, endAnchor, eased);

            // 페이드 아웃
            float alpha = t >= popupFadeStart
                ? 1f - (t - popupFadeStart) / (1f - popupFadeStart)
                : 1f;
            tmp.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        Destroy(popupGo);
    }

    /// <summary>월드 좌표 → Canvas 로컬 좌표 변환</summary>
    Vector3 WorldToCanvasPos(Vector3 worldPos)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || battleCanvas == null) return Vector3.zero;

        Vector2 screenPos = mainCam.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = battleCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return screenPos;

        // Screen Space - Overlay의 경우
        if (battleCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPos);
            return localPos;
        }
        // Screen Space - Camera의 경우
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, battleCanvas.worldCamera, out Vector2 localPosCam);
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

            // HP 비율에 따라 바 색상 변경 (녹색→노란색→빨간색)
            if (fillPlayerHP) fillPlayerHP.color = GetHPColor(ratio);
        }

        if (opponentUnit != null)
        {
            float maxHP = opponentUnit.derived.MaxHP;
            float curHP = Mathf.Max(0, opponentUnit.currentHP);
            float ratio = maxHP > 0 ? curHP / maxHP : 0f;

            if (sliderOpponentHP) sliderOpponentHP.value = ratio;
            if (textOpponentHP)   textOpponentHP.text    = $"{curHP:F0} / {maxHP:F0}";
            if (textOpponentName) textOpponentName.text  = opponentUnit.unitName;

            if (fillOpponentHP) fillOpponentHP.color = GetHPColor(ratio);
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
    //  더미 플레이어 유닛 생성 (테스트 모드용)
    // ====================================================
    private static CombatUnit MakeDummyPlayer()
    {
        var u = new CombatUnit();
        u.unitName    = "내 클론";
        u.rawStats    = new CombatBaseStats { STR=20, AGI=15, VIT=15, INT=5, GUT=5, SEN=10 };
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
        basicAttack.name             = "일반공격";   // Bug-B fix: Unity 내부 name도 일치시킴
        basicAttack.skillName        = "일반공격";
        basicAttack.category         = SkillCategory.Strike;
        basicAttack.weight           = 60;
        basicAttack.damageMultiplier = 1.0f;
        basicAttack.cooldownTurns    = 0;

        var guardStance = ScriptableObject.CreateInstance<SkillData>();
        guardStance.name             = "방어자세";
        guardStance.skillName        = "방어자세";
        guardStance.category         = SkillCategory.Defense;
        guardStance.weight           = 30;
        guardStance.damageMultiplier = 0.5f;
        guardStance.cooldownTurns    = 2;   // 2턴 쿨다운

        var quickStep = ScriptableObject.CreateInstance<SkillData>();
        quickStep.name             = "빠른발놀림";
        quickStep.skillName        = "빠른발놀림";
        quickStep.category         = SkillCategory.Mobility;
        quickStep.weight           = 40;
        quickStep.damageMultiplier = 0.8f;
        quickStep.avAdvance        = 500f;
        quickStep.cooldownTurns    = 3;     // 3턴 쿨다운

        unit.equippedSkills.Add(basicAttack);
        unit.equippedSkills.Add(guardStance);
        unit.equippedSkills.Add(quickStep);
    }
}
