using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClassroomEntryTrigger : MonoBehaviour
{
    [SerializeField]
    private GhostChaser chaser;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (chaser != null)
            chaser.StartChasing();
    }
}
