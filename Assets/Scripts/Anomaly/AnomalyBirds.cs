using System;
using System.Collections;
using UnityEngine;

public class AnomalyBirds : AnomalyEffectBase
{
    [SerializeField]
    private GameObject[] birdRoots;

    [SerializeField]
    private BirdWander[] birdWanders;

    [SerializeField]
    private BirdDiver birdDiver;

    [Header("울음소리 (선택)")]
    // caw마다 활성 새 위치로 이동시켜 재생할 3D AudioSource. 비우면 무음.
    [SerializeField]
    private AudioSource cawSource;

    // 까마귀 등 울음 클립. 여러 개면 매번 랜덤 선택.
    [SerializeField]
    private AudioClip[] cawClips;

    [SerializeField]
    private float minCawInterval = 1.5f;

    [SerializeField]
    private float maxCawInterval = 4f;

    public static event Action OnPlayerAttacked;

    public static void RaisePlayerAttacked() => OnPlayerAttacked?.Invoke();

    // Domain Reload OFF 환경 대비 — 정적 이벤트 잔존 구독 초기화.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents() => OnPlayerAttacked = null;

    public bool IsArmed { get; private set; }

    private Coroutine cawRoutine;

    private void Awake()
    {
        if (birdRoots == null)
            birdRoots = new GameObject[0];
        if (birdWanders == null)
            birdWanders = new BirdWander[0];
        foreach (var root in birdRoots)
            if (root != null)
                root.SetActive(false);
        if (birdDiver != null)
            birdDiver.gameObject.SetActive(false);
    }

    public override void Activate()
    {
        IsArmed = true;
        foreach (var root in birdRoots)
            if (root != null)
                root.SetActive(true);
        // birdDiver 명시적 재활성 — Awake/Deactivate에서 끄므로 대칭 보장.
        // (birdRoots 중복 포함에 의존하지 않도록 — 빠지면 비활성 객체 LaunchDive 소프트락.)
        if (birdDiver != null)
            birdDiver.gameObject.SetActive(true);
        foreach (var wander in birdWanders)
            if (wander != null)
                wander.StartWander();

        // 출현 즉시 1성 + 이후 랜덤 간격 반복.
        PlayCaw();
        if (cawRoutine != null)
            StopCoroutine(cawRoutine);
        cawRoutine = StartCoroutine(CawRoutine());

        Debug.Log("[Anomaly] A13 AnomalyBirds activated");
    }

    public override void Deactivate()
    {
        IsArmed = false;

        if (cawRoutine != null)
        {
            StopCoroutine(cawRoutine);
            cawRoutine = null;
        }
        if (cawSource != null)
            cawSource.Stop();

        foreach (var wander in birdWanders)
            if (wander != null)
                wander.StopWander();
        foreach (var root in birdRoots)
            if (root != null)
                root.SetActive(false);
        if (birdDiver != null)
            birdDiver.gameObject.SetActive(false);
    }

    private IEnumerator CawRoutine()
    {
        while (IsArmed)
        {
            yield return new WaitForSeconds(
                UnityEngine.Random.Range(minCawInterval, maxCawInterval)
            );
            PlayCaw();
        }
    }

    // 단일 소스를 랜덤 활성 새 위치로 옮겨 울음 재생 — 소스 1개로 위치감 부여.
    private void PlayCaw()
    {
        if (cawSource == null || cawClips == null || cawClips.Length == 0)
            return;

        Transform emitter = PickActiveBird();
        if (emitter != null)
            cawSource.transform.position = emitter.position;

        AudioClip clip = cawClips[UnityEngine.Random.Range(0, cawClips.Length)];
        if (clip != null)
            cawSource.PlayOneShot(clip);
    }

    private Transform PickActiveBird()
    {
        int activeCount = 0;
        foreach (var root in birdRoots)
            if (root != null && root.activeInHierarchy)
                activeCount++;
        if (activeCount == 0)
            return null;

        int pick = UnityEngine.Random.Range(0, activeCount);
        foreach (var root in birdRoots)
        {
            if (root == null || !root.activeInHierarchy)
                continue;
            if (pick == 0)
                return root.transform;
            pick--;
        }
        return null;
    }
}
