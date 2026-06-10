using System.Collections;
using UnityEngine;

public class BirdWander : MonoBehaviour
{
    [SerializeField]
    private Transform wanderCenter;

    [SerializeField]
    private Vector3 wanderHalfExtents = new Vector3(3f, 0.5f, 2f);

    [SerializeField]
    private float minSpeed = 1.2f;

    [SerializeField]
    private float maxSpeed = 2.5f;

    [SerializeField]
    private float arrivalThreshold = 0.4f;

    [SerializeField]
    private float rotationSpeed = 3f;

    private Vector3 fallbackCenter;

    // 풀 스폰 시 AnomalyBirds가 주입하는 공유 배회 중심(월드). 모든 새가 같은 교실 박스를
    // 돌도록 해 가장자리 새가 벽을 뚫지 않게 함. 미주입 시 wanderCenter→fallbackCenter 순.
    private bool hasConfiguredCenter;
    private Vector3 configuredCenter;

    // 공유 배회 중심·범위 주입. StartWander 전에 호출.
    public void Configure(Vector3 center, Vector3 halfExtents)
    {
        configuredCenter = center;
        wanderHalfExtents = halfExtents;
        hasConfiguredCenter = true;
    }

    public void StartWander()
    {
        StopAllCoroutines();
        // 풀 스폰 시 위치 확정 후 호출되므로 여기서 fallback 중심 갱신.
        // (Configure/wanderCenter 미지정 시에만 이 스폰 지점 주변을 배회.)
        fallbackCenter = transform.position;
        StartCoroutine(WanderRoutine());
    }

    public void StopWander()
    {
        StopAllCoroutines();
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            Vector3 center = hasConfiguredCenter
                ? configuredCenter
                : (wanderCenter != null ? wanderCenter.position : fallbackCenter);
            Vector3 target =
                center
                + new Vector3(
                    Random.Range(-wanderHalfExtents.x, wanderHalfExtents.x),
                    Random.Range(-wanderHalfExtents.y, wanderHalfExtents.y),
                    Random.Range(-wanderHalfExtents.z, wanderHalfExtents.z)
                );
            float speed = Random.Range(minSpeed, maxSpeed);

            while (Vector3.Distance(transform.position, target) > arrivalThreshold)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    speed * Time.deltaTime
                );
                Vector3 dir = target - transform.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        rotationSpeed * Time.deltaTime
                    );
                }
                yield return null;
            }
        }
    }
}
