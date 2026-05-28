using System.Collections;
using UnityEngine;

public class BirdAttackSequence : MonoBehaviour
{
    [SerializeField]
    private FadeController fadeController;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private CharacterController playerCharacter;

    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private Transform respawnPoint;

    [SerializeField]
    private BirdDiver birdDiver;

    [SerializeField]
    private float lookAtBirdSpeed = 6f;

    [SerializeField]
    private float fallPitchDegrees = 75f;

    [SerializeField]
    private float fallenCameraLocalY = 0.1f;

    [SerializeField]
    private float fallDuration = 0.5f;

    private bool inTransition;
    private Vector3 originalCameraLocalPos;
    private Coroutine lookAtBirdCoroutine;

    private void OnEnable()
    {
        AnomalyBirds.OnPlayerAttacked += HandlePlayerInDanger;
        BirdDiver.OnPlayerHit += HandleBirdHit;
    }

    private void OnDisable()
    {
        AnomalyBirds.OnPlayerAttacked -= HandlePlayerInDanger;
        BirdDiver.OnPlayerHit -= HandleBirdHit;
    }

    private void HandlePlayerInDanger()
    {
        if (inTransition)
            return;
        if (
            fadeController == null
            || playerController == null
            || playerCharacter == null
            || cameraTransform == null
            || respawnPoint == null
            || birdDiver == null
        )
        {
            Debug.LogWarning(
                "[BirdAttackSequence] 필수 참조가 비어있어 시퀀스를 실행할 수 없습니다.",
                this
            );
            return;
        }

        inTransition = true;
        originalCameraLocalPos = cameraTransform.localPosition;
        playerController.enabled = false;
        birdDiver.LaunchDive(playerController.transform.position);
        lookAtBirdCoroutine = StartCoroutine(LookAtBirdRoutine());
    }

    private void HandleBirdHit()
    {
        if (!inTransition)
            return;
        if (lookAtBirdCoroutine != null)
        {
            StopCoroutine(lookAtBirdCoroutine);
            lookAtBirdCoroutine = null;
        }
        StartCoroutine(FallRoutine());
    }

    private IEnumerator LookAtBirdRoutine()
    {
        while (true)
        {
            Vector3 dir = birdDiver.transform.position - cameraTransform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                cameraTransform.rotation = Quaternion.Slerp(
                    cameraTransform.rotation,
                    targetRot,
                    lookAtBirdSpeed * Time.deltaTime
                );
            }
            yield return null;
        }
    }

    private IEnumerator FallRoutine()
    {
        Quaternion startLocalRot = cameraTransform.localRotation;
        Vector3 startLocalPos = cameraTransform.localPosition;
        originalCameraLocalPos = startLocalPos;

        Quaternion targetLocalRot = Quaternion.Euler(fallPitchDegrees, 0f, 0f);
        Vector3 targetLocalPos = new Vector3(startLocalPos.x, fallenCameraLocalY, startLocalPos.z);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            float t = elapsed / fallDuration;
            cameraTransform.localRotation = Quaternion.Slerp(startLocalRot, targetLocalRot, t);
            cameraTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cameraTransform.localRotation = targetLocalRot;
        cameraTransform.localPosition = targetLocalPos;

        fadeController.StartTransition(OnMidpoint, OnComplete);
    }

    private void OnMidpoint()
    {
        CorridorDoor.RaiseDoorUsed(CorridorDoor.DoorDirection.Forward);

        bool ccEnabled = playerCharacter.enabled;
        playerCharacter.enabled = false;

        playerController.transform.SetPositionAndRotation(
            respawnPoint.position,
            Quaternion.Euler(0f, respawnPoint.eulerAngles.y, 0f)
        );

        playerCharacter.enabled = ccEnabled;

        playerController.ResetLook();
        cameraTransform.localPosition = originalCameraLocalPos;

        ResettableRegistry.ResetAll();
        AnomalyManager.Instance?.Refresh();
    }

    private void OnComplete()
    {
        playerController.enabled = true;
        inTransition = false;
        CorridorDoor.RaiseCorridorEntered();
    }
}
