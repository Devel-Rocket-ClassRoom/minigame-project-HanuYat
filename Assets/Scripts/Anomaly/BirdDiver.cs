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
