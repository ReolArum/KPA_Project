# 🛠️ 유니티 에디터 설정 TODO 리스트 (Living Document)

이 문서는 AI가 코드를 수정했을 때, 사용자가 **유니티 에디터에서 직접 수동으로 설정해야 하는 작업**들을 기록하는 공간입니다. 프로젝트를 켰을 때 이 리스트의 최상단을 확인하여 설정을 완료해 주세요.

---

## 🕒 실시간 작업 현황 (최신순)

### [🚨 긴급] TextMesh Pro 관련 에러 조치 (NullReferenceException)
> [!CAUTION]
> 씬 구성 도중 TMP 관련 `NullReferenceException`이 발생한다면 다음을 수행하세요:
> 1.  **필수 리소스 임포트**: `Window > TextMeshPro > Import TMP Essential Resources` 실행.
> 2.  **폰트 할당**: 생성된 모든 `TMP_Text` 오브젝트의 `Font Asset` 필드에 기본 폰트(LiberationSans SDF 등)가 할당되어 있는지 확인.

### [2026-04-03] 신규 Input System 전환 후속 조치
> [!IMPORTANT]
> `ExplorationManager.cs`가 신규 Input System 방식으로 리팩토링되었습니다. 아래 설정을 완료해야 정상 작동합니다.

1.  **ExplorationManager 오브젝트 설정**:
    *   `ExplorationManager` 컴포넌트가 붙어 있는 오브젝트를 선택합니다.
    *   **[`Add Component`]** 버튼을 눌러 **`PlayerInput`** 컴포넌트를 추가합니다.
    *   **[`Actions`]** 필드에 프로젝트 루트에 있는 **`InputSystem_Actions.inputactions`** 에셋을 드래그해서 할당합니다.
    *   **[`Behavior`]** 설정을 기본값으로 두어도 코드가 직접 이벤트를 구독하므로 상관없으나, **`Actions` 에셋 연결**은 필수입니다.
    *   `ExplorationManager` 인스펙터 창의 **`PlayerInput`** 필드에 방금 추가한 컴포넌트를 자기 자신으로 할당해 줍니다.

2.  **레이어(Layer) 및 씬 로드 확인**:
    *   `ExplorationManager` 인스펙터의 **`Ground Layer`**와 **`Obstacle Layer`** 설정을 확인하세요.
    *   탐사 종료 후 메인 게임으로 돌아가는 씬 이름이 `Scene_MainGame`으로 코드상 수정되었습니다. 빌드 세팅에 이 씬이 포함되어 있는지 확인하세요.

3.  **UI 컨트롤러 추가 설정 (ExplorationUIController)**:
    *   **[`Text Predicted Time`]** 필드가 추가되었습니다. HUD에서 예상 시간을 보여줄 `TMP_Text` 오브젝트를 찾아 할당해 주세요.

4.  **데이터 에셋 최신 인스펙터 확인 (ExplorationStageData)**:
    *   기존에 생성된 `ExplorationStageData` 에셋들을 확인하세요.
    *   각 **Node** 데이터 하단에 **`Force Penalty Time`** (선택권 없을 때 차감될 시간) 필드가 추가되었습니다. 적절한 패널티 수치를 입력해 주세요.
    *   선택지(`ExplorationChoiceData`) 중 소모성 물건을 사용하는 경우, **`Consumed Object Id`** 필드에 소모될 아이템의 ID를 입력하세요. (비워둘 경우 영구 유지됩니다.)

---

## 📚 기존 가이드 내용 (통합됨)

### 🏗️ 스크립트 참조 복구 (Broken References)
- 폴더 구조 변경 등으로 인해 기존 오브젝트의 스크립트가 `Missing` 상태라면, `_Project/Scripts/` 내의 해당 스크립트를 다시 연결해 주세요.

### 🖥️ UI 컨트롤러 설정 (ExplorationUIController)
- `HUD`, `VN Panel`, `Clue List` 등 인스펙터에 노출된 UI 요소들이 계층 구조 상의 실제 오브젝트들과 연결되어 있는지 확인이 필요합니다.

### 🧠 3. 네비메시 및 레이어 설정 (NavMesh & Layers) - Unity 6 권장
- **NavMeshSurface 활용**:
    *   `Navigation (Obsolete)` 창 대신, 씬에 **`NavMeshSurface`** 컴포넌트를 추가하여 베이킹하세요.
    *   `Include Layers`에서 바닥과 장애물 레이어가 포함되었는지 확인 후 **[Bake]** 버튼을 누릅니다.
- **Layer 할당**:
    *   `ExplorationManager` 인스펙터에서 `groundLayer`를 바닥 레이어로, `obstacleLayer`를 벽/장애물 레이어로 설정합니다.
    *   실제 월드의 바닥과 벽 오브젝트에 해당 레이어를 각각 할당하세요.

--- 

**마지막 갱신 일자**: 2026-04-03
**상태**: 대기 중인 수동 작업 있음 (Input System)
