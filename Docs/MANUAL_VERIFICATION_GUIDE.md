# KPA_Project 통합 수동 검증 가이드 (v1.0)

본 문서는 지금까지 진행된 모든 기능 구현 및 리팩토링 내역을 사용자가 직접 Unity 에디터에서 검증할 수 있도록 작성된 단계별 가이드입니다.

---

## 🛠️ Step 1: 프로젝트 준비 (Git & Unity)

### 1. 최신 코드 반영
- [ ] 터미널에서 `git pull origin main`을 실행하여 모든 최신 변경 사항(Scripts, Docs)을 내려받습니다.

### 2. Unity 컴파일 및 씬 체크
- [ ] Unity 에디터를 열고 **Console 탭**에 빨간색 에러 로그가 없는지 확인합니다.
- [ ] `File > Build Settings` 씬 목록에 다음이 포함되어 있는지 확인합니다:
  - `Main` (메인 게임 씬)
  - `ExplorationScene` (수동 생성 필요)

---

## 🧭 Step 2: 탐사(Exploration) 시스템 검증

### 1. 씬 구성 및 매니저 배치
- [ ] 새 씬을 만들고 `ExplorationScene`으로 저장합니다.
- [ ] 하이어라키에 `ExplorationManager`, `ExplorationUIController`, `ExplorationEventProcessor`를 각각 오브젝트로 배치합니다.
- [ ] `ExplorationUIController`의 인스펙터 슬롯에 Canvas 내의 HUD 텍스트와 버튼들을 드래그하여 연결합니다.

### 2. 자동 임포트 워크플로우 테스트
- [ ] Blender에서 가단한 큐브를 모델링한 후 이름을 `TRAP_Test`, `OBJ_Reward` 등으로 짓고 FBX로 내보냅니다.
- [ ] Unity의 `Assets/_Project/Models` 폴더로 해당 FBX를 임포트했을 때, 인스펙터에 자동으로 `MeshCollider`와 `HazardNode` 등이 붙어있는지 확인합니다.

---

## 🏙️ Step 3: 낮 행동(Daytime) 및 UI 리팩토링 검증

### 1. 장소 테마 시스템 설정
- [ ] 프로젝트 뷰에서 `Create > KPA > UI > LocationThemeData`를 클릭하여 5개 장소용 에셋을 만듭니다.
  - `Home`, `Shop`, `TrainingGround`, `Cafe`, `QuestBoard`
- [ ] 각 테마 에셋의 `Theme Color`와 `Location Name`을 설정합니다.
- [ ] 메인 씬의 `UIController` 산하 `PlaceActionUIController` 인스펙터의 `Themes` 리스트에 위 에셋들을 할당합니다.

### 2. 낮 행동 루프 테스트 (가장 중요)
- [ ] **이동**: 지도에서 장소 클릭 시 행동권이 1 차감되고 해당 장소 패널이 뜨는지 확인합니다.
- [ ] **테마**: 장소마다 배경색과 버튼 구성(대화, 업그레이드, 리롤 등)이 다르게 노출되는지 확인합니다.
- [ ] **복귀 & 밤 전환**: 행동권이 0인 상태에서 '지도 복귀' 버튼을 눌렀을 때, 지도로 가지 않고 즉시 **밤 선택(Night Choice) UI**로 전환되는지 확인합니다.

### 3. 장소별 고유 기능 체크
- [ ] **훈련장**: '시설 업그레이드' 클릭 시 골드가 소모되고 훈련 효율이 올라가는지(로그 확인) 테스트합니다.
- [ ] **카페**: '특수 음식' 구매 시 훈련 효율 버프가 활성화되는지 확인합니다.
- [ ] **게시판**: '리롤' 클릭 시 오늘의 의뢰가 새로 갱신되는지 확인합니다.

---

## 🐞 Step 4: 기존 버그 수정 사항 확인

1. **전투 후 복귀**: 아레나 전투 종료 후 다시 메인 화면으로 에러 없이 돌아오는지 확인합니다.
2. **퀘스트 저장**: 의뢰를 수락한 후 게임을 껐다 켰을 때(Save/Load), 수락한 의뢰가 `activeQuests` 목록에 그대로 남아있는지 확인합니다.

---

모든 과정을 마친 후 이슈가 발생한다면 언제든 리포트해 주세요!
