# 🌲 Scene_Exploration 완전 무결 마스터 설계도 (Master Specification)

이 문서는 프로젝트 KPA의 **탐사(Exploration) 시스템**을 밑바닥부터 구축하고, 리팩토링된 자동 상호작용 및 VN 통합 시스템을 완벽하게 세팅하기 위한 가이드입니다.

---

## 🏗️ 1단계: 씬 기초 인프라 및 핵심 매니저

탐사 시스템은 여러 매니저의 협업으로 이루어집니다. 하이어라키 최상단에 `Managers` 그룹을 생성합니다.

### 1.1 핵심 컴포넌트 설정
1. **ExplorationManager (Script)**: 씬 전체의 두뇌입니다.
    - **Settings**: `Move Speed`(캐릭터 속도), `Time Scale`(시간 소진 배율) 설정.
    - **Layers**: `Ground Layer`(바닥), `Obstacle Layer`(장애물)를 물리 레이어와 맞춥니다.
    - **Stage Data**: 해당 씬에서 사용할 `ExplorationStageData`(SO)를 할당합니다.
2. **ExplorationUIController (Script)**: 탐사 전용 HUD와 결과창을 관리합니다.
3. **ExplorationEventProcessor (Script)**: 선택지 클릭 시 실제 게임 데이터(골드, 아이템, 스탯)를 반영합니다.
4. **DialogueManager (Script)**: VN 연출 시스템의 중앙 허브입니다.

### 1.2 물리 환경 및 NavMesh (필수)
- **Ground (Floor)**: 탐사가 이루어지는 모든 바닥 오브젝트는 `Navigation Static`으로 설정되어야 합니다.
- **Bake**: `Window > AI > Navigation`에서 에이전트 크기에 맞춰 NavMesh를 베이킹합니다.
- **Agent**: `ExplorationManager`의 `fighterPrefab`에 들어갈 캐릭터는 반드시 **NavMeshAgent** 컴포넌트를 가지고 있어야 합니다.

---

## 🌳 2단계: 하이어라키 트리 및 실제 오브젝트 명세 (The Full Tree)

이 트리는 `ExplorationUIController`의 인스펙터 필드와 1:1로 매칭되도록 구성되었습니다. 계층 구조는 렌더링 순서와 직결되므로 반드시 준수하세요.

### 2.1 Scene-Level Objects (월드 공간)
*   **MainCamera_Group**: 카메라 전환 시스템
    *   `Camera_Top` (Camera / 인스펙터 `camTop` 할당)
    *   `Camera_Quarter` (Camera / 인스펙터 `camQuarter` 할당)
*   **Environment**: 맵 환경
    *   `Ground` (MeshCollider, Navigation Static / Layer: Ground)
    *   `Obstacles` (MeshCollider / Layer: Obstacle)
*   **Visualizers**: 시각 효과
    *   `PathRenderer` (LineRenderer / 인스펙터 `pathRenderer` 할당 / 캐릭터 이동 경로 표시)
*   **Markers_Container**: (오브젝트 배치용 빈 오브젝트)
    *   `StartMarker` (ExplorationStartMarker)
    *   `NodeMarker_ID1`, `NodeMarker_ID2`... (ExplorationNodeMarker)

### 2.2 ExplorationCanvas (UI 공간)
*기준 해상도: 1920x1080 (Scale With Screen Size / Match 0.5)*

> [!IMPORTANT]
> **유니티 앵커 설정 주의사항**:
> 1. **Anchor**: 인스펙터의 사각형 로고(Anchor Presets)에서 해당 위치를 선택하세요.
> 2. **Pos X/Y**: 앵커 지점으로부터의 **상대 거리**입니다. (예: 앵커가 Bottom-Right이면 Pos X가 음수여야 화면 안쪽으로 들어옵니다.)
> 3. **Pivot**: 오브젝트의 중심점입니다. (0.5, 0.5는 중앙, 0.5, 0은 하단 중앙)

