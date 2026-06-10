using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class AnomalyBirds : AnomalyEffectBase
{
    [Header("프리팹 (BirdWander 포함; diver는 BirdDiver도)")]
    [SerializeField]
    private GameObject bird01Prefab;

    [SerializeField]
    private GameObject bird03Prefab;

    [SerializeField]
    private GameObject bird05Prefab;

    [SerializeField]
    private GameObject diverPrefab;

    [Header("스폰 위치 (월드 좌표)")]
    [SerializeField]
    private Vector3[] bird01Positions;

    [SerializeField]
    private Vector3[] bird03Positions;

    [SerializeField]
    private Vector3[] bird05Positions;

    [SerializeField]
    private Vector3 diverPosition;

    [Header("배회 범위 (교실 경계 내 공유 박스)")]
    // 모든 새가 공유하는 배회 중심(월드). 가장자리 새가 벽 밖으로 나가지 않도록
    // 스폰 위치가 아닌 이 중심 기준으로 배회. y가 비행 고도.
    [SerializeField]
    private Vector3 wanderCenter = new Vector3(11.28f, 2.0f, 23.73f);

    [SerializeField]
    private Vector3 wanderHalfExtents = new Vector3(5f, 0.1f, 10f);

    [Header("풀")]
    // 스폰된 새의 부모. 비우면 이 오브젝트 transform.
    [SerializeField]
    private Transform birdContainer;

    [SerializeField]
    private int poolDefaultCapacity = 8;

    [SerializeField]
    private int poolMaxSize = 32;

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

    // 다이브 새를 실어 보냄 — 풀에서 스폰되므로 피격 시퀀스가 어느 인스턴스인지 알아야 함.
    public static event Action<BirdDiver> OnPlayerAttacked;

    public static void RaisePlayerAttacked(BirdDiver diver) => OnPlayerAttacked?.Invoke(diver);

    // Domain Reload OFF 환경 대비 — 정적 이벤트 잔존 구독 초기화.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents() => OnPlayerAttacked = null;

    public bool IsArmed { get; private set; }

    // 이번 활성화에서 스폰된 다이브 새. BirdDangerZone이 공격 발동 시 시퀀스로 전달.
    public BirdDiver ActiveDiver { get; private set; }

    private Coroutine cawRoutine;

    // crouch 힌트는 이번 활성화당 1회만 — 앞/뒤 교실문 둘 다 열어도 중복 안 뜨도록.
    private bool crouchHintShown;

    // 프리팹별 풀 (UnityEngine.Pool.ObjectPool). 무리 사이즈/다이버마다 별도 풀.
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new();

    // 현재 대여 중인 인스턴스 — 프리팹 기준으로 반납하기 위해 짝지어 추적.
    private readonly List<(GameObject prefab, GameObject instance)> active = new();

    private ObjectPool<GameObject> PoolFor(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<GameObject>(
                createFunc: () =>
                    Instantiate(prefab, birdContainer != null ? birdContainer : transform),
                actionOnGet: o => o.SetActive(true),
                actionOnRelease: o => o.SetActive(false),
                actionOnDestroy: o => Destroy(o),
                collectionCheck: true,
                defaultCapacity: poolDefaultCapacity,
                maxSize: poolMaxSize
            );
            pools[prefab] = pool;
        }
        return pool;
    }

    private GameObject Spawn(GameObject prefab, Vector3 worldPos)
    {
        if (prefab == null)
            return null;

        GameObject go = PoolFor(prefab).Get();
        go.transform.position = worldPos;
        active.Add((prefab, go));
        // 공유 배회 박스 주입 후 시작 — 모든 새가 교실 경계 내 같은 중심을 돌도록.
        BirdWander wander = go.GetComponent<BirdWander>();
        if (wander != null)
        {
            wander.Configure(wanderCenter, wanderHalfExtents);
            wander.StartWander();
        }
        return go;
    }

    public override void Activate()
    {
        // 중복 활성 방어: 사전배치 토글 시절엔 멱등이었으나 풀링은 재호출 시 중복 스폰됨.
        // Deactivate 없이 Activate가 두 번 와도 이전 대여분을 먼저 반납.
        if (active.Count > 0)
            Deactivate();

        IsArmed = true;
        crouchHintShown = false;

        // 출현 즉시 "왠 새 소리가..?" 힌트 — 교실 진입 전 경계 유도.
        HintMessage.Instance?.ShowBirdsSound();

        SpawnFlock(bird01Prefab, bird01Positions);
        SpawnFlock(bird03Prefab, bird03Positions);
        SpawnFlock(bird05Prefab, bird05Positions);

        // 다이브 새 1마리 — 추적해 두었다가 공격 발동 시 시퀀스로 넘김.
        GameObject diver = Spawn(diverPrefab, diverPosition);
        ActiveDiver = diver != null ? diver.GetComponent<BirdDiver>() : null;

        // 출현 즉시 1성 + 이후 랜덤 간격 반복.
        PlayCaw();
        if (cawRoutine != null)
            StopCoroutine(cawRoutine);
        cawRoutine = StartCoroutine(CawRoutine());

        AnomalyLog.Activated("A13 AnomalyBirds");
    }

    private void SpawnFlock(GameObject prefab, Vector3[] positions)
    {
        if (prefab == null || positions == null)
            return;
        foreach (Vector3 pos in positions)
            Spawn(prefab, pos);
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

        // 대여 인스턴스 전부 풀로 반납 (다이브로 이미 비활성된 것도 안전하게 반납).
        foreach (var (prefab, instance) in active)
        {
            if (instance == null)
                continue;
            instance.GetComponent<BirdWander>()?.StopWander();
            PoolFor(prefab).Release(instance);
        }
        active.Clear();
        ActiveDiver = null;
    }

    // 교실문 오픈 시 호출 — armed 상태에서만, 활성화당 1회 crouch 안내 힌트.
    public void TryShowCrouchHint()
    {
        if (!IsArmed || crouchHintShown)
            return;
        crouchHintShown = true;
        HintMessage.Instance?.ShowBirdsCrouch();
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
        foreach (var (_, inst) in active)
            if (inst != null && inst.activeInHierarchy)
                activeCount++;
        if (activeCount == 0)
            return null;

        int pick = UnityEngine.Random.Range(0, activeCount);
        foreach (var (_, inst) in active)
        {
            if (inst == null || !inst.activeInHierarchy)
                continue;
            if (pick == 0)
                return inst.transform;
            pick--;
        }
        return null;
    }
}
