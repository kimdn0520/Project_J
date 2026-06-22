# 👻 공포 쯔꾸르 게임 다이얼로그 시스템 가이드

공포 쯔꾸르 게임의 몰입도와 연출(갑툭튀, 카메라 흔들림 등)을 극대화하고, 재사용성과 유지보수성이 뛰어난 다이얼로그 시스템의 설계 및 구현 가이드입니다.

---

## 1. 핵심 질문에 대한 답변

### Q1. JSON 데이터를 매번 역직렬화할 것인가 vs 시작 시 메모리에 적재할 것인가?
> [!IMPORTANT]
> **결론: 게임 또는 챕터 시작 시 한 번에 역직렬화하여 메모리(Dictionary)에 적재하는 방식이 정답입니다.**

- **성능 및 연출 최적화**: 텍스트 데이터는 소설책 한 권 분량이어도 수 MB에 불과합니다. 게임 도중 갑툭튀(Jumpscare) 연출이나 긴박한 대사창이 뜰 때마다 디스크 I/O를 거쳐 역직렬화하면 미세한 프레임 드랍(Stuttering)이 생겨 공포 연출의 타이밍을 망칠 수 있습니다.
- **접근 속도**: `Dictionary<string, DialogueNode>` 형태로 메모리에 적재해두면 $O(1)$ 속도로 즉각 데이터를 조회할 수 있어 연출 연계가 매끄럽습니다.
- **비대화 시 로드 전략**: 스크립트 양이 아주 방대해진다면, 전체를 하나로 관리하지 말고 챕터/구역별로 JSON 파일을 쪼갠 뒤(`chapter1_dialog.json`, `chapter2_dialog.json` 등), 챕터 로딩 화면에서 메모리에 교체 적재하는 방식을 추천합니다.

---

### Q2. 다이얼로그 기획 시 엑셀(구글 시트) vs 유니티 노드 에디터?
> [!TIP]
> **추천: 1인 또는 소규모 개발팀이라면 구글 시트(엑셀) -> JSON 변환 방식이 압도적으로 유리합니다.**

| 방식 | 장점 | 단점 | 추천 상황 |
| :--- | :--- | :--- | :--- |
| **구글 시트 / 엑셀** | - 타이핑 속도가 빠르고 오타 찾기가 쉬움.<br>- 복사/붙여넣기, 일괄 찾기/바꾸기 용이.<br>- 번역(다국어) 작업 시 컬럼 추가로 간단히 해결.<br>- 개발 시간이 거의 들어가지 않음. | - 복잡한 분기 흐름을 시각적으로 한눈에 파악하기 다소 어려움.<br>- `NextNodeID`를 직접 손으로 입력해야 하므로 오타 위험. | - 대부분의 인디 게임<br>- 대사가 많고 분기가 직렬/중간 분기 위주인 경우 |
| **유니티 노드 에디터** | - 대화의 시각적 흐름(플로우 차트)을 직관적으로 확인 가능. | - 유니티 내에서 직접 에디터 툴을 개발하는 데 상당한 시간(수주~수개월) 소요.<br>- 노드 안의 좁은 텍스트 창에서 대사 편집하기 답답함.<br>- 다국어 번역 지원 시 추가 툴링 필수. | - 시나리오 분기가 엄청나게 복잡한 경우 (디트로이트 비컴 휴먼 스타일)<br>- 유료 에셋을 구입하는 경우 |

**현실적인 추천 전략:**
구글 스프레드시트를 사용해 다이얼로그를 정리하고, 해당 시트를 JSON 형태로 익스포트하는 간단한 스크립트나 외부 웹 툴을 이용하는 것이 개발 기간을 획기적으로 단축하는 방법입니다.

---

## 2. 시스템 아키텍처 및 구현 파일 목록

우리가 작성한 C# 코드 파일은 다음과 같으며, 상호 참조를 최소화(Decoupling)하여 유연하게 설계되었습니다.

1. [IInteractable.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/IInteractable.cs): 상호작용 인터페이스
2. [DialogueModel.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueModel.cs): 다이얼로그 데이터 구조(노드, 선택지) 정의
3. [DialogueDatabase.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueDatabase.cs): JSON 데이터를 파싱하고 `Dictionary`로 캐싱
4. [DialogueManager.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueManager.cs): 싱글톤 매니저 (UniTask 기반 진행 흐름 제어, 조건 체크)
5. [DialogueUI.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueUI.cs): UI 텍스트 출력, 타이핑 효과 및 선택지 버튼 생성
6. [InteractionTrigger.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/InteractionTrigger.cs): 오브젝트/NPC 상호작용 트리거 (아이템/플래그 조건 분기 지원)
7. [AreaNarrativeTrigger.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/AreaNarrativeTrigger.cs): 밟았을 때 재생되는 나레이션 트리거 (2D/3D 트리거 콜라이더 연계)
8. [DialogueEventDispatcher.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueEventDispatcher.cs): 대화 중 연출/이벤트를 쏘아주는 옵저버 디스패처
9. [DialogueSystemDemo.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueSystemDemo.cs): 외부 시스템(인벤토리, 퀘스트, 효과음 연출)과의 연동 예제 스크립트

