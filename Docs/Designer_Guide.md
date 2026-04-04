# 🎨 기획자 작업 가이드 (Level Designer Guide)

이 문서는 **기획자(레벨 디자이너)가 유니티 에디터에서 직접 수행해야 할 작업**들을 정리한 문서입니다.
프로그래머가 코드를 추가/수정할 때마다 이 문서가 갱신됩니다.

> [!TIP]
> 이 문서의 체크리스트를 위에서 아래로 따라가며 작업하면 됩니다.

---

## 📌 최종 갱신: 2026-04-04

---

## 1. 탐사 맵 레벨 디자인 (씬 오브젝트 배치)

기획자가 씬에 오브젝트를 배치하고 **이름 규칙**만 지키면, 코드가 런타임에 자동으로 위치를 읽어갑니다.

### 1.1 오브젝트 이름 규칙

| 이름 접두사 | 역할 | 예시 |
| :--- | :--- | :--- |
| `START_` | 캐릭터 시작 위치 (**씬 당 1개**) | `START_SpawnPoint` |
| `CLUE_xxx` | 단서 아이템 (범위 진입 시 자동 수집) | `CLUE_OldLetter` |
| `TRAP_xxx` | 위험/함정 이벤트 노드 | `TRAP_PitFall` |
| `OBJ_xxx` | 상호작용 오브젝트 | `OBJ_OldSafe` |
| `EXIT_xxx` | 탈출구 | `EXIT_MainDoor` |
| `RW_xxx` | 보상 오브젝트 | `RW_GoldChest` |

### 1.2 배치 방법

1. 씬에 **빈 GameObject** 또는 **3D 모델**을 배치합니다.
2. 오브젝트 이름을 위 규칙에 맞게 짓습니다.
3. 3D 모델 임포트 시 마커 컴포넌트가 **자동 부착**됩니다.
4. 수동으로 배치한 빈 오브젝트에는 `Add Component`로 직접 추가:
   - 시작 지점: `ExplorationStartMarker`
   - 이벤트 노드: `ExplorationNodeMarker` → `nodeId` 입력

### 1.3 씬 뷰 확인 포인트

| 기즈모 색상 | 의미 |
| :--- | :--- |
| 🟢 초록 구체 + "START" | 시작 지점 |
| 🟣 마젠타 구체 | 단서(Clue) 노드 — 수집만, 이벤트 없음 |
| 🔴 빨간 구체 | 위험(Hazard) 노드 |
| 🟠 주황 구체 | 상호작용(Interactive) 노드 |
| 🟡 노란 구체 | 보상(Reward) 노드 |
| 🔵 시안 구체 | 탈출구(Exit) 노드 |

노드를 **클릭(선택)**하면:
- **파란 원** = 단서 자동 획득 범위 (clueRange)
- **노란 원** = 상호작용 프롬프트 범위 (interactionRange)
- 원의 크기를 **씬 뷰에서 드래그하여 직접 조절** 가능

---

## 2. ExplorationStageData 에셋 설정

`Assets > Create > KPA > Exploration > StageData`로 에셋을 만들고 아래 필드를 채웁니다.

### 2.1 기본 정보

| 필드 | 설명 | 예시 |
| :--- | :--- | :--- |
| `stageName` | 스테이지 이름 | `"1장: 폐허 지하"` |
| `limitTime` | 제한 시간 (초) | `120` |
| `maxChoices` | 최대 선택 횟수 (`-1` = 무제한) | `5` |
| `startPosition` | 시작 위치 (씬에 START_ 배치 시 무시됨) | `(0, 0, 0)` |

### 2.2 노드 데이터 (nodes 리스트)

> [!IMPORTANT]
> `nodeId`는 씬에 배치한 오브젝트 이름과 **정확히 동일**해야 합니다.
> 예: 씬에 `TRAP_PitFall` 오브젝트 → nodeId = `TRAP_PitFall`

각 노드에 채울 항목:

| 필드 | 설명 |
| :--- | :--- |
| `nodeId` | 씬 오브젝트 이름과 동일 |
| `nodeName` | UI에 표시될 이름 (예: "낡은 금고") |
| `eventType` | Hazard / Interactive / Reward / Exit |
| `clueRange` | 단서 자동 획득 범위 (m) |
| `interactionRange` | 상호작용 프롬프트 범위 (m) |
| `interactPrompt` | 프롬프트 텍스트 (예: "조사하기") |
| `forcePenaltyTime` | 선택권 없을 때 차감 시간 |
| `forcePenaltyGold` | 선택권 없을 때 차감 골드 |

### 2.3 VN 대화 시퀀스 (vnSequence)

각 스텝마다:

| 필드 | 설명 |
| :--- | :--- |
| `characterName` | 발화 캐릭터 이름 |
| `dialogueText` | 대사 본문 |
| `leftSprite` | 좌측 스탠딩 이미지 |
| `rightSprite` | 우측 스탠딩 이미지 |
| `backgroundOverride` | 배경 변경 시 지정 (선택) |

### 2.4 선택지 (choices)

| 필드 | 설명 |
| :--- | :--- |
| `label` | 버튼에 표시될 텍스트 |
| `type` | 선택지 유형 |
| `goldReward` | 획득 골드 |
| `timePenalty` | 차감 시간 (초) |
| `timeGain` | 획득 시간 (초) |
| `rewardObjectId` | 획득 단서/오브젝트 ID |
| `consumedObjectId` | 소모 오브젝트 ID (빈칸 = 소모 없음) |
| `shouldRedrawPath` | 경로 재작성 여부 |
| `ownRequirements` | 이 선택지 표시 조건 |

---

## 3. 환경 세팅 (필수)

