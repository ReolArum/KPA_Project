# 🗺️ 탐사 씬 완전 무결 하이어라키 설계도 (100% Full Specification)

이 문서는 탐사 시스템의 모든 기능을 구동하기 위해 필요한 **단 하나의 오브젝트도 누락하지 않은 전체 구조**를 보여줍니다. 스크립트의 모든 `[SerializeField]` 필드와 1:1로 매핑되도록 작성되었습니다.

---

## 🌳 1. 전체 하이어라키 트리 (Full Hierarchy Tree)

```text
Exploration_Scene (Root)
├── EventSystem (EventSystem, StandaloneInputModule) -- [필수] UI 클릭 감지
├── Global Volume (Volume) -- [필수] URP 환경 노출/색감 설정
├── [Cameras]
│   ├── Main Camera (Camera, AudioListener) -- Top View (Planning 페이즈용)
│   └── Quarter Camera (Camera) -- 45도 Follow View (Moving 페이즈용)
├── [Managers]
│   ├── ExplorationManager (ExplorationManager, PlayerInput)
│   └── ExplorationEventProcessor (ExplorationEventProcessor - Singleton)
├── [Environment]
│   ├── Floor (MeshRenderer, MeshCollider) -- Layer: Ground / Static 체크
│   ├── Walls (MeshRenderer, MeshCollider) -- Layer: Obstacle / Static 체크
│   └── NavMeshSurface (NavMeshSurface) -- [Bake] 버튼 클릭 필수
├── [Visuals]
│   └── Path_Line_Renderer (LineRenderer) -- 경로가 그려질 선 (Width: 0.1, Material 필수)
└── [UI_Canvas] (Canvas, CanvasScaler, GraphicRaycaster, ExplorationUIController)
    ├── HUD_Panel (RectTransform, Image) -- 상단 UI 바
    │   ├── Text_Time (TMP_Text) -- 남은 시간 (00:00)
    │   ├── Text_Choices (TMP_Text) -- 선택권 (횟수: 5)
    │   ├── Text_Gold (TMP_Text) -- 골드 (100 G)
    │   ├── Text_Predicted_Time (TMP_Text) -- 예상 시간 (예상: 00:10)
    │   └── Btn_Confirm_Path (Button, Image) -- 경로 확정 [V] 버튼
    ├── Event_Popup_Panel (RectTransform, Image) -- 위험 조우/사건 발생 팝업 (평소 비활성화)
    │   ├── Text_Event_Title (TMP_Text) -- 사건 종류 (예: 장애물)
    │   ├── Text_Event_Desc (TMP_Text) -- 사건 설명 (예: 길을 막고 있습니다.)
    │   └── Choice_Button_Root (RectTransform, VerticalLayoutGroup) -- 선택지 버튼이 쌓이는 곳
    ├── VN_Story_Panel (RectTransform, Image) -- 스토리 진행/대화창 (평소 비활성화)
    │   ├── Img_VN_Background (Image) -- 배경 연출
    │   ├── Img_VN_Left (Image) -- 좌측 스탠딩 일러스트
    │   ├── Img_VN_Right (Image) -- 우측 스탠딩 일러스트
    │   ├── Text_VN_Name (TMP_Text) -- 말하는 사람 이름
    │   ├── Text_VN_Dialogue (TMP_Text) -- 대사 텍스트
    │   └── Btn_VN_Dialogue_Box (Button) -- 대화창 전체 투명 클릭 버튼 (다음 대사)
    ├── Clue_List_Panel (RectTransform, Image) -- 단서 보관함 창 (평소 비활성화)
    │   ├── Scroll_View (ScrollRect) -- 단서가 많을 경우 대비
    │   │   └── Clue_List_Content (RectTransform, VerticalLayoutGroup, ContentSizeFitter) -- 실제 생성 위치
    │   └── Btn_Close_Clue_List (Button) -- [X] 닫기 버튼
    ├── Interact_Prompt_Panel (RectTransform) -- 상호작용 안내 (평소 비활성화)
    │   └── Text_Interact_Prompt (TMP_Text) -- "[E] 상호작용"
    ├── Result_Panel (RectTransform, Image) -- 탐사 종료 결과창 (평소 비활성화)
    │   ├── Text_Result_Status (TMP_Text) -- 성공 / 실패 결과
    │   ├── Text_Result_Summary (TMP_Text) -- 최종 골드 / 단서 정보
    │   └── Btn_Exit_Stage (Button) -- 결과 확인 후 퇴장 버튼
    ├── Node_Icon_Container (RectTransform) -- 지도上に 뿌려지는 노드 아이콘들의 부모
    └── Btn_Toggle_Clue_List (Button, Image) -- HUD 옆 단서 보관함 열기 아이콘 버튼
```