```text
ExplorationCanvas (Canvas, GraphicRaycaster)
├── HUD_Layer (UI Panel / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / [Image OFF])
│   ├── Panel_TopInfo (UI Image / Anchor: Top-Center / Pivot: 0.5, 1 / Pos: 0, 0 / Size: 700 x 100 / Color: #1E1E1E / Alpha: 0.8)
│   │   ├── Text_Time (TMP_Text / Anchor: Middle-Left / Pivot: 0.5, 0.5 / PosX: 60 / Size: 180 x 50 / Color: White / FontSize: 24 / Alignment: Center)
│   │   ├── Text_Gold (TMP_Text / Anchor: Middle-Center / Pivot: 0.5, 0.5 / PosX: 0 / Size: 180 x 50 / Color: Yellow / FontSize: 24 / Alignment: Center)
│   │   └── Text_Tickets (TMP_Text / Anchor: Middle-Right / Pivot: 0.5, 0.5 / PosX: -60 / Size: 180 x 50 / Color: #FF4B4B / FontSize: 24 / Alignment: Center)
│   ├── Text_PredictedTime (TMP_Text / Anchor: Bottom-Center / Pivot: 0.5, 0.5 / PosY: 180 / Size: 500 x 50 / Color: White / Alpha: 0.7 / FontSize: 22 / Alignment: Center)
│   ├── Text_ActionResult (TMP_Text / Anchor: Middle-Center / Pivot: 0.5, 0.5 / PosY: 200 / Size: 1000 x 80 / Color: White / FontSize: 42 / Alignment: Center)
│   ├── Btn_ConfirmPath (UI Button / Anchor: Bottom-Right / Pivot: 1, 0 / Pos: -50, 50 / Size: 220 x 80 / Color: #32CD32)
│   ├── Btn_ToggleFindings (UI Button / Anchor: Top-Right / Pivot: 1, 1 / Pos: -50, -50 / Size: 80 x 80 / Color: #F1C40F)
│   └── Node_Icon_Container (Empty RectTransform / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / [Raycast Target OFF])
│
├── Overlay_VN_Layer (UI Panel / Active: False / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / Color: Black / Alpha: 0.6)
│   ├── Img_VN_BG (UI Image / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / [Raycast Target ON])
│   ├── Port_Left (UI Image / Anchor: Bottom-Left / Pivot: 0.5, 0 / Pos: 450, 0 / Size: 600 x 900 / Color: White)
│   ├── Port_Right (UI Image / Anchor: Bottom-Right / Pivot: 0.5, 0 / Pos: -450, 0 / Size: 600 x 900 / Color: White)
│   └── Dialogue_Box (UI Button / Anchor: Bottom-Center / Pivot: 0.5, 0 / Pos: 0, 50 / Size: 1300 x 300 / Color: #2A2A2A / Alpha: 0.9)
│       ├── Text_Name (TMP_Text / Anchor: Top-Left / Pivot: 0, 1 / Pos: 80, 50 / Size: 400 x 60 / Color: White / FontSize: 32 / Alignment: Left)
│       ├── Text_Dialogue (TMP_Text / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / Left: 80, Top: 80, Right: 80, Bottom: 60 / Color: White / FontSize: 26 / Alignment: Left)
│       └── Choice_Button_Root (Empty RectTransform / Anchor: Top-Center / Pivot: 0.5, 0 / Pos: 0, 60 / Size: 800 x 400)
│           └── [ChoiceButtonPrefab] (UI Button / Pivot: 0.5, 0.5 / Size: 650 x 70 / Color: #3C3C3C)
│
├── Findings_Layer (UI Panel / Active: False / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / [Image OFF])
│   └── Panel_FindingsWindow (UI Image / Anchor: Middle-Right / Pivot: 1, 0.5 / Pos: -20, 0 / Size: 450 x 800 / Color: #2C3E50 / Alpha: 0.95)
│       ├── Text_FindingsTitle (TMP_Text / Anchor: Top-Center / Pivot: 0.5, 1 / Pos: 0, -30 / Size: 400 x 60 / Color: White / FontSize: 32 / Alignment: Center)
│       └── Scroll_Findings (UI ScrollRect / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / Left: 20, Top: 100, Right: 20, Bottom: 30)
│           └── Findings_Content (Empty RectTransform / Anchor: Top-Center / Pivot: 0.5, 1 / Size: 380 x 1500)
│               └── [FindingsItemPrefab] (UI Button / Pivot: 0.5, 1 / Size: 380 x 80 / Color: #34495E)
│
└── Result_Layer (UI Panel / Active: False / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / Color: Black / Alpha: 0.8)
    ├── Panel_Background (UI Image / Anchor: Stretch-Stretch / Pivot: 0.5, 0.5 / Color: #121212 / Alpha: 0.5)
    ├── Text_Status (TMP_Text / Anchor: Top-Center / Pivot: 0.5, 1 / Pos: 0, -150 / Size: 800 x 120 / Color: Yellow / FontSize: 72 / Alignment: Center)
    ├── Text_Summary (TMP_Text / Anchor: Center / Pivot: 0.5, 0.5 / Pos: 0, 50 / Size: 1100 x 500 / Color: White / FontSize: 24 / Alignment: Left)
    └── Btn_Exit (UI Button / Anchor: Bottom-Center / Pivot: 0.5, 0 / Pos: 0, 100 / Size: 300 x 90 / Color: #E67E22)
```

