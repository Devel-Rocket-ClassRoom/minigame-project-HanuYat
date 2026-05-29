# CLAUDE.md

Claude Code(`claude.ai/code`) 작업 시 참고 가이드.

## 프로젝트 개요

Unity 6 (`6000.3.15f1`) 미니게임. *Exit 8*풍 이상현상 탐지 워킹 루프 1인칭 장르. 설계 단일 진실: `docs/GDD_8번출구류_미니게임.docx` — `.docx` 바이너리, Claude Code 직접 읽기 불가. 필요 섹션 사용자에게 붙여넣기 요청.

### 코드 구조

스크립트 전부 `Assets/Scripts/` 서브폴더. 새 스크립트는 적합한 서브폴더에.

- `Anomaly/`: `AnomalyEffectBase` (추상) + anomaly 구현 (A01 추가, A02 제거, A03 색, A04 조명, A05 위치/회전, A06 스케일, A08 머티리얼) + `AnomalyManager` 싱글톤 + GDD 외 자체 시퀀스 2종: **A12 ghost** (`AnomalyGhost`/`GhostChaser`/`ClassroomEntryTrigger`), **A13 새 떼** (`AnomalyBirds`/`BirdWander`/`BirdDiver`/`BirdDangerZone` — crouch 진입 시 안전 통과). A07(사운드)·A09~A11 미구현.
- `Corridor/`: `CorridorDoor` — 양 끝 문 페이드 + 텔레포트 + anomaly 재추첨.
- `Gameplay/`: `JudgementSystem` — 카운터 갱신 (pending → apply 패턴). `ExitSequence` — 카운터 8 + 전진 + anomaly 비활성 시 클리어. `GhostCatchSequence` / `BirdAttackSequence` — ghost·새 떼 피격 시 카메라 연출 + 페이드 + 복도 리셋.
- `Interaction/`: `IInteractable` / `IResettable` 인터페이스, `Interactor` (Raycast hover/interact), `InteractableOutline` (EPOOutline 의존), `ResettableRegistry`, `ClassroomLightSwitch`, `SlidingDoor`.
- `Player/`: `PlayerController` — 1인칭 CharacterController + Input System. crouch 지원 (`IsCrouching` — A13 안전 통과 게이트).
- `Settings/`: `SettingsManager` — `DontDestroyOnLoad` 싱글톤, 마우스 감도·볼륨 `PlayerPrefs` 영구화 + 변경 이벤트 발행.
- `UI/`: `CounterUI`, `FadeController`, `MainMenuController`, `PauseController` (ESC 일시정지/설정), `ClearScreenController`, `SettingsPanelController`.

`Assets/Editor/`: `NavMeshBakeHelper` (Tools/Bake Classroom NavMesh), `GrassScatter` (Tools/Grass Scatter).

### 코어 게임 루프 (GDD §3.2, §4)

`[복도 진입] → [관찰/판단] → [전진 또는 후진] → [카운터 갱신] → [다음 복도] → 카운터 8 도달 시 출구 등장 → 클리어`

- **루프 공간**: 단일 복도 재사용. 양 끝 문(`CorridorDoor`, `IInteractable`) **E(Interact)** → 페이드아웃(~1초) → 시작 지점 텔레포트 + 카메라 리셋 + anomaly 재추첨.
  - 구현: `CorridorDoor.Interact()` → `FadeController` 알파 코루틴 → 페이드 중간(`OnMidpoint`)에 `transform.position` 재설정 + `ResettableRegistry.ResetAll()` + `AnomalyManager.Refresh()`.
- **이상현상**: `AnomalyManager` 싱글톤이 복도 진입 시 50% 확률 anomaly 결정, 후보 중 1개 `Random.Range` 선택, 이전 anomaly 연속 방지.
- **판정**: `되돌아감` + anomaly 활성 → 정답 `+1`; 그 외 → 카운터 **0 리셋**. `전진함` 로직 반전.
- **카운터 UI**: TextMeshPro `0 / 8`. 갱신 시 효과음 + 깜빡임.

