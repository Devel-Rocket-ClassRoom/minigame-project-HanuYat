using UnityEngine;

public class JudgementSystem : MonoBehaviour
{
    [SerializeField]
    private CounterUI counterUI;

    // 첫 턴(튜토리얼)은 무조건 점수 0 — 종이+슬롯이 0개로 깨끗하게 등장하도록.
    [SerializeField]
    private bool suppressFirstTurnScore = true;

    private int current;
    private int pendingCount;
    private bool hasPending;
    private bool firstTurnConsumed;

    public int Current => current;

    public int Goal => counterUI != null ? counterUI.Goal : 0;

    // 첫 턴(튜토리얼) 여부 — 아직 첫 문 사용 전. 첫 턴엔 후진(들어온 문) 차단에 사용.
    public bool IsFirstTurn => suppressFirstTurnScore && !firstTurnConsumed;

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

        // 첫 턴 튜토리얼: 정답 여부 무관 점수 0 유지 → 종이+슬롯 0개로 등장.
        if (suppressFirstTurnScore && !firstTurnConsumed)
        {
            firstTurnConsumed = true;
            pendingCount = 0;
            hasPending = true;
            return;
        }

        bool anomalyActive =
            AnomalyManager.Instance != null && AnomalyManager.Instance.IsAnomalyActive;
        bool correct =
            (direction == CorridorDoor.DoorDirection.Forward && !anomalyActive)
            || (direction == CorridorDoor.DoorDirection.Backward && anomalyActive);

        pendingCount = correct ? Mathf.Min(current + 1, counterUI.Goal) : 0;
        hasPending = true;
    }

    private void ApplyPending()
    {
        if (!hasPending)
            return;

        hasPending = false;
        current = pendingCount;
        counterUI.UpdateCounter(current);
    }
}