---

## 📍 3단계: 마커(Marker) 시스템 및 데이터 연결

탐사 노드들을 씬의 매시와 정밀하게 일치시키기 위해 마커 시스템을 활용합니다.

### 3.1 씬 마커 배치
1. **ExplorationStartMarker (Script)**: 캐릭터가 탐사를 시작할 위치에 배치합니다.
2. **ExplorationNodeMarker (Script)**: `StageData`에 정의된 각 노드의 위치에 배치합니다.
    - **Node ID**: 반드시 ScriptableObject에 정의된 `nodeId`와 텍스트가 일치해야 자동 매칭됩니다.
    - **장점**: 개발자는 씬 뷰에서 오브젝트를 배치하고 이름만 맞춰주면 됩니다. 나머지는 `ScanSceneMarkers()`가 처리합니다.

### 3.2 ExplorationStageData (SO) 구성
- **Nodes List**: 씬에 존재하는 모든 상호작용 포인트를 정의합니다.
    - `Node Name`: UI에 노출될 이름.
    - `Event Type`: Combat(전투), Reward(보상), Exit(탈출) 등.
    - `Interaction Range`: 자동 발동을 위한 감지 거리 (보통 1.5 ~ 2.0 권장).
    - `VN Sequence`: 진입 시 출력될 비주얼 노벨 대사들.

---

## ⚙️ 4단계: 상호작용 자동화 로직 (Workflow)

리팩토링된 시스템은 플레이어의 조작 없이 다음 흐름으로 작동합니다.

1. **감지 (Sensing)**: 캐릭터가 이동 중 `currentState.currentPosition`과 노드 마커의 거리를 매 프레임 체크합니다.
2. **발동 (Triggering)**: 거리가 `interactionRange` 이내가 되면 `TriggerEvent(node)`가 호출됩니다.
3. **연출 (Sequence)**: `OnExplorationVNStarted` 이벤트가 발생하여 UI의 VN 패널이 활성화됩니다.
4. **선택 및 결과 (Choice & Result)**: 
    - VN 대사가 끝나면 노드에 설정된 **선택지**가 나타납니다.
    - 선택 시 `ExplorationEventProcessor`가 효과를 적용하고 이동을 재개(`ResumeMovement`)합니다.
5. **종료 (Finalize)**: 'Exit' 타입 노드에 도달하면 VN 종료 후 즉시 정산 페이즈로 전환됩니다.

---

## ✅ 5단계: 최종 체크리스트 (Quality Assurance)

씬 오픈 전 다음 항목을 반드시 확인하세요.

- [ ] **네임스페이스**: `GameManager` 등 필수 매니저에 `System.Collections.Generic` 등이 포함되어 컴파일 오류가 없는가?
- [ ] **레이어 설정**: 캐릭터 프리팹의 바닥 감지 레이어가 올바르게 설정되었는가?
- [ ] **프리팹 할당**: `ExplorationUIController`의 `choiceButtonRoot`와 `choiceButtonPrefab`이 할당되었는가? (누락 시 선택지 안 뜸)
- [ ] **NavMesh**: 경로가 끊긴 곳이 없는가? (Baked Data 확인)
- [ ] **탈출 노드**: 씬에 하나 이상의 `Exit` 타입 노드가 배치되어 있고 마커가 연결되었는가?

---

> [!TIP]
> **디버깅 팁**: 자동 상호작용이 의도치 않게 반복된다면 `ExplorationManager`의 `lastTriggeredNodeId` 변수가 정상적으로 초기화/할당되는지 로그를 통해 확인하세요.

**최종 갱신**: 2026-04-12 (Antigravity Refactoring Ver.)
