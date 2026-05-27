using System.Collections;
using UnityEngine;

public class GhostCatchSequence : MonoBehaviour
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
    private AnomalyGhost anomalyGhost;

    [SerializeField]
    private Transform ghostFaceAnchor;

    [SerializeField]
    private float zoomDuration = 0.5f;

    [SerializeField]
    private float crouchedCameraLocalY = 0.7f;

    private bool inTransition;
    private Vector3 originalCameraLocalPos;

    private void OnEnable()
    {
        GhostChaser.OnPlayerCaught += HandleCaught;
    }

    private void OnDisable()
    {
        GhostChaser.OnPlayerCaught -= HandleCaught;
    }

    private void HandleCaught()
    {
        if (inTransition)
            return;
        if (
            fadeController == null
            || playerController == null
            || playerCharacter == null
            || cameraTransform == null
            || respawnPoint == null
            || anomalyGhost == null
            || ghostFaceAnchor == null
        )
        {
            Debug.LogWarning(
                "[GhostCatchSequence] 필수 참조가 비어있어 시퀀스를 실행할 수 없습니다.",
                this
            );
            return;
        }

        inTransition = true;
        anomalyGhost.ForceVisible();
        playerController.enabled = false;
        StartCoroutine(CatchRoutine());
    }

    private IEnumerator CatchRoutine()
    {
        // 1) 카메라 시작 상태 저장 + crouch 타겟 계산
        Quaternion startRot = cameraTransform.rotation;
        Vector3 startLocalPos = cameraTransform.localPosition;
        originalCameraLocalPos = startLocalPos;
        Vector3 targetLocalPos = new Vector3(
            startLocalPos.x,
            crouchedCameraLocalY,
            startLocalPos.z
        );

        // 2) 카메라 내리면서 ghost face 향해 회전 (매 프레임 방향 재계산)
        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            float t = elapsed / zoomDuration;
            cameraTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);

            Vector3 dir = ghostFaceAnchor.position - cameraTransform.position;
            if (dir.sqrMagnitude < 0.0001f)
                dir = cameraTransform.forward;
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            cameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cameraTransform.localPosition = targetLocalPos;
        {
            Vector3 dir = ghostFaceAnchor.position - cameraTransform.position;
            if (dir.sqrMagnitude < 0.0001f)
                dir = cameraTransform.forward;
            cameraTransform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // 3) Fade + teleport + counter reset
        fadeController.StartTransition(OnMidpoint, OnComplete);
    }

    private void OnMidpoint()
    {
        // JudgementSystem이 "anomaly active + Forward door 사용" = 오답 판정으로 카운터 0 큐잉.
        CorridorDoor.RaiseDoorUsed(CorridorDoor.DoorDirection.Forward);

        bool ccEnabled = playerCharacter.enabled;
        playerCharacter.enabled = false;

        playerController.transform.SetPositionAndRotation(
            respawnPoint.position,
            Quaternion.Euler(0f, respawnPoint.eulerAngles.y, 0f)
        );

        playerCharacter.enabled = ccEnabled;

        playerController.ResetLook();

        // 카메라 원위치 복귀 (페이드 중이라 보이지 않음)
        cameraTransform.localPosition = originalCameraLocalPos;

        ResettableRegistry.ResetAll();
        AnomalyManager.Instance?.Refresh();
    }

    private void OnComplete()
    {
        playerController.enabled = true;
        inTransition = false;

        // JudgementSystem.ApplyPending → 카운터 0 적용 + pulse/SFX.
        CorridorDoor.RaiseCorridorEntered();
    }
}
