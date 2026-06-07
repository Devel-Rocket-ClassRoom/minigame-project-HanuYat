using System.Collections;
using UnityEngine;

// 물 익사(숨 소진) 시 휩쓸림 연출 → 페이드 → 복도 리셋.
// BirdAttackSequence 패턴 — FadeController 전역 락 공유, OnMidpoint에서 리셋/Refresh.
public class WaterSweepSequence : MonoBehaviour
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

    [Header("휩쓸림 연출")]
    [SerializeField]
    private float sweepDuration = 1.2f;

    [SerializeField]
    private float sweepRollDegrees = 70f; // 카메라 옆으로 휩쓸리는 roll

    [SerializeField]
    private float sweepSinkLocalY = -0.4f; // 가라앉는 느낌

    [Header("사운드 (휩쓸림 순간 Water Loop 볼륨 스파이크)")]
    [SerializeField]
    private AudioSource waterLoopSource;

    [SerializeField]
    private float spikeVolume = 1.5f;

    private bool inTransition;
    private Vector3 originalCameraLocalPos;
    private PlayerFootsteps footsteps;

    private void OnEnable()
    {
        AnomalyRisingWater.OnPlayerDrowning += HandleDrowning;
    }

    private void OnDisable()
    {
        AnomalyRisingWater.OnPlayerDrowning -= HandleDrowning;
    }

    private void HandleDrowning()
    {
        if (inTransition)
            return;
        if (
            fadeController == null
            || playerController == null
            || playerCharacter == null
            || cameraTransform == null
            || respawnPoint == null
        )
        {
            Debug.LogWarning(
                "[WaterSweepSequence] 필수 참조가 비어있어 시퀀스를 실행할 수 없습니다.",
                this
            );
            return;
        }

        // 전역 전환 락 — CorridorDoor/Ghost/Bird 피격과 FadeController 공유 충돌 방지.
        if (!fadeController.TryLockTransition())
            return;

        inTransition = true;
        originalCameraLocalPos = cameraTransform.localPosition;
        playerController.enabled = false;
        footsteps = playerController.GetComponent<PlayerFootsteps>();
        if (footsteps != null)
            footsteps.enabled = false;

        StartCoroutine(SweepRoutine());
    }

    private IEnumerator SweepRoutine()
    {
        Quaternion startLocalRot = cameraTransform.localRotation;
        Vector3 startLocalPos = cameraTransform.localPosition;
        originalCameraLocalPos = startLocalPos;

        Quaternion targetLocalRot = startLocalRot * Quaternion.Euler(0f, 0f, sweepRollDegrees);
        Vector3 targetLocalPos = new Vector3(
            startLocalPos.x,
            startLocalPos.y + sweepSinkLocalY,
            startLocalPos.z
        );

        float startVol = waterLoopSource != null ? waterLoopSource.volume : 0f;

        float elapsed = 0f;
        while (elapsed < sweepDuration)
        {
            float t = elapsed / sweepDuration;
            cameraTransform.localRotation = Quaternion.Slerp(startLocalRot, targetLocalRot, t);
            cameraTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            if (waterLoopSource != null)
                waterLoopSource.volume = Mathf.Lerp(startVol, spikeVolume, t);
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

        // anomaly 해제(물/사운드/언더워터 전부 원복) + 복도 리셋.
        ResettableRegistry.ResetAll();
        AnomalyManager.Instance?.Refresh();
    }

    private void OnComplete()
    {
        playerController.enabled = true;
        if (footsteps != null)
            footsteps.enabled = true;
        inTransition = false;
        fadeController.UnlockTransition();
        CorridorDoor.RaiseCorridorEntered();
    }
}
