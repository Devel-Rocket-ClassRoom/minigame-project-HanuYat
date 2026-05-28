using UnityEngine;

public class BirdDangerZone : MonoBehaviour
{
    [SerializeField]
    private AnomalyBirds anomalyBirds;

    [SerializeField]
    private PlayerController playerController;

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
        AnomalyBirds.RaisePlayerAttacked();
    }
}
