using UnityEngine;
using UnityEngine.AI;

// A11 동적 이상: 평소 벽에 기대 있던 빗자루가 스스로 교실 바닥을 쓸며 배회한다.
// NavMeshAgent 로 교실 navmesh 위 랜덤 지점을 순회 (책상 등 장애물 회피).
// 회전은 직접 제어 — 이동 방향을 향하되 자루를 앞뒤로 까딱여(빗질 스트로크) 바닥을 쓰는 느낌.
// 켜고 끄는 제어는 AnomalyBroom 가 담당. agent.enabled 토글은 SetSweeping 내부에서.
[RequireComponent(typeof(NavMeshAgent))]
public class BroomSweep : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent agent;

    [Header("Wander")]
    [SerializeField]
    private Vector3 wanderCenter = new Vector3(11f, 0.1f, 24f);

    [SerializeField]
    private float wanderRadius = 6f;

    [SerializeField]
    private float moveSpeed = 1.2f;

    [SerializeField]
    private float arriveDistance = 0.4f;

    [Header("Sweep look")]
    [SerializeField]
    private float sweepAmplitude = 30f; // 앞뒤 스윙 진폭(도) — 수직 기준 앞/뒤로 넘나듦

    [SerializeField]
    private float sweepFrequency = 5f; // 스트로크 속도

    [SerializeField]
    private float forwardLean = 0f; // 스윙 중심 기울기(0=수직 중심 대칭 빗질)

    [SerializeField]
    private float handleHeight = 0.75f; // 회전 피벗 높이(손잡이) — 빗솔(밑)이 호를 그리며 쓸림

    [SerializeField]
    private float turnSpeed = 8f;

    [Header("Sweep sound")]
    [SerializeField]
    private AudioSource sweepAudio;

    [SerializeField]
    private AudioClip[] swooshClips; // 스윙 중심 통과마다 랜덤 재생 (중복 허용)

    [SerializeField]
    private float[] clipLeadSkip; // 클립별 선행 무음 스킵(초) — onset 정렬용. swooshClips 와 인덱스 대응

    private bool sweeping;
    private float faceYaw;
    private float prevSin;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.updateRotation = false; // 회전 직접 제어
        agent.updatePosition = false; // 위치도 직접 제어 — 손잡이 피벗 보정 위해 nextPosition 사용
        agent.enabled = false; // 평소엔 꺼둠 (벽에 기대 정지)
    }

    public void SetSweeping(bool on)
    {
        sweeping = on;
        if (on)
        {
            agent.enabled = true;
            // navmesh 위 안전 지점으로 워프 후 첫 목적지 지정
            if (
                NavMesh.SamplePosition(
                    wanderCenter,
                    out NavMeshHit hit,
                    wanderRadius,
                    NavMesh.AllAreas
                )
            )
                agent.Warp(hit.position);
            agent.isStopped = false;
            faceYaw = transform.eulerAngles.y;
            prevSin = Mathf.Sin(Time.time * sweepFrequency); // 시작 직후 가짜 크로싱 방지
            PickNewDestination();
        }
        else
        {
            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false; // 끈 뒤 AnomalyBroom 가 원래 transform 복원
            }
        }
    }

    private void Update()
    {
        if (!sweeping || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            PickNewDestination();

        // 이동 방향을 향하도록 yaw 보간
        Vector3 vel = agent.desiredVelocity;
        vel.y = 0f;
        if (vel.sqrMagnitude > 0.01f)
        {
            float targetYaw = Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;
            faceYaw = Mathf.LerpAngle(faceYaw, targetYaw, turnSpeed * Time.deltaTime);
        }

        // 앞뒤 쓸기 스트로크 — pitch(앞뒤 기울기)를 진동
        float sinv = Mathf.Sin(Time.time * sweepFrequency);

        // 스윙 중심(최고 속도) 통과 시 swoosh — 사이클당 2회(앞/뒤), 2클립 중 랜덤
        if (
            sweepAudio != null
            && swooshClips != null
            && swooshClips.Length > 0
            && ((prevSin <= 0f && sinv > 0f) || (prevSin >= 0f && sinv < 0f))
        )
        {
            int idx = Random.Range(0, swooshClips.Length);
            AudioClip clip = swooshClips[idx];
            if (clip != null)
            {
                // 선행 무음 스킵 — clip+time+Play 로 재생 시점(onset) 정렬
                float skip =
                    (clipLeadSkip != null && idx < clipLeadSkip.Length) ? clipLeadSkip[idx] : 0f;
                sweepAudio.clip = clip;
                sweepAudio.time = Mathf.Clamp(skip, 0f, Mathf.Max(0f, clip.length - 0.01f));
                sweepAudio.Play();
            }
        }
        prevSin = sinv;

        float stroke = sinv * sweepAmplitude;
        Quaternion rot =
            Quaternion.Euler(0f, faceYaw, 0f) * Quaternion.Euler(forwardLean + stroke, 0f, 0f);

        // 손잡이(위)를 피벗으로 회전 — 빗솔(밑)이 바닥을 호 그리며 쓸린다.
        // agent.nextPosition = 경로상 바닥 지점. 그 위 handleHeight 에 손잡이를 고정하고
        // 회전 오프셋만큼 본체 위치를 보정해 빗솔 끝이 앞뒤로 스윙.
        Vector3 handleAnchor = agent.nextPosition + Vector3.up * handleHeight;
        transform.rotation = rot;
        transform.position = handleAnchor - rot * (Vector3.up * handleHeight);
    }

    private void PickNewDestination()
    {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = wanderCenter + new Vector3(r.x, 0f, r.y);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }
}
