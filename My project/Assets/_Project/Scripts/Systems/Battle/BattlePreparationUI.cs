// ===== BattlePreparationUI.cs =====
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePreparationUI : MonoBehaviour
{
    // ====================================================
    //  References
    // ====================================================
    [Header("Databases")]
    [SerializeField] private SchoolDatabase schoolDB;
    [SerializeField] private SkillDatabase skillDB;
    [SerializeField] private EquipmentDatabase equipDB;

    // ====================================================
    //  Panel 전환
    // ====================================================
    [Header("Panels")]
    [SerializeField] private GameObject panelPreparation;
    [SerializeField] private GameObject panelSchool;
    [SerializeField] private GameObject panelEquipment;
    [SerializeField] private GameObject panelSkill;
    [SerializeField] private GameObject panelOpponent;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnTabSchool;
    [SerializeField] private Button btnTabEquipment;
    [SerializeField] private Button btnTabSkill;
    [SerializeField] private Button btnTabOpponent;

    // ====================================================
    //  유파 탭
    // ====================================================
    [Header("School Tab")]
    [SerializeField] private Transform schoolListRoot;
    [SerializeField] private GameObject schoolItemPrefab;
    [SerializeField] private TMP_Text textSchoolInfo;
    [SerializeField] private TMP_Text textActiveSchool;

    // ====================================================
    //  장비 탭
    // ====================================================
    [Header("Equipment Tab")]
    [SerializeField] private TMP_Text textEquipHead;
    [SerializeField] private TMP_Text textEquipBody;
    [SerializeField] private TMP_Text textEquipArms;
    [SerializeField] private TMP_Text textEquipLegs;
    [SerializeField] private Button btnEquipHead;
    [SerializeField] private Button btnEquipBody;
    [SerializeField] private Button btnEquipArms;
    [SerializeField] private Button btnEquipLegs;

    [Header("Equipment Selection Popup")]
    [SerializeField] private GameObject popupEquipSelect;
    [SerializeField] private Transform equipListRoot;
    [SerializeField] private GameObject equipItemPrefab;
    [SerializeField] private Button btnEquipSelectClose;
    [SerializeField] private TMP_Text textEquipSelectTitle;

    // ====================================================
    //  스킬 탭
    // ====================================================
    [Header("Skill Tab")]
    [SerializeField] private Transform equippedSkillRoot;
    [SerializeField] private Transform unlockedSkillRoot;
    [SerializeField] private GameObject skillItemPrefab;
    [SerializeField] private TMP_Text textSkillSlotInfo;

    // ====================================================
    //  상대 정보 탭
    // ====================================================
    [Header("Opponent Tab")]
    [SerializeField] private TMP_Text textOpponentName;
    [SerializeField] private TMP_Text textOpponentRank;
    [SerializeField] private TMP_Text textOpponentStats;
    [SerializeField] private TMP_Text textOpponentSchool;
    [SerializeField] private TMP_Text textOpponentEquipment;
    [SerializeField] private TMP_Text textOpponentSkills;

    // ====================================================
    //  하단
    // ====================================================
    [Header("Bottom")]
    [SerializeField] private TMP_Text textPlayerStatsPreview;
    [SerializeField] private Button btnStartBattle;
    [SerializeField] private Button btnBack;

    // ====================================================
    //  내부 변수
    // ====================================================
    private GameState gameState;
    private CombatUnit previewOpponent;
    private EquipSlot currentEquipSlot;
    private System.Action onBattleStart;
    private System.Action onBack;

    // ====================================================
    //  초기화
    // ====================================================
    void Awake()
    {
        if (btnTabSchool) btnTabSchool.onClick.AddListener(() => ShowTab(0));
        if (btnTabEquipment) btnTabEquipment.onClick.AddListener(() => ShowTab(1));
        if (btnTabSkill) btnTabSkill.onClick.AddListener(() => ShowTab(2));
        if (btnTabOpponent) btnTabOpponent.onClick.AddListener(() => ShowTab(3));

        if (btnEquipHead) btnEquipHead.onClick.AddListener(() => OpenEquipSelect(EquipSlot.Head));
        if (btnEquipBody) btnEquipBody.onClick.AddListener(() => OpenEquipSelect(EquipSlot.Body));
        if (btnEquipArms) btnEquipArms.onClick.AddListener(() => OpenEquipSelect(EquipSlot.Arms));
        if (btnEquipLegs) btnEquipLegs.onClick.AddListener(() => OpenEquipSelect(EquipSlot.Legs));

        if (btnEquipSelectClose) btnEquipSelectClose.onClick.AddListener(CloseEquipSelect);

        if (btnStartBattle) btnStartBattle.onClick.AddListener(() => onBattleStart?.Invoke());
        if (btnBack) btnBack.onClick.AddListener(() => onBack?.Invoke());

        if (popupEquipSelect) popupEquipSelect.SetActive(false);
    }

    // ====================================================
    //  외부 호출: 열기/닫기
    // ====================================================
    public void Open(GameState state, System.Action onStart, System.Action onCancel)
    {
        gameState = state;
        onBattleStart = onStart;
        onBack = onCancel;

        state.combatData.LinkGameState(state);

        if (skillDB != null)
            state.combatData.CheckAndUnlockSkills(skillDB.allSkills);

        previewOpponent = CombatUnit.CreateOpponent(state.arena.currentRank, state.day);

        if (panelPreparation) panelPreparation.SetActive(true);
        ShowTab(0);
        RefreshAll();
    }

    public void Close()
    {
        if (panelPreparation) panelPreparation.SetActive(false);
    }

    // ====================================================
    //  탭 전환
    // ====================================================
    void ShowTab(int index)
    {
        if (panelSchool) panelSchool.SetActive(index == 0);
        if (panelEquipment) panelEquipment.SetActive(index == 1);
        if (panelSkill) panelSkill.SetActive(index == 2);
        if (panelOpponent) panelOpponent.SetActive(index == 3);

        RefreshCurrentTab(index);
        RefreshStatsPreview(); // 하단 수치는 항상 갱신
    }

    void RefreshCurrentTab(int index)
    {
        switch (index)
        {
            case 0: RefreshSchoolTab(); break;
            case 1: RefreshEquipmentTab(); break;
            case 2: RefreshSkillTab(); break;
            case 3: RefreshOpponentTab(); break;
        }
    }

    // ====================================================
    //  전체 갱신
    // ====================================================
    void RefreshAll()
    {
        RefreshSchoolTab();
        RefreshEquipmentTab();
        RefreshSkillTab();
        RefreshOpponentTab();
        RefreshStatsPreview();
    }

    // ====================================================
    //  유파 탭
    // ====================================================
    void RefreshSchoolTab()
    {
        if (schoolDB == null || schoolListRoot == null) return;

        for (int i = schoolListRoot.childCount - 1; i >= 0; i--)
            Destroy(schoolListRoot.GetChild(i).gameObject);

        var data = gameState.combatData;

        if (textActiveSchool) textActiveSchool.text = "유파 현황 (패시브 자동 적용)";

        foreach (var school in schoolDB.schools)
        {
            if (schoolItemPrefab == null) break;

            var go = Instantiate(schoolItemPrefab, schoolListRoot);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            var btn = go.GetComponentInChildren<Button>();

            int playerLevel = data.GetSchoolLevel(school.schoolType);

            // 이름 + 레벨
            if (texts.Length >= 1)
                texts[0].text = $"{school.schoolName}  Lv.{playerLevel} / {school.MaxLevel}";

            // 레벨별 해금 현황 + 현재 적용 중인 보너스
            if (texts.Length >= 2)
            {
                string info = "";
                for (int i = 0; i < school.levels.Count; i++)
                {
                    string check = i < playerLevel ? "★" : "☆";
                    info += $"  {check} Lv.{i + 1} {school.levels[i].levelName}: {school.levels[i].description}\n";
                }

                if (playerLevel > 0)
                {
                    var bonus = school.GetCumulativeBonus(playerLevel);
                    info += "\n  [적용 중인 보너스]\n";
                    if (bonus.physAtkBonus != 0) info += $"    공격력 +{bonus.physAtkBonus}%\n";
                    if (bonus.physDefBonus != 0) info += $"    방어력 +{bonus.physDefBonus}%\n";
                    if (bonus.spdBonus != 0) info += $"    속도 +{bonus.spdBonus}%\n";
                    if (bonus.maxHPBonus != 0) info += $"    최대HP +{bonus.maxHPBonus}%\n";
                    if (bonus.hitRateBonus != 0) info += $"    명중률 +{bonus.hitRateBonus}\n";
                    if (bonus.evasionBonus != 0) info += $"    회피율 +{bonus.evasionBonus}\n";
                    if (bonus.critRateBonus != 0) info += $"    크리확률 +{bonus.critRateBonus}\n";
                    if (bonus.critDamageBonus != 0) info += $"    크리데미지 +{bonus.critDamageBonus}%\n";
                    if (bonus.ignoreDefenseChance != 0) info += $"    방무확률 +{bonus.ignoreDefenseChance}%\n";
                    if (bonus.counterAttackChance != 0) info += $"    반격확률 +{bonus.counterAttackChance}%\n";
                    if (bonus.damageReduction != 0) info += $"    피해감소 +{bonus.damageReduction}%\n";
                }
                else
                {
                    info += "\n  (미해금 - 훈련/이벤트를 통해 레벨업)";
                }

                texts[1].text = info;
            }

            // 버튼 숨기기 (선택 기능 제거)
            if (btn != null) btn.gameObject.SetActive(false);
        }
    }

    // ====================================================
    //  장비 탭
    // ====================================================
    void RefreshEquipmentTab()
    {
        var data = gameState.combatData;

        RefreshEquipSlotText(textEquipHead, data.GetEquippedItem(EquipSlot.Head), "머리");
        RefreshEquipSlotText(textEquipBody, data.GetEquippedItem(EquipSlot.Body), "몸통");
        RefreshEquipSlotText(textEquipArms, data.GetEquippedItem(EquipSlot.Arms), "팔");
        RefreshEquipSlotText(textEquipLegs, data.GetEquippedItem(EquipSlot.Legs), "다리");
    }

    void RefreshEquipSlotText(TMP_Text text, EquipmentData equip, string slotName)
    {
        if (text == null) return;

        if (equip == null)
        {
            text.text = $"[{slotName}] 비어있음";
        }
        else
        {
            string stats = "";
            if (equip.bonusSTR != 0) stats += $" STR+{equip.bonusSTR}";
            if (equip.bonusAGI != 0) stats += $" AGI+{equip.bonusAGI}";
            if (equip.bonusVIT != 0) stats += $" VIT+{equip.bonusVIT}";
            if (equip.bonusINT != 0) stats += $" INT+{equip.bonusINT}";
            if (equip.bonusGUT != 0) stats += $" GUT+{equip.bonusGUT}";
            if (equip.bonusSEN != 0) stats += $" SEN+{equip.bonusSEN}";
            text.text = $"[{slotName}] {equip.equipName} ({equip.grade}){stats}";
        }
    }

    void OpenEquipSelect(EquipSlot slot)
    {
        currentEquipSlot = slot;
        if (popupEquipSelect) popupEquipSelect.SetActive(true);

        string slotName = slot switch
        {
            EquipSlot.Head => "머리",
            EquipSlot.Body => "몸통",
            EquipSlot.Arms => "팔",
            EquipSlot.Legs => "다리",
            _ => ""
        };
        if (textEquipSelectTitle) textEquipSelectTitle.text = $"{slotName} 장비 선택";

        if (equipListRoot == null) return;
        for (int i = equipListRoot.childCount - 1; i >= 0; i--)
            Destroy(equipListRoot.GetChild(i).gameObject);

        // "해제" 버튼
        if (equipItemPrefab != null)
        {
            var unequipGo = Instantiate(equipItemPrefab, equipListRoot);
            var unequipTexts = unequipGo.GetComponentsInChildren<TMP_Text>();
            var unequipBtn = unequipGo.GetComponentInChildren<Button>();
            if (unequipTexts.Length >= 1) unequipTexts[0].text = "장비 해제";
            if (unequipBtn != null)
            {
                unequipBtn.onClick.AddListener(() =>
                {
                    gameState.combatData.UnequipItem(currentEquipSlot);
                    CloseEquipSelect();
                    RefreshAll();
                });
            }
        }

        // 보유 장비 중 해당 슬롯 필터
        var owned = gameState.combatData.ownedEquipment;
        foreach (var equip in owned)
        {
            if (equip.slot != slot) continue;
            if (equipItemPrefab == null) break;

            var go = Instantiate(equipItemPrefab, equipListRoot);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            var btn = go.GetComponentInChildren<Button>();

            string statInfo = GetEquipStatString(equip);
            if (texts.Length >= 1) texts[0].text = $"{equip.equipName} ({equip.grade}) {statInfo}";
            if (texts.Length >= 2 && !string.IsNullOrEmpty(equip.description)) texts[1].text = equip.description;

            bool isEquipped = gameState.combatData.GetEquippedItem(slot) == equip;

            if (btn != null)
            {
                var btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText) btnText.text = isEquipped ? "장착중" : "장착";
                btn.interactable = !isEquipped;

                EquipmentData captured = equip;
                btn.onClick.AddListener(() =>
                {
                    gameState.combatData.EquipItem(captured);
                    CloseEquipSelect();
                    RefreshAll();
                });
            }
        }
    }

    void CloseEquipSelect()
    {
        if (popupEquipSelect) popupEquipSelect.SetActive(false);
    }

    string GetEquipStatString(EquipmentData equip)
    {
        string s = "";
        if (equip.bonusSTR != 0) s += $"STR+{equip.bonusSTR} ";
        if (equip.bonusAGI != 0) s += $"AGI+{equip.bonusAGI} ";
        if (equip.bonusVIT != 0) s += $"VIT+{equip.bonusVIT} ";
        if (equip.bonusINT != 0) s += $"INT+{equip.bonusINT} ";
        if (equip.bonusGUT != 0) s += $"GUT+{equip.bonusGUT} ";
        if (equip.bonusSEN != 0) s += $"SEN+{equip.bonusSEN} ";
        return s.Trim();
    }

    // ====================================================
    //  스킬 탭
    // ====================================================
    void RefreshSkillTab()
    {
        var data = gameState.combatData;

        int maxSlots = gameState.MaxSkillSlots;
        if (textSkillSlotInfo) textSkillSlotInfo.text = $"스킬 슬롯: {data.equippedSkills.Count}/{maxSlots}";

        // 장착된 스킬
        if (equippedSkillRoot != null)
        {
            for (int i = equippedSkillRoot.childCount - 1; i >= 0; i--)
                Destroy(equippedSkillRoot.GetChild(i).gameObject);

            foreach (var skill in data.equippedSkills)
            {
                if (skillItemPrefab == null) break;
                var go = Instantiate(skillItemPrefab, equippedSkillRoot);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                var btn = go.GetComponentInChildren<Button>();

                if (texts.Length >= 1) texts[0].text = $"{skill.skillName} [{skill.category}]";
                if (texts.Length >= 2) texts[1].text = skill.description;

                if (btn != null)
                {
                    var btnText = btn.GetComponentInChildren<TMP_Text>();
                    if (btnText) btnText.text = "해제";
                    SkillData captured = skill;
                    btn.onClick.AddListener(() =>
                    {
                        data.UnequipSkill(captured);
                        RefreshAll();
                    });
                }
            }
        }

        // 해금된 스킬 (미장착)
        if (unlockedSkillRoot != null)
        {
            for (int i = unlockedSkillRoot.childCount - 1; i >= 0; i--)
                Destroy(unlockedSkillRoot.GetChild(i).gameObject);

            foreach (var skill in data.unlockedSkills)
            {
                if (data.equippedSkills.Contains(skill)) continue;
                if (skillItemPrefab == null) break;

                var go = Instantiate(skillItemPrefab, unlockedSkillRoot);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                var btn = go.GetComponentInChildren<Button>();

                if (texts.Length >= 1) texts[0].text = $"{skill.skillName} [{skill.category}]";
                if (texts.Length >= 2) texts[1].text = skill.description;

                bool canEquip = data.equippedSkills.Count < maxSlots;

                if (btn != null)
                {
                    var btnText = btn.GetComponentInChildren<TMP_Text>();
                    if (btnText) btnText.text = canEquip ? "장착" : "슬롯 부족";
                    btn.interactable = canEquip;

                    SkillData captured = skill;
                    btn.onClick.AddListener(() =>
                    {
                        data.EquipSkill(captured);
                        RefreshAll();
                    });
                }
            }
        }
    }

    // ====================================================
    //  상대 정보 탭
    // ====================================================
    void RefreshOpponentTab()
    {
        if (previewOpponent == null) return;

        if (textOpponentName) textOpponentName.text = $"이름: {previewOpponent.unitName}";
        if (textOpponentRank) textOpponentRank.text = $"등급: {gameState.arena.GetRankName()}";

        if (textOpponentStats)
        {
            var s = previewOpponent.rawStats;
            var d = previewOpponent.derived;
            textOpponentStats.text =
                $"[기본 스탯]\n" +
                $"  STR: {s.STR}  AGI: {s.AGI}  VIT: {s.VIT}\n" +
                $"  INT: {s.INT}  GUT: {s.GUT}  SEN: {s.SEN}\n\n" +
                $"[파생 스탯]\n" +
                $"  HP: {d.MaxHP:F0}  ATK: {d.PhysAtk:F0}  DEF: {d.PhysDef:F0}\n" +
                $"  SPD: {d.SPD:F0}  명중: {d.HitRate:F0}%  회피: {d.EvasionRate:F0}%\n" +
                $"  크리: {d.CritRate:F0}%";
        }

        if (textOpponentSchool)
        {
            string schoolName = previewOpponent.schoolType == SchoolType.None ? "없음" : GetSchoolName(previewOpponent.schoolType);
            textOpponentSchool.text = $"유파: {schoolName}";
        }

        if (textOpponentEquipment)
        {
            string equipText = "[장비]\n";
            if (previewOpponent.equipment != null)
            {
                foreach (var eq in previewOpponent.equipment)
                {
                    if (eq != null) equipText += $"  {eq.equipName}\n";
                }
            }
            if (equipText == "[장비]\n") equipText += "  없음\n";
            textOpponentEquipment.text = equipText;
        }

        if (textOpponentSkills)
        {
            string skillText = "[스킬]\n";
            if (previewOpponent.equippedSkills != null)
            {
                foreach (var sk in previewOpponent.equippedSkills)
                {
                    if (sk != null) skillText += $"  {sk.skillName} [{sk.category}]\n";
                }
            }
            if (skillText == "[스킬]\n") skillText += "  기본 스킬\n";
            textOpponentSkills.text = skillText;
        }
    }

    // ====================================================
    //  스탯 미리보기 (하단)
    // ====================================================
    void RefreshStatsPreview()
    {
        if (textPlayerStatsPreview == null) return;

        var baseStats = gameState.GetCombatStats();
        var derived = new CombatDerivedStats();
        derived.Calculate(baseStats);

        textPlayerStatsPreview.text =
            $"[내 전투체 예상 스탯]  " +
            $"HP:{derived.MaxHP:F0}  ATK:{derived.PhysAtk:F0}  DEF:{derived.PhysDef:F0}  " +
            $"SPD:{derived.SPD:F0}  명중:{derived.HitRate:F0}%  회피:{derived.EvasionRate:F0}%  " +
            $"크리:{derived.CritRate:F0}%";
    }

    // ====================================================
    //  유틸리티
    // ====================================================
    string GetSchoolName(SchoolType type)
    {
        if (schoolDB == null) return type.ToString();
        var data = schoolDB.GetSchool(type);
        return data != null ? data.schoolName : type.ToString();
    }
}