### 이상현상 카탈로그 (GDD §5)

목표 8~10개. 시각 60% / 청각 20% / 환경(조명·FOV) 20%.

| ID | 이름 | 난이도 | 구현 핵심 |
|---|---|---|---|
| A01 | 오브젝트 추가 | ★ | `SetActive(true)` |
| A02 | 오브젝트 제거 | ★ | `SetActive(false)` |
| A03 | 색 변경 | ★ | `Renderer.material.color` |
| A04 | 조명 변화 | ★ | `Light.color` / `intensity` |
| A05 | 위치/회전 변경 | ★★ | `transform.rotation` / Animator |
| A06 | 스케일 왜곡 | ★★ | `localScale` (subtle) |
| A07 | 사운드 이상 | ★★ | `AudioSource` on/off |
| A08 | 텍스처 교체 | ★★ | Material 스왑 |
| A09 | 정적 NPC | ★★★ | Mixamo idle + prefab |
| A10 | 시점 왜곡 | ★★★ | URP Volume 프로파일 스왑 |
| A11 | 마네킹 움직임 | ★★★ | Animator Trigger |

큐레이션: 첫 플레이 평균 3~4번째 발견 난이도. 동일 anomaly 연속 노출 방지.

## 작업 환경

- **빌드/테스트 CLI 없음** — 빌드·Play Mode Unity Editor 안에서만. `dotnet build` 금지.
- 테스트: Unity Test Framework (Window → General → Test Runner). 현재 테스트 없음.
- `.csproj` / `.slnx` Unity 재생성, gitignore됨 — 수동 편집 금지. 갱신 필요 시 Unity 재오픈 또는 Edit → Preferences → External Tools → "Regenerate project files".
- VS Code 기본 솔루션: `minigame-project-HanuYat.slnx`(`.vscode/settings.json` 지정).

## 자동 포맷 / 코드 스타일

- **CSharpier 1.2.6 PostToolUse 훅**: `Edit` / `Write` / `MultiEdit` 직후 `.claude/hooks/format-csharp.ps1` 자동 실행, `Assets/*.cs` 포맷. 직접 호출 불필요.
- `dotnet-tools.json` 비표준 위치 **저장소 루트**(일반 `.config/dotnet-tools.json` 아님). 수동 `dotnet tool restore` 시 루트에서 실행.
- `.editorconfig`: `*.cs` / `*.csx` → `end_of_line = lf`, `insert_final_newline = true`.

## 권한 정책 (`.claude/settings.json`)

- **읽기 차단**: `Library/`, `Temp/`, `Logs/`, `obj/`, `*.csproj`, `*.sln` — Unity 생성물.
- **쓰기 차단**: `*.meta`, `*.unity` — **Unity Editor 안에서만** 편집. Claude Code 텍스트 직접 수정 금지.

## 아키텍처

