# Scene_MainGame 완전 무결 마스터 설계도 (Master Specification)

이 문서는 씬의 기초 공사부터 최종 스크립트 연결까지 모든 과정을 단 하나의 문서로 끝낼 수 있도록 설계되었습니다.

---

## 🏗️ 1단계: 기초 인프라 및 매니저 설정

### 하이어라키 구성 (Managers)
1. **Managers** (GameObject / Pos: 0,0,0)
    - `GameManager` (Script)
    - `TrainingManager` (Script)
    - `QuestManager` (Script) : `Quest Pool` 리스트에 퀘스트 데이터(SO) 할당
    - `DialogueManager` (Script)
    - `SaveSystem` (주의: Static 클래스이므로 **추가하지 마세요**)

### 캔버스 기본 설정 (MainCanvas)
*   **Canvas Scaler**: UI Scale Mode = `Scale With Screen Size` / Resolution = `1920x1080` / Match = `0.5`
*   **EventSystem**: 삭제 금지. 반드시 존재해야 클릭이 작동합니다.

---

계층 구조와 필수 컴포넌트를 정의합니다. 계층 순서(자식의 깊이)는 렌더링 우선순위와 직결되므로 반드시 준수하세요.

### 🛠️ 씬 초기화 가이드 (Step-by-Step)
1. **Managers 생성**: 빈 오브젝트 `Managers`를 생성하고 `GameManager`, `TrainingManager`, `QuestManager`, `DialogueManager`를 컴포넌트로 추가합니다.
2. **MainCanvas 생성**: `UI > Canvas`를 생성하고 `Canvas Scaler`를 1920x1080 (Match 0.5)로 설정합니다.
3. **루트 패널 구성**: `MainCanvas` 자식으로 `Global_HUD`, `Panels_Root`, `Overlay_VN_Layer`를 생성하여 레이어를 분리합니다.
4. **기능 패널 배치**: `Panels_Root` 하위에 `Panel_Schedule`, `Panel_DayMap` 등을 배치하고 모두 비활성(`Active=False`) 처리합니다 (단, `Panel_Schedule`은 시작 시 보여야 하므로 `Active=True`).
5. **컴포넌트 바인딩**: 각 컨트롤러(MainGameUIController 등)에 하이어라키의 오브젝트들을 할당(Drag & Drop)합니다.

---

## 🌳 2단계: 하이어라키 트리 & 컴포넌트 명세 (The Tree)

