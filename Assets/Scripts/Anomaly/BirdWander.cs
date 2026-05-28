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

    private void Awake()
    {
        fallbackCenter = transform.position;
    }

    public void StartWander()
    {
        StopAllCoroutines();
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
            Vector3 center = wanderCenter != null ? wanderCenter.position : fallbackCenter;
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