---

## ⚙️ 2. 상세 컴포넌트 설정 및 변수 매핑

### 2.1 ExplorationUIController (모든 UI 연결)
*   프로젝트의 `ExplorationUIController` 스크립트를 인스펙터에서 아래와 같이 채워야 합니다:
    *   **HUD**: `textTime`, `textChoices`, `textGold`, `textPredictedTime`, `btnConfirmPath`
    *   **Event Popup**: `panelEvent`(Event_Popup_Panel), `textEventTitle`, `textEventDesc`, `choiceButtonRoot`
    *   **Prefab**: `choiceButtonPrefab` (자식으로 TMP_Text가 있는 Button 프리팹)
    *   **Result**: `panelResult`(Result_Panel), `textResultStatus`, `textResultSummary`, `btnExit`
    *   **VN**: `panelVN`(VN_Story_Panel), `textVNName`, `textVNDialogue`, `imgVNLeft/Right/Background`, `btnVNDialogueBox`
    *   **Interaction/Clue**: `panelInteractPrompt`, `textInteractPrompt`, `panelClueList`, `clueListContent`, `clueItemPrefab` (텍스트 표시용 prefab), `btnToggleClueList`
    *   **Hardware**: `camTop`, `camQuarter`, `pathRenderer`(Path_Line_Renderer)
    *   **Icons**: `nodeIconPrefab`, `nodeContainer`(Node_Icon_Container)

### 2.2 ExplorationManager (시스템 설정)
*   **Settings**: `moveSpeed`(5), `timeScale`(1)
*   **Input**: `playerInput` (자기 자신 PlayerInput 오브젝트 연결)
*   **Layers**: `groundLayer`(Ground 체크), `obstacleLayer`(Obstacle 체크)
*   **Visuals**: `playerTransform` (실제 움직이는 3D 캐릭터 모델 할당)

---

## 🏗️ 3. 필수 프리팹 내부 구조 (Prefab Inner Structure)

1.  **Choice_Button_Prefab**:
    *   Root: `Button`
    *   Child: `TMP_Text` (선택지 텍스트)
2.  **Node_Icon_Prefab**:
    *   Root: `Image` (원형 아이콘 등)
    *   Child: `TMP_Text` (노드 이름/ID)
3.  **Clue_Item_Prefab**:
    *   Root: `RectTransform`
    *   Child: `TMP_Text` (획득한 단서 이름)

---

## 📐 4. 패널 레이아웃 세부 수치 (RectTransform Specs)

모든 수치는 **Canvas Scaler (1920x1080)** 기준이며, 해상도 대응을 위한 **Anchors** 설정을 포함합니다.

| 패널 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 주요 특징 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **HUD_Panel** | (0.5, 1) / (0.5, 1) | (0.5, 1) | 0, -20 | W: 1200, H: 100 | 상단 중앙 고정 바 |
| **Event_Popup_Panel** | (0.5, 0.5) / (0.5, 0.5) | (0.5, 0.5) | 0, 0 | W: 850, H: 450 | 중앙 집중 팝업 |
| **VN_Story_Panel** | (0, 0) / (1, 0) | (0.5, 0) | Pos Y: 50 | L: 260, R: 260, H: 320 | 하단 대화창 영역 (가로 Stretch) |
| **Clue_List_Panel** | (1, 0.5) / (1, 0.5) | (1, 0.5) | -50, 0 | W: 400, H: 800 | 우측 단서 보관함 |
| **Interact_Prompt** | (0.5, 0) / (0.5, 0) | (0.5, 0) | 0, 400 | W: 400, H: 80 | 하단 중앙 안내 (E) |
| **Result_Panel** | (0.5, 0.5) / (0.5, 0.5) | (0.5, 0.5) | 0, 0 | W: 900, H: 600 | 결과 정산 풀 팝업 |
| **Node_Icon_Container** | (0, 0) / (1, 1) | (0.5, 0.5) | - | L: 0, R: 0, T: 0, B: 0 | 전체 캔버스 위 노드 아이콘 배치용 (Full Stretch) |
| **Btn_Toggle_Clue_List** | (1, 1) / (1, 1) | (1, 1) | -160, -20 | W: 100, H: 100 | 우측 상단 HUD 옆 단서 보관함 열기 버튼 |

