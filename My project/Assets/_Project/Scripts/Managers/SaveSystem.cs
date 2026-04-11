// ===== SaveSystem.cs =====
using System.Collections.Generic;
using UnityEngine;

public static class SaveSystem
{
    private const string Key = "save_json";

    // ====================================================
    //  직렬화용 내부 DTO
    // ====================================================
    [System.Serializable]
    private class FighterSlotDto
    {
        public int type;
        public int trainingStat;
        public float efficiencyMultiplier;
    }

    [System.Serializable]
    private class StatDto
    {
        public int enumValue;
        public int amount;
    }

    [System.Serializable]
    private class ProfDto
    {
        public int enumValue;
        public int level;
        public int exp;
    }

    [System.Serializable]
    private class EndingDto
    {
        public int enumValue;
        public int amount;
    }

    [System.Serializable]
    private class ArenaDto
    {
        public int rank;
        public int wins;
        public int losses;
        public int promotionWins;
        public int promotionLosses;
    }

    // 유파 레벨 저장 (SchoolType enum int → level)
    [System.Serializable]
    private class SchoolLevelDto
    {
        public int schoolType;
        public int level;
    }

    // 장비 저장 (슬롯 인덱스 + 에셋 이름으로 식별)
    [System.Serializable]
    private class EquippedGearDto
    {
        public int    slotIndex;
        public string equipName; // EquipmentData.equipName 으로 식별
    }

    // 스킬 저장 (스킬 이름으로 식별)
    [System.Serializable]
    private class EquippedSkillDto
    {
        public string skillName;
    }

    // 퀘스트 저장
    [System.Serializable]
    private class QuestDto
    {
        public int    id;
        public string title;
        public string description;
        public int    pickupLocation;
        public int    deliverLocation;
        public int    goldReward;
        public bool   isAccepted;
        public bool   isCompleted;
    }

    // [NEW] 인벤토리 저장
    [System.Serializable]
    private class InventoryItemDto
    {
        public string itemId;
        public int count;
    }

    [System.Serializable]
    private class SaveData
    {
        // ── 기본 ──
        public int  day;
        public int  gold;
        public int  stress;
        public int  fatigue;

        // ── 전투체 스케줄 ──
        public int           fighterSlotProgress;
        public FighterSlotDto[] fighterSchedule;

        // ── 플레이어 위치/행동 ──
        public int  playerActionsUsed;
        public int  playerLocation;

        // ── 밤 ──
        public int  nightChoice;
        public bool nightCompleted;

        // ── [NEW] 장소/상태 ──
        public int   facilityUpgradeLevel;
        public bool  dailyRerollUsed;
        public float trainingEfficiency;

        // ── [NEW] 탐사 결과 ──
        public int      explorationGoldTotal;
        public string[] explorationFoundKeys;

        // ── [NEW] 어제 스케줄 ──
        public FighterSlotDto[] yesterdaySchedule;

        // ── 훈련 스탯 ──
        public StatDto[] stats;

        // ── 숙련도 ──
        public ProfDto[] proficiencies;

        // ── 엔딩 변수 ──
        public EndingDto[] endingVars;

        // ── 아레나 ──
        public ArenaDto arena;

        // ── 전투 데이터 (유파·장비·스킬) ──
        public int              activeSchool;
        public SchoolLevelDto[] schoolLevels;
        public EquippedGearDto[] equippedGear;
        public EquippedSkillDto[] equippedSkills;

        // ── 퀘스트 ──
        public QuestDto[] availableQuests;
        public QuestDto[] activeQuests;
        public QuestDto[] completedQuests;

        // ── [NEW] 인벤토리 ──
        public InventoryItemDto[] inventory;
    }

