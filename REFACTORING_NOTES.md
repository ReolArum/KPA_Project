# KPA_Project 리팩토링 및 탐사 콘텐츠 프로토타입 보고서

아키텍처 확장성을 위한 이벤트 시스템 도입과 새로운 '탐사' 콘텐츠의 프로토타입 구현을 완료했습니다.

## 🛠️ 주요 변경 사항

### 1. 이벤트 기반 아키텍처 도입 (확장성 핵심)
- **[NEW] Assets/_Project/Scripts/GameEvents.cs**: 중앙 이벤트 버스를 생성했습니다.
- **[MODIFY] Assets/_Project/Scripts/GameManager.cs**: 이제 UI를 직접 참조하지 않고 이벤트를 발생(`Raise`)시킵니다.
- **[MODIFY] Assets/_Project/Scripts/UIController.cs**: GameManager의 지시를 기다리는 대신 이벤트를 구독(`Subscribe`)하여 스스로 갱신합니다.
  - **효과**: 앞으로 사운드, 이펙트, 업적 시스템 등을 추가할 때 기존 코드를 수정하지 않고 이벤트만 구독하면 됩니다.

### 2. '탐사(Exploration)' 콘텐츠 프로토타입 구현
기획서의 핵심 메커니즘을 검증하기 위한 기초 시스템을 별도 씬으로 구축했습니다.
- **주요 기능**: 경로 드래그 계획, 자동 이동(시간 소모), 위험 조우 및 동적 선택지 팝업.
- **자동화 워크플로우**: Blender에서 임포트 시 `TRAP_`, `OBJ_` 등 네이밍 규칙에 따라 스크립트가 자동 부착됩니다. ([ExplorationMapAutoSetup.cs](Assets/_Project/Scripts/Editor/ExplorationMapAutoSetup.cs))
- **유연한 설계**: 선택 횟수 시스템을 유지하되, 나중에 쉽게 끌 수 있도록 구성했습니다.

### 3. 주요 버그 수정 (안정성)
- **씬 전환 오류 수정**: `MainScene`과 `Main`으로 갈리던 이름을 `Main`으로 통일하여 전투 후 복귀가 안 되던 문제를 해결했습니다.
- **퀘스트 저장 기능 추가**: [SaveSystem.cs](Assets/_Project/Scripts/SaveSystem.cs)를 수정하여 게임 재시작 시 퀘스트가 사라지던 버그를 고쳤습니다.

---

## 📋 유저 체크리스트 (컴퓨터 사용 가능 시)

코드 수정을 모두 마쳤으므로, 나중에 Unity 에디터를 여셨을 때 다음 사항만 확인해 주세요:

1. **컴파일 확인**: Unity 에디터 하단에 에러(빨간색 로그)가 없는지 확인하세요.
2. **새로운 파일 확인**:
   - `ExplorationManager.cs`, `ExplorationUIController.cs`, `ExplorationEventProcessor.cs` 등이 추가되었습니다.
3. **탐사 프로토타입 테스트**:
   - 빌드 설정에 `ExplorationScene` (별도 생성 필요)을 추가하거나 씬을 직접 열어 테스트하세요.
   - 밤 행동에서 '탐사'를 선택하면 해당 씬으로 전환됩니다.
4. **게임 루프 테스트**:
   - 내부 로직은 동일하므로 기존과 똑같이 동작해야 합니다.

---

## 💡 향후 확장 팁 (사운드 추가 예시)
이제 이벤트 시스템이 갖춰졌으므로, 새로운 기능을 추가하기 매우 쉽집니다.
예를 들어 **"골드를 얻을 때 소리를 내고 싶다"**면, 사운드 매니저에서 다음과 같이 작성하면 됩니다.

```csharp
void OnEnable() {
    GameEvents.OnGoldChanged += PlayGoldSound;
}
```

수고하셨습니다!
