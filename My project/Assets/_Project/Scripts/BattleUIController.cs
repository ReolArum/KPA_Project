// ===== BattleUIController.cs =====

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("HP Bars")]
    [SerializeField] private Slider sliderPlayerHP;
    [SerializeField] private Slider sliderOpponentHP;
    [SerializeField] private TMP_Text textPlayerHP;
    [SerializeField] private TMP_Text textOpponentHP;

    [Header("Names")]
    [SerializeField] private TMP_Text textPlayerName;
    [SerializeField] private TMP_Text textOpponentName;

    [Header("Timeline")]
    [SerializeField] private TMP_Text textTimeline;

    [Header("Battle Log")]
    [SerializeField] private TMP_Text textBattleLog;
    [SerializeField] private ScrollRect scrollLog;

    [Header("Directive Buttons")]
    [SerializeField] private Button btnAggressive;
    [SerializeField] private Button btnNormal;
    [SerializeField] private Button btnDefensive;
    [SerializeField] private Button btnTechnical;

    [Header("Controls")]
    [SerializeField] private Button btnPause;
    [SerializeField] private TMP_Text textDirectiveLabel;

    [Header("Result Panel")]
    [SerializeField] private GameObject panelResult;
    [SerializeField] private TMP_Text textResult;
    [SerializeField] private Button btnCloseResult;

    private BattleManager bm;
    private string logBuffer = "";

    void Start()
    {
        bm = BattleManager.Instance;
        if (bm == null)
        {
            Debug.LogError("[BattleUI] BattleManager를 찾을 수 없습니다!");
            return;
        }

        bm.OnTurnStart += HandleTurnStart;
        bm.OnSkillSelected += HandleSkillSelected;
        bm.OnDamageApplied += HandleDamageApplied;
        bm.OnBattleEnd += HandleBattleEnd;
        bm.OnTimelineUpdated += HandleTimelineUpdated;

        SetupButtons();

        if (panelResult) panelResult.SetActive(false);
        logBuffer = "";
        if (textBattleLog) textBattleLog.text = "";

        Debug.Log("[BattleUI] 이벤트 등록 완료");
    }

    void OnDestroy()
    {
        if (bm == null) return;

        bm.OnTurnStart -= HandleTurnStart;
        bm.OnSkillSelected -= HandleSkillSelected;
        bm.OnDamageApplied -= HandleDamageApplied;
        bm.OnBattleEnd -= HandleBattleEnd;
        bm.OnTimelineUpdated -= HandleTimelineUpdated;
    }

    void SetupButtons()
    {
        if (btnAggressive) btnAggressive.onClick.AddListener(() =>
        {
            bm.ChangeDirective(BattleDirective.Aggressive);
            UpdateDirectiveLabel();
        });
        if (btnNormal) btnNormal.onClick.AddListener(() =>
        {
            bm.ChangeDirective(BattleDirective.Normal);
            UpdateDirectiveLabel();
        });
        if (btnDefensive) btnDefensive.onClick.AddListener(() =>
        {
            bm.ChangeDirective(BattleDirective.Defensive);
            UpdateDirectiveLabel();
        });
        if (btnTechnical) btnTechnical.onClick.AddListener(() =>
        {
            bm.ChangeDirective(BattleDirective.Technical);
            UpdateDirectiveLabel();
        });
        if (btnPause) btnPause.onClick.AddListener(() =>
        {
            bm.TogglePause();
            var pauseText = btnPause.GetComponentInChildren<TMP_Text>();
            if (pauseText)
                pauseText.text = bm.State == BattleState.Paused ? "재개" : "일시정지";
        });
        if (btnCloseResult) btnCloseResult.onClick.AddListener(() =>
        {
            if (panelResult) panelResult.SetActive(false);
        });
    }

    void UpdateDirectiveLabel()
    {
        if (textDirectiveLabel == null) return;
        textDirectiveLabel.text = bm.CurrentDirective switch
        {
            BattleDirective.Aggressive => "현재 방침: 밀어붙여",
            BattleDirective.Normal => "현재 방침: 평소대로",
            BattleDirective.Defensive => "현재 방침: 버텨",
            BattleDirective.Technical => "현재 방침: 기술위주",
            _ => ""
        };
    }

    // ===== 이벤트 핸들러 =====

    void HandleTurnStart(CombatUnit actor)
    {
        RefreshHP();
        UpdateDirectiveLabel();
    }

    void HandleSkillSelected(CombatUnit actor, SkillData skill)
    {
        string name = skill != null ? skill.skillName : "일반공격";
        AppendLog($"<b>{actor.unitName}</b>: {name} 시전!");
    }

    void HandleDamageApplied(DamageResult result)
    {
        string msg = result.outcome switch
        {
            HitOutcome.Miss => "  → 빗나감!",
            HitOutcome.Evaded => $"  → {result.defender.unitName} 회피!",
            HitOutcome.Hit => $"  → {result.defender.unitName}에게 <color=yellow>{result.finalDamage:F0}</color> 데미지!",
            HitOutcome.Critical => $"  → <color=red>크리티컬!</color> {result.defender.unitName}에게 <color=red>{result.finalDamage:F0}</color> 데미지!",
            _ => ""
        };
        AppendLog(msg);
        RefreshHP();
    }

    void HandleBattleEnd(BattleReport report)
    {
        RefreshHP();

        string resultColor = report.playerWon ? "<color=green>" : "<color=red>";
        string resultText = report.playerWon ? "승리!" : "패배...";
        AppendLog($"\n{resultColor}===== {resultText} =====</color>");

        if (panelResult != null && textResult != null)
        {
            textResult.text = report.ToReportString();
            panelResult.SetActive(true);
        }
    }

    void HandleTimelineUpdated(List<(CombatUnit unit, float av)> timeline)
    {
        if (textTimeline == null) return;

        string text = "행동 순서: ";
        int count = 0;
        foreach (var (unit, av) in timeline)
        {
            if (count > 0) text += " → ";
            text += $"{unit.unitName}({av:F0})";
            count++;
            if (count >= 4) break;
        }
        textTimeline.text = text;
    }

    // ===== HP 갱신 =====

    void RefreshHP()
    {
        if (bm.PlayerUnit != null)
        {
            float pMax = bm.PlayerUnit.derived.MaxHP;
            float pCur = Mathf.Max(0, bm.PlayerUnit.currentHP);
            if (sliderPlayerHP) sliderPlayerHP.value = pMax > 0 ? pCur / pMax : 0;
            if (textPlayerHP) textPlayerHP.text = $"{pCur:F0}/{pMax:F0}";
            if (textPlayerName) textPlayerName.text = bm.PlayerUnit.unitName;
        }

        if (bm.OpponentUnit != null)
        {
            float oMax = bm.OpponentUnit.derived.MaxHP;
            float oCur = Mathf.Max(0, bm.OpponentUnit.currentHP);
            if (sliderOpponentHP) sliderOpponentHP.value = oMax > 0 ? oCur / oMax : 0;
            if (textOpponentHP) textOpponentHP.text = $"{oCur:F0}/{oMax:F0}";
            if (textOpponentName) textOpponentName.text = bm.OpponentUnit.unitName;
        }
    }

    // ===== 로그 =====

    void AppendLog(string line)
    {
        logBuffer += line + "\n";
        if (textBattleLog) textBattleLog.text = logBuffer;

        // 스크롤 맨 아래로
        if (scrollLog != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollLog.verticalNormalizedPosition = 0f;
        }
    }
}
