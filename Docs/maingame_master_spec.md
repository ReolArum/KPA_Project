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

## 🌳 2단계: 하이어라키 트리 & 컴포넌트 명세 (The Tree)

계층 구조와 필수 컴포넌트를 정의합니다. 계층 순서(자식의 깊이)는 렌더링 우선순위와 직결되므로 반드시 준수하세요.

```text
MainCanvas
├── Global_HUD (Image, GlobalHUDController / Anchor: Top-Stretch / PosY: -50, Height: 100)
│   ├── Text_Day (TMP_Text / Anchor: Top-Left / PosX: 200, PosY: -50)
│   ├── Text_Time (TMP_Text / Anchor: Top-Center)
│   ├── Text_Gold (TMP_Text / Anchor: Top-Right / PosX: -200, PosY: -50)
│   └── Stat_Bars (VerticalLayoutGroup)
│       └── [Slider_Strength ... Slider_Evaluation] (10개 슬라이더)
│
├── Panels_Root (Stretch-Stretch / 0,0,0,0)
│   ├── Panel_Schedule (MainGameUIController 연결용 / Image)
│   │   ├── Schedule_Grid (GridLayoutGroup / Anchor: Middle-Center / Size: 1200, 300)
│   │   ├── Tab_Group (HorizontalLayoutGroup)
│   │   │   └── Btn_TabTraining, Btn_TabWork, Btn_TabRest (Button)
│   │   └── Btn_StartDay (Button / Anchor: Bottom-Right / Size: 250, 80)
│   │
│   ├── Panel_DayMap (Image - 배경 / Active: False)
│   │   └── Btn_Home, Btn_Shop, Btn_Gym, Btn_Cafe (Button, Image)
│   │
│   ├── Panel_DayPlaceAction (PlaceActionUIController / Active: False)
│   │   ├── Img_LocationBG (Image)
│   │   ├── Text_Title (TMP_Text)
│   │   ├── Text_Description (TMP_Text)
│   │   └── Button_Grid (VerticalLayoutGroup)
│   │       └── [Btn_Talk, Btn_Buy, Btn_Rest ... 11개 버튼]
│   │
│   ├── Panel_NightChoice (Active: False)
│   │   └── Btn_Exploration, Btn_Arena, Btn_Rest (Button)
│   │
│   └── Panel_LateNightReport (Image / Active: False)
│       ├── Text_Summary (TMP_Text)
│       └── Btn_NextDay (Button)
│
└── Overlay_VN_Layer (Active: False)
    ├── Img_VN_BG (Image)
    ├── Img_LeftPort (Image / Anchor: Bottom-Left)
    ├── Img_RightPort (Image / Anchor: Bottom-Right)
    └── Dialogue_Box (Button, Image / Anchor: Bottom-Stretch)
        ├── Text_Name (TMP_Text)
        └── Text_Dialogue (TMP_Text)
```

---

## 🔗 3단계: 인스펙터 바인딩 마스터 테이블 (Binding Map)

`MainGameUIController` 등 매니저 클래스 인스펙터의 비어있는 슬롯을 채우는 가이드입니다.

### [MainGameUIController] 인스펙터 설정
| 인스펙터 필드명 (SerializedField) | 하이어라키에서 드래그할 오브젝트 경로 |
| :--- | :--- |
| **Global HUD** | `MainCanvas > Global_HUD` |
| **Panel VN** | `MainCanvas > Overlay_VN_Layer` |
| **Text VN Name** | `Overlay_VN_Layer > Dialogue_Box > Text_Name` |
| **Text VN Dialogue** | `Overlay_VN_Layer > Dialogue_Box > Text_Dialogue` |
| **Panel Schedule** | `Panels_Root > Panel_Schedule` |
| **Panel Day Map** | `Panels_Root > Panel_DayMap` |
| **Panel Day Place Action** | `Panels_Root > Panel_DayPlaceAction` |
| **Panel Day Summary** | `Panels_Root > Panel_LateNightReport` |
| **Panel Night Choice** | `Panels_Root > Panel_NightChoice` |
| **Schedule Grid Root** | `Panel_Schedule > Schedule_Grid` |
| **Slot Prefab** | `Assets/Prefabs/ScheduleSlotView` 프리팹 할당 |
| **Btn Start Day** | `Panel_Schedule > Btn_StartDay` |
| **Btn Tab Training** | `Panel_Schedule > Tab_Group > Btn_TabTraining` |
| **Btn Map Home** | `Panel_DayMap > Btn_Home` |
| **Btn Map Training Center** | `Panel_DayMap > Btn_Gym` |

---

### [GlobalHUDController] (Global_HUD 오브젝트에 위치)
*   **Text Day / Gold / Time / Actions**: 각 텍스트 오브젝트 1:1 매칭
*   **Bar Strength ~ Evaluation**: 10개의 Slider 오브젝트 순서대로 할당

---

### [PlaceActionUIController] (Panel_DayPlaceAction 오브젝트에 위치)
*   **Visuals**: `Text_Title`, `Text_Description`, `Img_LocationBG` 할당
*   **Buttons**: `Btn_Talk`, `Btn_Buy`, `Btn_Rest` 등 11개 버튼을 이름에 맞게 드래그

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
