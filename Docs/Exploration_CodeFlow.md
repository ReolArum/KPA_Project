# 탐사 시스템 코드 플로우 분석

## 전체 흐름 다이어그램

```
씬 로드 → Start() → stageData 있으면 StartExploration() 자동 호출
                                │
                ┌───────────────┘
                ▼
    ┌─────────────────────┐
    │  StartExploration() │
    │  · 씬 마커 스캔      │
    │  · 시작 위치 세팅    │
    │  · Phase: Planning  │
    └────────┬────────────┘
             ▼
    ┌─────────────────────┐
    │  Planning Phase     │ ◄──────────────────────────┐
    │  · 좌클릭 드래그     │                             │
    │    = 경로 그리기     │                             │
    │  · 우클릭 = Undo    │                             │
    │  · ConfirmPath()    │                             │
    └────────┬────────────┘                             │
             ▼                                          │
    ┌─────────────────────┐                             │
    │  Moving Phase       │                             │
    │  · MovementRoutine  │                             │
    │  · 매 프레임:       │                             │
    │    - 위치 이동      │                             │
    │    - 시간 차감      │                             │
    │    - ScanNearbyNodes│                             │
    └──┬───────────┬──────┘                             │
       │           │                                    │
    [Clue]    [이벤트 노드]                              │
       │           │                                    │
       ▼           ▼                                    │
  자동 수집    ┌──────────────┐                          │
  (멈추지않음) │ EventProcess │                          │
              │ Phase        │                          │
              │ · 이동 일시정지│                          │
              │ · VN 재생     │                          │
              │ · 선택지 팝업 │                          │
              └──────┬───────┘                          │
                     ▼                                  │
            ┌────────────────┐                          │
            │ 선택지 클릭     │                          │
            │ · 효과 적용    │                          │
            │ · ResumeMovement│                          │
            └──┬──────────┬──┘                          │
               │          │                             │
      shouldRedraw=false  shouldRedraw=true              │
               │          │                             │
               ▼          └─────────────────────────────┘
         Moving으로 복귀
         (남은 경로 계속)
               │
               ▼
    ┌─────────────────────┐
    │ 경로 끝 도달         │
    │ → Planning으로 복귀  │
    └─────────────────────┘

    ── 종료 조건 ──
    · 시간 초과 → OnExplorationFailed()
    · Exit 노드 도달 → OnExplorationSucceeded() (수동 호출 필요)
    · 모두 → Phase: Result → ExitExploration() → Scene_MainGame 씬 로드
```

---

## 단계별 상세 코드 흐름

### 1단계: 씬 진입 + 초기화

```
ExplorationManager.Start()
  └─ stageData가 인스펙터에 할당되어 있으면
       └─ StartExploration(stageData) 자동 호출
```

**`StartExploration()` 내부:**
1. `ScanSceneMarkers()` → 씬의 `ExplorationStartMarker` / `ExplorationNodeMarker` 탐색 → 위치를 딕셔너리에 저장
2. 시작 위치 결정: 씬 마커 있으면 마커 위치, 없으면 `data.startPosition`
3. `currentState.Reset()` → 시간, 선택권, 골드, 위치 초기화
4. `playerTransform.position` = 시작 위치
5. `GameEvents.RaiseExplorationStarted()` → UI 컨트롤러가 HUD 표시
6. `SetPhase(Planning)` → 경로 그리기 모드

### 2단계: Planning (경로 그리기)

**입력 처리 (Input System):**
- **좌클릭 시작** → `StartDrawing()`: 마우스 Raycast → Ground 레이어 히트 → NavMesh 경로 계산 → `pathSegments`에 새 세그먼트 추가
- **좌클릭 드래그** → `UpdateDrawing()`: 점들을 `drawThreshold` 간격으로 세그먼트에 추가. 벽(Obstacle) 충돌 시 그리기 중단
- **우클릭** → `UndoLastSegment()`: 마지막 세그먼트 삭제

**필요 조건:**
- `Camera.main` 존재 필수 (Raycast용)
- 바닥에 `Ground Layer` + Collider 필수
- NavMeshSurface Bake 필수

### 3단계: ConfirmPath (경로 확정)

```
ConfirmPath()  ← UI의 확인 버튼에서 호출
  └─ pathSegments → plannedPath로 병합 (하나의 연속 경로)
  └─ SetPhase(Moving)
  └─ StartCoroutine(MovementRoutine())
```

> 이 메서드는 ExplorationUIController의 확인 버튼이 호출합니다.

### 4단계: Moving (자동 이동)

**`MovementRoutine()` 코루틴 — 매 프레임:**
1. `plannedPath[0]`(다음 경로점)을 향해 `MoveTowards` 이동
2. 시간 차감: `ConsumeTime(deltaTime * timeScale)`
3. `GameEvents.RaiseExplorationUpdated()` → UI 갱신
4. `playerTransform.position` 동기화
5. **`ScanNearbyNodes()`** → 핵심 스캔 로직 실행
6. 시간 초과 시 `OnExplorationFailed()` → 종료

