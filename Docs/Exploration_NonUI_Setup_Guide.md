# 🎮 탐사 씬 비-UI 세팅 가이드 (Non-UI Setup Guide)

UI 구성이 완료된 이후, 탐사 씬을 완전히 작동시키기 위해 필요한 **나머지 세팅 항목들**을 정리한 문서입니다.
UI 관련 세팅은 [`Exploration_Scene_Setup_Guide.md`](./Exploration_Scene_Setup_Guide.md)를 참조하세요.

---

## ✅ 세팅 진행 체크리스트

- [ ] 1. 카메라 세팅
- [ ] 2. 매니저 세팅
- [ ] 3. 환경(Environment) 세팅
- [ ] 4. 비주얼(Visuals) 세팅
- [ ] 5. 프리팹 제작 (3종)
- [ ] 6. 데이터 에셋 설정
- [ ] 7. 빌드 세팅 확인

---

## 📷 1. 카메라 세팅 (`[Cameras]`)

씬에는 두 개의 카메라가 필요합니다. `ExplorationUIController`의 `camTop`, `camQuarter` 필드에 각각 연결합니다.

### 1.1 Main Camera (Top View — Planning 페이즈용)

| 항목 | 값 |
| :--- | :--- |
| **Tag** | `MainCamera` |
| **Clear Flags** | Skybox 또는 Solid Color |
| **Projection** | Orthographic 권장 (탑뷰 맵 표시용) |
| **Position** | 맵 중앙 정 위 (예: `0, 20, 0`) |
| **Rotation** | `X: 90, Y: 0, Z: 0` (정사영) |
| **AudioListener** | ✅ 이 카메라에 붙어 있어야 함 |

### 1.2 Quarter Camera (45도 Follow View — Moving 페이즈용)

| 항목 | 값 |
| :--- | :--- |
| **Projection** | Perspective |
| **Position** | 플레이어 뒤 대각선 위 (예: `0, 10, -8`) |
| **Rotation** | `X: 45, Y: 0, Z: 0` |
| **Follow Target** | `ExplorationManager`의 `playerTransform`을 기준으로 코드에서 따라감 |
| **AudioListener** | ❌ Main Camera에만 있어야 함 |

> [!NOTE]
> 두 카메라 모두 **평소에는 둘 다 활성화** 상태로 두고, `ExplorationUIController`가 페이즈에 따라 전환합니다.

---

## 🧠 2. 매니저 세팅 (`[Managers]`)

### 2.1 ExplorationManager

`ExplorationManager` 오브젝트를 선택하고 인스펙터에서 아래 필드를 채웁니다.

| 필드 | 할당 대상 | 비고 |
| :--- | :--- | :--- |
| `moveSpeed` | `5` | 기본 이동 속도 |
| `timeScale` | `1` | 기본 시간 배율 |
| `playerInput` | 자기 자신 (`PlayerInput` 컴포넌트) | `Add Component > PlayerInput` 추가 후 연결 |
| `groundLayer` | `Ground` 레이어 | 바닥 레이어 체크 |
| `obstacleLayer` | `Obstacle` 레이어 | 벽/장애물 레이어 체크 |
| `playerTransform` | 씬 내 3D 캐릭터 모델 오브젝트 | 실제 움직이는 모델의 Transform |

#### PlayerInput 설정

1. `ExplorationManager` 오브젝트에 `Add Component > Player Input` 추가.
2. `Actions` 필드에 **`InputSystem_Actions.inputactions`** 에셋 할당.
3. `Behavior`는 기본값 유지 (코드가 직접 이벤트를 구독하므로).

### 2.2 ExplorationEventProcessor

- 싱글톤 패턴으로 동작합니다.
- 씬에 **1개만** 배치하면 됩니다. 추가 설정 불필요.

---

## 🌍 3. 환경(Environment) 세팅 (`[Environment]`)

### 3.1 Floor (바닥)

