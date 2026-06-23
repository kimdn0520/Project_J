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

## 🗑️ 레벨에디터 시스템 (제거 예정)
> 개발자 전용이므로 유니티 에디터 씬 직접 편집으로 대체. 런타임 레벨에디터 불필요 판단.
> 제거 대상: `Assets/Scripts/LevelEditor/` 전체, `Assets/Editor/LevelEditorSetupHelper.cs`, LevelEditor 씬

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
