# [KPA_Project] 유니티 에디터 수동 작업 가이드 (Editor Setup Guide)

스크립트 리팩토링 및 시스템 구축이 완료됨에 따라, 실제 유니티 에디터(Unity Editor)에서 사용자가 직접 수행해야 하는 필수 작업 목록을 정리했습니다.

---

## 🏗️ 1. 스크립트 참조 복구 (Broken References)
- **현상**: 폴더 구조 변경으로 인해 기존 게임 오브젝트에 붙어있던 스크립트가 `Missing (Mono Script)` 상태일 수 있습니다.
- **작업**: 
    - `ExplorationManager`, `ExplorationUIController`, `GameManager` 등이 붙은 오브젝트를 확인합니다.
    - `_Project/Scripts/Systems/Exploration/` 폴더 내의 최신 스케쥴에 맞춰 스크립트를 다시 드래그 앤 드롭하여 연결하세요.

---

## 🖥️ 2. UI 컨트롤러 설정 (ExplorationUIController)
- **HUD 연동**: `HUD` 섹션의 `Text Time`, `Text Gold`, `Btn Confirm Path` 등을 UI 계층 구조에서 찾아 할당합니다.
- **비주얼 노벨(VN) 패널**: 
    - `panelVN`: 컷씬 전체 부모 패널.
    - `textVNName`, `textVNDialogue`: 이름 및 대화 텍스트.
    - `imgVNLeft`, `imgVNRight`, `imgVNBackground`: 일러스트 및 배경 이미지.
    - `btnVNDialogueBox`: 대화창 자체에 붙어 있는 (혹은 텍스트 영역을 덮는) 투명한 `Button` 컴포넌트.
- **단서 목록**: `panelClueList`와 `clueItemPrefab`을 할당합니다.

---

## 🧠 3. 네비메시 및 레이어 설정 (NavMesh & Layers)
- **NavMesh Bake**: 캐릭터의 지능형 이동을 위해 바닥(Ground) 오브젝트를 **Static**으로 설정하고 네비메시를 **Bake** 하세요. (`Window > AI > Navigation`)
- **Layer 할당**:
    - `ExplorationManager` 인스펙터에서 `groundLayer`를 바닥 레이어로, `obstacleLayer`를 벽/장애물 레이어로 설정합니다.
    - 실제 월드의 바닥과 벽 오브젝트에 해당 레이어를 각각 할당하세요.

---

## 💾 4. 데이터 에셋 생성 (ExplorationStageData)
- **작업**: 프로젝트 창에서 우클릭 -> `Create > KPA > Exploration Stage Data`를 선택하여 에셋을 생성합니다.
- **입력**: 
    - `Stage Name`, `Limit Time` 등을 설정합니다.
    - `Nodes` 리스트를 만들고, 월드 좌표(`World Position`)를 실제 3D 프로젝트 씬의 좌표와 맞춥니다.
    - `VN Sequence`에 테스트용 대화 데이터를 입력하세요.

---

## 🧩 5. 3D 오브젝트 및 태그 연동
- **오브젝트 배치**: 단서나 상호작용 지점에 실제 가시적인 프롭(Prop)이나 빈 오브젝트를 배치합니다.
- **좌표 동기화**: `ExplorationStageData`에 입력한 `node.worldPosition`이 실제 씬의 오브젝트 위치와 일치하는지 확인하세요 (이 위치를 기준으로 캐릭터가 멈추고 감지합니다).

---

**작성 일자**: 2026-03-31
**상태**: 유저 수동 작업 필요