| 항목 | 값 |
| :--- | :--- |
| **컴포넌트** | `MeshRenderer`, `MeshCollider` |
| **Layer** | `Ground` |
| **Static** | ✅ Static 체크 (NavMesh 베이킹 대상) |

### 3.2 Walls (벽/장애물)

| 항목 | 값 |
| :--- | :--- |
| **컴포넌트** | `MeshRenderer`, `MeshCollider` |
| **Layer** | `Obstacle` |
| **Static** | ✅ Static 체크 |

### 3.3 NavMeshSurface (경로 탐색용)

> [!IMPORTANT]
> NavMesh 베이킹은 Floor, Walls의 Static 체크 및 Layer 설정을 **먼저 완료한 뒤** 진행해야 합니다.

1. `NavMeshSurface` 컴포넌트가 붙은 오브젝트를 선택합니다.
2. `Include Layers`에서 **`Ground`** 레이어가 포함되어 있는지 확인합니다.
3. **`[Bake]`** 버튼을 클릭합니다.
4. 씬 뷰에 파란색 NavMesh 오버레이가 나타나면 성공입니다.

---

## ✏️ 4. 비주얼(Visuals) 세팅 (`[Visuals]`)

### Path_Line_Renderer (경로 미리보기 선)

| 항목 | 값 |
| :--- | :--- |
| **컴포넌트** | `LineRenderer` |
| **Width** | `0.1` (Start/End 모두) |
| **Material** | ✅ **반드시** 할당 필요 (없으면 경로가 보이지 않음) |
| **Color** | 권장: 반투명 흰색 또는 하늘색 |
| **Use World Space** | ✅ 체크 |

> [!CAUTION]
> Material이 할당되지 않으면 경로 선이 렌더링되지 않아 플레이어가 경로를 확인할 수 없습니다.

---

## 📦 5. 프리팹 제작 (3종)

아래 3개의 프리팹을 `_Project/Prefabs/` 폴더 등에 생성하고, `ExplorationUIController` 인스펙터의 각 Prefab 필드에 연결합니다.

### 5.1 Choice_Button_Prefab

- **Root**: `Button` (Button 컴포넌트)
- **Child**: `TMP_Text` (선택지 텍스트)
- **연결 필드**: `choiceButtonPrefab`
- **권장 Height**: `60`
- **TMP 정렬**: 중앙

### 5.2 Node_Icon_Prefab

- **Root**: `Image` (원형 아이콘 등, W/H: `40 x 40`)
- **Child**: `TMP_Text` (노드 이름, 폰트 Size: `14`)
- **연결 필드**: `nodeIconPrefab`
- **Raycast Target**: ✅ 클릭 가능하도록 체크

### 5.3 Clue_Item_Prefab

- **Root**: `RectTransform` (H: `50`)
- **Child**: `TMP_Text` (단서 이름 표시, Left 정렬)
- **연결 필드**: `clueItemPrefab`

---

## 📊 6. 데이터 에셋 설정 (`ExplorationStageData`)

`Assets > Create > KPA > Exploration > StageData` 메뉴로 에셋을 생성합니다.

### 6.1 ExplorationStageData (ScriptableObject 루트)

| 필드 | 타입 | 설명 | 예시 |
| :--- | :--- | :--- | :--- |
| `stageName` | string | 탐사 스테이지 이름 | `"1장: 폐허 지하"` |
| `limitTime` | float | 탐사 제한 시간 (초) | `120` (기본 2분) |
| `maxChoices` | int | 최대 선택 가능 횟수. `-1`이면 무제한 | `5` |
| `startPosition` | Vector3 | 캐릭터 탐사 시작 위치 | `(0, 0, 0)` |
| `blueprintSprite` | Sprite | HUD에 표시될 지도 배경(청사진) | 지도 이미지 |
| `mapPrefab` | GameObject | 실제 3D 맵 프리팹 (선택사항) | |
| `nodes` | List | 이벤트 노드 목록 (아래 참조) | |