### 🛠️ UI 정렬 보조 컴포넌트 설정
1.  **Choice_Button_Root**:
    *   `Vertical Layout Group` 추가.
    *   `Control Child Size`: Width(V), Height(V) 체크.
    *   `Child Force Expand`: Width(V) 체크.
2.  **Clue_List_Content**:
    *   `Vertical Layout Group` 및 `Content Size Fitter` 추가.
    *   `Vertical Fit`: Preferred Size 로 설정.

---

## 🧩 5. 자식 오브젝트 세부 레이아웃 (Child Element Specs)

각 패널 내부 자식들의 수치입니다. 부속 컴포넌트(TMP_Text 가로/세로 정렬 등) 설정도 포함됩니다.

### 5.1 HUD_Panel 내부 자식 (1200 x 100)
| 자식 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 비고 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Text_Time** | (0, 0.5) / (0, 0.5) | (0, 0.5) | 50, 0 | W: 250, H: 60 | 왼쪽 정렬, Center |
| **Text_Choices** | (0, 0.5) / (0, 0.5) | (0, 0.5) | 320, 0 | W: 300, H: 60 | 시간 옆 배치 |
| **Text_Gold** | (1, 0.5) / (1, 0.5) | (1, 0.5) | -250, 0 | W: 250, H: 60 | 오른쪽 정렬 |
| **Text_Predicted**| (0.5, 0) / (0.5, 0) | (0.5, 1) | 0, -10 | W: 400, H: 40 | 바 밑으로 살짝 노출 |
| **Btn_Confirm** | (1, 0.5) / (1, 0.5) | (1, 0.5) | -50, 0 | W: 80, H: 80 | 가장 우측 아이콘 버튼 |

### 5.2 Event_Popup_Panel 내부 자식 (850 x 450)
| 자식 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 비고 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Text_Title** | (0, 1) / (1, 1) | (0.5, 1) | Pos Y: -40 | L: 0, R: 0, H: 80 | 상단 Stretch (여백 0) |
| **Text_Desc** | (0, 0.5) / (1, 0.5) | (0.5, 0.5) | Pos Y: 20 | L: 50, R: 50, H: 200 | 중앙 Stretch (좌우 50) |
| **Button_Root** | (0, 0) / (1, 0) | (0.5, 0) | Pos Y: 50 | L: 75, R: 75, H: 120 | 하단 버튼 정렬 구역 |

### 5.3 VN_Story_Panel 내부 자식 (1400 x 320)
| 자식 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 비고 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Img_Char_Left** | (0, 0) / (0, 0) | (0, 0) | 100, 320 | W: 500, H: 700 | 패널 위로 솟아오른 형태 |
| **Img_Char_Right**| (1, 0) / (1, 0) | (1, 0) | -100, 320 | W: 500, H: 700 | 우측 스탠딩 |
| **Text_Name** | (0, 1) / (0, 1) | (0, 0) | 50, 10 | W: 300, H: 60 | 대화창 좌측 상단 이름 |
| **Text_Dialogue**| (0, 0) / (1, 1) | (0.5, 0.5) | - | L: 80, R: 80, T: 80, B: 20 | 내부 여백 준 Full Stretch |
| **Btn_Dialogue** | (0, 0) / (1, 1) | (0.5, 0.5) | - | L: 0, R: 0, T: 0, B: 0 | 전체 클릭용 (Full) |

