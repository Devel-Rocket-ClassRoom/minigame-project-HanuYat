using UnityEngine;

public class BirdDangerZone : MonoBehaviour
{
    [SerializeField]
    private AnomalyBirds anomalyBirds;

    [SerializeField]
    private PlayerController playerController;

    // 공격 제외 구역(예: ClassroomEntry). 플레이어가 이 콜라이더 안이면 공격 안 함 —
    // 교실 입구에서 crouch 힌트 보고 자세 잡을 여유. 비워두면 제외 없음.
    [SerializeField]
    private Collider safeZone;

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerAttack(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTriggerAttack(other);
    }

    private void TryTriggerAttack(Collider other)
    {
        if (anomalyBirds == null || !anomalyBirds.IsArmed)
            return;
        if (!other.CompareTag("Player"))
            return;
        if (playerController != null && playerController.IsCrouching)
            return;
        // 안전 구역(교실 입구) 안이면 공격 제외. ClosestPoint가 입력점과 같으면 콜라이더 내부.
        if (safeZone != null)
        {
            Vector3 playerPos = other.bounds.center;
            if (safeZone.ClosestPoint(playerPos) == playerPos)
                return;
        }
        // 풀 스폰된 다이브 새 인스턴스를 시퀀스로 전달 (사전배치 단일 참조 → 동적).
        AnomalyBirds.RaisePlayerAttacked(anomalyBirds.ActiveDiver);
    }
}