    // ====================================================
    //  공개 API
    // ====================================================
    public static bool HasSave() =>
        !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));

    public static void Save(GameState state)
    {
        var data = new SaveData
        {
            day                  = state.player.day,
            gold                 = state.player.gold,
            stress               = state.fighter.stress,
            fatigue              = state.fighter.fatigue,
            fighterSlotProgress  = state.fighter.slotProgress,
            playerActionsUsed    = state.player.actionsUsed,
            playerLocation       = (int)state.player.location,
            nightChoice          = (int)state.nightChoice,
            nightCompleted       = state.nightCompleted,

            facilityUpgradeLevel = state.facilityUpgradeLevel,
            dailyRerollUsed     = QuestManager.Instance != null ? QuestManager.Instance.IsRerollUsed : false,
            trainingEfficiency   = state.trainingEfficiency,
            explorationGoldTotal = state.explorationGoldTotal,
            explorationFoundKeys = state.explorationFoundKeys.ToArray()
        };

        // 전투체 스케줄
        data.fighterSchedule = new FighterSlotDto[GameState.DaySlotCount];
        for (int i = 0; i < GameState.DaySlotCount; i++)
        {
            data.fighterSchedule[i] = new FighterSlotDto
            {
                type        = (int)state.fighter.schedule[i].type,
                trainingStat = (int)state.fighter.schedule[i].trainingStat,
                efficiencyMultiplier = state.fighter.schedule[i].efficiencyMultiplier
            };
        }

        // [NEW] 어제 스케줄
        data.yesterdaySchedule = new FighterSlotDto[GameState.DaySlotCount];
        for (int i = 0; i < GameState.DaySlotCount; i++)
        {
            data.yesterdaySchedule[i] = new FighterSlotDto
            {
                type        = (int)state.fighter.yesterdaySchedule[i].type,
                trainingStat = (int)state.fighter.yesterdaySchedule[i].trainingStat,
                efficiencyMultiplier = state.fighter.yesterdaySchedule[i].efficiencyMultiplier
            };
        }

        // 훈련 스탯
        var statList = new List<StatDto>();
        foreach (var kv in state.fighter.stats)
            statList.Add(new StatDto { enumValue = (int)kv.Key, amount = kv.Value });
        data.stats = statList.ToArray();

        // 숙련도
        var profList = new List<ProfDto>();
        foreach (var kv in state.proficiencies)
            profList.Add(new ProfDto { enumValue = (int)kv.Key, level = kv.Value.level, exp = kv.Value.exp });
        data.proficiencies = profList.ToArray();

        // 엔딩 변수
        var endList = new List<EndingDto>();
        foreach (EndingVar v in System.Enum.GetValues(typeof(EndingVar)))
            endList.Add(new EndingDto { enumValue = (int)v, amount = state.endingVars.Get(v) });
        data.endingVars = endList.ToArray();

        // 아레나
        data.arena = new ArenaDto
        {
            rank             = (int)state.arena.currentRank,
            wins             = state.arena.wins,
            losses           = state.arena.losses,
            promotionWins    = state.arena.promotionWins,
            promotionLosses  = state.arena.promotionLosses
        };

        // ── 전투 데이터 ──
        var cd = state.combatData;
        data.activeSchool = (int)cd.activeSchool;

        // 유파 레벨
        var schoolList = new List<SchoolLevelDto>();
        foreach (var kv in cd.schoolLevels)
            schoolList.Add(new SchoolLevelDto { schoolType = (int)kv.Key, level = kv.Value });
        data.schoolLevels = schoolList.ToArray();

        // 장착 장비 (이름으로 식별 - 런타임에 DB 없이도 저장 가능)
        var gearList = new List<EquippedGearDto>();
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
        {
            var equip = cd.GetEquippedItem(slot);
            if (equip != null)
                gearList.Add(new EquippedGearDto { slotIndex = (int)slot, equipName = equip.equipName });
        }
        data.equippedGear = gearList.ToArray();

        // 장착 스킬 (이름으로 식별)
        var skillList = new List<EquippedSkillDto>();
        foreach (var skill in cd.equippedSkills)
            if (skill != null)
                skillList.Add(new EquippedSkillDto { skillName = skill.skillName });
        data.equippedSkills = skillList.ToArray();

        // 퀘스트
        data.availableQuests = SerializeQuests(state.quests.availableQuests);
        data.activeQuests    = SerializeQuests(state.quests.activeQuests);
        data.completedQuests = SerializeQuests(state.quests.completedQuests);

        // [NEW] 인벤토리
        var invList = new List<InventoryItemDto>();
        foreach (var item in state.inventory.items)
            invList.Add(new InventoryItemDto { itemId = item.itemDataId, count = item.count });
        data.inventory = invList.ToArray();

        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void Load(GameState state)
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return;

        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(json); }
        catch { Debug.LogWarning("[SaveSystem] 세이브 파싱 실패, 초기화합니다."); return; }

        // 기본
        state.player.day        = data.day;
        state.player.gold       = data.gold;
        state.fighter.stress    = data.stress;
        state.fighter.fatigue   = data.fatigue;
        state.fighter.slotProgress = data.fighterSlotProgress;
        state.player.actionsUsed = data.playerActionsUsed;
        state.player.location    = (MapLocation)data.playerLocation;
        state.nightChoice       = (NightActionType)data.nightChoice;
        state.nightCompleted    = data.nightCompleted;

        // [NEW]
        state.facilityUpgradeLevel = data.facilityUpgradeLevel;
        if (QuestManager.Instance != null) QuestManager.Instance.SetRerollUsed(data.dailyRerollUsed);
        state.trainingEfficiency   = data.trainingEfficiency;
        state.explorationGoldTotal = data.explorationGoldTotal;
        state.explorationFoundKeys = new List<string>(data.explorationFoundKeys ?? new string[0]);

        // 전투체 스케줄
        if (data.fighterSchedule != null)
        {
            for (int i = 0; i < data.fighterSchedule.Length && i < GameState.DaySlotCount; i++)
            {
                state.fighter.schedule[i].type         = (FighterSlotType)data.fighterSchedule[i].type;
                state.fighter.schedule[i].trainingStat = (TrainingStat)data.fighterSchedule[i].trainingStat;
                state.fighter.schedule[i].efficiencyMultiplier = data.fighterSchedule[i].efficiencyMultiplier;
            }
        }

        // [NEW] 어제 스케줄
        if (data.yesterdaySchedule != null)
        {
            for (int i = 0; i < data.yesterdaySchedule.Length && i < GameState.DaySlotCount; i++)
            {
                state.fighter.yesterdaySchedule[i].type         = (FighterSlotType)data.yesterdaySchedule[i].type;
                state.fighter.yesterdaySchedule[i].trainingStat = (TrainingStat)data.yesterdaySchedule[i].trainingStat;
                state.fighter.yesterdaySchedule[i].efficiencyMultiplier = data.yesterdaySchedule[i].efficiencyMultiplier;
            }
        }

        // 훈련 스탯
        if (data.stats != null)
        {
            foreach (var s in data.stats)
                state.fighter.stats[(TrainingStat)s.enumValue] = s.amount;
        }

        // 숙련도
        if (data.proficiencies != null)
        {
            foreach (var p in data.proficiencies)
            {
                var prof = new Proficiency { level = p.level, exp = p.exp };
                state.proficiencies[(ProficiencyType)p.enumValue] = prof;
            }
        }

        // 엔딩 변수
        if (data.endingVars != null)
        {
            foreach (var e in data.endingVars)
                state.endingVars.Set((EndingVar)e.enumValue, e.amount);
        }

        // 아레나
        if (data.arena != null)
        {
            state.arena.currentRank      = (ArenaRank)data.arena.rank;
            state.arena.wins             = data.arena.wins;
            state.arena.losses           = data.arena.losses;
            state.arena.promotionWins    = data.arena.promotionWins;
            state.arena.promotionLosses  = data.arena.promotionLosses;
        }

        // ── 전투 데이터 ──
        var cd = state.combatData;
        cd.activeSchool = (SchoolType)data.activeSchool;

        // 유파 레벨
        if (data.schoolLevels != null)
        {
            foreach (var sl in data.schoolLevels)
                cd.schoolLevels[(SchoolType)sl.schoolType] = sl.level;
        }

        // 장착 장비: DB 참조가 없으므로 ownedEquipment에서 이름으로 검색
        if (data.equippedGear != null)
        {
            foreach (var g in data.equippedGear)
            {
                var equip = cd.ownedEquipment.Find(e => e != null && e.equipName == g.equipName);
                if (equip != null) cd.equippedGear[(EquipSlot)g.slotIndex] = equip;
            }
        }

        // 장착 스킬: unlockedSkills에서 이름으로 검색
        if (data.equippedSkills != null)
        {
            cd.equippedSkills.Clear();
            foreach (var sk in data.equippedSkills)
            {
                var skill = cd.unlockedSkills.Find(s => s != null && s.skillName == sk.skillName);
                if (skill != null) cd.equippedSkills.Add(skill);
            }
        }

        // 퀘스트
        state.quests.availableQuests = DeserializeQuests(data.availableQuests);
        state.quests.activeQuests    = DeserializeQuests(data.activeQuests);
        state.quests.completedQuests = DeserializeQuests(data.completedQuests);

        // [NEW] 인벤토리 복원
        if (data.inventory != null)
        {
            state.inventory.items.Clear();
            foreach (var dto in data.inventory)
                state.inventory.items.Add(new InventoryItem(dto.itemId, dto.count));
        }
    }

    // ====================================================
    //  퀘스트 직렬화 헬퍼
    // ====================================================
    private static QuestDto[] SerializeQuests(List<Quest> quests)
    {
        if (quests == null) return new QuestDto[0];
        var list = new List<QuestDto>();
        foreach (var q in quests)
        {
            list.Add(new QuestDto
            {
                id              = q.id,
                title           = q.title,
                description     = q.description,
                pickupLocation  = (int)q.pickupLocation,
                deliverLocation = (int)q.deliverLocation,
                goldReward      = q.goldReward,
                isAccepted      = q.isAccepted,
                isCompleted     = q.isCompleted
            });
        }
        return list.ToArray();
    }

    private static List<Quest> DeserializeQuests(QuestDto[] dtos)
    {
        var list = new List<Quest>();
        if (dtos == null) return list;
        foreach (var d in dtos)
        {
            list.Add(new Quest
            {
                id              = d.id,
                title           = d.title,
                description     = d.description,
                pickupLocation  = (MapLocation)d.pickupLocation,
                deliverLocation = (MapLocation)d.deliverLocation,
                goldReward      = d.goldReward,
                isAccepted      = d.isAccepted,
                isCompleted     = d.isCompleted
            });
        }
        return list;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
