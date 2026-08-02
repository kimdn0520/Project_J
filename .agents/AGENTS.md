# Antigravity Workspace Rules & Project Status

## 📌 Project Context
- **게임 타이틀**: **`귀야행 (鬼夜行 / Gui Ya Haeng)`**
- **장르**: Unity 2D 호러 쯔꾸르 장르 (이브, 괴이증후군 류)
- **주요 시스템**: Match-3 퍼즐 + 씬 기반 맵 시스템 + 다이얼로그 시스템 + 수칙 수첩/삭선 연출 + 동적 Y-Sorting

---

## ✅ 완료 및 다음 작업 (우선순위 순)

- [x] **LevelEditor 씬 및 관련 스크립트 전체 제거** (완료)
- [x] **씬 기반 맵 시스템 구축** (완료)
  - `SceneTransitionManager.cs`, `SceneDoor.cs`, `SpawnPoint.cs` 생성 완료.
- [x] **세이브 & 로드 시스템 구축** (완료)
  - `SaveData.cs`, `PlayerStatus.cs`, `SaveManager.cs`, `UniqueId.cs` 및 UI 프리젠터 2종 완료.
- [x] **사운드 관리 시스템 구축** (완료)
  - `SoundLibrarySO.cs`, `SoundManager.cs` 완료.
- [x] **상태 패턴 기반 플레이어 이동 & 상호작용 컨트롤러 구축** (완료)
  - `PlayerController.cs`, `PlayerInteraction.cs`, `PlayerTriggerZone.cs`, `IPlayerState.cs`, `PlayerStates.cs` 완료.
  - 대화 종료 후 무한 조사 방지용 0.3초 입력 쿨다운 시스템 연동 완료.
  - 전통적인 쯔꾸르 키보드 입력(Space/Z/Enter) 및 New Input System 병행 지원 완료.
  - **4방향 동적 Trigger Position 오프셋** 및 아래(Down) 방향 오프셋(`downOffset = (0, -0.95f)`) 보정 완료.
  - **조작 잠금 시스템 (`SetControlEnabled`)** 구현: 연출 및 씬 전환 직후 다이얼로그 개시 전 찰나의 순간에 플레이어가 움직이는 현상 완벽 방지.
- [x] **동적 2D Y-Sorting 깊이 정렬 시스템 구축 (`YSortable.cs`)** (완료)
  - Y 좌표에 따른 $\text{SortingOrder} = 5000 - (\text{Y} \times 100)$ 자동 계산 엔진 구현.
  - `isStatic` (고정 가구/기둥 최적화) 및 `followParentYSort` (가구 위 메모/열쇠 얹기: `orderOffsetFromParent = 2`) 지원.
  - `Player` 프리팹, `Map_01_Lobby` 씬 내 `FrontDesk`, `MEMO_PAPER`에 자동 적용 및 가림 현상 해결.
- [x] **타일맵 & 레이어 구조 구축 (`SetupLobbyTilemap.cs`)** (완료)
  - `Tilemap_Floor` (Order: 0), `Tilemap_FloorDecor` (Order: 1), `Tilemap_Walls` (Order: 10 + CompositeCollider2D) 3개 레이어 셋업 및 `Map_01_Lobby.unity` 구축 완료.
- [x] **통합 애니메이터 컨트롤러(PlayerAnimator) 및 캐릭터 연동** (완료)
  - Idle 및 Walk에 2D Simple Directional Blend Tree 적용하여 플레이어에 연동 완료.
  - 캐릭터 우측 이동 시 SpriteRenderer 좌우 반전(flipX) 동적 갱신 적용 완료.
- [x] **물리-비주얼-감지 분리형 플레이어 구조 구축** (완료)
  - 물리용 Root (`Player`) + 애니메이션용 Child (`Visual`) + 감지용 Child (`Trigger`, Kinematic Rigidbody2D + BoxCollider2D 0.75x0.75 + PlayerTriggerZone) 분리 셋업 완료.
- [x] **다이얼로그 UI Canvas & EventSystem & 테스트 시나리오 기획 구축** (완료)
  - 우측 세로 정렬 `ChoiceContainer` 및 좌측 `DialogueText` UI Canvas 바인딩 완료.
  - `chapter1_dialogues.json` 텍스트 검토 및 3층 310호 표기 수정 완료.