- **URP 듀얼 파이프라인**: `Assets/Settings/Mobile_RPAsset.asset` + `PC_RPAsset.asset` (각각 `Mobile_Renderer.asset` / `PC_Renderer.asset`와 짝). Quality Settings가 플랫폼별 선택. Render Feature 추가/수정 시 양쪽 갱신 또는 타겟 플랫폼 명시. A10은 `DefaultVolumeProfile.asset` / `SampleSceneProfile.asset` 스왑 방식.
- **Input System**: `Assets/InputSystem_Actions.inputactions`(Assets 루트) 프로젝트 전역 액션 맵. C# 래퍼 생성 후 사용. 레거시 `UnityEngine.Input` API 동작 안 함. 시점 **1인칭**.
- **`Assets/Imported/` 외부 Asset Store 전용 + gitignore**: private repo 별도 동기화. 일부 코드 경로 하드코딩(`InteractableOutline.cs`의 `EPOOutline` namespace, `GrassScatter.cs`의 `Assets/Imported/EnvironmentAssetKit/...`) — **폴더 위치/이름 변경 금지**. 패키지 추가/제거 시 private repo도 반영.
- **설치 패키지**: `com.unity.ai.navigation` 2.0.12 (NavMesh), `com.unity.cloud.gltfast` 6.14.1 (GLB ScriptedImporter — `EmergencyExitDoor.glb` / `PrincipalBust.glb`). `com.unity.ai.assistant` 제거됨. gltfast는 AI Assistant transitive dependency였으나 제거 시 mesh reference 끊김 → manifest 자체 등록 분리 (PR #46). GLB importer 필요한 동안 gltfast 직접 의존 유지.
- **씬**: `Assets/Scenes/MainMenu Scene.unity`(메뉴) + `Assets/Scenes/Game Scene.unity`(본편). 새 씬 추가 시 Build Settings 등록. 씬 이름 하드코딩 시 정확한 표기(`MainMenuController.gameSceneName`, `PauseController.mainMenuSceneName` 참고).
- **TextMesh Pro**: 에센셜 `Assets/TextMesh Pro/` import됨. 카운터 UI TMP 사용.
- **ProBuilder**: 복도/벽 블록아웃용. `com.unity.probuilder` manifest 등록 — Unity Editor 자동 install.
- **계획 문서**: `Assets/Plans/` 마크다운, git 추적.
- **1인칭 FOV**: GDD 기준 **70~75도**. Camera 기본값(60)과 다름 — 씬 카메라 설정 시 주의.

## 운영 룰 — 사고 방지

### 씬 직렬화 (ForceBinary)

- `ProjectSettings/EditorSettings.asset`의 `m_SerializationMode: 2` → **ForceBinary**. `Assets/Scenes/*.unity` 진짜 binary (수 MB).
- `.gitattributes`에서 `*.unity binary` 마킹 — Git line-ending normalization 비활성.
- **PR에서 씬 diff 안 보임**. 머지 충돌 시 텍스트 머지 불가 → **Unity Editor 안에서만** 수동 해결.
- 씬 파일 텍스트 직접 편집/패치 금지 (PR#14 binary 손상, PR#42 복구).

### NavMesh 데이터 인라인 임베디드

- NavMeshSurface의 `NavMeshData` sibling `.asset` 분리 없이 **씬 binary 인라인 직렬화** (ForceBinary 동작).
- 클론 후 즉시 NavMesh 사용 가능 — 별도 bake 불필요.
- **re-bake = 씬 binary 통째 변경** → 거대 binary diff. 신중히 결정.
- 재bake 필요 시 `Tools/Bake Classroom NavMesh` 사용. 변경 사항 명확히 의도된 경우만 commit.

### 머티리얼 변형 금지 룰

- **금지**: `r.sharedMaterial.SetColor(...)`, `r.sharedMaterial.EnableKeyword(...)` 등 sharedMaterial **method 호출** → asset 파일 영구 변형 → Play Mode 종료 후 Edit Mode 잔존 (PR#40).
- **허용**:
  - `r.material.SetColor(...)` — 첫 호출 시 unique instance 생성 + 캐싱. asset 분리.
  - `MaterialPropertyBlock` — per-renderer 오버라이드, asset 분리. keyword 제어 불가.
  - `new Material(src)` 캐싱 후 `r.sharedMaterial = cachedInstance` — 우리 인스턴스만 변형, src asset 보존 (`AnomalyMaterialSwap.cs` 참고).
- 새 코드에서 머티리얼 변형 패턴 도입 시 위 룰 확인.

## 디자인 레퍼런스

`docs/GDD_8번출구류_미니게임.docx` 게임 디자인 단일 진실. Claude Code `.docx` 직접 읽기 불가 — 위 요약에 없는 디테일 필요 시 사용자에게 해당 섹션 붙여넣기 요청.