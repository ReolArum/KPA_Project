// ===== 전투 스탯 (6종) =====
public enum CombatStat
{
    STR,
    AGI,
    VIT,
    INT,
    GUT,
    SEN
}

// ===== 유파 =====
public enum SchoolType
{
    None,
    Crusher,
    Ironclad,
    Agile,
    Tactician
}

// ===== 스킬 카테고리 =====
public enum SkillCategory
{
    Strike,
    Defense,
    Mobility,
    Tactics
}

// ===== 전투 방침 =====
public enum BattleDirective
{
    Aggressive,
    Normal,
    Defensive,
    Technical
}

// ===== 판정 결과 =====
public enum HitOutcome
{
    Miss,
    Evaded,
    Hit,
    Critical
}

// ===== 장비 슬롯 =====
public enum EquipSlot
{
    Head,
    Body,
    Arms,
    Legs
}

// ===== 전투 상태 =====
public enum BattleState
{
    NotStarted,
    Running,
    Paused,
    Finished
}
