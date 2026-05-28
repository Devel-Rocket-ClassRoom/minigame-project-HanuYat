using System;
using UnityEngine;

public class JudgementSystem : MonoBehaviour
{
    public static event Action OnGoalReached;

    [SerializeField]
    private CounterUI counterUI;

    private int current;
    private int pendingCount;
    private bool hasPending;
    private bool goalReached;

    public int Current => current;

    public int Goal => counterUI != null ? counterUI.Goal : 0;

    public bool WouldClearOnDoorUse(CorridorDoor.DoorDirection direction)
    {
        if (counterUI == null)
            return false;
        bool anomalyActive =
            AnomalyManager.Instance != null && AnomalyManager.Instance.IsAnomalyActive;
        bool exitCandidate = direction == CorridorDoor.DoorDirection.Forward && !anomalyActive;
        return exitCandidate && current >= counterUI.Goal - 1;
    }

    private void OnEnable()
    {
        CorridorDoor.OnDoorUsed += HandleDoorUsed;
        CorridorDoor.OnCorridorEntered += ApplyPending;
    }

    private void OnDisable()
    {
        CorridorDoor.OnDoorUsed -= HandleDoorUsed;
        CorridorDoor.OnCorridorEntered -= ApplyPending;
    }

    private void Start()
    {
        if (counterUI != null)
            counterUI.UpdateCounter(0);
    }

    private void HandleDoorUsed(CorridorDoor.DoorDirection direction)
    {
        if (counterUI == null)
        {
            Debug.LogWarning("[JudgementSystem] CounterUI 참조가 비어있습니다.", this);
            return;
        }

        bool anomalyActive =
            AnomalyManager.Instance != null && AnomalyManager.Instance.IsAnomalyActive;
        bool correct =
            (direction == CorridorDoor.DoorDirection.Forward && !anomalyActive)
            || (direction == CorridorDoor.DoorDirection.Backward && anomalyActive);

        pendingCount = correct ? Mathf.Min(current + 1, counterUI.Goal) : 0;
        if (!correct)
            goalReached = false;

        hasPending = true;
    }

    private void ApplyPending()
    {
        if (!hasPending)
            return;

        hasPending = false;
        current = pendingCount;
        counterUI.UpdateCounter(current);

        if (current >= counterUI.Goal && !goalReached)
        {
            goalReached = true;
            Debug.Log("[JudgementSystem] Goal reached — exit sequence event fired.");
            OnGoalReached?.Invoke();
        }
    }
}
