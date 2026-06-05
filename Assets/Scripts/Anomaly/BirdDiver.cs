using System;
using System.Collections;
using UnityEngine;

public class BirdDiver : MonoBehaviour
{
    [SerializeField]
    private float diveSpeed = 8f;

    [SerializeField]
    private float deactivateDelay = 0.5f;

    public static event Action OnPlayerHit;

    // Domain Reload OFF 환경 대비 — 정적 이벤트 잔존 구독 초기화.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents() => OnPlayerHit = null;

    private bool isDiving;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool homeCached;

    // 다이브는 LaunchDive 이후에만 발생 → Awake(혹은 첫 활성) 시점은 항상 홈 위치. 1회만 캐시.
    private void Awake()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
        homeCached = true;
    }

    // 활성화될 때마다 홈 복귀 + 상태 초기화 — 직전 다이브 끝 위치/잔여 isDiving/코루틴 잔존 방지.
    // (OnEnable은 Awake 직후 보장 → homeCached. 이 시점엔 LaunchDive 전이라 다이브 코루틴 없음.)
    private void OnEnable()
    {
        StopAllCoroutines();
        isDiving = false;
        if (homeCached)
            transform.SetPositionAndRotation(homePosition, homeRotation);
    }

    public void LaunchDive(Vector3 playerPos)
    {
        // 재진입 방어: 이전 다이브/비활성화 코루틴 잔여 정리 후 시작 (중복 DiveRoutine 방지).
        StopAllCoroutines();
        // 와운더 중단 후 돌진
        GetComponent<BirdWander>()?.StopWander();
        isDiving = true;
        StartCoroutine(DiveRoutine(playerPos));
    }

    private IEnumerator DiveRoutine(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                diveSpeed * Time.deltaTime
            );
            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            yield return null;
        }

        // 트리거가 발동되지 않은 경우 직접 발동
        if (isDiving)
        {
            isDiving = false;
            OnPlayerHit?.Invoke();
        }

        yield return new WaitForSeconds(deactivateDelay);
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDiving || !other.CompareTag("Player"))
            return;
        isDiving = false;
        StopAllCoroutines();
        OnPlayerHit?.Invoke();
        StartCoroutine(DelayedDeactivate());
    }

    private IEnumerator DelayedDeactivate()
    {
        yield return new WaitForSeconds(deactivateDelay);
        gameObject.SetActive(false);
    }
}