### 6.2 ExplorationNodeData (nodes 리스트 각 항목)

| 필드 | 타입 | 설명 | 예시 |
| :--- | :--- | :--- | :--- |
| `nodeId` | string | 노드 고유 ID | `"node_01"` |
| `nodeName` | string | 아이콘/UI에 표시될 이름 | `"낡은 금고"` |
| `worldPosition` | Vector3 | 씬 내 3D 위치 | `(3, 0, 5)` |
| `eventType` | enum | 이벤트 종류 (`ExplorationEventType`) | |
| `clueRange` | float | 자동 단서 획득 범위 (m) | `2.0` |
| `interactionRange` | float | 상호작용 프롬프트 표시 범위 (m) | `1.5` |
| `interactPrompt` | string | 프롬프트 안내 텍스트 | `"조사하기"` |
| `vnSequence` | List | VN 대화 시퀀스 (아래 참조) | |
| `requirements` | List | 이 노드 진입 조건 (아래 참조) | |
| `choices` | List | 선택지 목록 (아래 참조) | |
| `forcePenaltyTime` | float | 선택권 없을 때 강제 차감 시간(초) | `10` |
| `forcePenaltyGold` | int | 선택권 없을 때 강제 차감 골드 | `0` |

### 6.3 VNDialogueStep (vnSequence 리스트 각 항목)

| 필드 | 타입 | 설명 |
| :--- | :--- | :--- |
| `characterName` | string | 발화 캐릭터 이름 |
| `dialogueText` | string | 대사 내용 |
| `leftSprite` | Sprite | 좌측 스탠딩 일러스트 |
| `rightSprite` | Sprite | 우측 스탠딩 일러스트 |
| `backgroundOverride` | Sprite | 이 스텝에서 배경을 변경할 경우 지정 |

### 6.4 ExplorationRequirement (requirements / ownRequirements 각 항목)

| 필드 | 타입 | 설명 |
| :--- | :--- | :--- |
| `type` | enum | `None`, `StatAtLeast`, `HasItem`, `HasEnvObject`, `HasClue` |
| `statType` | TrainingStat | `StatAtLeast` 타입일 때 사용하는 스탯 종류 |
| `minValue` | int | 최소 요구 수치 |
| `targetId` | string | Item/EnvObject/Clue 타입일 때의 ID 또는 이름 |

### 6.5 ExplorationChoiceData (choices 리스트 각 항목)

| 필드 | 타입 | 설명 |
| :--- | :--- | :--- |
| `label` | string | 선택지 버튼에 표시될 텍스트 |
| `type` | enum | `ExplorationChoiceType` |
| `goldReward` | int | 이 선택 시 획득 골드 |
| `timePenalty` | float | 이 선택 시 차감 시간(초) |
| `timeGain` | float | 이 선택 시 획득 시간(초) |
| `rewardObjectId` | string | 획득할 단서/오브젝트 ID |
| `consumedObjectId` | string | 이 선택 시 소모될 보관 오브젝트 ID (비우면 소모 없음) |
| `shouldRedrawPath` | bool | 선택 후 경로를 다시 그려야 하면 `true` |
| `ownRequirements` | List | 이 선택지가 **표시되기 위한** 조건 목록 |

> [!NOTE]
> `ownRequirements`가 있으면 조건 미충족 시 해당 선택지 자체가 버튼에 노출되지 않습니다.

---

## 🏗️ 7. 빌드 세팅 확인

1. `File > Build Settings` 메뉴를 엽니다.
2. **`Scene_MainGame`** 씬이 목록에 포함되어 있는지 확인합니다.
3. 누락되어 있다면 해당 씬을 열고 **`Add Open Scenes`** 버튼으로 추가합니다.

---

**최종 갱신**: 2026-04-04
**상태**: UI 세팅 완료 후 진행할 나머지 항목 정리
