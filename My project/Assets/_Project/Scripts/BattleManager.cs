// ===== BattleManager.cs =====

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float turnDelay = 0.8f;  // 턴 간 딜레이 (연출용)

    // 전투 시스템 컴포넌트
    private ATBTimeline timeline = new();
    private SkillSelector skillSelector = new();
    private CombatResolver resolver = new();
    private BuffSystem buffSystem;

    // 전투 유닛
    public CombatUnit PlayerUnit { get; private set; }
    public CombatUnit OpponentUnit { get; private set; }

    // 상태
    public BattleState State { get; private set; } = BattleState.NotStarted;
    public BattleDirective CurrentDirective { get; private set; } = BattleDirective.Normal;
    public BattleReport Report { get; private set; }
    private int currentTurn = 0;

    // 이벤트 (UI 연동용)
    public event Action<CombatUnit> OnTurnStart;
    public event Action<CombatUnit, SkillData> OnSkillSelected;
    public event Action<DamageResult> OnDamageApplied;
    public event Action<BattleReport> OnBattleEnd;
    public event Action<List<(CombatUnit unit, float av)>> OnTimelineUpdated;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        buffSystem = new BuffSystem(timeline);
    }

    // ====================================================
    //  공개 API
    // ====================================================

    /// <summary>전투 시작. GameState에서 플레이어 유닛을 생성하고, 랭크 기반 상대를 생성.</summary>
    public void StartBattle(GameState gameState)
    {
        PlayerUnit = CombatUnit.CreateFromGameState(gameState);
        OpponentUnit = CombatUnit.CreateOpponent(gameState.arena.currentRank, gameState.day);

        // 각자 별도 스킬 인스턴스 부여
        AssignDefaultSkills(PlayerUnit);
        AssignDefaultSkills(OpponentUnit);

        timeline.Initialize(PlayerUnit, OpponentUnit);
        Report = new BattleReport();
        currentTurn = 0;
        CurrentDirective = BattleDirective.Normal;
        State = BattleState.Running;

        Debug.Log($"[전투] {PlayerUnit.unitName} (HP:{PlayerUnit.derived.MaxHP:F0}, SPD:{PlayerUnit.derived.SPD:F1}, 스킬:{PlayerUnit.equippedSkills.Count}개) vs {OpponentUnit.unitName} (HP:{OpponentUnit.derived.MaxHP:F0}, SPD:{OpponentUnit.derived.SPD:F1}, 스킬:{OpponentUnit.equippedSkills.Count}개)");

        OnTimelineUpdated?.Invoke(timeline.GetTimeline());

        StartCoroutine(BattleLoop());
    }
    /// <summary>방침 변경 (코치의 유일한 개입)</summary>
    public void ChangeDirective(BattleDirective newDirective)
    {
        CurrentDirective = newDirective;
    }

    /// <summary>일시정지/재개</summary>
    public void TogglePause()
    {
        if (State == BattleState.Running) State = BattleState.Paused;
        else if (State == BattleState.Paused) State = BattleState.Running;
    }

    // ====================================================
    //  전투 루프 (코루틴)
    // ====================================================

    private IEnumerator BattleLoop()
    {
        Debug.Log("[전투] BattleLoop 시작");

        while (State == BattleState.Running || State == BattleState.Paused)
        {
            while (State == BattleState.Paused)
                yield return null;

            if (State != BattleState.Running)
                break;

            currentTurn++;
            Debug.Log($"[전투] ===== 턴 {currentTurn} =====");

            // 1. 다음 행동자 결정
            CombatUnit actor = timeline.AdvanceAndGetNext();
            CombatUnit target = (actor == PlayerUnit) ? OpponentUnit : PlayerUnit;
            bool isPlayer = (actor == PlayerUnit);

            Debug.Log($"[전투] 행동자: {actor.unitName} | 스킬 수: {actor.equippedSkills.Count}");

            OnTurnStart?.Invoke(actor);

            // 2. 버프 턴 감소
            buffSystem.OnUnitTurnStart(actor);

            // 3. 스킬 선택
            BattleDirective directive = isPlayer ? CurrentDirective : BattleDirective.Normal;
            SkillData chosen = skillSelector.SelectSkill(actor, target, directive);

            if (chosen == null)
            {
                Debug.LogError("[전투] 스킬이 null입니다! 스킬 목록을 확인하세요.");
                yield break;
            }

            Debug.Log($"[전투] {actor.unitName} → {chosen.skillName} 사용");

            OnSkillSelected?.Invoke(actor, chosen);

            // 4. 연출 대기
            yield return new WaitForSeconds(turnDelay * 0.5f);

            // 5. 판정 + 데미지
            DamageResult result = resolver.Resolve(actor, target, chosen);

            // 데미지 적용
            if (result.outcome == HitOutcome.Hit || result.outcome == HitOutcome.Critical)
            {
                target.currentHP -= result.finalDamage;
                target.currentHP = Mathf.Max(0, target.currentHP);

                if (chosen.avAdvance > 0)
                    timeline.AdvanceUnit(actor, chosen.avAdvance);
                if (chosen.avDelay > 0)
                    timeline.DelayUnit(target, chosen.avDelay);

                if (chosen.appliedBuff != null)
                    buffSystem.ApplyBuff(actor, chosen.appliedBuff);
                if (chosen.appliedDebuff != null)
                    buffSystem.ApplyBuff(target, chosen.appliedDebuff);
            }

            // 6. 리포트 기록
            Report.LogAction(actor, chosen, result, currentTurn, isPlayer);

            // 7. 이벤트 발사
            OnDamageApplied?.Invoke(result);
            OnTimelineUpdated?.Invoke(timeline.GetTimeline());

            // 8. 연출 대기
            yield return new WaitForSeconds(turnDelay * 0.5f);

            // 9. 승패 판정
            if (target.currentHP <= 0)
            {
                State = BattleState.Finished;
                Report.playerWon = (target == OpponentUnit);
                Debug.Log($"[전투] 전투 종료! {(Report.playerWon ? "승리" : "패배")}");
                OnBattleEnd?.Invoke(Report);
                yield break;
            }
        }

        Debug.Log("[전투] BattleLoop 종료 (State: " + State + ")");
    }


    // ====================================================
    //  기본 스킬 부여 (임시)
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

        Debug.Log($"[전투] {unit.unitName}에게 스킬 {unit.equippedSkills.Count}개 부여 완료");
    }

}
