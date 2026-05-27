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

    private CharacterController characterController;
    private bool inTransition;

    private void Start()
    {
        if (playerController != null)
            characterController = playerController.GetComponent<CharacterController>();
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

        inTransition = true;
        OnDoorUsed?.Invoke(direction);
        playerController.enabled = false;

        fadeController.StartTransition(OnMidpoint, OnComplete);
    }

    private void OnMidpoint()
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

    private void OnComplete()
    {
        playerController.enabled = true;
        inTransition = false;
        OnCorridorEntered?.Invoke();
    }
}