- [x] **시나리오 스토리 플롯 & Chapter 1 (1층 로비) 다이얼로그 플로우 기획** (완료)
  - **주인공**: 이은주 (22세, 대학생, 무당 외할머니 핏줄 - 서늘함/귀기 예민 감지).
  - **배경**: 산속 외딴 곳에 위치한 서양식 호텔 '귀야행 (鬼夜行)'.
  - **오프닝 (Map_00_HotelExterior)**: 독백 다이얼로그 및 정문 자동 진입 연출 연동.
  - **Chapter 1 (Map_01_Lobby & Map_02_Corridor)**:
    1. 로비 진입 시 자동 독백 (`"어라..? 조용하네 / 저 메모는 뭐지?"`)
    2. 프런트 메모 조사 시 [지배인의 가죽 수첩] 아이템 획득 (`night_shift_rules`).
    3. `[I]` 키 인벤토리에서 수첩 열람 시 야간 근무 수칙 팝업 출력.
    4. **시간 불분명 기믹**: 시계 숫자는 보이지 않고, 수첩을 닫는 순간 **시간을 알 수 없는 12시 괘종시계 소리(쿵... 쿵...)**와 함께 밤 괴이 개시.
    5. **방탈출 카페 스타일 퍼즐 및 수색 연동**: 비밀번호 키패드 금고, 객실 수색, 다이어리/암호문 풀어내기.
- [ ] **규칙 1. [암전 복도] & 업무 매뉴얼 연동 1층 (101~104호) 수색 기믹 구축** (다음 작업)
  - **문서 시스템 분리 & 규칙 해제 시각 연출**:
    - **[야간 근무 수칙]**: `"1층 복도를 순찰할 때 조명이 깜빡이거나 완전히 꺼진다면 즉시 걸음을 멈추고 제자리에 서 계십시오. 어둠 속에서 발소리가 완전히 멀어질 때까지 움직여서는 안 됩니다."`
    - **규칙 삭선 연출**: 배전반에 퓨즈 장착 시 복도가 밝아지며, 수칙 수첩의 **규칙 1 문구 위에 빨간 줄(취소선)이 지워지듯 그어짐**.
    - **[업무 매뉴얼]**: `"1) 104호 체크아웃 객실 정돈 및 물품 점검  2) 2층 객실 점검..."`
  - **퍼즐 진행 순서 (수칙 어둠 복도 ➔ 101~103호 수색 ➔ 배전반 퓨즈 ➔ 104호 침대 정돈 ➔ 전화벨 연동)**:
    1. **암전 복도 수색**: 퓨즈 복구 전까지 1층 복도에는 **규칙 1 (주기적 조명 암전 & 발소리 멈춤 Freeze)**이 항상 작동.
    2. 매뉴얼대로 104호로 가나 문이 잠겨있음을 확인 (2층 계단도 잠김).
    3. 복도의 어둠과 발소리를 피하며 101~103호 객실 수색 ➔ **`[전원 퓨즈]`** 획득.
    4. **배전반 수리**: 배전반에 퓨즈 장착 ➔ 1층 복도 조명 완전 복구 (규칙 1 상시 해제 + 수칙 수첩 빨간선 삭선) + **`[104호 열쇠]`** 획득.
    5. **104호 청소 미션**: 104호 진입 후 침대 정돈/청소 상호작용 성공 ➔ 그 즉시 로비 카운터 **"따르릉!" 전화벨 발동**.
    6. **위험/이벤트 구간 제약**: 전화벨 발동 시 긴장감 조성을 위해 **세이브 제한 (저장 불가 상태)** 처리.
    7. 로비 카운터 전화 받기 ➔ 섬뜩한 다이얼로그 후 **`[2층 계단 열쇠]`** 획득 및 챕터 1 완수.

---

## 🗺️ 씬 기반 맵 시스템