| 작업 | 방법 |
| :--- | :--- |
| 바닥 레이어 | Floor 오브젝트 → Layer: `Ground`, Static ✅ |
| 벽 레이어 | Walls 오브젝트 → Layer: `Obstacle`, Static ✅ |
| NavMesh 베이크 | NavMeshSurface 선택 → `[Bake]` 클릭 |
| 경로 선 머티리얼 | Path_Line_Renderer → Material 할당 (없으면 경로 안 보임) |

---

## 4. 프리팹 제작 (3종)

아직 없다면 아래 프리팹을 만들어 `ExplorationUIController` 인스펙터에 연결합니다.

| 프리팹 | 구조 | 연결 필드 |
| :--- | :--- | :--- |
| Choice_Button_Prefab | Button > TMP_Text | `choiceButtonPrefab` |
| Node_Icon_Prefab | Image > TMP_Text | `nodeIconPrefab` |
| Clue_Item_Prefab | RectTransform > TMP_Text | `clueItemPrefab` |

---

## 5. 빌드 세팅

- `File > Build Settings`에서 **`Scene_MainGame`** 씬이 포함되어 있는지 확인.
- 누락 시 해당 씬 열고 `Add Open Scenes` 클릭.

---

## 🚀 6. 빠른 테스트 가이드 (탐사 씬 단독 실행)

GameManager·메인 씬 없이 **탐사 씬만 독립으로 테스트**하는 방법입니다.

### 6.1 최소 세팅 (5단계)

```
① ExplorationScene 열기
② 바닥·벽 배치 + 레이어·NavMesh
③ 시작 마커 배치
④ StageData 에셋 연결
⑤ Play
```

### 6.2 상세 절차

#### ① 씬 열기
- `_Project/Scenes/Exploration/ExplorationScene.unity`를 더블 클릭합니다.

#### ② 바닥/벽 + NavMesh

1. **바닥 Plane** 생성 (`3D Object > Plane`).
    - Scale: `(5, 1, 5)` 정도 (50x50m).
    - Layer: `Ground`, Static ✅.
2. **벽/장애물** 배치 (Cube 등).
    - Layer: `Obstacle`, Static ✅.
3. **NavMeshSurface** 오브젝트가 없다면 빈 GameObject에 `Add Component > NavMeshSurface`.
4. `[Bake]` 클릭 → 파란색 오버레이 확인.

#### ③ 시작 마커 배치

1. 빈 GameObject 생성, 이름: `START_Test`.
2. `Add Component > ExplorationStartMarker`.
3. 바닥 위 원하는 위치에 배치.
4. 씬 뷰에서 **초록 구체**가 보이면 성공.

#### ④ StageData 연결

1. `_Project/ScriptableObjects/Exploration/SampleExplorationStage` 에셋을 선택하여 내용 확인.
2. Hierarchy에서 `ExplorationManager` 오브젝트 선택.
3. 인스펙터의 `Stage Data` 필드에 위 에셋을 **드래그**.
4. 함께 확인할 인스펙터 필드:

| 필드 | 할당 대상 |
| :--- | :--- |
| `Stage Data` | SampleExplorationStage 에셋 |
| `Ground Layer` | `Ground` 체크 |
| `Obstacle Layer` | `Obstacle` 체크 |
| `Player Transform` | 씬 내 캐릭터 모델 (없으면 빈 Cube라도 배치) |
| `Player Input` | 자기 자신 (ExplorationManager에 PlayerInput 컴포넌트 추가 후) |

> [!NOTE]
> `Player Transform`이 없으면 캐릭터 이동이 보이지 않을 뿐 로직은 돌아갑니다.
> 간단 테스트 시 Cube 하나를 만들어 할당해도 됩니다.

#### ⑤ Play

- Play 버튼 클릭.
- Console에 `[MarkerScan] 시작 마커 감지: (x, y, z)` 로그가 뜨면 마커 인식 성공.
- Console에 `탐사 시작: (스테이지 이름)` 로그가 뜨면 시스템 가동 성공.

### 6.3 테스트 시 확인 사항

| 확인 항목 | 정상 동작 |
| :--- | :--- |
| Console `[MarkerScan]` 로그 | 마커 개수 + 위치 출력 |
| HUD 표시 | 남은 시간, 선택권, 골드 표시 |
| 바닥 클릭 드래그 | 경로 선이 그려짐 |
| 경로 확정 버튼 [V] | 캐릭터가 경로를 따라 이동 |
| 노드 접근 | 이벤트 팝업 or 상호작용 프롬프트 |

### 6.4 자주 하는 실수

| 증상 | 원인 | 해결 |
| :--- | :--- | :--- |
| 클릭해도 경로가 안 그려짐 | `Ground Layer` 미설정 또는 바닥에 Collider 없음 | 바닥 Layer = `Ground`, MeshCollider 확인 |
| 경로 선이 안 보임 | Path_Line_Renderer에 Material 미할당 | Material 할당 |
| Play 시 아무 반응 없음 | `Stage Data`가 None | 에셋 할당 |
| `NullReferenceException` 다수 | UI 필드 미연결 | ExplorationUIController 인스펙터 필드 채우기 |
| 캐릭터가 안 움직임 | `Player Transform` 미할당 | 아무 오브젝트라도 할당 |
| NavMesh 경로 실패 | Bake 안 함 | NavMeshSurface `[Bake]` 클릭 |

---

## 📝 변경 이력

| 날짜 | 내용 |
| :--- | :--- |
| 2026-04-04 | 초판 작성. 맵 마커 시스템, StageData 설정, 환경/프리팹/빌드 세팅 포함 |
| 2026-04-04 | 빠른 테스트 가이드 (섹션 6) 추가 |

