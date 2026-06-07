using UnityEngine;

// 거리 누적 기반 발자국 재생. 속도에 따라 걷기/달리기 클립 풀 전환, crouch 시 음량 감쇠.
// PlayerController와 분리 — CharacterController.velocity로 자체 이동 판정.
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField]
    private AudioClip[] walkClips;

    [SerializeField]
    private AudioClip[] runClips;

    [Header("Water Clips (물 이상현상 발동 시 전환)")]
    [SerializeField]
    private AudioClip[] waterWalkClips;

    [SerializeField]
    private AudioClip[] waterRunClips;

    // 물 이상현상 동안 true — 발자국 풀을 Water로 전환.
    private bool waterMode;

    [Header("Cadence (거리 누적 — 빠를수록 잦게)")]
    // 걷기: 이 거리(m)마다 1보.
    [SerializeField]
    private float walkStepDistance = 2.0f;

    // 달리기: 보폭 짧게 = 잦은 발소리.
    [SerializeField]
    private float runStepDistance = 1.5f;

    // 이 수평속도 미만이면 정지로 간주(누적 리셋).
    [SerializeField]
    private float minMoveSpeed = 0.3f;

    // 이 수평속도 이상이면 달리기 풀 사용.
    [SerializeField]
    private float runSpeedThreshold = 4.0f;

    [Header("Volume / Pitch")]
    [SerializeField]
    private float walkVolume = 0.7f;

    // crouch 시 음량 배율(잠입 — 조용히).
    [SerializeField]
    private float crouchVolumeScale = 0.4f;

    // 매 보 피치 랜덤 폭 — 반복 티 제거.
    [SerializeField]
    private float pitchJitter = 0.06f;

    // 두 발소리 사이 최소 간격(s) — isGrounded 떨림/탭 이동에 의한 즉시-첫-보 난사 방지.
    [SerializeField]
    private float minStepInterval = 0.18f;

    private CharacterController controller;
    private PlayerController player;
    private float distanceAccum;
    private int lastIndex = -1;
    private bool wasMoving;
    private float lastStepTime = -999f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!controller.isGrounded)
        {
            distanceAccum = 0f;
            wasMoving = false;
            return;
        }

        Vector3 v = controller.velocity;
        v.y = 0f;
        float speed = v.magnitude;

        if (speed < minMoveSpeed)
        {
            distanceAccum = 0f;
            wasMoving = false;
            return;
        }

        bool running = speed >= runSpeedThreshold;
        float stepDistance = running ? runStepDistance : walkStepDistance;

        // 정지 → 이동 전환 시 즉시 1보 (누적 대기 없이).
        if (!wasMoving)
        {
            wasMoving = true;
            distanceAccum = 0f;
            PlayStep(running);
            return;
        }

        distanceAccum += speed * Time.deltaTime;
        if (distanceAccum >= stepDistance)
        {
            distanceAccum = 0f;
            PlayStep(running);
        }
    }

    private void PlayStep(bool running)
    {
        // 최소 간격 게이트 — 즉시-첫-보 + 누적 양 경로 공통 차단.
        if (Time.time - lastStepTime < minStepInterval)
            return;
        lastStepTime = Time.time;

        AudioClip[] runPool = waterMode ? waterRunClips : runClips;
        AudioClip[] walkPool = waterMode ? waterWalkClips : walkClips;
        AudioClip[] pool = running && runPool != null && runPool.Length > 0 ? runPool : walkPool;
        if (audioSource == null || pool == null || pool.Length == 0)
            return;

        AudioClip clip = pool[PickIndex(pool.Length)];
        if (clip == null)
            return;

        bool crouch = player != null && player.IsCrouching;
        float volume = walkVolume * (crouch ? crouchVolumeScale : 1f);

        audioSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        audioSource.PlayOneShot(clip, volume);
    }

    // 물 이상현상 발동/해제 시 발자국 풀 전환. lastIndex 리셋 — 풀 길이 다를 수 있어 인덱스 무효화.
    public void SetWaterMode(bool value)
    {
        if (waterMode == value)
            return;
        waterMode = value;
        lastIndex = -1;
    }

    // 직전 클립과 다른 인덱스 — 동일 발소리 연속 방지.
    private int PickIndex(int count)
    {
        if (count == 1)
            return lastIndex = 0;
        int i;
        do
        {
            i = Random.Range(0, count);
        } while (i == lastIndex);
        lastIndex = i;
        return i;
    }
}