```text
MainCanvas
├── Global_HUD (Image, GlobalHUDController / Anchor: Top-Stretch / PosY: -50, Height: 100)
│   ├── Text_Day (TMP_Text)
│   ├── Text_Time (TMP_Text)
│   ├── Text_Gold (TMP_Text)
│   ├── Text_Actions (TMP_Text)
│   ├── Stat_Bars (Vertical/Horizontal Layout)
│   │   ├── Slider_Strength ~ Slider_Sensitivity (6개)
│   │   ├── Slider_Stress, Slider_Fatigue (2개)
│   │   └── Slider_Reputation, Slider_Evaluation (2개)
│   └── Btn_OpenCalendar (Button)
│
├── Panels_Root (Stretch-Stretch)
│   ├── Panel_Schedule (Image / MainGameUIController 연결됨)
│   │   ├── Schedule_Grid (GridLayoutGroup)
│   │   ├── Tab_Group (HorizontalLayoutGroup)
│   │   │   └── Btn_TabTraining, Btn_TabWork, Btn_TabRest (Button)
│   │   ├── Content_Windows (창 관리)
│   │   │   ├── Panel_TrainingContent (Active: True)
│   │   │   ├── Panel_WorkContent (Active: False)
│   │   │   └── Panel_RestContent (Active: False)
│   │   ├── Text_SchedulePreviewResult (TMP_Text)
│   │   ├── Btn_ApplyYesterday, Btn_ResetSchedule (Button)
│   │   └── Btn_StartDay (Button)
│   │
│   ├── Panel_DayMap (Image - 배경)
│   │   └── Btn_Home, Btn_Shop, Btn_Gym, Btn_Cafe, Btn_QuestBoard (Button)
│   │
│   ├── Panel_DayPlaceAction (PlaceActionUIController / Active: False)
│   │   ├── Img_LocationBG (Image)
│   │   ├── Img_ThemeOverlay (Image - 색상 오버레이)
│   │   ├── Text_Title (TMP_Text)
│   │   ├── Text_Description (TMP_Text)
│   │   ├── Text_PlaceActionResult (TMP_Text - 하단 안내 메시지)
│   │   └── Button_Grid (Vertical/Grid Layout)
│   │       └── [Btn_Talk, Btn_Buy, Btn_Sell, Btn_Rest, Btn_Upgrade, Btn_Support, Btn_Food, Btn_Reroll, Btn_AcceptQuest, Btn_DeliverQuest, Btn_Back]
│   │
│   ├── Panel_NightChoice (Active: False)
│   │   └── Night_Buttons (Group)
│   │       └── Btn_Exploration, Btn_Arena, Btn_Rest (Button)
│   │
│   └── Panel_LateNightReport (Image / Active: False)
│       ├── Text_Summary (TMP_Text)
│       └── Btn_NextDay (Button)
│
├── Panel_Calendar (CalendarUIController / Active: False)
│   └── [캘린더 내부 구조 생략 - 프리팹 사용 권장]
│
└── Overlay_VN_Layer (Active: False)
    ├── Img_VN_BG (Image)
    ├── Port_Left (Image), Port_Right (Image)
    └── Dialogue_Box (Button, Image)
        ├── Text_Name (TMP_Text)
        └── Text_Dialogue (TMP_Text)
```

---

## 🔗 3단계: 인스펙터 바인딩 마스터 테이블 (Binding Map)

`MainGameUIController` 등 매니저 클래스 인스펙터의 비어있는 슬롯을 채우는 가이드입니다.

### [MainGameUIController] 인스펙터 설정
| 인스펙터 필드명 | 하이어라키에서 드래그할 오브젝트 경로 |
| :--- | :--- |
| **Global HUD** | `MainCanvas/Global_HUD` |
| **Panel VN** | `MainCanvas/Overlay_VN_Layer` |
| **Text VN Name** | `Overlay_VN_Layer/Dialogue_Box/Text_Name` |
| **Text VN Dialogue** | `Overlay_VN_Layer/Dialogue_Box/Text_Dialogue` |
| **Panel Schedule** | `Panels_Root/Panel_Schedule` |
| **Panel Day Map** | `Panels_Root/Panel_DayMap` |
| **Panel Day Place Action** | `Panels_Root/Panel_DayPlaceAction` |
| **Panel Day Summary** | `Panels_Root/Panel_LateNightReport` |
| **Panel Night Choice** | `Panels_Root/Panel_NightChoice` |
| **Schedule Grid Root** | `Panel_Schedule/Schedule_Grid` |
| **Slot Prefab** | `Assets/Prefabs/ScheduleSlotView` (프리팹) |
| **Btn Start Day** | `Panel_Schedule/Btn_StartDay` |
| **Btn Tab Training** | `Panel_Schedule/Tab_Group/Btn_TabTraining` |
| **Panel Training Content**| `Panel_Schedule/Content_Windows/Panel_TrainingContent` |
| **Btn Map Home** | `Panel_DayMap/Btn_Home` |
| **Btn Map Gym** | `Panel_DayMap/Btn_Gym` |
| **Btn Map Cafe** | `Panel_DayMap/Btn_Cafe` |
| **Btn Map Board** | `Panel_DayMap/Btn_QuestBoard` |

---

