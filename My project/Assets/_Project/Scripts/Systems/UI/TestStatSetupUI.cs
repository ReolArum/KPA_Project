// ===== TestStatSetupUI.cs =====
// Main 씬에서 테스트 전투 시작 전 캐릭터 스탯을 직접 입력하는 UI.
// BattleSceneController가 BattleSceneData.playerUnit/opponentUnit이 null이면
// 더미 유닛을 사용하므로, 여기서 값을 설정한 뒤 전투 씬으로 넘어가면 된다.
//
// 연결 방법 (Unity Inspector):
//  1. Main 씬 캔버스 아래에 빈 오브젝트 "TestStatSetupUI" 생성
//  2. 이 스크립트 컴포넌트 추가
//  3. panelSetup 에 전용 패널 오브젝트 할당
//  4. 각 TMP_InputField, TMP_Dropdown 필드에 UI 요소 할당
//  5. btnOpenSetup 에 "스탯 설정" 버튼 할당 (OnClickOpen 호출)
//  6. btnStartBattle 에 "전투 시작" 버튼 할당 (OnClickStartBattle 호출)
//  7. btnClose 에 "닫기" 버튼 할당 (OnClickClose 호출)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TestStatSetupUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  패널 루트
    // ─────────────────────────────────────────────
    [Header("패널")]
    [SerializeField] private GameObject panelSetup;

    // ─────────────────────────────────────────────
    //  플레이어 스탯 입력 필드
    // ─────────────────────────────────────────────
    [Header("플레이어 스탯")]
    [SerializeField] private TMP_InputField inputPlayerName;
    [SerializeField] private TMP_InputField inputPlayerSTR;
    [SerializeField] private TMP_InputField inputPlayerAGI;
    [SerializeField] private TMP_InputField inputPlayerVIT;
    [SerializeField] private TMP_InputField inputPlayerINT;
    [SerializeField] private TMP_InputField inputPlayerGUT;
    [SerializeField] private TMP_InputField inputPlayerSEN;
    [SerializeField] private TMP_Dropdown   dropPlayerSchool;
    [SerializeField] private TMP_InputField inputPlayerSchoolLevel;

    // ─────────────────────────────────────────────
    //  상대방 스탯 입력 필드
    // ─────────────────────────────────────────────
    [Header("상대방 스탯")]
    [SerializeField] private TMP_InputField inputOpponentName;
    [SerializeField] private TMP_InputField inputOpponentSTR;
    [SerializeField] private TMP_InputField inputOpponentAGI;
    [SerializeField] private TMP_InputField inputOpponentVIT;
    [SerializeField] private TMP_InputField inputOpponentINT;
    [SerializeField] private TMP_InputField inputOpponentGUT;
    [SerializeField] private TMP_InputField inputOpponentSEN;
    [SerializeField] private TMP_Dropdown   dropOpponentSchool;
    [SerializeField] private TMP_InputField inputOpponentSchoolLevel;

    // ─────────────────────────────────────────────
    //  파생 스탯 미리보기 (읽기 전용)
    // ─────────────────────────────────────────────
    [Header("파생 스탯 미리보기 (선택)")]
    [SerializeField] private TMP_Text textPlayerPreview;
    [SerializeField] private TMP_Text textOpponentPreview;

    // ─────────────────────────────────────────────
    //  버튼
    // ─────────────────────────────────────────────
    [Header("버튼")]
    [SerializeField] private Button btnOpenSetup;
    [SerializeField] private Button btnStartBattle;
    [SerializeField] private Button btnClose;

    // ─────────────────────────────────────────────
    //  내부 상태
    // ─────────────────────────────────────────────
    private static readonly string[] SchoolNames =
        { "없음", "파쇄류(Crusher)", "강철류(Ironclad)", "속보류(Agile)", "책략류(Tactician)" };

    // ─────────────────────────────────────────────
    //  초기화
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // 드롭다운 항목 채우기
        SetupDropdown(dropPlayerSchool);
        SetupDropdown(dropOpponentSchool);

        // 기본값 설정
        SetDefaults();

        // 버튼 리스너
        if (btnOpenSetup   != null) btnOpenSetup.onClick.AddListener(OnClickOpen);
        if (btnStartBattle != null) btnStartBattle.onClick.AddListener(OnClickStartBattle);
        if (btnClose       != null) btnClose.onClick.AddListener(OnClickClose);

        // 스탯 변경 시 미리보기 자동 갱신
        RegisterPreviewListeners();

        // 패널은 기본 숨김
        if (panelSetup != null) panelSetup.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  버튼 콜백
    // ─────────────────────────────────────────────
    public void OnClickOpen()
    {
        if (panelSetup != null) panelSetup.SetActive(true);
        RefreshPreviews();
    }

    public void OnClickClose()
    {
        if (panelSetup != null) panelSetup.SetActive(false);
    }

    public void OnClickStartBattle()
    {
        // 스탯 읽기 → CombatUnit 생성
        CombatUnit player   = BuildUnitFromInputs(
            inputPlayerName,   inputPlayerSTR,  inputPlayerAGI,
            inputPlayerVIT,    inputPlayerINT,  inputPlayerGUT, inputPlayerSEN,
            dropPlayerSchool,  inputPlayerSchoolLevel,
            "내 클론");

        CombatUnit opponent = BuildUnitFromInputs(
            inputOpponentName,  inputOpponentSTR, inputOpponentAGI,
            inputOpponentVIT,   inputOpponentINT, inputOpponentGUT, inputOpponentSEN,
            dropOpponentSchool, inputOpponentSchoolLevel,
            "상대");

        // BattleSceneData에 저장
        BattleSceneData.playerUnit   = player;
        BattleSceneData.opponentUnit = opponent;
        BattleSceneData.battleReport    = null;
        BattleSceneData.battleCompleted = false;

        // 전투 씬 로드
        SceneManager.LoadScene(BattleSceneData.SceneBattle);
    }

    // ─────────────────────────────────────────────
    //  유닛 생성
    // ─────────────────────────────────────────────
    private CombatUnit BuildUnitFromInputs(
        TMP_InputField fName,
        TMP_InputField fSTR, TMP_InputField fAGI,
        TMP_InputField fVIT, TMP_InputField fINT,
        TMP_InputField fGUT, TMP_InputField fSEN,
        TMP_Dropdown   dSchool, TMP_InputField fSchoolLv,
        string fallbackName)
    {
        var u = new CombatUnit();
        u.unitName = (fName != null && fName.text.Trim().Length > 0)
                     ? fName.text.Trim()
                     : fallbackName;

        u.rawStats = new CombatBaseStats
        {
            STR = ParseInt(fSTR, 20),
            AGI = ParseInt(fAGI, 15),
            VIT = ParseInt(fVIT, 15),
            INT = ParseInt(fINT,  5),
            GUT = ParseInt(fGUT,  5),
            SEN = ParseInt(fSEN, 10)
        };

        u.schoolType  = IndexToSchool(dSchool != null ? dSchool.value : 0);
        u.schoolLevel = Mathf.Clamp(ParseInt(fSchoolLv, 1), 1, 5);

        u.Recalculate();
        u.currentHP = u.derived.MaxHP;
        u.currentAV = u.derived.SPD > 0 ? 10000f / u.derived.SPD : 10000f;
        return u;
    }

    // ─────────────────────────────────────────────
    //  파생 스탯 미리보기
    // ─────────────────────────────────────────────
    private void RefreshPreviews()
    {
        if (textPlayerPreview   != null) textPlayerPreview.text   = BuildPreviewText(
            inputPlayerSTR, inputPlayerAGI, inputPlayerVIT,
            inputPlayerINT, inputPlayerGUT, inputPlayerSEN,
            dropPlayerSchool, inputPlayerSchoolLevel);

        if (textOpponentPreview != null) textOpponentPreview.text = BuildPreviewText(
            inputOpponentSTR, inputOpponentAGI, inputOpponentVIT,
            inputOpponentINT, inputOpponentGUT, inputOpponentSEN,
            dropOpponentSchool, inputOpponentSchoolLevel);
    }

    private string BuildPreviewText(
        TMP_InputField fSTR, TMP_InputField fAGI, TMP_InputField fVIT,
        TMP_InputField fINT, TMP_InputField fGUT, TMP_InputField fSEN,
        TMP_Dropdown dSchool, TMP_InputField fLv)
    {
        var bs = new CombatBaseStats
        {
            STR = ParseInt(fSTR, 20), AGI = ParseInt(fAGI, 15),
            VIT = ParseInt(fVIT, 15), INT = ParseInt(fINT,  5),
            GUT = ParseInt(fGUT,  5), SEN = ParseInt(fSEN, 10)
        };
        var d = new CombatDerivedStats();
        SchoolType school = IndexToSchool(dSchool != null ? dSchool.value : 0);
        int lv = Mathf.Clamp(ParseInt(fLv, 1), 1, 5);
        d.Calculate(bs, school, lv);

        return $"HP {d.MaxHP:F0}  ATK {d.PhysAtk:F1}  DEF {d.PhysDef:F1}\n" +
               $"SPD {d.SPD:F1}  명중 {d.HitRate:F1}%  회피 {d.EvasionRate:F1}%\n" +
               $"크리 {d.CritRate:F1}%  크리배율 {d.CritDamage:F2}";
    }

    // ─────────────────────────────────────────────
    //  초기값 설정
    // ─────────────────────────────────────────────
    private void SetDefaults()
    {
        // 플레이어 기본값
        SetText(inputPlayerName,        "내 클론");
        SetText(inputPlayerSTR,         "20");
        SetText(inputPlayerAGI,         "15");
        SetText(inputPlayerVIT,         "15");
        SetText(inputPlayerINT,         "5");
        SetText(inputPlayerGUT,         "5");
        SetText(inputPlayerSEN,         "10");
        SetText(inputPlayerSchoolLevel, "1");

        // 상대방 기본값
        SetText(inputOpponentName,        "상대");
        SetText(inputOpponentSTR,         "15");
        SetText(inputOpponentAGI,         "15");
        SetText(inputOpponentVIT,         "15");
        SetText(inputOpponentINT,         "5");
        SetText(inputOpponentGUT,         "5");
        SetText(inputOpponentSEN,         "10");
        SetText(inputOpponentSchoolLevel, "1");
    }

    // ─────────────────────────────────────────────
    //  드롭다운 초기화
    // ─────────────────────────────────────────────
    private static void SetupDropdown(TMP_Dropdown drop)
    {
        if (drop == null) return;
        drop.ClearOptions();
        drop.AddOptions(new System.Collections.Generic.List<string>(SchoolNames));
        drop.value = 0;
    }

    // ─────────────────────────────────────────────
    //  미리보기 리스너 등록
    // ─────────────────────────────────────────────
    private void RegisterPreviewListeners()
    {
        RegisterInputListener(inputPlayerSTR);
        RegisterInputListener(inputPlayerAGI);
        RegisterInputListener(inputPlayerVIT);
        RegisterInputListener(inputPlayerINT);
        RegisterInputListener(inputPlayerGUT);
        RegisterInputListener(inputPlayerSEN);
        RegisterInputListener(inputPlayerSchoolLevel);
        if (dropPlayerSchool   != null) dropPlayerSchool.onValueChanged.AddListener(_  => RefreshPreviews());

        RegisterInputListener(inputOpponentSTR);
        RegisterInputListener(inputOpponentAGI);
        RegisterInputListener(inputOpponentVIT);
        RegisterInputListener(inputOpponentINT);
        RegisterInputListener(inputOpponentGUT);
        RegisterInputListener(inputOpponentSEN);
        RegisterInputListener(inputOpponentSchoolLevel);
        if (dropOpponentSchool != null) dropOpponentSchool.onValueChanged.AddListener(_ => RefreshPreviews());
    }

    private void RegisterInputListener(TMP_InputField field)
    {
        if (field != null) field.onEndEdit.AddListener(_ => RefreshPreviews());
    }

    // ─────────────────────────────────────────────
    //  유틸리티
    // ─────────────────────────────────────────────
    private static int ParseInt(TMP_InputField field, int fallback)
    {
        if (field == null) return fallback;
        return int.TryParse(field.text, out int v) ? Mathf.Max(0, v) : fallback;
    }

    private static void SetText(TMP_InputField field, string text)
    {
        if (field != null) field.text = text;
    }

    private static SchoolType IndexToSchool(int index) => index switch
    {
        1 => SchoolType.Crusher,
        2 => SchoolType.Ironclad,
        3 => SchoolType.Agile,
        4 => SchoolType.Tactician,
        _ => SchoolType.None
    };
}
