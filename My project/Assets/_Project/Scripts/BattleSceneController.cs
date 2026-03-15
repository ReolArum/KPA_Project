// ===== BattleSceneController.cs =====
// BattleSceneController + BattleUIController 통합
// 전투 씬의 모든 로직(전투 루프, 애니메이션, UI, 방침)을 단일 MonoBehaviour에서 관리
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
    //  UI - HP / 이름
    // ====================================================
    [Header("HP / 이름")]
    [SerializeField] private Slider    sliderPlayerHP;
    [SerializeField] private Slider    sliderOpponentHP;
    [SerializeField] private TMP_Text  textPlayerHP;
    [SerializeField] private TMP_Text  textOpponentHP;
    [SerializeField] private TMP_Text  textPlayerName;
    [SerializeField] private TMP_Text  textOpponentName;

    // ====================================================
    //  UI - 타임라인 / 로그
    // ====================================================
    [Header("타임라인 / 로그")]
    [SerializeField] private TMP_Text   textTimeline;
    [SerializeField] private TMP_Text   textBattleLog;
    [SerializeField] private ScrollRect scrollLog;

    // ====================================================
    //  UI - 방침 버튼
    // ====================================================
    [Header("방침 버튼")]
    [SerializeField] private Button    btnAggressive;
    [SerializeField] private Button    btnNormal;
    [SerializeField] private Button    btnDefensive;
    [SerializeField] private Button    btnTechnical;
    [SerializeField] private TMP_Text  textDirectiveLabel;
    [SerializeField] private Button    btnPause;

    // ====================================================
    //  UI - 결과 패널
    // ====================================================
    [Header("결과 패널")]
    [SerializeField] private GameObject panelResult;
    [SerializeField] private TMP_Text   textResult;
    [SerializeField] private Button     btnCloseResult;

    // ====================================================
    //  전투 시스템 (순수 C# - MonoBehaviour 불필요)
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

    // ====================================================
    //  초기화
    // ====================================================
    void Start()
    {
        // 전투 데이터 검증
        if (BattleSceneData.playerUnit == null || BattleSceneData.opponentUnit == null)
        {
            Debug.LogError("[BattleScene] 전투 데이터 없음! 메인 씬에서 시작하세요.");
            return;
        }

        playerUnit   = BattleSceneData.playerUnit;
        opponentUnit = BattleSceneData.opponentUnit;

        // 스킬이 비어있으면 기본 스킬 부여
        if (playerUnit.equippedSkills  == null || playerUnit.equippedSkills.Count  == 0) AssignDefaultSkills(playerUnit);
        if (opponentUnit.equippedSkills == null || opponentUnit.equippedSkills.Count == 0) AssignDefaultSkills(opponentUnit);

        // 전투 시스템 초기화
        report       = new BattleReport();
        timeline     = new ATBTimeline();
        buffSystem   = new BuffSystem(timeline);
        skillSelector = new SkillSelector();
        resolver     = new CombatResolver();
        timeline.Initialize(playerUnit, opponentUnit);

        // 위치 기록
        if (playerTransform)   playerStartPos   = playerTransform.position;
        if (opponentTransform) opponentStartPos = opponentTransform.position;

        // 애니메이터 초기 정지
        if (playerAnimator)   playerAnimator.speed   = 0;
        if (opponentAnimator) opponentAnimator.speed = 0;

        // UI 초기화
        SetupButtons();
        if (panelResult) panelResult.SetActive(false);
        RefreshHP();
        UpdateDirectiveLabel();

        StartCoroutine(BattleLoop());
    }

    // ====================================================
    //  버튼 세팅
    // ====================================================
    void SetupButtons()
    {
        if (btnAggressive) btnAggressive.onClick.AddListener(() => SetDirective(BattleDirective.Aggressive));
        if (btnNormal)     btnNormal.onClick.AddListener(()     => SetDirective(BattleDirective.Normal));
        if (btnDefensive)  btnDefensive.onClick.AddListener(()  => SetDirective(BattleDirective.Defensive));
        if (btnTechnical)  btnTechnical.onClick.AddListener(()  => SetDirective(BattleDirective.Technical));

        if (btnPause) btnPause.onClick.AddListener(TogglePause);

        if (btnCloseResult) btnCloseResult.onClick.AddListener(() =>
        {
            if (panelResult) panelResult.SetActive(false);
        });
    }

    void SetDirective(BattleDirective d)
    {
        currentDirective = d;
        UpdateDirectiveLabel();
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
            BattleDirective.Aggressive => "현재 방침: 밀어붙여",
            BattleDirective.Normal     => "현재 방침: 평소대로",
            BattleDirective.Defensive  => "현재 방침: 버텨",
            BattleDirective.Technical  => "현재 방침: 기술위주",
            _                          => ""
        };
    }

    // ====================================================
    //  전투 메인 루프
    // ====================================================
    IEnumerator BattleLoop()
    {
        battleActive = true;
        yield return new WaitForSeconds(0.8f);  // 인트로 대기

        while (battleActive)
        {
            // 일시정지 대기
            while (isPaused) yield return null;

            turnCount++;

            // 1. ATB 행동순서 결정
            CombatUnit attacker = timeline.AdvanceAndGetNext();
            CombatUnit defender = (attacker == playerUnit) ? opponentUnit : playerUnit;
            bool isPlayerTurn   = (attacker == playerUnit);

            Animator   attackerAnim     = isPlayerTurn ? playerAnimator   : opponentAnimator;
            Animator   defenderAnim     = isPlayerTurn ? opponentAnimator : playerAnimator;
            Transform  attackerTr       = isPlayerTurn ? playerTransform   : opponentTransform;
            Transform  defenderTr       = isPlayerTurn ? opponentTransform : playerTransform;
            Vector3    attackerStartPos = isPlayerTurn ? playerStartPos    : opponentStartPos;

            // 2. 버프 턴 감소
            buffSystem.OnUnitTurnStart(attacker);

            // 3. 스킬 선택 (플레이어는 현재 방침 적용, 상대는 Normal)
            BattleDirective directive = isPlayerTurn ? currentDirective : BattleDirective.Normal;
            SkillData skill = skillSelector.SelectSkill(attacker, defender, directive);
            if (skill == null) { Debug.LogError("[BattleScene] 스킬 null"); yield break; }

            AppendLog($"<b>{attacker.unitName}</b>: {skill.skillName} 시전!");
            UpdateTimeline();

            // 4. 공격 연출 + 판정
            yield return StartCoroutine(AttackSequence(
                attackerAnim, defenderAnim,
                attackerTr, defenderTr, attackerStartPos,
                attacker, defender, skill, isPlayerTurn
            ));

            RefreshHP();

            // 5. 승패 판정
            if (defender.currentHP <= 0)
            {
                report.playerWon = isPlayerTurn;
                yield return StartCoroutine(BattleEnd(isPlayerTurn));
                yield break;
            }

            // 6. 턴 한계 판정 (HP 많은 쪽 승리)
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
        // 1. 공격자 애니메이션 활성화 & 돌진
        if (attackerAnim) attackerAnim.speed = 1;
        Vector3 targetPos = Vector3.Lerp(attackerTr.position, defenderTr.position, 0.75f);
        yield return StartCoroutine(MoveToPosition(attackerTr, targetPos, attackMoveSpeed));

        // 2. 공격 트리거
        if (attackerAnim) attackerAnim.SetTrigger("Attack");

        // 3. Attack 상태 진입 대기 (Animator null-safe)
        if (attackerAnim != null)
        {
            yield return null;
            yield return new WaitUntil(() =>
                attackerAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack"));

            // 4. 타격 타이밍 (40%)
            yield return new WaitUntil(() =>
                attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.4f);
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        // 5. 데미지 판정
        DamageResult dmgResult = resolver.Resolve(attacker, defender, skill);
        report.LogAction(attacker, skill, dmgResult, turnCount, isPlayerTurn);

        // 6. 결과 처리
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

                logMsg = dmgResult.outcome == HitOutcome.Critical
                    ? $"  → <color=red>크리티컬!</color> {defender.unitName}에게 <color=red>{dmgResult.finalDamage:F0}</color> 데미지!"
                    : $"  → {defender.unitName}에게 <color=yellow>{dmgResult.finalDamage:F0}</color> 데미지!";
                break;

            case HitOutcome.Evaded:
                logMsg = $"  → {defender.unitName} 회피!";
                break;

            default: // Miss
                logMsg = "  → 빗나감!";
                break;
        }
        AppendLog(logMsg);

        // 7. 공격 애니메이션 종료 대기
        if (attackerAnim != null)
        {
            yield return new WaitUntil(() =>
                attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.85f ||
                !attackerAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
        }

        // 8. 피격 애니메이션 종료 대기
        if (defenderAnim != null && defenderAnim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
        {
            yield return new WaitUntil(() =>
                defenderAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.85f ||
                !defenderAnim.GetCurrentAnimatorStateInfo(0).IsName("Hit"));
        }

        // 9. 복귀 & 정지
        yield return StartCoroutine(MoveToPosition(attackerTr, startPos, attackMoveSpeed * 1.2f));
        if (attackerAnim) attackerAnim.speed = 0;
        if (defenderAnim) defenderAnim.speed = 0;

        yield return new WaitForSeconds(0.15f);
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

        // 승패 애니메이션
        if (playerAnimator)   playerAnimator.SetTrigger(playerWon ? "Victory" : "Defeat");
        if (opponentAnimator) opponentAnimator.SetTrigger(playerWon ? "Defeat" : "Victory");

        // 로그 출력
        string resultColor = playerWon ? "<color=green>" : "<color=red>";
        AppendLog($"\n{resultColor}===== {(playerWon ? "승리!" : "패배...")} =====</color>");

        yield return new WaitForSeconds(resultDelay);

        // 결과 패널 표시
        if (panelResult != null && textResult != null)
        {
            textResult.text = report.ToReportString();
            panelResult.SetActive(true);

            // 닫기 버튼 누를 때까지 대기
            bool closed = false;
            if (btnCloseResult)
            {
                btnCloseResult.onClick.RemoveAllListeners();
                btnCloseResult.onClick.AddListener(() => closed = true);
            }
            else
            {
                closed = true;
            }

            yield return new WaitUntil(() => closed);
        }

        // 메인 씬으로 복귀
        BattleSceneData.CompleteBattle(report);
        SceneManager.LoadScene("MainScene");
    }

    // ====================================================
    //  UI 갱신
    // ====================================================
    void RefreshHP()
    {
        if (playerUnit != null)
        {
            float max = playerUnit.derived.MaxHP;
            float cur = Mathf.Max(0, playerUnit.currentHP);
            if (sliderPlayerHP)  sliderPlayerHP.value  = max > 0 ? cur / max : 0;
            if (textPlayerHP)    textPlayerHP.text      = $"{cur:F0} / {max:F0}";
            if (textPlayerName)  textPlayerName.text    = playerUnit.unitName;
        }

        if (opponentUnit != null)
        {
            float max = opponentUnit.derived.MaxHP;
            float cur = Mathf.Max(0, opponentUnit.currentHP);
            if (sliderOpponentHP)  sliderOpponentHP.value  = max > 0 ? cur / max : 0;
            if (textOpponentHP)    textOpponentHP.text      = $"{cur:F0} / {max:F0}";
            if (textOpponentName)  textOpponentName.text    = opponentUnit.unitName;
        }
    }

    void UpdateTimeline()
    {
        if (textTimeline == null || timeline == null) return;

        var order = timeline.GetTimeline();
        var parts = new System.Text.StringBuilder("행동 순서: ");
        int count = 0;
        foreach (var (unit, av) in order)
        {
            if (count > 0) parts.Append(" → ");
            parts.Append($"{unit.unitName}({av:F0})");
            if (++count >= 4) break;
        }
        textTimeline.text = parts.ToString();
    }

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
    //  기본 스킬 부여 (스킬 DB 없는 경우 폴백)
    // ====================================================
    private static void AssignDefaultSkills(CombatUnit unit)
    {
        unit.equippedSkills.Clear();

        var basicAttack = ScriptableObject.CreateInstance<SkillData>();
        basicAttack.skillName        = "일반공격";
        basicAttack.category         = SkillCategory.Strike;
        basicAttack.weight           = 60;
        basicAttack.damageMultiplier = 1.0f;

        var guardStance = ScriptableObject.CreateInstance<SkillData>();
        guardStance.skillName        = "방어자세";
        guardStance.category         = SkillCategory.Defense;
        guardStance.weight           = 30;
        guardStance.damageMultiplier = 0.5f;

        var quickStep = ScriptableObject.CreateInstance<SkillData>();
        quickStep.skillName        = "빠른발놀림";
        quickStep.category         = SkillCategory.Mobility;
        quickStep.weight           = 40;
        quickStep.damageMultiplier = 0.8f;
        quickStep.avAdvance        = 500f;

        unit.equippedSkills.Add(basicAttack);
        unit.equippedSkills.Add(guardStance);
        unit.equippedSkills.Add(quickStep);
    }
}
