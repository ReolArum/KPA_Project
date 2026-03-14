// ===== BattleSceneController.cs =====
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleSceneController : MonoBehaviour
{
    [Header("캐릭터")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator opponentAnimator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform opponentTransform;

    [Header("연출 설정")]
    [SerializeField] private float attackMoveSpeed = 8f;
    [SerializeField] private float returnSpeed = 5f;
    [SerializeField] private float turnDelay = 0.5f;
    [SerializeField] private float resultDelay = 2f;

    // 전투 시스템
    private CombatUnit playerUnit;
    private CombatUnit opponentUnit;
    private BattleReport report;
    private ATBTimeline timeline;
    private BuffSystem buffSystem;
    private SkillSelector skillSelector;
    private CombatResolver resolver;
    private int turnCount = 0;
    private bool battleActive = false;

    private Vector3 playerStartPos;
    private Vector3 opponentStartPos;

    void Start()
    {
        if (BattleSceneData.playerUnit == null || BattleSceneData.opponentUnit == null)
        {
            Debug.LogError("전투 데이터가 없습니다! 메인 씬에서 시작하세요.");
            return;
        }

        playerUnit = BattleSceneData.playerUnit;
        opponentUnit = BattleSceneData.opponentUnit;

        // 기본 스킬 부여 (스킬이 없는 경우)
        if (playerUnit.equippedSkills == null || playerUnit.equippedSkills.Count == 0)
            AssignDefaultSkills(playerUnit);
        if (opponentUnit.equippedSkills == null || opponentUnit.equippedSkills.Count == 0)
            AssignDefaultSkills(opponentUnit);

        report = new BattleReport();
        timeline = new ATBTimeline();
        buffSystem = new BuffSystem(timeline);
        skillSelector = new SkillSelector();
        resolver = new CombatResolver();

        timeline.Initialize(playerUnit, opponentUnit);

        playerStartPos = playerTransform.position;
        opponentStartPos = opponentTransform.position;

        // Idle 상태에서 애니메이션 정지 (Idle 클립이 없을 때)
        playerAnimator.speed = 0;
        opponentAnimator.speed = 0;

        StartCoroutine(BattleLoop());
    }

    // ====================================================
    //  메인 전투 루프
    // ====================================================
    IEnumerator BattleLoop()
    {
        battleActive = true;

        yield return new WaitForSeconds(1f);

        while (battleActive)
        {
            turnCount++;

            // ATB로 행동 순서 결정
            CombatUnit attacker = timeline.AdvanceAndGetNext();
            CombatUnit defender = (attacker == playerUnit) ? opponentUnit : playerUnit;
            bool isPlayerTurn = (attacker == playerUnit);

            Animator attackerAnim = isPlayerTurn ? playerAnimator : opponentAnimator;
            Animator defenderAnim = isPlayerTurn ? opponentAnimator : playerAnimator;
            Transform attackerTr = isPlayerTurn ? playerTransform : opponentTransform;
            Transform defenderTr = isPlayerTurn ? opponentTransform : playerTransform;
            Vector3 attackerStartPos = isPlayerTurn ? playerStartPos : opponentStartPos;

            // 버프 턴 처리
            buffSystem.OnUnitTurnStart(attacker);

            // 스킬 선택
            SkillData skill = skillSelector.SelectSkill(attacker, defender, BattleDirective.Normal);

            if (skill == null)
            {
                Debug.LogError("스킬이 null입니다!");
                yield break;
            }

            Debug.Log($"[전투] 턴 {turnCount}: {attacker.unitName} → {skill.skillName}");

            // 공격 연출
            yield return StartCoroutine(AttackSequence(
                attackerAnim, defenderAnim,
                attackerTr, defenderTr,
                attackerStartPos,
                attacker, defender, skill, isPlayerTurn
            ));

            // 승패 체크
            if (defender.currentHP <= 0)
            {
                report.playerWon = isPlayerTurn;
                yield return StartCoroutine(BattleEnd(isPlayerTurn));
                yield break;
            }

            // 턴 최대치 체크
            if (turnCount >= 100)
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
    // 1. 공격자 애니메이션 활성화
    attackerAnim.speed = 1;

    // 2. 빠르게 접근 (돌진 느낌)
    Vector3 targetPos = Vector3.Lerp(attackerTr.position, defenderTr.position, 0.75f);
    yield return StartCoroutine(MoveToPosition(attackerTr, targetPos, 15f));

    // 3. 공격 애니메이션 재생
    attackerAnim.SetTrigger("Attack");

    // 4. Attack 상태 진입 대기
    yield return null;
    yield return new WaitUntil(() =>
        attackerAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack"));

    // 5. 타격 타이밍에 데미지 판정
    yield return new WaitUntil(() =>
        attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.35f);

    // 6. 데미지 계산
    DamageResult dmgResult = resolver.Resolve(attacker, defender, skill);
    report.LogAction(attacker, skill, dmgResult, turnCount, isPlayerTurn);

    // 7. 피격 처리
    switch (dmgResult.outcome)
    {
        case HitOutcome.Hit:
        case HitOutcome.Critical:
            defender.currentHP -= dmgResult.finalDamage;
            if (defender.currentHP < 0) defender.currentHP = 0;
            defenderAnim.speed = 1;
            defenderAnim.SetTrigger("Hit");

            if (skill.avAdvance > 0)
                timeline.AdvanceUnit(attacker, skill.avAdvance);
            if (skill.avDelay > 0)
                timeline.DelayUnit(defender, skill.avDelay);

            if (skill.appliedBuff != null)
                buffSystem.ApplyBuff(attacker, skill.appliedBuff);
            if (skill.appliedDebuff != null)
                buffSystem.ApplyBuff(defender, skill.appliedDebuff);

            Debug.Log($"[전투] {attacker.unitName} → {skill.skillName} → " +
                      $"{dmgResult.finalDamage:F0} 데미지! (HP: {defender.currentHP:F0})");
            break;

        case HitOutcome.Evaded:
            Debug.Log($"[전투] {defender.unitName} 회피!");
            break;

        case HitOutcome.Miss:
            Debug.Log($"[전투] {attacker.unitName} 빗나감!");
            break;
    }

    // 8. 공격 애니메이션 끝날 때까지 대기
    yield return new WaitUntil(() =>
        attackerAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.85f ||
        !attackerAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack"));

    // 9. 피격 애니메이션도 끝날 때까지 대기
    if (defenderAnim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
    {
        yield return new WaitUntil(() =>
            defenderAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.85f ||
            !defenderAnim.GetCurrentAnimatorStateInfo(0).IsName("Hit"));
    }

    // 10. 빠르게 복귀
    yield return StartCoroutine(MoveToPosition(attackerTr, startPos, 12f));

    // 11. 정지
    attackerAnim.speed = 0;
    defenderAnim.speed = 0;

    // 12. 짧은 대기
    yield return new WaitForSeconds(0.2f);
}


    // ====================================================
    //  캐릭터 이동 코루틴
    // ====================================================
    IEnumerator MoveToPosition(Transform tr, Vector3 target, float speed)
    {
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

        Debug.Log($"[전투] 전투 종료! {(playerWon ? "승리" : "패배")}");

        if (playerWon)
        {
            playerAnimator.SetTrigger("Victory");
            opponentAnimator.SetTrigger("Defeat");
        }
        else
        {
            playerAnimator.SetTrigger("Defeat");
            opponentAnimator.SetTrigger("Victory");
        }

        yield return new WaitForSeconds(resultDelay);

        // 결과 저장 후 메인 씬으로 복귀
        BattleSceneData.CompleteBattle(report);
        SceneManager.LoadScene("MainScene");
    }

    // ====================================================
    //  기본 스킬 부여
    // ====================================================
    private void AssignDefaultSkills(CombatUnit unit)
    {
        unit.equippedSkills.Clear();

        var basicAttack = ScriptableObject.CreateInstance<SkillData>();
        basicAttack.skillName = "일반공격";
        basicAttack.category = SkillCategory.Strike;
        basicAttack.weight = 60;
        basicAttack.damageMultiplier = 1.0f;

        var guardStance = ScriptableObject.CreateInstance<SkillData>();
        guardStance.skillName = "방어자세";
        guardStance.category = SkillCategory.Defense;
        guardStance.weight = 30;
        guardStance.damageMultiplier = 0.5f;

        var quickStep = ScriptableObject.CreateInstance<SkillData>();
        quickStep.skillName = "빠른발놀림";
        quickStep.category = SkillCategory.Mobility;
        quickStep.weight = 40;
        quickStep.damageMultiplier = 0.8f;
        quickStep.avAdvance = 500f;

        unit.equippedSkills.Add(basicAttack);
        unit.equippedSkills.Add(guardStance);
        unit.equippedSkills.Add(quickStep);
    }
}