### 설계 방향
- 맵 하나 = 씬 하나 (`Assets/Scenes/Maps/Map_01_Lobby.unity` 등)
- 맵 이동: `LoadSceneAsync` + 암전 연출 (로딩 자체가 공포 연출)
- 플레이어/BGM/GameManager 등 영속 오브젝트는 **Persistent 씬**에서 관리

### 씬 구조
```
Scenes/
  Persistent.unity       ← DontDestroyOnLoad 오브젝트들 (항상 유지)
  Maps/
    Map_00_HotelExterior.unity
    Map_01_Lobby.unity
    Map_02_Corridor.unity
```

---

## 💾 세이브 & 로드 시스템 (Assets/Scripts/SaveSystem/)

| 스크립트 | 역할 |
|---------|------|
| `SaveData.cs` | 세이브 파일 직렬화 모델. 플래그(gameFlags), 키-값(customStates) 데이터 확장 지원. |
| `PlayerStatus.cs` | 체력(하트), 수집/해결된 규칙 목록, 인벤토리 아이템을 관리하는 런타임 데이터 홀더 싱글톤. |
| `SaveManager.cs` | 로컬 디스크 파일 입출력, 시간 누적, 저장/로드 처리 및 세이브 이벤트 전송 싱글톤. |
| `UniqueId.cs` | 씬 내 세이브 가능한 오브젝트용 고유 ID 발급기. 에디터 내 복제(Ctrl+D) 시 충돌 감지 및 자동 갱신. |

---

## 🚶 플레이어 & 카메라 시스템 (Assets/Scripts/Player/ & Assets/Scripts/0.Common/)

| 스크립트 | 역할 |
|---------|------|
| `PlayerController.cs` | 상태 패턴을 탑재한 플레이어 메인 컨트롤러. `SetControlEnabled` 조작 잠금 내장. |
| `PlayerInteraction.cs` | 4방향 오프셋(특히 Down: -0.95f) 및 Space 시 실시간 OverlapBox 탐색 담당. |
| `PlayerTriggerZone.cs` | 자식 Trigger 오브젝트 전용 2D 물리 트리거 탐색기. |
| `YSortable.cs` | 동적 2D Y-Sorting 깊이 정렬 엔진 (가구 위 아이템 `followParentYSort` 지원). |
| `IPlayerState.cs` | 플레이어 상태 인터페이스 (Enter, Update, FixedUpdate, Exit). |
| `PlayerStates.cs` | 플레이어 Idle, Move, Busy 상태 클래스 구현체. |
| `CameraFollow.cs` | 플레이어를 부드럽게 추적하고, 동적 맵 밖 검은 영역 노출을 막는 Clamp boundary 기능 지원. |
| `PersistentSceneBootstrapper.cs` | 게임 시작 시 DialogueManager 바인딩, JSON 다이얼로그 주입, 첫 맵 로드 가속화. |

---

## 💬 다이얼로그 시스템 (Assets/Scripts/DialogSystem/)

1. `IInteractable.cs` — 다형성 상호작용 인터페이스 (NPC, 조사 스팟, 서랍)
2. `DialogueModel.cs` — 노드/분기 선택지 데이터 모델
3. `DialogueDatabase.cs` — Dictionary O(1) 노드 캐시
4. `DialogueManager.cs` — Singleton, UniTask 진행 루프, 조건 평가
5. `DialogueUI.cs` — 타자기 효과(Rich Text 태그 무시), 선택지 버튼 스폰, Space/Z/Enter
6. `InteractionTrigger.cs` — 씬 오브젝트 상호작용, 조건부 대화 오버라이드
7. `DialogueEventDispatcher.cs` — Observer 패턴. 카메라 쉐이크, 점프스케어 트리거

---

## 🔧 개발 규칙
- 입력: **Unity New Input System** (`UnityEngine.InputSystem`) 사용. 구형 Input.GetKey 사용 금지
- 비동기: **UniTask** 사용. async/await 패턴
- 싱글턴: `SingletonMonoBehaviour<T>` 상속
- 에디터 전용 코드: `#if UNITY_EDITOR` 가드 또는 `Assets/Editor/` 폴더
- 저장 경로: 레벨 프리팹 → `Assets/Prefabs/Levels/` (타임스탬프 파일명)