**경로점 도달 시:**
- `plannedPath.RemoveAt(0)` → 다음 점으로
- 모든 점 소모 시 → `SetPhase(Planning)` (다시 경로 그리기로)

### 5단계: ScanNearbyNodes (범위 체크)

**매 프레임 이동 중에 실행:**

```
foreach (node in stageData.nodes)
  │
  ├─ triggeredNodeIds에 있으면 → 스킵 (이미 처리됨)
  │
  ├─ [Clue 타입] + clueRange 이내
  │    → foundObjectIds에 추가 (단서 수집)
  │    → triggeredNodeIds에 추가 (중복 방지)
  │    → 이동은 계속됨 (멈추지 않음)
  │
  └─ [그 외 타입] + interactionRange 이내
       → triggeredNodeIds에 추가
       → TriggerEvent(node) → 이동 일시정지(EventProcessing)
       → return (한번에 하나만 처리)
```

### 6단계: TriggerEvent (이벤트 처리)

```
TriggerEvent(node)
  └─ SetPhase(EventProcessing)  ← MovementRoutine이 WaitUntil로 대기함
  │
  ├─ [vnSequence가 있으면]
  │    └─ GameEvents.RaiseExplorationVNStarted()
  │         → ExplorationUIController가 VN 패널로 대화 재생
  │         → VN 완료 후 콜백 → ProcessEvent()
  │
  └─ [vnSequence 없으면]
       └─ ExplorationEventProcessor.ProcessEvent(node) 직행
```

### 7단계: ProcessEvent (선택지 처리)

```
ExplorationEventProcessor.ProcessEvent(node)
  │
  ├─ FilterChoices(node.choices) → 조건 미충족 선택지 제거
  │    └─ ownRequirements 체크:
  │         · StatAtLeast: 스탯 ≥ minValue?
  │         · HasItem: 인벤토리에 있는지? (미구현)
  │         · HasEnvObject: foundObjectIds에 있는지?
  │         · HasClue: foundObjectIds에 있는지?
  │
  ├─ [선택권 0 or 유효 선택지 0개]
  │    → 강제 패널티 적용 (forcePenaltyTime, forcePenaltyGold)
  │    → ResumeMovement(false) → 바로 이동 재개
  │
  └─ [선택지 있음]
       → GameEvents.RaiseExplorationEventTriggered(node, visibleChoices)
       → ExplorationUIController가 선택지 버튼 팝업 표시
```

### 8단계: 선택지 클릭 → 효과 적용

```
ExplorationEventProcessor.ApplyChoiceEffect(choice)
  │
  ├─ remainingChoices-- (선택 횟수 차감)
  ├─ remainingTime -= timePenalty (시간 차감)
  ├─ collectedGold += goldReward (골드 획득)
  ├─ rewardObjectId → foundObjectIds에 추가 (단서 획득)
  ├─ consumedObjectId → foundObjectIds에서 제거 (아이템 소모)
  │
  └─ ResumeMovement(shouldRedrawPath)
       ├─ false → SetPhase(Moving) → 남은 경로 계속 이동
       └─ true → ClearPath + SetPhase(Planning) → 경로 재설정
```

### 9단계: 종료

| 조건 | 메서드 | 동작 |
| :--- | :--- | :--- |
| 시간 초과 | `OnExplorationFailed()` | Phase: Result, 로그 출력 |
| Exit 노드에서 성공 처리 | `OnExplorationSucceeded()` | Phase: Result, 골드/단서 → GameState 반영, 저장 |
| 씬 복귀 | `ExitExploration()` | `Scene_MainGame` 씬 로드 |

---

## 의존성 정리 (테스트 시 필요한 것)

| 컴포넌트 | 역할 | 없으면? |
| :--- | :--- | :--- |
| `ExplorationManager` | 메인 로직 | ❌ 전혀 안 돌아감 |
| `ExplorationEventProcessor` | 선택지 처리 | ❌ 이벤트 노드 접근 시 NullRef |
| `ExplorationUIController` | 이벤트 구독 → UI 표시 | UI만 안 뜸. 로직은 돌아감 |
| `GameManager.Instance` | GameState 참조 (스탯 체크, 종료 처리) | ❌ 선택지 필터링 + 종료 시 NullRef |
| `Camera.main` | Raycast 기준 | ❌ 경로 그리기 불가 |
| `PlayerInput` | 입력 처리 | ❌ 조작 불가 (Input System 사용 시) |
| `NavMeshSurface` (Baked) | 경로 계산 | 직선 이동만 됨 |
| `Ground Layer 바닥` | 클릭 Raycast 대상 | ❌ 바닥 클릭 불가 |

---

## 주의사항

> `OnExplorationSucceeded()`와 `OnExplorationFailed()`는 **GameManager.Instance.State**에 직접 접근합니다.
> GameManager 없이 테스트하면 이 두 메서드에서 NullReferenceException이 발생합니다.
> 테스트 시 GameManager 오브젝트를 씬에 배치하거나, null 체크를 추가해야 합니다.