### [GlobalHUDController] 바인딩 상세
- **Text Day / Time / Gold / Actions**: 이름에 대응하는 TMP_Text 오브젝트
- **Bar Strength ~ Evaluation**: Stat_Bars 하위의 각 Slider 오브젝트 (10개)
- **Btn Open Calendar**: HUD 내의 달력 열기 버튼

---

### [PlaceActionUIController] 바인딩 상세
- **Img Background**: `Panel_DayPlaceAction/Img_LocationBG`
- **Img Theme Overlay**: `Panel_DayPlaceAction/Img_ThemeOverlay`
- **Text Title / Description**: 상단 이름/설명 텍스트
- **Buttons (11개)**: Button_Grid 하위의 각 대응하는 버튼들 (`btnTalk`, `btnBuy`, `btnSell`, `btnRest`, `btnUpgrade`, `btnSupport`, `btnFood`, `btnReroll`, `btnAcceptQuest`, `btnDeliverQuest`, `btnBack`)
- **Themes**: `Assets/Data/LocationThemes` 소장된 데이터 리스트 할당

---

## 📦 4단계: 프리팹(Prefab) 상세 구조 정보

**ScheduleSlotView 프리팹 (반드시 갖춰야 할 컴포넌트)**
- **Root**: `ScheduleSlotView` 스크립트, `Button`, `Outline` (선택 강조용)
- **TimeLabel**: `TMP_Text` (자식) -> 인스펙터 `timeLabelText`에 할당
- **TypeText**: `TMP_Text` (자식) -> 인스펙터 `typeText`에 할당
- **Background**: `Image` (자식) -> 인스펙터 `background`에 할당 (Raycast Target 켜기)

> [!IMPORTANT]
> **마지막 Play 체크리스트**:
> 1. 모든 패널(`Panels_Root` 하위)은 `Panel_Schedule`을 제외하고 모두 **Active = False** 상태로 시작해야 합니다.
> 2. `Managers` 오브젝트의 `GameManager`에서 **Start Day** 페이즈가 정상적으로 설정되어 있는지 확인하세요.

---

## 🔄 5단계: 데이루프 시스템 로직 (The DayLoop)

이 씬의 핵심은 플레이어의 행동(이동/활동)과 전투체의 스케줄 이행이 **동기화**되는 것입니다.

### 1. 전반적인 흐름 (Flow)
1. **오전 스케줄 (MorningSchedule)**: 플레이어가 전투체의 4개 시간 슬롯을 채웁니다.
2. **주간 활동 (DayMap/Action)**:
   - 플레이어가 맵 이동(`OnClickMapLocation`) 또는 특정 장소 액션 수행 시 행동력 1 소모.
   - 행동력이 소모될 때마다 `GameManager.ConsumeTime(1)`이 호출됨.
   - 이 함수 내부에서 `fighter.slotProgress`가 1 증가하며 해당 인덱스의 스케줄이 **즉시 실행**됩니다.
3. **야간 전환 (Transition)**: 주간 행동력(4포인트)을 모두 소모하면 `LateNightReport` 혹은 `NightChoice`로 넘어갑니다.
4. **야간 행동 (NightAction)**: 탐사, 아레나, 혹은 휴식을 선택합니다.
5. **결산 (Report)**: 하루 동안의 스탯 변화와 로그를 확인하고 다음 날로 넘어갑니다.

### 2. 주요 로직 기믹
- **훈련 보조 (Support Training)**: 전투체의 현재 `slotProgress`가 '훈련' 상태일 때만 활성화됩니다. 수행 시 스테미나 소모 대신 보너스 스탯을 얻습니다.
- **이동 = 시간 소모**: 장소 간 이동 시 행동력이 소모되므로, 효율적인 동선을 짜는 것이 중요합니다.
- **밤 활동의 제약**: 스트레스가 너무 높으면(>80) 밤에 탐사나 아레나를 갈 수 없으며 강제로 휴식해야 할 수도 있습니다.
