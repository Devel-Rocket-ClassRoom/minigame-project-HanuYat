# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 저장소에서 작업할 때 참고해야 할 가이드입니다.

## 프로젝트 개요

Unity 6 (`6000.3.15f1`) 미니게임 프로젝트. *Exit 8*풍 이상현상 탐지 워킹 루프 장르의 1인칭 게임이다. 게임 디자인의 단일 진실 원천(single source of truth)은 `docs/GDD_8번출구류_미니게임.docx`이며, `.docx` 바이너리이므로 Claude Code가 직접 읽을 수 없다 — 필요한 섹션은 사용자에게 붙여넣기 요청.

저장소는 현재 게임플레이 스크립트가 없는 빈 프로젝트 골격 상태다. 새 `MonoBehaviour` 코드는 `Assets/` 하위에 두며, 첫 스크립트 작성 시 `Assets/Scripts/` 서브폴더를 제안한다.

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
- **`Assets/Imported/`는 gitignore**: Asset Store, FBX 팩 등 외부 임포트 자산은 여기에 두며 추적하지 않는다. 버전 관리가 필요한 자산은 다른 위치에.
- **설치된 Unity AI/NavMesh 패키지**: `com.unity.ai.assistant` 2.9.0-pre.2, `com.unity.ai.navigation` 2.0.12.
- **씬 구성**: `Assets/Scenes/MainMenu Scene.unity`(메뉴) + `Assets/Scenes/Game Scene.unity`(게임 본편). 새 씬 추가 시 Build Settings에 등록. 씬 이름 하드코딩 시 정확한 표기 사용(`MainMenuController.gameSceneName`, `GameSceneController.mainMenuSceneName` 참고).
- **TextMesh Pro**: 에센셜이 `Assets/TextMesh Pro/`에 import되어 있음. 카운터 UI는 TMP 사용.
- **ProBuilder**: 복도/벽 블록아웃용. `com.unity.probuilder` manifest에 등록됨 — Unity Editor가 자동 install.
- **계획 문서**: `Assets/Plans/`에 마크다운으로 관리하며 git 추적됨.
- **1인칭 FOV**: GDD Art Direction 기준 **70~75도**. Camera 컴포넌트 기본값(60)과 다르므로 씬 카메라 설정 시 주의.

## 디자인 레퍼런스

`docs/GDD_8번출구류_미니게임.docx`가 게임 디자인의 단일 진실 원천. Claude Code는 `.docx`를 직접 읽을 수 없으므로 위 요약에 없는 디테일이 필요하면 사용자에게 해당 섹션 붙여넣기를 요청한다.
