# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 저장소에서 작업할 때 참고해야 할 가이드입니다.

## 프로젝트 개요

Unity 6 (`6000.3.15f1`) 미니게임 프로젝트. *Exit 8*풍 이상현상 탐지 워킹 루프 장르의 1인칭 게임이다. 게임 디자인의 단일 진실 원천(single source of truth)은 `docs/GDD_8번출구류_미니게임.docx`이며, `.docx` 바이너리이므로 Claude Code가 직접 읽을 수 없다 — 필요한 섹션은 사용자에게 붙여넣기 요청.

### 코드 구조

스크립트는 모두 `Assets/Scripts/` 하위 서브폴더로 분류되어 있다. 새 스크립트는 가장 적합한 서브폴더에 둔다.

- `Anomaly/`: `AnomalyEffectBase` (추상) + 개별 anomaly 구현 (A01 추가, A02 제거, A03 색, A04 조명, A05 위치/회전, A06 스케일, A08 머티리얼) + `AnomalyManager` 싱글톤 + `AnomalyGhost`/`GhostChaser`/`ClassroomEntryTrigger` (GDD 외 자체 ghost 시퀀스).
- `Corridor/`: `CorridorDoor` — 양 끝 문에서 페이드 + 텔레포트 + anomaly 재추첨 트리거.
- `Gameplay/`: `JudgementSystem` — 카운터 갱신 로직 (pending → apply 패턴). `GhostCatchSequence` — ghost 에 잡힐 때 카메라 줌+페이드.
- `Interaction/`: `IInteractable` / `IResettable` 인터페이스, `Interactor` (Raycast hover/interact), `InteractableOutline` (EPOOutline 의존), `ResettableRegistry`, `ClassroomLightSwitch`, `SlidingDoor`.
- `Player/`: `PlayerController` — 1인칭 CharacterController + Input System.
- `UI/`: `CounterUI`, `FadeController`, `MainMenuController`, `GameSceneController`.

`Assets/Editor/` 에는 에디터 툴이 있다: `NavMeshBakeHelper` (Tools/Bake Classroom NavMesh), `GrassScatter` (Tools/Grass Scatter).

### 코어 게임 루프 (GDD §3.2, §4)

`[복도 진입] → [관찰/판단] → [전진 또는 후진] → [카운터 갱신] → [다음 복도] → 카운터 8 도달 시 출구 등장 → 클리어`

- **루프 공간**: 양 끝 트리거 콜라이더로 막힌 단일 복도 하나를 재사용. 끝 도달 시 짧은 페이드아웃(약 1초) 후 시작 지점 텔레포트 + 카메라 방향 리셋 + anomaly 상태 재추첨.
  - 구현: `OnTriggerEnter` → `Image` UI 알파 코루틴 → `transform.position` 재설정.
- **이상현상 시스템**: `AnomalyManager` 싱글톤이 복도 진입 시 50% 확률로 anomaly 발생 결정(밸런싱 변수), 후보 중 1개 `Random.Range` 선택, 이전 anomaly 연속 선택 방지.
- **판정**: `되돌아감` 트리거 + anomaly 활성 → 정답 `+1`; 그 외 오답 → 카운터 **0으로 리셋**. `전진함` 트리거는 위 로직 반전.
- **카운터 UI**: TextMeshPro로 `0 / 8` 표시. 갱신 시 짧은 효과음 + 깜빡임.

### 이상현상 카탈로그 (GDD §5)

목표 8~10개. 카테고리 분산은 시각 60% / 청각 20% / 환경(조명·FOV) 20%.

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

큐레이션 원칙: 첫 플레이 평균 3~4번째에 발견될 정도, 같은 anomaly 연속 노출 방지.

## 작업 환경

- **빌드/테스트 CLI 없음** — 빌드 및 Play Mode는 Unity Editor 안에서만. `dotnet build` 같은 명령 사용 금지.
- 테스트는 Unity Test Framework (Window → General → Test Runner). 현재 테스트 없음.
- `.csproj` / `.slnx`는 Unity가 재생성하며 gitignore됨 — 수동 편집 금지. 갱신이 필요하면 Unity에서 프로젝트 재오픈 또는 Edit → Preferences → External Tools → "Regenerate project files".
- VS Code 기본 솔루션은 `minigame-project-HanuYat.slnx`(`.vscode/settings.json` 지정).

## 자동 포맷 / 코드 스타일

- **CSharpier 1.2.6 PostToolUse 훅**: `Edit` / `Write` / `MultiEdit` 도구 사용 직후 `.claude/hooks/format-csharp.ps1`가 자동 실행되어 `Assets/*.cs`를 포맷한다. 직접 호출할 필요 없음.
- `dotnet-tools.json`은 비표준 위치인 **저장소 루트**에 있다(일반적인 `.config/dotnet-tools.json` 아님). 수동으로 `dotnet tool restore`를 실행해야 한다면 루트에서 실행.
- `.editorconfig`: `*.cs` / `*.csx`는 `end_of_line = lf`, `insert_final_newline = true`.

## 권한 정책 (`.claude/settings.json`)

- **읽기 차단**: `Library/`, `Temp/`, `Logs/`, `obj/`, `*.csproj`, `*.sln` — Unity 생성물.
- **쓰기 차단**: `*.meta`, `*.unity` — Unity 씬/메타 파일은 **반드시 Unity Editor 안에서만** 편집한다. Claude Code가 텍스트로 직접 수정하지 않는다.

## 아키텍처

