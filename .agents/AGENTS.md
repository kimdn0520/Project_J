# Antigravity Workspace Rules & Project Status

## 📌 Project Context
Unity 2D 호러 쯔꾸르 장르 게임 프로젝트. (이브, 괴이증후군 류)
Match-3 퍼즐 + 씬 기반 맵 시스템 + 다이얼로그 시스템으로 구성.

---

## ✅ 다음 작업 (우선순위 순)

1. **LevelEditor 씬 및 관련 스크립트 전체 제거**
   - 제거 대상: `Assets/Scripts/LevelEditor/` 폴더 전체, `Assets/Editor/LevelEditorSetupHelper.cs`, LevelEditor 씬
   - `LevelSaveLoad.cs`, `LevelItemData.cs` → 일단 보류 후 필요 없으면 제거

2. **씬 기반 맵 시스템 구축** → 아래 섹션 참고

---

## 🗺️ 씬 기반 맵 시스템 (구축 예정)

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

### 구현할 스크립트
| 스크립트 | 위치 | 역할 |
|---------|------|------|
| `SceneTransitionManager.cs` | Persistent 씬 | 씬 로드/언로드, 암전 연출, NextSpawnId 전달 |
| `SceneDoor.cs` | 각 맵 씬 출입구 | targetScene + targetSpawnId 보유, 플레이어 접촉 시 전환 트리거 |
| `SpawnPoint.cs` | 각 맵 씬 | spawnId 보유, 씬 시작 시 NextSpawnId와 매칭되면 플레이어 이동 |

### 전환 흐름
```
플레이어가 SceneDoor에 닿음
  → SceneTransitionManager.Load(targetScene, targetSpawnId)
  → 암전 (DOTween 페이드)
  → 현재 맵 씬 UnloadSceneAsync
  → 다음 맵 씬 LoadSceneAsync
  → SpawnPoint가 플레이어 위치 설정
  → 밝아짐
```

### 메모리 관련
- 씬 Unload 시 Unity가 해당 씬 에셋 자동 해제 (프리팹 방식보다 유리)
- Persistent 씬 오브젝트만 메모리에 유지됨

---

## 🗺️ 레벨에디터 시스템 (LevelEditor Scene)
> **마지막 작업: 2026-06-23** — 레벨에디터 UI 전면 개편

### 씬 재생성 방법
Unity 재컴파일 완료 후 → **Unity 메뉴바 → `Level Editor → Setup Scene`**

### 구성 스크립트 (Assets/Scripts/LevelEditor/)

| 스크립트 | 역할 |
|---------|------|
| `LevelEditorManager.cs` | Singleton. Grid/Tilemap 관리, 입력 처리, Spotlight 배치/삭제 |
| `LevelEditorUI.cs` | 타일·오브젝트 패널 분리, 카테고리 탭, 도구 버튼, Spotlight 설정 패널 |
| `LevelItemData.cs` | ScriptableObject. `category` 필드로 탭 분류 |
| `TileSpotlightMarker.cs` | Point Light 마커. 셀 단위 배치. color/intensity/range 설정 |
| `LevelSaveLoad.cs` | 레벨 루트를 타임스탬프 붙은 Prefab으로 저장 |
| `GridLineRenderer.cs` | GL 즉시모드로 그리드 시각화 |

### 씬 UI 레이아웃
```
┌─────────────────────────────────────────────────┐
│ [TopBar] ✏Draw ⌫Erase ☀Light │ 힌트 │ 지우기 💾저장 │
├──────────┬──────────────────────────┬───────────┤
│ TILES    │                          │ OBJECTS   │
│[카테고리탭]│     게임뷰 (페인팅 영역)   │[카테고리탭]│
│ 96×96 그리드 팔레트               │ 96×96 그리드 팔레트 │
│         │          ┌─────────────┐ │           │
│         │          │ ☀Spotlight  │ │           │
│         │          │ 색상 프리셋   │ │           │
│         │          │ 강도 슬라이더 │ │           │
│         │          │ 범위 슬라이더 │ │           │
└──────────┴──────────┴─────────────┴─┴───────────┘
```

### Canvas 설정
- **RenderMode**: `Screen Space Camera` (mainCam 사용, planeDistance=1)
- **CanvasScaler**: ScaleWithScreenSize, 기준해상도 1920×1080, match=0.5

### 도구 단축키
| 키 | 기능 |
|----|------|
| Q | 그리기 모드 |
| E | 지우기 모드 (모든 레이어 일괄 삭제) |
| R | 스팟라이트 배치 모드 |
| WASD | 카메라 이동 |
| 스크롤 | 카메라 줌 |
| 우클릭 드래그 | 지우기 (모드 무관) |

### LevelRoot 계층 구조
```
Grid
└── LevelRoot
    ├── Floor_Tilemap      (sortOrder: 0)
    ├── Wall_Tilemap       (sortOrder: 1)
    ├── Overlay_Tilemap    (sortOrder: 2)
    ├── Object_Layer       (GameObject 배치용)
    └── Spotlight_Layer    (TileSpotlightMarker 프리팹 인스턴스)
```

### Spotlight 시스템
- **프리팹**: `Assets/Prefabs/SpotlightMarker.prefab` (Light + TileSpotlightMarker)
- R 모드에서 셀 클릭 → 해당 셀 정중앙 (Z=-1)에 Point Light 배치
- 우클릭으로 제거
- 설정 패널: 색상 프리셋 6가지 + Intensity(0.1~5) + Range(0.5~10) 슬라이더
- `SetSpotlightSettings(Color, float, float)`로 LevelEditorManager에 전달

### UI 차단 방식
- 기존: 하드코딩 픽셀 좌표 비교 → **현재: EventSystem.RaycastAll()** 사용
- 사이드바/탑바 Image 컴포넌트가 레이캐스트를 막아 씬 클릭 차단

### LevelItemData 카테고리 예시
- Tile: `"바닥"`, `"벽"`, `"특수"`
- GameObject: `"소품"`, `"적"`, `"조명"`
- 기본값: `"기본"`

### 라이팅 관련 주의사항
- 현재 `TileSpotlightMarker`는 Unity 3D `Light` (Point) 사용
- **URP 사용 시**: `Light2D (Point)`로 교체 권장 (Light2D 임포트 후 교체)
- **Global Light**: 씬 분위기 연출용. 에디터에서 건드리지 말고 Lighting Settings에서 관리
  - 호러 장르면 Global Light를 0~0.1 수준으로 낮추고 SpotLight로만 조명 연출
  - 레벨에디터에 슬라이더 추가하는 것보다 별도 LightingManager 스크립트로 관리 추천

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
