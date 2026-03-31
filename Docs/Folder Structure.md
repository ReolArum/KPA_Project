Assets/
│
├── _Project/                          # 프로젝트 전용 에셋만 (라이브러리 제외)
│   │
│   ├── Scenes/
│   │   ├── _Core/
│   │   │   ├── Bootstrap.unity        # 게임 초기화
│   │   │   └── Persistence.unity      # 데이터 관리
│   │   ├── Title/
│   │   │   └── Scene_Title.unity
│   │   ├── MainGame/
│   │   │   └── Scene_MainGame.unity
│   │   ├── Exploration/
│   │   │   ├── ExplorationScene.unity
│   │   │   └── ExplorationTest.unity
│   │   └── Battle/
│   │       ├── BattleScene.unity
│   │       └── BattleTest.unity
│   │
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── GameState.cs
│   │   │   ├── GameEvents.cs
│   │   │   ├── Enums.cs
│   │   │   └── Constants.cs
│   │   │
│   │   ├── Systems/
│   │   │   ├── Exploration/
│   │   │   │   ├── ExplorationManager.cs
│   │   │   │   ├── ExplorationState.cs
│   │   │   │   ├── ExplorationUIController.cs
│   │   │   │   ├── ExplorationEventProcessor.cs
│   │   │   │   ├── ExplorationStageData.cs
│   │   │   │   ├── LocationThemeData.cs
│   │   │   │   └── ExplorationDataGenerator.cs (Editor)
│   │   │   │
│   │   │   ├── Battle/
│   │   │   │   ├── BattleSceneController.cs
│   │   │   │   ├── BattleManager.cs (추가: 전투 매니저)
│   │   │   │   ├── ArenaSystem.cs
│   │   │   │   ├── ATBTimeline.cs
│   │   │   │   ├── CombatResolver.cs
│   │   │   │   ├── CombatUnit.cs
│   │   │   │   ├── CombatStats.cs
│   │   │   │   ├── CombatEnums.cs
│   │   │   │   ├── BuffSystem.cs
│   │   │   │   ├── BuffData.cs
│   │   │   │   ├── BattleUIController.cs
│   │   │   │   ├── BattleReport.cs
│   │   │   │   ├── BattleSceneData.cs
│   │   │   │   ├── BattlePreparationUI.cs
│   │   │   │   └── CombatDataGenerator.cs (Editor)
│   │   │   │
│   │   │   ├── Breeding/
│   │   │   │   └── (추후 추가될 육성 시스템)
│   │   │   │
│   │   │   ├── Calendar/
│   │   │   │   ├── CalendarSystem.cs
│   │   │   │   └── DirectiveTable.cs
│   │   │   │
│   │   │   ├── Quest/
│   │   │   │   └── QuestSystem.cs
│   │   │   │
│   │   │   └── UI/
│   │   │       ├── MainGameUIController.cs
│   │   │       ├── TitleUIController.cs
│   │   │       ├── PlaceActionUIController.cs
│   │   │       ├── BattlePreparationUI.cs
│   │   │       ├── GlobalHUDController.cs
│   │   │       ├── ScheduleSlotView.cs
│   │   │       └── SkillSelector.cs
│   │   │
│   │   ├── Creature/
│   │   │   ├── PlayerCombatData.cs
│   │   │   ├── CombatUnit.cs
│   │   │   └── (아트팀이 애니메이터 추가)
│   │   │
│   │   ├── Data/
│   │   │   ├── SkillData.cs
│   │   │   ├── EquipmentData.cs
│   │   │   ├── SchoolData.cs
│   │   │   ├── EndingVariables.cs
│   │   │   └── Proficiency.cs
│   │   │
│   │   ├── Database/
│   │   │   ├── SkillDataBase.cs
│   │   │   ├── EquipmentDataBase.cs
│   │   │   └── SchoolDatabase.cs
│   │   │
│   │   ├── Managers/
│   │   │   ├── SaveSystem.cs
│   │   │   └── (추가: ResourceManager, AudioManager 등)
│   │   │
│   │   └── Utils/
│   │       └── (추가: Helper functions)
│   │
│   ├── Prefabs/
│   │   ├── Battle/
│   │   │   ├── ATBSlot_Prefab.prefab
│   │   │   ├── SkillSlot_Prefab.prefab
│   │   │   └── Btn_Choice.prefab
│   │   ├── UI/
│   │   │   ├── PF_EquipItem.prefab
│   │   │   ├── PF_ScheduleSlot.prefab
│   │   │   ├── PF_SchoolItem.prefab
│   │   │   ├── PF_SkillItem.prefab
│   │   │   └── (기타 UI 프리팹)
│   │   ├── Creatures/
│   │   │   └── (생물 프리팹들 - 아트팀 관리)
│   │   └── Effects/
│   │       └── (이펙트 프리팹들)
│   │
│   ├── Animations/
│   │   ├── Battle/
│   │   │   ├── BattleFighterController.controller
│   │   │   ├── PlayerAnimator.controller
│   │   │   └── OpponentAnimator.controller
│   │   └── UI/
│   │       └── (UI 애니메이션)
│   │
│   ├── Materials/
│   │   ├── Creatures/
│   │   ├── UI/
│   │   └── Effects/
│   │
│   ├── Sprites/
│   │   ├── UI/
│   │   │   ├── Icons/
│   │   │   ├── Buttons/
│   │   │   └── Panels/
│   │   ├── Creatures/
│   │   └── Effects/
│   │
│   ├── Resources/
│   │   ├── Data/
│   │   │   ├── BalanceConfig.json       # 게임 밸런스 (기획자 관리)
│   │   │   ├── GameConfig.json
│   │   │   └── Localization/
│   │   │       ├── Korean.json
│   │   │       └── English.json
│   │   └── Audio/
│   │       ├── BGM/
│   │       └── SFX/
│   │
│   ├── ScriptableObjects/
│   │   ├── _Database/              # 마스터 데이터
│   │   │   ├── EquipmentDatabase.asset
│   │   │   ├── SchoolDatabase.asset
│   │   │   └── SkillDatabase.asset
│   │   ├── Skills/                 # 개별 스킬 (기획자가 생성)
│   │   │   ├── Skill_BasicAttack.asset
│   │   │   ├── Skill_PowerStrike.asset
│   │   │   └── ...
│   │   ├── Equipment/              # 개별 장비
│   │   │   ├── Equip_IronHelm.asset
│   │   │   └── ...
│   │   ├── Schools/                # 개별 학파
│   │   │   ├── School_Agile.asset
│   │   │   └── ...
│   │   ├── Exploration/            # 탐사 스테이지
│   │   │   ├── SampleExplorationStage.asset
│   │   │   └── NodeIcon.prefab
│   │   └── Battle/
│   │       └── SampleStageData.asset
│   │
│   ├── Fonts/
│   │   ├── Pretendard-Regular.otf
│   │   └── TMP_Pretendard_Regular.asset
│   │
│   └── Config/
│       └── EditorSettings.asset
│
├── EEJANAI_Team/                   # 외부 라이브러리/에셋 (별도 폴더)
│   ├── FreeFighterAnimations/
│   ├── Commons/
│   └── Readme.pdf
│
├── TextMesh Pro/                   # 외부 라이브러리
├── Settings/
│
└── Docs/                           # 문서 (Assets 외부)
    ├── Architecture.md
    ├── ExplorationSystem.md
    ├── BattleSystem.md
    ├── DataFormat.md
    ├── CodeConventions.md
    └── API.md