### 5.4 Clue_List_Panel 내부 자식 (400 x 800)
| 자식 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 비고 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Scroll_View** | (0, 0) / (1, 1) | (0.5, 0.5) | - | L: 20, R: 20, T: 125, B: 25 | 리스트 영역 |
| **Btn_Close** | (1, 1) / (1, 1) | (1, 1) | -10, -10 | W: 50, H: 50 | 우측 상단 X 버튼 |

### 5.5 Interact_Prompt_Panel 내부 자식 (400 x 80)
| 자식 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 비고 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Text_Prompt** | (0, 0) / (1, 1) | (0.5, 0.5) | - | L: 0, R: 0, T: 0, B: 0 | 패널 전체 채움 |

### 5.6 Result_Panel 내부 자식 (900 x 600)
| 자식 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 비고 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Text_Status** | (0.5, 1) / (0.5, 1) | (0.5, 1) | 0, -80 | W: 800, H: 100 | 상단 결과 타이틀 (성공/실패) |
| **Text_Summary** | (0, 0.5) / (1, 0.5) | (0.5, 0.5) | Pos Y: 0 | L: 100, R: 100, H: 300 | 중앙 정산 상세 내용 |
| **Btn_Exit** | (0.5, 0) / (0.5, 0) | (0.5, 0) | 0, 80 | W: 300, H: 80 | 하단 나가기 버튼 |

### 5.7 Canvas 직속 기타 요소

> [!NOTE]
> 아래 두 요소는 특정 패널의 자식이 아닌 **UI_Canvas 바로 아래** 위치합니다.

| 요소 명칭 | Anchors (Min/Max) | Pivot | Pos X, Y | Rect Values (W/H or L/R/T/B) | 비고 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Node_Icon_Container** | (0, 0) / (1, 1) | (0.5, 0.5) | - | L: 0, R: 0, T: 0, B: 0 | 캔버스 전체를 덮는 Full Stretch. 노드 아이콘 프리팹이 런타임에 생성됨 |
| **Btn_Toggle_Clue_List** | (1, 1) / (1, 1) | (1, 1) | -160, -20 | W: 100, H: 100 | 우측 상단 고정 아이콘 버튼. HUD_Panel Btn_Confirm 왼쪽에 배치 |

---

---

## 🔴 6. 씬 시작 시 비활성화 상태 체크리스트

> [!CAUTION]
> 아래 오브젝트들은 **인스펙터에서 반드시 비활성화(체크 해제)** 상태로 두어야 합니다.
> 코드가 필요한 시점에 `SetActive(true)`로 켜줍니다. 깜빡하면 씬 시작 시 UI가 중첩되어 노출됩니다.

### ❌ 비활성화로 시작해야 하는 오브젝트

| 오브젝트 | 이유 | 활성화 시점 |
| :--- | :--- | :--- |
| **Event_Popup_Panel** | 이벤트가 없을 때 노출 금지 | `ExplorationEventProcessor`가 이벤트 발생 시 |
| **VN_Story_Panel** | 대화가 없을 때 노출 금지 | VN 스토리 시퀀스 시작 시 |
| **Clue_List_Panel** | 기본 닫힘 상태 | `Btn_Toggle_Clue_List` 클릭 시 |
| **Interact_Prompt_Panel** | 상호작용 가능 오브젝트 미감지 상태 | 플레이어가 인터랙션 오브젝트 접근 시 |
| **Result_Panel** | 탐사 진행 중에는 미노출 | 탐사 성공/실패 확정 시 |

### ✅ 활성화로 시작해야 하는 오브젝트

| 오브젝트 | 이유 |
| :--- | :--- |
| **HUD_Panel** | 탐사 중 항상 표시 |
| **Node_Icon_Container** | 씬 시작과 동시에 노드 아이콘 생성 대기 |
| **Btn_Toggle_Clue_List** | HUD와 함께 항상 접근 가능 |
| **Path_Line_Renderer** | LineRenderer는 오브젝트 활성화 후 `positionCount = 0`으로 초기화하여 숨김 처리 |

---

**최종 갱신**: 2026-04-04
**상태**: 패널 및 모든 자식 오브젝트의 배치를 포함한 완성판 구축 가이드
