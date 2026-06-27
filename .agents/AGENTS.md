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
  - 다음 단계: 실제 세이브/로드 및 사운드 설정 UI 디자인 작업 및 씬 테스트.

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
    Map_01_Gallery.unity
    Map_02_Corridor.unity
    Map_03_Basement.unity
    ...
```

### SpawnPoint 시스템
- 각 맵 씬에 빈 GameObject `SpawnPoint` 여러 개 배치, 고유 `spawnId` 부여
- 문(출입구) 오브젝트가 `targetScene` + `targetSpawnId` 를 가짐
- 씬 로드 완료 후 해당 `spawnId`의 SpawnPoint 위치로 플레이어 텔포

```
Map_02_Corridor.unity
  └ SpawnPoints/
      └ SpawnPoint (spawnId: "from_map01")
      └ SpawnPoint (spawnId: "from_map03")
```

### 핵심 스크립트
| 스크립트 | 위치 | 역할 |
|---------|------|------|
| `SceneTransitionManager.cs` | MapSystem/ | 씬 로드/언로드, 암전 연출, NextSpawnId 전달 (세이브 로드 시 좌표 오버라이드 지원) |
| `SceneDoor.cs` | MapSystem/ | targetScene + targetSpawnId 보유, 플레이어 접촉 시 전환 트리거 |
| `SpawnPoint.cs` | MapSystem/ | spawnId 보유, 씬 시작 시 NextSpawnId와 매칭되면 플레이어 이동 |

---

## 💾 세이브 & 로드 시스템 (Assets/Scripts/SaveSystem/)

### 설계 방향
- **5개 슬롯**: 플레이 타임, 저장 일시, 하트(체력), 해결 규칙 수, 위치 표시 지원.
- **높은 확장성**: `OnBeforeSave`, `OnAfterLoad` static 이벤트를 활용해 개별 스크립트(상자, 스위치, 몬스터 등)가 매니저 변경 없이 데이터를 세이브에 주입/복구 가능.
- **오브젝트 식별**: `UniqueId` 컴포넌트가 유니티 에디터상에서 절대 겹치지 않는 고유 ID(GUID)를 자동 부여하여 데이터 키 충돌 방지.
- **암호화**: 스토리 유출 및 데이터 변조를 막기 위해 XOR 대칭 암호화 및 Base64 인코딩 저장.

### 핵심 스크립트
| 스크립트 | 역할 |
|---------|------|
| `SaveData.cs` | 세이브 파일 직렬화 모델. 플래그(gameFlags), 키-값(customStates) 데이터 확장 지원. |
| `PlayerStatus.cs` | 체력(하트), 수집/해결된 규칙 목록, 인벤토리 아이템을 관리하는 런타임 데이터 홀더 싱글톤. |
| `SaveManager.cs` | 로컬 디스크 파일 입출력, 시간 누적, 저장/로드 처리 및 세이브 이벤트 전송 싱글톤. |
| `UniqueId.cs` | 씬 내 세이브 가능한 오브젝트용 고유 ID 발급기. 에디터 내 복제(Ctrl+D) 시 충돌 감지 및 자동 갱신. |
| `SaveSlotUI.cs` | 각 세이브 슬롯 UI 바인딩 (플레이타임 포맷팅 및 비주얼 제어). |
| `SaveMenuUI.cs` | 저장/불러오기 모드를 동적으로 처리하는 UI 메뉴 매니저. |

---

## 🔊 사운드 시스템 (Assets/Scripts/0.Common/)

### 설계 방향
- **데이터 분리**: 사운드 에셋 관리를 스크립터블 오브젝트(SO)로 분리하여 BGM 및 SFX 리스트를 직관적으로 구축.
- **BGM 크로스페이드**: 2개 오디오 채널(A/B)을 교차 페이드하여 자연스러운 배경음악 연출 지원 (DOTween 연동).
- **실시간 볼륨 갱신**: Master/BGM/SFX 볼륨 값 조정 시 즉시 사운드 볼륨 갱신 및 `PlayerPrefs` 영구 저장.
- **AudioMixer 유연성**: 믹서가 지정되면 오디오 믹서의 데시벨 파라미터로 제어하며, 없어도 개별 오디오소스 수동 감쇠로 자동 폴백.
- **문자열 기반 편의성**: `PlaySFX("audio_key")` 호출 시 SO 및 `Resources/` 동적 경로 자동 검색 및 캐싱 재생.

### 핵심 스크립트
| 스크립트 | 역할 |
|---------|------|
| `SoundLibrarySO.cs` | BGM 및 SFX 오디오 클립과 고유 매핑 Key를 관리하는 데이터 에셋. |
| `SoundManager.cs` | BGM 페이드 전환, SFX 오브젝트 풀링, 볼륨 정책(믹서/수동) 및 실시간 조절 통합 관리 싱글톤. |

---

## 🧩 Match-3 퍼즐 시스템 (Play Scene)

### 핵심 스크립트 (Assets/Scripts/Puzzle/)
| 스크립트 | 역할 |
|---------|------|
| `PuzzleController.cs` | 메인 컨트롤러. GridSystem(6×8), UniTask 비동기 애니메이션 큐 |
| `GridSystem.cs` | 제네릭 2D 그리드. `IGridNode` 기반, `OnNodeChanged` 이벤트 |
| `PuzzleTile.cs` | 타일 (Red/Blue/Green/Yellow/Purple). isMovable, isMatchable |
| `IceTile.cs` | 장애물 타일. HP 2. 인접 매치 시 데미지, 알파/색상 피드백 |
| `Match3Strategy.cs` | `IMatchStrategy<PuzzleTile>` 구현. 3+ 연속 매치 탐지 |
| `PuzzleScopes.cs` | `BusyScope`(입력 가드), `SwapTransaction`(자동 롤백 스왑) |

---

## 💬 다이얼로그 시스템 (Assets/Scripts/DialogSystem/)

1. `IInteractable.cs` — 다형성 상호작용 인터페이스 (NPC, 조사 스팟, 서랍)
2. `DialogueModel.cs` — 노드/분기 선택지 데이터 모델
3. `DialogueDatabase.cs` — Dictionary O(1) 노드 캐시
4. `DialogueManager.cs` — Singleton, UniTask 진행 루프, 조건 평가
5. `DialogueUI.cs` — 타자기 효과(Rich Text 태그 무시), 선택지 버튼 스폰, Space/Z/Enter
6. `InteractionTrigger.cs` — 씬 오브젝트 상호작용, 조건부 대화 오버라이드
7. `AreaNarrativeTrigger.cs` — 구역 진입(2D/3D Collider Trigger) 나레이션
8. `DialogueEventDispatcher.cs` — Observer 패턴. 카메라 쉐이크, 점프스케어 트리거
9. `DialogueObfuscation.cs` — XOR 암호화로 스토리 유출 방지
10. **Editor 툴링** (Editor/ 폴더):
    - `DialogueContainerSO.cs` — 그래프 노드 ScriptableObject
    - `DialogueGraphView.cs` — 비주얼 그래프 편집 캔버스
    - `DialogueGraphEditorWindow.cs` — 커스텀 에디터 윈도우
    - `DialogueBinaryExporter.cs` — 난독화 바이너리(`dialogues.bin`) 컴파일
    - `DialogueSampleGenerator.cs` — 테스트용 맨션 호러 시나리오 샘플 자동 생성

### 실행 모드
- **에디터**: `editorDialogueGraph` ScriptableObject에서 직접 로드
- **빌드**: `dialogues.bin` 복호화 후 Dictionary 캐시 (JSON 파일 미사용)

---

## ⚙️ 공통 유틸리티 (Assets/Scripts/0.Common/)

| 스크립트 | 역할 |
|---------|------|
| `SingletonMonoBehaviour.cs` | 제네릭 스레드세이프 Singleton, DontDestroyOnLoad |
| `PoolManager.cs` | PoolSO 기반 오브젝트 풀 (Get/Return) |
| `PoolContainer.cs` | 동일 프리팹 풀 큐 관리 |
| `SpriteManager.cs` | SpriteAtlasSO 기반 스프라이트 이름 O(1) 조회 |
| `CameraShake.cs` | 코루틴 쉐이크. ImpactShake(), DamageShake() |

---

## 🔧 개발 규칙
- 입력: **Unity New Input System** (`UnityEngine.InputSystem`) 사용. 구형 Input.GetKey 사용 금지
- 비동기: **UniTask** 사용. async/await 패턴
- 싱글턴: `SingletonMonoBehaviour<T>` 상속
- 에디터 전용 코드: `#if UNITY_EDITOR` 가드 또는 `Assets/Editor/` 폴더
- 저장 경로: 레벨 프리팹 → `Assets/Prefabs/Levels/` (타임스탬프 파일명)