- **URP 듀얼 파이프라인**: `Assets/Settings/Mobile_RPAsset.asset` + `PC_RPAsset.asset` (각각 `Mobile_Renderer.asset` / `PC_Renderer.asset`와 짝). Quality Settings가 플랫폼별로 하나를 선택. Render Feature를 추가/수정할 때는 양쪽 모두 갱신하거나 어느 플랫폼을 타겟하는지 명시한다. A10(시점 왜곡)은 `DefaultVolumeProfile.asset` / `SampleSceneProfile.asset`을 스왑하는 방식.
- **Input System**: `Assets/InputSystem_Actions.inputactions`(Assets 루트)이 프로젝트 전역 액션 맵. 이 파일에서 C# 래퍼를 생성해 사용. 새 Input System이 활성 상태이므로 레거시 `UnityEngine.Input` API는 동작하지 않는다. 시점은 **1인칭**.
- **`Assets/Imported/`는 외부 Asset Store 패키지 전용 + gitignore**: 개인 private repo 로 별도 동기화 관리한다. 일부 코드가 이 경로를 하드코딩하므로(`InteractableOutline.cs` 의 `EPOOutline` namespace, `GrassScatter.cs` 의 `Assets/Imported/EnvironmentAssetKit/...`) **폴더 위치/이름 변경 금지**. 패키지 추가/제거 시 private repo 측에도 반영.
- **설치된 NavMesh / GLB importer 패키지**: `com.unity.ai.navigation` 2.0.12 (NavMesh), `com.unity.cloud.gltfast` 6.14.1 (GLB ScriptedImporter — `EmergencyExitDoor.glb` / `PrincipalBust.glb` import). `com.unity.ai.assistant` 는 사용 안 해 제거됨. gltfast 는 원래 AI Assistant 의 transitive dependency 였는데 AI Assistant 제거 시 GLB importer 사라져 mesh reference 끊기는 사고 발생 → manifest 에 자체 등록으로 분리 (PR #46 컨텍스트). GLB importer 가 필요한 동안 gltfast 직접 의존 유지.
- **씬 구성**: `Assets/Scenes/MainMenu Scene.unity`(메뉴) + `Assets/Scenes/Game Scene.unity`(게임 본편). 새 씬 추가 시 Build Settings에 등록. 씬 이름 하드코딩 시 정확한 표기 사용(`MainMenuController.gameSceneName`, `GameSceneController.mainMenuSceneName` 참고).
- **TextMesh Pro**: 에센셜이 `Assets/TextMesh Pro/`에 import되어 있음. 카운터 UI는 TMP 사용.
- **ProBuilder**: 복도/벽 블록아웃용. `com.unity.probuilder` manifest에 등록됨 — Unity Editor가 자동 install.
- **계획 문서**: `Assets/Plans/`에 마크다운으로 관리하며 git 추적됨.
- **1인칭 FOV**: GDD Art Direction 기준 **70~75도**. Camera 컴포넌트 기본값(60)과 다르므로 씬 카메라 설정 시 주의.

## 운영 룰 — 사고 방지

### 씬 직렬화 (ForceBinary)

- `ProjectSettings/EditorSettings.asset` 의 `m_SerializationMode: 2` → **ForceBinary**. `Assets/Scenes/*.unity` 는 진짜 binary 파일 (수 MB 크기).
- `.gitattributes` 에서 `*.unity binary` 마킹 — Git 의 line-ending normalization 비활성.
- **PR 에서 씬 diff 안 보임**. 머지 충돌 시 텍스트 머지 불가 → **Unity Editor 안에서만** 수동 해결.
- 씬 파일을 텍스트로 직접 편집/패치하지 말 것 (PR#14 에서 binary 손상 사고 발생, PR#42 에서 복구).

### NavMesh 데이터 인라인 임베디드

- NavMeshSurface 의 `NavMeshData` 가 sibling `.asset` 으로 분리되지 않고 **씬 binary 안에 인라인 직렬화**됨 (ForceBinary 모드 동작).
- 즉 클론 받은 사람도 즉시 NavMesh 사용 가능 — 별도 bake 불필요.
- **단 re-bake = 씬 binary 통째 변경** → 거대한 binary diff 발생. 신중히 결정.
- 재bake 가 필요하면 `Tools/Bake Classroom NavMesh` (메뉴) 사용. 베이크 전후 씬 변경 사항이 명확히 의도된 경우만 commit.

### 머티리얼 변형 금지 룰

- **금지**: `r.sharedMaterial.SetColor(...)`, `r.sharedMaterial.EnableKeyword(...)` 등 sharedMaterial 의 **method 호출**. → 머티리얼 asset 파일 자체를 영구 변형 → Play Mode 종료 후에도 Edit Mode 에 잔존 (PR#40 사고).
- **허용**:
  - `r.material.SetColor(...)` — getter 가 첫 호출에 unique instance 생성 + 캐싱. asset 분리.
  - `MaterialPropertyBlock` — per-renderer 오버라이드, asset 분리. 단 keyword 제어 불가.
  - `new Material(src)` 캐싱 후 `r.sharedMaterial = cachedInstance` — 우리가 만든 인스턴스 위에서만 변형, src asset 안건드림 (`AnomalyMaterialSwap.cs` 참고).
- 새 코드에서 머티리얼 변형 패턴 도입 시 위 룰 확인.

## 디자인 레퍼런스

`docs/GDD_8번출구류_미니게임.docx`가 게임 디자인의 단일 진실 원천. Claude Code는 `.docx`를 직접 읽을 수 없으므로 위 요약에 없는 디테일이 필요하면 사용자에게 해당 섹션 붙여넣기를 요청한다.
