using System;
using System.Collections;
using UnityEngine;

// 맵 전체에 물이 차오르는 이상현상.
// 발동 → 수면 Y가 바닥 아래(숨김)에서 천장 위까지 천천히 상승.
//   - Water Loop 사운드 볼륨 = 수위 진행도 비례
//   - 발자국 Water 전환 + 발밑 splash 파티클
//   - 긴장 비네팅 = 진행도 비례
// 카메라 완전 잠김 → 언더워터 효과 ON + 숨참기 타이머 시작(BreathMeterUI).
// 숨 소진 → OnPlayerDrowning 발생(WaterSweepSequence가 휩쓸림 연출).
// 탈출 = CorridorDoor 통과 → AnomalyManager.Refresh() → Deactivate()로 전부 원복.
public class AnomalyRisingWater : AnomalyEffectBase
{
    [Header("수면")]
    [SerializeField]
    private Transform waterSurface; // WaterBlock_50m

    [SerializeField]
    private float hiddenY = -5f; // 숨김(비발동, 바닥 아래) Y

    [SerializeField]
    private float startY = 0f; // 발동 시작 수위 — 발밑에 깔린 상태

    [SerializeField]
    private float ceilingY = 4f; // 최대 차오름 Y (천장 위)

    [SerializeField]
    private float riseDuration = 35f; // startY→천장 도달 시간(초)

    [Header("잠김 판정")]
    [SerializeField]
    private Transform cameraTransform; // 플레이어 카메라

    [SerializeField]
    private float submergeOffset = 0.2f; // 카메라가 수면보다 이만큼 아래여야 완전 잠김

    [SerializeField]
    private float breathHoldDuration = 8f; // 숨참기 시간(초)

    [Header("사운드")]
    [SerializeField]
    private AudioSource waterLoopSource; // Water (4) Loop

    [SerializeField]
    private float maxLoopVolume = 1f;

    [Header("이동 둔화")]
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private float walkSpeedMultiplier = 0.7f; // 발동 시 (물 발목) 살짝 둔화

    [SerializeField]
    private float submergedSpeedMultiplier = 0.4f; // 완전 잠김 시 더 둔화

    [Header("연동")]
    [SerializeField]
    private PlayerFootsteps footsteps;

    [SerializeField]
    private UnderwaterEffect underwaterEffect;

    [SerializeField]
    private GameObject feetSplash; // 발밑 splash 파티클 부모

    public static event Action OnPlayerDrowning;

    public static void RaiseDrowning() => OnPlayerDrowning?.Invoke();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents() => OnPlayerDrowning = null;

    private Coroutine riseRoutine;
    private bool submerged;
    private bool drowned;

    private void Awake()
    {
        // 안전 초기화 — 씬에서 실수로 활성/노출 상태로 뒀을 때.
        if (waterSurface != null)
            SetSurfaceY(hiddenY);
        if (waterLoopSource != null)
            waterLoopSource.Stop();
        if (feetSplash != null)
            feetSplash.SetActive(false);
    }

    public override void Activate()
    {
        submerged = false;
        drowned = false;

        // 발동 즉시 발밑 수위로 — 첫 프레임 깜빡임 방지.
        if (waterSurface != null)
            SetSurfaceY(startY);

        if (footsteps != null)
            footsteps.SetWaterMode(true);
        if (playerController != null)
            playerController.SetSpeedMultiplier(walkSpeedMultiplier); // 발동 즉시 살짝 둔화
        if (feetSplash != null)
            feetSplash.SetActive(true);
        if (waterLoopSource != null)
        {
            waterLoopSource.loop = true;
            waterLoopSource.volume = 0f;
            waterLoopSource.Play();
        }

        if (riseRoutine != null)
            StopCoroutine(riseRoutine);
        riseRoutine = StartCoroutine(RiseRoutine());

        HintMessage.Instance?.ShowWater();

        Debug.Log("[Anomaly] AnomalyRisingWater activated");
    }

    public override void Deactivate()
    {
        if (riseRoutine != null)
        {
            StopCoroutine(riseRoutine);
            riseRoutine = null;
        }

        submerged = false;
        drowned = false;

        if (waterSurface != null)
            SetSurfaceY(hiddenY);
        if (waterLoopSource != null)
            waterLoopSource.Stop();
        if (footsteps != null)
            footsteps.SetWaterMode(false);
        if (playerController != null)
            playerController.SetSpeedMultiplier(1f); // 속도 정상 복원
        if (underwaterEffect != null)
            underwaterEffect.ResetEffect();
        if (feetSplash != null)
            feetSplash.SetActive(false);
        BreathMeterUI.Instance?.Hide();
    }

    private IEnumerator RiseRoutine()
    {
        float elapsed = 0f;
        float breathRemaining = breathHoldDuration;

        while (true)
        {
            // 수위 진행도 0~1 (천장 도달 후 1 유지).
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(riseDuration > 0f ? elapsed / riseDuration : 1f);
            float surfaceY = Mathf.Lerp(startY, ceilingY, t);
            SetSurfaceY(surfaceY);

            // 사운드 볼륨 + 긴장 비네팅 = 진행도 비례.
            if (waterLoopSource != null)
                waterLoopSource.volume = maxLoopVolume * t;
            if (underwaterEffect != null)
                underwaterEffect.SetRisingTension(t);

            // 완전 잠김 판정.
            bool nowSubmerged =
                cameraTransform != null && cameraTransform.position.y < surfaceY - submergeOffset;

            if (nowSubmerged && !submerged)
            {
                submerged = true;
                breathRemaining = breathHoldDuration;
                underwaterEffect?.SetUnderwater(true);
                BreathMeterUI.Instance?.Show();
                if (playerController != null)
                    playerController.SetSpeedMultiplier(submergedSpeedMultiplier); // 잠김 시 더 둔화
            }
            else if (!nowSubmerged && submerged)
            {
                // 수면이 다시 카메라 아래로 (이론상 미발생 — 안전 복구).
                submerged = false;
                underwaterEffect?.SetUnderwater(false);
                BreathMeterUI.Instance?.Hide();
                if (playerController != null)
                    playerController.SetSpeedMultiplier(walkSpeedMultiplier); // 잠김 해제 → 발목 둔화로 복귀
            }

            // 잠긴 동안 숨 카운트다운.
            if (submerged && !drowned)
            {
                breathRemaining -= Time.deltaTime;
                float breathT = breathHoldDuration > 0f ? breathRemaining / breathHoldDuration : 0f;
                BreathMeterUI.Instance?.SetProgress(breathT);

                if (breathRemaining <= 0f)
                {
                    drowned = true;
                    RaiseDrowning();
                    // 휩쓸림 시퀀스가 Fade/리셋 처리 — 코루틴 종료.
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void SetSurfaceY(float y)
    {
        if (waterSurface == null)
            return;
        Vector3 p = waterSurface.position;
        p.y = y;
        waterSurface.position = p;
    }
}
