using System;
using UnityEngine;

public class CorridorDoor : MonoBehaviour, IInteractable
{
    public enum DoorDirection
    {
        Forward,
        Backward,
    }

    public static event Action<DoorDirection> OnDoorUsed;
    public static event Action OnCorridorEntered;

    public static void RaiseDoorUsed(DoorDirection direction) => OnDoorUsed?.Invoke(direction);

    public static void RaiseCorridorEntered() => OnCorridorEntered?.Invoke();

    [SerializeField]
    private DoorDirection direction;

    [SerializeField]
    private FadeController fadeController;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private JudgementSystem judgement;

    [SerializeField]
    private ExitSequence exitSequence;

    private CharacterController characterController;
    private PlayerFootsteps footsteps;
    private bool inTransition;
    private bool pendingClear;

    private void Start()
    {
        if (playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
            footsteps = playerController.GetComponent<PlayerFootsteps>();
        }
    }

    public void Interact()
    {
        if (inTransition)
            return;
        if (fadeController == null || spawnPoint == null || playerController == null)
        {
            Debug.LogWarning(
                $"[CorridorDoor] {name}: 필수 참조가 비어있어 전환을 실행할 수 없습니다.",
                this
            );
            return;
        }

        // 첫 턴 후진 차단: 들어온 문(Backward)으로는 나갈 수 없다 — 출구로 전진해야 한다.
        // 책 게이트보다 먼저 → 후진 문은 책 회수 여부와 무관하게 항상 안내.
        if (direction == DoorDirection.Backward && judgement != null && judgement.IsFirstTurn)
        {
            HintMessage.Instance?.ShowEntrance();
            return;
        }

        // 턴 목표 게이트: 책 미회수 시 전환 차단(힌트 표시).
        // inTransition/OnDoorUsed/pendingClear 세팅 전에 return → JudgementSystem 카운터 desync 없음.
        if (TurnObjective.Instance != null && !TurnObjective.Instance.IsBookCollected)
        {
            HintMessage.Instance?.ShowLocked();
            return;
        }

        // 전역 전환 락 — 다른 시퀀스(귀신/새 피격) 전환 진행 중이면 진입 차단(소프트락 방지).
        // hint 게이트 통과 후, 상태 세팅/OnDoorUsed 전에 획득 → 실패 시 카운터 desync 없음.
        if (!fadeController.TryLockTransition())
            return;

        pendingClear = judgement != null && judgement.WouldClearOnDoorUse(direction);
        inTransition = true;
        OnDoorUsed?.Invoke(direction);
        playerController.enabled = false;
        if (footsteps != null)
            footsteps.enabled = false;

        fadeController.StartTransition(OnMidpoint, OnComplete);
    }

    private void OnMidpoint()
    {
        if (!pendingClear)
        {
            CharacterController cc = characterController;
            bool ccEnabled = cc != null && cc.enabled;
            if (cc != null)
                cc.enabled = false;

            playerController.transform.SetPositionAndRotation(
                spawnPoint.position,
                Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f)
            );

            if (cc != null)
                cc.enabled = ccEnabled;

            playerController.ResetLook();

            ResettableRegistry.ResetAll();

            AnomalyManager.Instance?.Refresh();
        }
        else
        {
            exitSequence?.TriggerClear();
        }
    }

    private void OnComplete()
    {
        if (!pendingClear)
        {
            playerController.enabled = true;
            if (footsteps != null)
                footsteps.enabled = true;
            fadeController.UnlockTransition();
            OnCorridorEntered?.Invoke();
        }
        // pendingClear(클리어) 경로는 엔딩 종단 — 락 유지로 추가 전환 차단.
        inTransition = false;
    }
}