---

## 3. 핵심 설계 포인트

### 1) 느슨한 결합 (Decoupling) - 인벤토리 & 퀘스트 매니저 연동
`DialogueManager`가 인벤토리 시스템이나 퀘스트 매니저를 직접 참조(`ref` 또는 `Instance`)하게 만들면, 해당 시스템이 바뀔 때 다이얼로그 코드까지 깨집니다. 
이를 해결하기 위해 **C# Delegate(대리자)**를 활용했습니다.

```csharp
// DialogueManager.cs 내부
public Func<string, bool> OnCheckFlag;  // 외부 플래그 매니저에 조건 검사 위임
public Func<string, bool> OnCheckItem;  // 외부 인벤토리에 아이템 소지 여부 검사 위임
public Action<string, bool> OnSetFlag;  // 선택지 선택 시 외부 플래그 변경 위임
```

실제 게임 내 매니저(예: `QuestManager`, `InventoryManager`)에서 시작할 때 이 함수 연결만 해주면 다이얼로그 시스템은 완벽히 독립적으로 동작합니다.

### 2) 이벤트 기반 연출 (DialogueEventDispatcher)
대화가 도중에 끊기거나 특정 대사가 나왔을 때 전등이 깜빡이거나 카메라가 흔들리는 연출을 처리하기 위해 이벤트 디스패처를 만들었습니다.
- **JSON 작성**: 다이얼로그 노드나 선택지에 `"triggerEvent": "light_flicker"` 또는 `"triggerEvent": "camera_shake"`를 기입합니다.
- **코드 연결**: 연출을 담당하는 스크립트에서 다음과 같이 이벤트를 수신합니다.
```csharp
void OnEnable() {
    DialogueEventDispatcher.Register("camera_shake", ShakeCamera);
}
void ShakeCamera() {
    // 실제 카메라 흔들림 스크립트 호출
}
```

### 3) 텍스트 타이핑 연출 (Rich Text 보호)
공포 게임에서는 붉은색 텍스트(`<color=red>대사</color>`)를 많이 사용합니다. 일반적인 글자 출력 방식을 사용하면 `<`,`c`,`o` 순서대로 타이핑되어 태그 문자가 노출됩니다.
이번에 구현한 `DialogueUI.cs`는 `<` 문자를 감지하면 `>` 문자까지 한 번에 스킵하여 리치 텍스트 서식이 타이핑 도중 깨지는 현상을 방지합니다.

---

## 4. 실제 적용 및 연동 예시

### 1. 씬 구성하기
1. 씬 내에 빈 오브젝트를 만들고 `DialogueManager` 컴포넌트를 추가합니다.
2. 대화창 UI(Panel, Speaker Text, Dialogue Text, Next Arrow, Choice Panel, Choice Button Prefab)를 생성한 뒤 `DialogueUI` 컴포넌트를 붙이고 `DialogueManager`에 연결합니다.
3. [sample_dialogue.json](file:///C:/Users/kimdn/My%20Project/Assets/Resources/Dialogues/sample_dialogue.json) 파일을 만들고 `DialogueManager`의 `defaultDialogueJson` 필드에 넣어두거나 스크립트에서 동적으로 로드합니다.

### 2. 특정 물체 상호작용 (예: Locked Drawer)
서랍 오브젝트에 `InteractionTrigger` 컴포넌트와 2D/3D Collider를 붙집니다.
- **Default Dialogue Node ID**: `drawer_locked` (열쇠가 없을 때 기본 재생되는 대사 ID)
- **Overrides**: 리스트에 항목 추가
  - **Required Item**: `drawer_key`
  - **Node ID**: `drawer_unlocked` (열쇠가 있을 때 재생되는 대사 ID)

플레이어가 다가가 상호작용 버튼을 누르면 Player 스크립트에서 해당 오브젝트의 `IInteractable`을 가져와 `Interact()`를 실행시킵니다.

```csharp
// Player.cs 예시 (상호작용 키 입력 시)
void Update() {
    if (Input.GetKeyDown(KeyCode.E)) {
        IInteractable target = GetLookAtInteractable();
        if (target != null) {
            target.Interact(); // DialogueManager가 실행되며 알맞은 대사 분기 재생!
        }
    }
}
```

### 3. 나레이션/스토리 이벤트 (Area Narrative Trigger)
플레이어가 특정 방에 들어가면 나레이션이 나와야 할 때:
1. 방 입구에 `BoxCollider2D` (Is Trigger 체크)를 씌웁니다.
2. `AreaNarrativeTrigger` 컴포넌트를 붙입니다.
3. `Dialogue Node ID`에 `room_narration`을 기입합니다.
4. 플레이어가 콜라이더를 밟는 순간, 플레이어가 조건을 만족하는지(특정 플래그가 켜졌는지 등) 체크한 뒤 대사 및 연출을 재생시킵니다.
