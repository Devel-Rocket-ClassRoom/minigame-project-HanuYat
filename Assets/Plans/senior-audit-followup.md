# 시니어 감사 후속 작업 (Senior Audit Follow-up)

> 작성일: 2026-05-27 (PR #43 머지 + Unity Play 테스트 완료 직후 진행한 종합 스캔 결과)
>
> 이 문서는 다음 세션에서 작업 진입을 위한 self-contained 가이드. 직전 세션 컨텍스트 없이도 우선순위와 작업 위치를 바로 잡을 수 있도록 정리.

---

## 작성 배경

- PR #43 (`feature/issue-14-code-audit-polish-2`) 머지로 코드 감사 폴리싱 2차 완료. AnomalyMaterialSwap leak fix, InputAction null guard, GhostChaser repath throttle, AnomalyManager Instance reset, AnomalyColorChange property guard, CLAUDE.md 운영 룰 문서화.
- Unity Play 모드 테스트도 사용자가 직접 수행 완료.
- 이 시점에 시니어 게임 개발자 시선으로 ProjectSettings / 코드 전반을 종합 스캔한 결과를 정리한 문서.
- 항목은 우선순위별로 정렬되어 있으며, 각 항목은 파일 경로 / 라인 / 현재 상태 / 필요 작업 / 검증 방법까지 포함.

---

## 우선순위 요약

| 우선 | 항목 | 작업 위치 | 예상 난이도 |
|---|---|---|---|
| 🔴 High | Application Identifier 교체 | `ProjectSettings/ProjectSettings.asset` | 즉시 (5분) |
| 🔴 High | `OnGoalReached` 후속 (출구 시퀀스) 구현 | 신규 스크립트 + `JudgementSystem` | 중 (반나절~1일) |
| 🟡 Mid | `scriptingDefineSymbols` 잔재 정리 | `ProjectSettings/ProjectSettings.asset` | 즉시 (5분) |
| 🟡 Mid | `EditorBuildSettings` configObject 정리 | `ProjectSettings/EditorBuildSettings.asset` | 즉시 (5분) |
| 🟡 Mid | Audio 시스템 셋업 + A07 사운드 anomaly | 신규 `AudioManager` + `AnomalyAudio` | 중 (반나절) |
| 🟡 Mid | Mobile `vSyncCount` 활성화 | `ProjectSettings/QualitySettings.asset` (Unity 안에서) | 즉시 (5분) |
| 🟢 Low | `Interactor.maxDistance` 디자인 검토 | `Interactor.cs:10` | playtest 기반 |
| 🟢 Low | Stack trace 빌드 최적화 | Player Settings | 출시 직전 |
| 🟢 Low | Standalone IL2CPP 전환 | Player Settings | 출시 직전 |
| 🟢 Low | Splash screen 조정 | Player Settings (라이센스 확인 후) | 출시 직전 |

---

## 🔴 High — 출시 차단 / 게임 완성 필수

### H1. Application Identifier 템플릿 잔재 교체

**현재 상태** (`ProjectSettings/ProjectSettings.asset:169-178`):
```yaml
applicationIdentifier:
  Android: com.UnityTechnologies.com.unity.template.urpblank
  Standalone: com.Unity-Technologies.com.unity.template.urp-blank
  iPhone: com.Unity-Technologies.com.unity.template.urp-blank
overrideDefaultApplicationIdentifier: 1
```

- Product 명은 "Weird Friday", company 명은 "DevelRocket" 으로 셋팅 완료.
- 하지만 bundle ID 가 Unity URP 템플릿 기본값 그대로. `overrideDefaultApplicationIdentifier: 1` 이라 빌드 시 그대로 들어감.

**작업**: Unity Editor 에서 Edit → Project Settings → Player → Other Settings → Identification → Package Name 변경.
- 추천 형식: `com.DevelRocket.WeirdFriday` (3 플랫폼 동일하게 또는 플랫폼별)

**스토어 등록 시 BundleId 변경 불가**하므로 출시 전 반드시 확정.

**또한 같은 파일의 stale 항목**:
- `templatePackageId: com.unity.template.urp-blank@17.0.14` — 잔재 (정보용, 직접 손댈 필요 없음)
- `templateDefaultScene: Assets/Scenes/SampleScene.unity` — 존재하지 않는 씬. Build Settings 가 우선이라 동작엔 영향 없지만 stale. Unity 가 자동 갱신할 수도 있음.

**검증**: Build → 빌드 후 메타데이터 또는 Android Studio / Xcode 에서 bundle ID 확인.

---

### H2. `OnGoalReached` 후속 시퀀스 미구현

**현재 상태** (`Assets/Scripts/Gameplay/JudgementSystem.cs:66-71`):
```csharp
if (current >= counterUI.Goal && !goalReached)
{
    goalReached = true;
    Debug.Log("[JudgementSystem] Goal reached — exit sequence event fired.");
    OnGoalReached?.Invoke();
}
```

- 이벤트만 발화. **구독자 없음** (grep 검증: `OnGoalReached` 구독 코드 0건).
- GDD 코어 루프 `[복도 진입] → ... → 카운터 8 도달 시 출구 등장 → 클리어` 의 **클리어 시퀀스 미구현**.

**필요 작업** (디자인 결정 포함):

1. 출구 표현 방식 선택:
   - (a) 복도 한쪽 끝 문이 "비상구 문" (`EmergencyExitDoor`) 으로 변하고 interact 가능해짐
   - (b) 새 출구 GameObject 가 등장 (`SetActive(true)`)
   - (c) 자동 카메라 연출 + 페이드 + 엔딩 씬 전환

2. 신규 컴포넌트 후보:
   - `Assets/Scripts/Gameplay/ExitSequence.cs` — `OnGoalReached` 구독 → 페이드 + 출구 활성 + 엔딩 트리거
   - `Assets/Scripts/UI/EndingController.cs` 또는 신규 `Ending Scene.unity`

3. Build Settings 에 `Ending Scene` 추가 (만들 경우).

**검증**: Play 모드에서 의도적으로 카운터 8 도달 시 출구 시퀀스가 발화하는지 확인.

**우선순위 사유**: 게임 클리어가 불가능한 상태. 1인 개발 마일스톤 상 "완성된 한 사이클" 도달의 핵심.

---

## 🟡 Mid — 정리 / 폴리싱

### M1. `scriptingDefineSymbols` 잔재 정리

**현재 상태** (`ProjectSettings/ProjectSettings.asset:829-832`):
```yaml
scriptingDefineSymbols:
  Android: URP_OUTLINE
  Standalone: SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY;URP_OUTLINE
  iPhone: URP_OUTLINE
```

**조사 결과**:
- `SENTIS_ANALYTICS_ENABLED` — 코드 어디에도 사용 안됨, `manifest.json` 에 Sentis 패키지 없음.
- `APP_UI_EDITOR_ONLY` — `com.unity.dt.app-ui` 패키지 잔재. `manifest.json` 에 없음.
- `URP_OUTLINE` — `InteractableOutline` / EPOOutline 시스템에서 사용. 유지.

**작업**: Unity Editor → Player Settings → Other Settings → Scripting Define Symbols.
- Standalone 에서 `SENTIS_ANALYTICS_ENABLED`, `APP_UI_EDITOR_ONLY` 제거
- `URP_OUTLINE` 만 남기기 (3 플랫폼 동일하게)

**검증**: 변경 후 컴파일 클린 통과 + Play 모드 작동 확인.

---

### M2. `EditorBuildSettings` configObject 잔재 정리

**현재 상태** (`ProjectSettings/EditorBuildSettings.asset:14-17`):
```yaml
m_configObjects:
  com.unity.dt.app-ui: {fileID: 11400000, guid: 1b1c20d82303e4b5781c3ef50ac1449f, type: 2}
  com.unity.input.settings.actions: {fileID: -944628639613478452, guid: 052faaac586de48259a63d0c4782560b, type: 3}
```

**조사 결과**:
- `com.unity.dt.app-ui` 패키지가 `Packages/manifest.json` 에 없음 — 과거 import 후 제거된 잔재.
- `com.unity.input.settings.actions` 는 Input System 의 정상 reference. 유지.

**작업**: Unity Editor 에서 직접 patch 어려움. 두 가지 옵션:
- (a) 텍스트 에디터로 `EditorBuildSettings.asset` 의 `com.unity.dt.app-ui` 라인 삭제 (씬은 binary 지만 이 파일은 yaml 이라 안전).
- (b) `com.unity.dt.app-ui` 패키지 manifest 에 추가 후 정상 제거 절차로 cleanup.

**권장**: (a) 텍스트 에디터로 라인 직접 삭제 (가장 간단).

**검증**: Unity 재오픈 후 Console 에 missing package 경고 없는지 확인.

---

### M3. Audio 시스템 셋업 + A07 사운드 anomaly

**현재 상태**:
- 코드 안에 `AudioSource` 사용처 = `CounterUI.updateSfx` 1개.
- 누락: 발걸음, ambient (복도 hum / 시계 tick / 외부 바람), anomaly trigger 음, ghost catch, 문 열림, 라이트 토글.
- GDD A07 (사운드 anomaly) 미구현 — 카테고리 분산 시각 60% / 청각 20% / 환경 20% 중 청각 0%.

**필요 작업** (단계별):

1. **AudioManager 싱글톤** (`Assets/Scripts/Audio/AudioManager.cs`):
   - 카테고리별 AudioSource 풀 (SFX / Ambient / Music)
   - ScriptableObject 기반 SoundBank (or 단순 AudioClip[]) — `Assets/Audio/SFX/footstep_01.wav` 등
   - 볼륨 컨트롤 API (`PlaySfx(clip)`, `SetAmbient(clip)`)

2. **A07 사운드 anomaly** (`Assets/Scripts/Anomaly/AnomalyAudio.cs`):
   - `AnomalyEffectBase` 상속
   - Activate 시 특정 AudioSource on/off 또는 비정상 SFX 재생 (예: 멀리서 들리는 발자국, 갑자기 끊긴 시계 tick)
   - Deactivate 시 원상 복귀
   - `AnomalyManager.candidates` 에 등록

3. **footsteps**: `PlayerController.Update` 에서 grounded + horizontal speed 임계 초과 시 일정 주기로 footstep SFX 재생. 표면별 분기는 옵션.

4. **ambient**: Game Scene 의 빈 GameObject 에 loop AudioSource 부착. 단순 작업.

**스코프 결정 필요**: 1, 2, 3, 4 다 할지 / 일단 ambient + A07 만 할지.

**검증**: Play 모드에서 각 카테고리 음향 들리는지. Profiler 에서 AudioSource count.

---

### M4. Mobile `vSyncCount` 활성화

**현재 상태** (`ProjectSettings/QualitySettings.asset` Mobile + PC 둘 다):
```yaml
vSyncCount: 0
```

**문제**: 모바일 빌드 시 fps 무제한 → GPU/CPU 풀가동 → 발열, 배터리 소모, throttle.

**작업**: Unity Editor → Project Settings → Quality → Mobile level 선택 → V Sync Count → "Every V Blank" (1).
- PC 도 `vSyncCount: 1` 옵션 (모니터 refresh rate 일치) 또는 `0` 유지 (fps 무제한 디버깅용).

**검증**: 모바일 디바이스에서 fps cap 작동 (보통 60fps), 발열 감소.

---

## 🟢 Low — 출시 직전 / playtest 기반

### L1. `Interactor.maxDistance` 디자인 검토

**현재 상태** (`Assets/Scripts/Interaction/Interactor.cs:10`):
```csharp
[SerializeField]
private float maxDistance = 1.5f;
```

- 1.5m 는 1인칭 게임에서 짧음. 코앞까지 다가가야 상호작용 가능.
- 일반적인 호러/탐색 게임 = 2.5~3m.
- 의도적으로 가까이 (긴장감) 라면 OK.

**작업**: playtest 후 결정. 변경 시 `Interactor` Inspector 에서 직접 조정 (코드 변경 불필요).

---

### L2. Stack trace 빌드 최적화

**현재 상태** (`ProjectSettings/ProjectSettings.asset:58`):
```yaml
m_StackTraceTypes: 010000000100000001000000010000000100000001000000
```
모든 로그 레벨 (Log/Warning/Error/Assert/Exception) 에 full stack trace 활성.

**문제**: 빌드 시 string allocation + 성능 영향 (특히 모바일).

**작업**: Player Settings → Stack Trace 에서:
- `Log` → None 또는 ScriptOnly
- `Warning` → ScriptOnly
- `Error` / `Assert` / `Exception` → ScriptOnly 또는 Full (crash report 용)

**Editor / Development Build / Release Build 분리 가능** — Release 빌드에만 적용.

---

### L3. Standalone IL2CPP 전환

**현재 상태**: Standalone backend = Mono (`scriptingBackend.Standalone: 0`).
- Android 는 이미 IL2CPP (1).

**작업**: Player Settings → Other Settings → Scripting Backend → IL2CPP.
- 빌드 시간 증가 (~ 분 단위).
- AOT 컴파일로 런타임 성능 향상 + 코드 보호 (decompile 어려움).

**시점**: 출시 직전. 개발 중엔 Mono 유지로 iteration 속도 확보.

---

### L4. Splash screen 조정

**현재 상태**:
```yaml
m_ShowUnitySplashScreen: 1
m_ShowUnitySplashLogo: 1
m_SplashScreenLogos: []
```

**옵션**:
- Unity Personal 라이센스 → Unity 로고 강제. 게임 로고 추가 가능.
- Unity Plus/Pro → Unity 로고 끄기 가능, 커스텀 로고만 표시.

**작업**: 라이센스 확인 후 Player Settings → Splash Image 에서 조정.

---

## 의도적으로 보류한 항목 (기록용)

다음은 시니어 시각에서 봤지만 작업 안하기로 결정한 항목들:

- **InputActionReference null guard 일관화** (모든 컴포넌트에 적용) — PR #43 에서 3개 컴포넌트만 적용. 나머지는 사용처 없거나 필요시점에 추가.
- **`ClassroomLightSwitch` material instance leak 재시도** — 재검토 결과 실제 leak 아님 (`r.material` getter 가 첫 access 에 unique instance 1개 생성 + 캐싱). PR #40 트레이드오프 노트의 가정이 잘못됐던 항목으로 이슈 #14 후속에서 삭제됨.
- **EPOOutline 의존성 분리** — `Assets/Imported/` 를 private repo 로 동기화 관리하니 클론 시 컴파일 에러 시나리오 해당 없음. 이슈 #14 후속에서 삭제됨.
- **AnomalyDefinition ScriptableObject 패턴** — 향후 anomaly 가짓수 늘면 도입. 현재 8개 + ghost 라 직접 컴포넌트 등록으로 충분.
- **PlayerPrefs 기반 저장 시스템** — 출시 시 클리어 횟수, 통계용. 게임 완성 후 작업.

---

## 코드 아키텍처 종합 평가 (참고)

**잘 된 부분**:
- Event-driven coupling: `CorridorDoor` static event → `JudgementSystem`, `GhostCatchSequence` 가 구독. 직접 참조 없이 메시지로 분리.
- Interface 분리: `IInteractable`, `IResettable` 명확.
- Anomaly 추상화: `AnomalyEffectBase` 상속 패턴, 새 anomaly 추가 쉬움.
- Resettable 패턴: `ResettableRegistry` 로 복도 사이클 무결성 보장.
- State machine 명료성: `JudgementSystem` 의 pending → apply 패턴 (페이드 중간에 카운터 변화 숨김).
- Material 변형 패턴 (PR #40 사고 후): `r.material` instance / MPB / `new Material` 캐싱 룰 — CLAUDE.md 에 문서화.
- null guards (PR #43): InputAction, sharedMaterial, property 존재 검증.

**향후 도입 고려**:
- AnomalyDefinition ScriptableObject — anomaly 별 메타데이터, 카테고리, 난이도, 발생 가중치 (가짓수 늘 때).
- AudioManager 싱글톤 + SoundBank ScriptableObject (M3 와 묶음).
- 저장/통계 시스템 (출시 직전).

---

## 검증 체크리스트 (작업 완료 후)

- [ ] Bundle ID 교체 → Build → 패키지 메타에서 확인
- [ ] OnGoalReached 후속 → Play 에서 카운터 8 도달 후 출구 시퀀스 발화
- [ ] scriptingDefineSymbols 정리 → 컴파일 클린 + Play 작동
- [ ] EditorBuildSettings configObject 정리 → Unity 재오픈 시 missing package 경고 0
- [ ] Audio 작업 → Play 에서 SFX / Ambient 들림, Profiler 에서 AudioSource count
- [ ] Mobile vSync → 모바일 빌드에서 fps cap 작동, 발열 감소

---

## 관련 파일 빠른 참조

- `ProjectSettings/ProjectSettings.asset` — bundle ID, define symbols, splash, stack trace, scripting backend
- `ProjectSettings/QualitySettings.asset` — vSync, shadow distance, antialiasing
- `ProjectSettings/EditorBuildSettings.asset` — configObjects, scene list
- `Assets/Scripts/Gameplay/JudgementSystem.cs` — OnGoalReached 이벤트
- `Assets/Scripts/Interaction/Interactor.cs` — maxDistance
- `Assets/Scripts/UI/CounterUI.cs` — 기존 audio 사용처 (updateSfx)
- `Packages/manifest.json` — 패키지 목록 (잔재 검증용)
- `.claude/CLAUDE.md` — 운영 룰 (씬 직렬화 / NavMesh / 머티리얼 변형 금지)
