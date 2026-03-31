// ===== CombatDataGenerator.cs =====
// Assets/_Project/Scripts/Editor/ 폴더에 저장
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CombatDataGenerator : EditorWindow
{
    [MenuItem("Tools/Combat/기초 데이터 생성")]
    public static void GenerateAll()
    {
        // 폴더 생성
        CreateFolder("Assets/_Project/ScriptableObjects");
        CreateFolder("Assets/_Project/ScriptableObjects/Schools");
        CreateFolder("Assets/_Project/ScriptableObjects/Skills");
        CreateFolder("Assets/_Project/ScriptableObjects/Equipment");
        CreateFolder("Assets/_Project/ScriptableObjects/Database");

        // 생성
        var schools = GenerateSchools();
        var skills = GenerateSkills();
        var equipment = GenerateEquipment();
        GenerateDatabases(schools, skills, equipment);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("===== 전투 기초 데이터 생성 완료! =====");
    }

    // ========================================
    //  유파 4개 생성
    // ========================================
    static List<SchoolData> GenerateSchools()
    {
        var list = new List<SchoolData>();

        // --- 파쇄류 (공격형) ---
        var crusher = CreateAsset<SchoolData>("Assets/_Project/ScriptableObjects/Schools/School_Crusher.asset");
        crusher.schoolName = "파쇄류";
        crusher.schoolType = SchoolType.Crusher;
        crusher.description = "압도적인 공격력으로 상대를 제압하는 유파. 높은 물리 데미지와 방어 무시에 특화.";
        crusher.levels = new List<SchoolLevel>
        {
            new SchoolLevel
            {
                levelName = "초식",
                description = "기본적인 파쇄류 공격법을 익힌다.",
                bonus = new SchoolBonus { physAtkBonus = 5f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "중식",
                description = "파괴력이 증가하고 방어를 무시하는 기술을 습득한다.",
                bonus = new SchoolBonus { physAtkBonus = 10f, ignoreDefenseChance = 10f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "오의",
                description = "파쇄류의 극의에 도달하여 치명적인 일격을 가한다.",
                bonus = new SchoolBonus { physAtkBonus = 15f, critDamageBonus = 20f, ignoreDefenseChance = 15f },
                unlockedSkill = null
            }
        };
        EditorUtility.SetDirty(crusher);
        list.Add(crusher);

        // --- 철벽류 (방어형) ---
        var ironclad = CreateAsset<SchoolData>("Assets/_Project/ScriptableObjects/Schools/School_Ironclad.asset");
        ironclad.schoolName = "철벽류";
        ironclad.schoolType = SchoolType.Ironclad;
        ironclad.description = "견고한 수비로 상대의 공격을 막아내는 유파. 높은 방어력과 HP에 특화.";
        ironclad.levels = new List<SchoolLevel>
        {
            new SchoolLevel
            {
                levelName = "초식",
                description = "기본적인 방어 자세를 익힌다.",
                bonus = new SchoolBonus { physDefBonus = 8f, maxHPBonus = 5f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "중식",
                description = "철벽 방어로 피해를 크게 줄인다.",
                bonus = new SchoolBonus { physDefBonus = 12f, maxHPBonus = 10f, damageReduction = 5f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "오의",
                description = "난공불락의 경지에 이르러 반격까지 가능해진다.",
                bonus = new SchoolBonus { physDefBonus = 15f, maxHPBonus = 15f, damageReduction = 10f, counterAttackChance = 15f },
                unlockedSkill = null
            }
        };
        EditorUtility.SetDirty(ironclad);
        list.Add(ironclad);

        // --- 질풍류 (스피드형) ---
        var agile = CreateAsset<SchoolData>("Assets/_Project/ScriptableObjects/Schools/School_Agile.asset");
        agile.schoolName = "질풍류";
        agile.schoolType = SchoolType.Agile;
        agile.description = "빠른 움직임으로 상대를 압도하는 유파. 높은 속도, 회피, 크리티컬에 특화.";
        agile.levels = new List<SchoolLevel>
        {
            new SchoolLevel
            {
                levelName = "초식",
                description = "빠른 발놀림의 기초를 익힌다.",
                bonus = new SchoolBonus { spdBonus = 8f, evasionBonus = 3f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "중식",
                description = "순간적인 가속으로 급소를 노린다.",
                bonus = new SchoolBonus { spdBonus = 12f, evasionBonus = 5f, critRateBonus = 5f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "오의",
                description = "바람처럼 빠른 움직임으로 적을 농락한다.",
                bonus = new SchoolBonus { spdBonus = 18f, evasionBonus = 8f, critRateBonus = 10f, critDamageBonus = 10f },
                unlockedSkill = null
            }
        };
        EditorUtility.SetDirty(agile);
        list.Add(agile);

        // --- 책사류 (전술형) ---
        var tactician = CreateAsset<SchoolData>("Assets/_Project/ScriptableObjects/Schools/School_Tactician.asset");
        tactician.schoolName = "책사류";
        tactician.schoolType = SchoolType.Tactician;
        tactician.description = "교묘한 전술로 전투의 흐름을 지배하는 유파. 버프/디버프와 스킬 활용에 특화.";
        tactician.levels = new List<SchoolLevel>
        {
            new SchoolLevel
            {
                levelName = "초식",
                description = "기본적인 전술적 사고를 익힌다.",
                bonus = new SchoolBonus { hitRateBonus = 5f, skillCooldownReduction = 5f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "중식",
                description = "상대의 약점을 간파하고 활용한다.",
                bonus = new SchoolBonus { hitRateBonus = 8f, skillCooldownReduction = 10f, critRateBonus = 3f },
                unlockedSkill = null
            },
            new SchoolLevel
            {
                levelName = "오의",
                description = "전장을 완벽히 통제하는 전술의 대가가 된다.",
                bonus = new SchoolBonus { hitRateBonus = 12f, skillCooldownReduction = 15f, critRateBonus = 5f, counterAttackChance = 10f },
                unlockedSkill = null
            }
        };
        EditorUtility.SetDirty(tactician);
        list.Add(tactician);

        Debug.Log($"유파 {list.Count}개 생성 완료");
        return list;
    }

    // ========================================
    //  기본 스킬 생성
    // ========================================
    static List<SkillData> GenerateSkills()
    {
        var list = new List<SkillData>();

        // --- 기본 스킬 (조건 없이 해금) ---
        list.Add(CreateSkill("Skill_BasicAttack", "일반 공격", SkillCategory.Strike,
            weight: 3f, dmgMul: 1.0f, desc: "기본적인 공격을 가한다."));

        list.Add(CreateSkill("Skill_PowerStrike", "강타", SkillCategory.Strike,
            weight: 2f, dmgMul: 1.5f, desc: "힘을 모아 강력한 일격을 가한다.",
            unlockType: SkillUnlockType.StatThreshold, reqStat: CombatStat.STR, reqStatVal: 10));

        list.Add(CreateSkill("Skill_GuardStance", "방어 자세", SkillCategory.Defense,
            weight: 2f, dmgMul: 0.3f, desc: "방어 태세를 취하여 피해를 줄인다."));

        list.Add(CreateSkill("Skill_QuickStep", "빠른 발놀림", SkillCategory.Mobility,
            weight: 2f, dmgMul: 0.5f, avAdvance: 1500f, desc: "빠르게 움직여 다음 행동을 앞당긴다.",
            unlockType: SkillUnlockType.StatThreshold, reqStat: CombatStat.AGI, reqStatVal: 10));

        list.Add(CreateSkill("Skill_Feint", "페인트", SkillCategory.Tactics,
            weight: 1.5f, dmgMul: 0.7f, avDelay: 1000f, desc: "허를 찔러 상대의 행동을 지연시킨다."));

        list.Add(CreateSkill("Skill_HeavyBlow", "헤비 블로우", SkillCategory.Strike,
            weight: 1.5f, dmgMul: 2.0f, avDelay: 500f, desc: "전력을 다한 강력한 일격. 상대 행동도 지연.",
            unlockType: SkillUnlockType.StatThreshold, reqStat: CombatStat.STR, reqStatVal: 20));

        list.Add(CreateSkill("Skill_IronWall", "철벽 수비", SkillCategory.Defense,
            weight: 1.5f, dmgMul: 0.1f, desc: "완벽한 방어 태세로 피해를 최소화한다.",
            unlockType: SkillUnlockType.StatThreshold, reqStat: CombatStat.VIT, reqStatVal: 15));

        list.Add(CreateSkill("Skill_SwiftStrike", "질풍 타격", SkillCategory.Mobility,
            weight: 2f, dmgMul: 0.8f, avAdvance: 2000f, desc: "빠른 공격 후 즉시 다음 행동 준비.",
            unlockType: SkillUnlockType.StatThreshold, reqStat: CombatStat.AGI, reqStatVal: 20));

        list.Add(CreateSkill("Skill_Analyze", "분석", SkillCategory.Tactics,
            weight: 1f, dmgMul: 0.4f, avDelay: 1500f, desc: "상대를 분석하여 약점을 파악한다.",
            unlockType: SkillUnlockType.StatThreshold, reqStat: CombatStat.SEN, reqStatVal: 15));

        list.Add(CreateSkill("Skill_ArmorBreak", "방어 파쇄", SkillCategory.Strike,
            weight: 1f, dmgMul: 1.3f, desc: "상대의 방어를 무시하는 공격.",
            ignDef: true,
            unlockType: SkillUnlockType.SchoolLevel, reqSchool: SchoolType.Crusher, reqSchoolLv: 2));

        Debug.Log($"스킬 {list.Count}개 생성 완료");
        return list;
    }

    static SkillData CreateSkill(string fileName, string name, SkillCategory cat,
        float weight, float dmgMul, string desc,
        float avAdvance = 0, float avDelay = 0, bool ignDef = false,
        SkillUnlockType unlockType = SkillUnlockType.None,
        CombatStat reqStat = CombatStat.STR, int reqStatVal = 0,
        SchoolType reqSchool = SchoolType.None, int reqSchoolLv = 0,
        string reqEvent = "")
    {
        var skill = CreateAsset<SkillData>($"Assets/_Project/ScriptableObjects/Skills/{fileName}.asset");
        skill.skillName = name;
        skill.category = cat;
        skill.weight = weight;
        skill.damageMultiplier = dmgMul;
        skill.description = desc;
        skill.avAdvance = avAdvance;
        skill.avDelay = avDelay;
        skill.ignoreDefense = ignDef;

        skill.unlockCondition = new SkillUnlockCondition
        {
            unlockType = unlockType,
            requiredStat = reqStat,
            requiredStatValue = reqStatVal,
            requiredSchoolType = reqSchool,
            requiredSchoolLevel = reqSchoolLv,
            requiredEventId = reqEvent
        };

        EditorUtility.SetDirty(skill);
        return skill;
    }

    // ========================================
    //  기본 장비 생성
    // ========================================
    static List<EquipmentData> GenerateEquipment()
    {
        var list = new List<EquipmentData>();

        // --- 머리 ---
        list.Add(CreateEquip("Equip_LeatherHelm", "가죽 투구", EquipSlot.Head,
            EquipmentGrade.Common, "기본적인 가죽 투구.", price: 50,
            vit: 1, sen: 1));

        list.Add(CreateEquip("Equip_IronHelm", "철제 투구", EquipSlot.Head,
            EquipmentGrade.Uncommon, "단단한 철제 투구.", price: 150,
            vit: 2, gut: 1));

        // --- 몸통 ---
        list.Add(CreateEquip("Equip_LeatherArmor", "가죽 갑옷", EquipSlot.Body,
            EquipmentGrade.Common, "기본적인 가죽 갑옷.", price: 80,
            vit: 2));

        list.Add(CreateEquip("Equip_ChainMail", "사슬 갑옷", EquipSlot.Body,
            EquipmentGrade.Uncommon, "촘촘한 사슬로 엮은 갑옷.", price: 200,
            vit: 3, str: 1));

        list.Add(CreateEquip("Equip_ShadowVest", "그림자 조끼", EquipSlot.Body,
            EquipmentGrade.Rare, "어둠 속에서 움직임을 돕는 특수 조끼.", price: 0,
            agi: 3, sen: 2, exploration: true));

        // --- 팔 ---
        list.Add(CreateEquip("Equip_LeatherGloves", "가죽 장갑", EquipSlot.Arms,
            EquipmentGrade.Common, "기본적인 가죽 장갑.", price: 40,
            str: 1));

        list.Add(CreateEquip("Equip_IronGauntlets", "철제 건틀릿", EquipSlot.Arms,
            EquipmentGrade.Uncommon, "단단한 철제 건틀릿.", price: 120,
            str: 2, gut: 1));

        list.Add(CreateEquip("Equip_TacticalGloves", "전술 장갑", EquipSlot.Arms,
            EquipmentGrade.Rare, "정밀한 동작을 돕는 특수 장갑.", price: 300,
            sen: 3, intStat: 2));

        // --- 다리 ---
        list.Add(CreateEquip("Equip_LeatherBoots", "가죽 장화", EquipSlot.Legs,
            EquipmentGrade.Common, "기본적인 가죽 장화.", price: 60,
            agi: 1));

        list.Add(CreateEquip("Equip_WindBoots", "질풍 장화", EquipSlot.Legs,
            EquipmentGrade.Uncommon, "바람처럼 빠른 장화.", price: 180,
            agi: 3));

        list.Add(CreateEquip("Equip_TitanGreaves", "타이탄 경갑", EquipSlot.Legs,
            EquipmentGrade.Rare, "거인의 힘이 깃든 경갑.", price: 0,
            str: 2, vit: 2, gut: 2, eventReward: true));

        Debug.Log($"장비 {list.Count}개 생성 완료");
        return list;
    }

    static EquipmentData CreateEquip(string fileName, string name, EquipSlot slot,
        EquipmentGrade grade, string desc, int price = 0,
        int str = 0, int agi = 0, int vit = 0, int intStat = 0, int gut = 0, int sen = 0,
        bool exploration = false, bool eventReward = false)
    {
        var equip = CreateAsset<EquipmentData>($"Assets/_Project/ScriptableObjects/Equipment/{fileName}.asset");
        equip.equipName = name;
        equip.slot = slot;
        equip.grade = grade;
        equip.description = desc;
        equip.buyPrice = price;
        equip.bonusSTR = str;
        equip.bonusAGI = agi;
        equip.bonusVIT = vit;
        equip.bonusINT = intStat;
        equip.bonusGUT = gut;
        equip.bonusSEN = sen;
        equip.isExplorationReward = exploration;
        equip.isEventReward = eventReward;
        EditorUtility.SetDirty(equip);
        return equip;
    }

    // ========================================
    //  데이터베이스 생성
    // ========================================
    static void GenerateDatabases(List<SchoolData> schools, List<SkillData> skills, List<EquipmentData> equipment)
    {
        // School Database
        var schoolDB = CreateAsset<SchoolDatabase>("Assets/_Project/ScriptableObjects/Database/SchoolDatabase.asset");
        schoolDB.schools = schools;
        EditorUtility.SetDirty(schoolDB);

        // Skill Database
        var skillDB = CreateAsset<SkillDatabase>("Assets/_Project/ScriptableObjects/Database/SkillDatabase.asset");
        skillDB.allSkills = skills;
        EditorUtility.SetDirty(skillDB);

        // Equipment Database
        var equipDB = CreateAsset<EquipmentDatabase>("Assets/_Project/ScriptableObjects/Database/EquipmentDatabase.asset");
        equipDB.allEquipment = equipment;
        EditorUtility.SetDirty(equipDB);

        Debug.Log("데이터베이스 3개 생성 완료");
    }

    // ========================================
    //  유틸리티
    // ========================================
    static T CreateAsset<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            Debug.Log($"기존 에셋 덮어쓰기: {path}");
            return existing;
        }

        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void CreateFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
