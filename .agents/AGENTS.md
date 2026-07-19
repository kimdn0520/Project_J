# Antigravity Workspace Rules & Project Status

## 📌 Project Context
Unity 2D 호러 쯔꾸르 장르 게임 프로젝트. (이브, 괴이증후군 류)
Match-3 퍼즐 + 씬 기반 맵 시스템 + 다이얼로그 시스템으로 구성.

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
  - `PlayerController.cs`, `PlayerInteraction.cs`, `IPlayerState.cs`, `PlayerStates.cs` 완료.
  - 대화 종료 후 무한 조사 방지용 0.3초 입력 쿨다운 시스템 연동 완료.
  - 전통적인 쯔꾸르 키보드 입력(Space/Z/Enter) 및 New Input System 병행 지원 완료.
- [x] **통합 애니메이터 컨트롤러(PlayerAnimator) 및 캐릭터 연동** (완료)
  - Idle 및 Walk에 2D Simple Directional Blend Tree 적용하여 플레이어에 연동 완료.
  - 캐릭터 우측 이동 시 SpriteRenderer 좌우 반전(flipX) 동적 갱신 적용 완료.
- [x] **물리-비주얼-감지 분리형 플레이어 구조 구축** (완료)
  - 물리용 Root (`Player`) + 애니메이션용 Child (`Visual`) + 감지용 Child (`Trigger`, Untagged) 분리 셋업 완료.
  - 기존 사용자가 설정한 플레이어 스프라이트(`female1_10`) 데이터 안전 마이그레이션 및 영구 보존 조치 완료.
- [x] **다이얼로그 UI Canvas & EventSystem & 테스트 시나리오 기획 구축** (완료)
  - 우측 세로로 아담하게 정렬되는 `ChoiceContainer`와 좌측 65% 너비의 `DialogueText` UI Canvas 자동 구축 및 바인딩 완료.
  - `EventSystem` (InputSystemUIInputModule 탑재) 자동 생성 완료.
  - `PersistentSceneBootstrapper.cs`를 이용한 첫 맵(`Map_01_Start`) 자동 기동, 런타임 JSON 다이얼로그 데이터 동적 주입, 열쇠 획득/잠금 문 열기 이벤트 델리게이트 연동 완료.
- [x] **시나리오 스토리 플롯 & Chapter 1 (1층 로비) 다이얼로그 플로우 기획** (완료)
  - **주인공**: 이은주 (동생 병원비/학자금 대출 때문에 고액 야간 관리인 알바 지원).
  - **배경**: 산속 외딴 곳에 위치한 '라포르 호텔'.
  - **오프닝 (Map_00_Exterior)**: 독백 다이얼로그 및 정문 진입 조건 (`intro_dialogue_done`) 확정.
  - **Chapter 1 (Map_01_Lobby)**:
    1. 로비 진입 시 자동 독백 (`"어라..? 조용하네 / 저 메모는 뭐지?"`)
    2. 프런트 메모 조사 시 지배인 야간 업무 지시서 다이얼로그 출력.
    3. 조작 안내 튜토리얼 나레이션 (`[I]` 키 인벤토리 안내 등).
    4. 1층 필수 야간 업무(로비 조명 켜기, 카운터 서랍에서 2층 열쇠 획득) 완료 후 2층 계단 이동 조건 해제 플로우 확정.
- [x] **프롤로그 (Map_00_HotelExterior) 및 Chapter 1 (Map_01_Lobby) 씬, 다이얼로그, 플로우 구축** (완료)
  - `Player.prefab` 프리팹화 (`Assets/Prefabs/Player.prefab`) 및 Persistent 씬 연동 완료.
  - `chapter1_dialogues.json` 프롤로그 독백, 로비 자동독백, 카운터 메모 조사 독백 및 조작법 시스템 안내 나레이션 작성 완료.
  - `Map_00_HotelExterior.unity`, `Map_01_Lobby.unity` 씬 구축 및 빌드 세팅 등록 완료.
  - 독백 종료 후 자동으로 호텔 입구 문으로 진입하여 로비 씬으로 이동하는 트리거/이벤트 연동 완료.
- [x] **메인 타이틀 씬 (Title.unity) 구축 및 시작 플로우 연동** (완료)
  - 호러 분위기의 "라포르 호텔" 메인 타이틀 UI 및 "게임 시작" 단일 버튼 구축 완료.
  - "게임 시작" 클릭 시 `Persistent` 씬 자동 Additive 로드 + `SaveManager` 초기화 + 첫 프롤로그 씬 (`Map_00_HotelExterior`) 자연스러운 암전 전환 연동 완료.
- [ ] **Chapter 1 필수 야간 업무(조명 켜기, 카운터 서랍에서 2층 열쇠 획득) 상호작용 및 퍼즐 연동** (다음 진행 예정)

---

## 🗺️ 씬 기반 맵 시스템

### 설계 방향
- 맵 하나 = 씬 하나 (`Assets/Scenes/Maps/Map_01_xxx.unity` 등)
- 맵 이동: `LoadSceneAsync` + 암전 연출 (로딩 자체가 공포 연출)
- 플레이어/BGM/GameManager 등 영속 오브젝트는 **Persistent 씬**에서 관리

### 씬 구조
```
Scenes/
  Persistent.unity       ← DontDestroyOnLoad 오브젝트들 (항상 유지)
  Maps/
    Map_01_Start.unity
    Map_02_Corridor.unity
```

---

## 💾 세이브 & 로드 시스템 (Assets/Scripts/SaveSystem/)

### 핵심 스크립트
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
| `PlayerController.cs` | 상태 패턴을 탑재한 플레이어 메인 컨트롤러. 물리 이동 및 애니메이터 매핑 담당. |
| `PlayerInteraction.cs` | 바라보는 방향 앞쪽의 IInteractable 감지. 대화 종료 쿨다운(0.3s) 처리 및 쯔꾸르 입력 폴백 내장. |
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
