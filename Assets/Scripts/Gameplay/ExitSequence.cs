using System.Collections;
using UnityEngine;

public class ExitSequence : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private ClearScreenController clearScreen;

    // 마지막 슬롯 점화 — TriggerClear는 페이드 미드포인트(화면 암전) 시점이라 시각 효과는 미미.
    // 상태 정합성 유지 목적. (goal 값 의존 없이 전 슬롯 점화)
    [SerializeField]
    private FlameCounterDisplay flameDisplay;

    [Header("엔딩 컷씬")]
    // 1인칭 카메라(PlayerController 자식 = 머리). 책 응시 틸트 + 보빙 대상.
    [SerializeField]
    private Transform cameraTransform;

    // "손에 든 책" 프롭. 플레이어 몸(루트)의 자식 = 허리/가슴 높이에 들려 있음.
    // 카메라(머리)가 숙여 이 책을 내려다본다(책은 몸에 고정, 시선만 이동). 평소 비활성.
    [SerializeField]
    private GameObject heldBook;

    // 풀스크린 흰 오버레이(알파 0 시작). 아침빛 화이트아웃.
    [SerializeField]
    private CanvasGroup whiteout;

    // 암전 시점에 플레이어를 출구 문 정면으로 텔레포트할 기준점(출구 앞, 문 바라보는 방향).
    [SerializeField]
    private Transform exitStandPoint;

    [Header("타이밍")]
    // CorridorDoor의 페이드인(검정 1→0)이 복도+책을 드러낼 때까지 대기.
    [SerializeField]
    private float revealDelay = 1.2f;

    // 책 응시 전 잠시 정면을 본다.
    [SerializeField]
    private float preGazeHold = 0.5f;

    // 책 내려다보는 pitch(양수 = 아래). 책은 몸(허리)에 들려 있고 카메라(머리)만 숙여 응시.
    [SerializeField]
    private float gazePitchDegrees = 32f;

    [SerializeField]
    private float tiltDuration = 0.9f;

    // 책 응시 유지 시간.
    [SerializeField]
    private float gazeHold = 1.4f;

    // 다시 정면으로 올린 뒤 전진 시작 전 간격.
    [SerializeField]
    private float preWalkHold = 0.4f;

    [Header("출구로 전진 + 화이트아웃")]
    // 출구 문 쪽으로 전진하는 거리/시간.
    [SerializeField]
    private float walkDistance = 2.4f;

    [SerializeField]
    private float walkDuration = 3.0f;

    // 전진 진행도의 이 지점(0~1)부터 화이트아웃 시작 → 문에 다가가며 하얗게.
    [SerializeField]
    private float whiteoutStartFraction = 0.1f;

    // 걷는 흔들림(보빙) 진폭/빈도 — 딱딱하지 않게.
    [SerializeField]
    private float walkBobAmount = 0.035f;

    [SerializeField]
    private float walkBobFrequency = 1.7f;

    private bool cleared;

    private void Awake()
    {
        // 에디터에서 책/화이트아웃을 켜둔 채 플레이를 시작해도 강제로 초기 상태로.
        // 엔딩 시퀀스에서만 등장해야 한다.
        if (heldBook != null && heldBook.activeSelf)
            heldBook.SetActive(false);

        if (whiteout != null)
            whiteout.alpha = 0f;
    }

    public void TriggerClear()
    {
        if (cleared)
            return;
        cleared = true;

        flameDisplay?.LightAll();

        // 암전 상태(페이드 미드포인트)에서 출구 문 정면으로 정렬 — 전환은 안 보인다.
        TeleportToExit();

        if (playerController != null)
            playerController.enabled = false;

        StartCoroutine(EndingRoutine());
    }

    // 플레이어를 출구 문 앞 정면으로 텔레포트(CharacterController 토글 + 카메라 정면 리셋).
    private void TeleportToExit()
    {
        if (playerController == null || exitStandPoint == null)
            return;

        CharacterController cc = playerController.GetComponent<CharacterController>();
        bool ccEnabled = cc != null && cc.enabled;
        if (cc != null)
            cc.enabled = false;

        playerController.transform.SetPositionAndRotation(
            exitStandPoint.position,
            Quaternion.Euler(0f, exitStandPoint.eulerAngles.y, 0f)
        );

        if (cc != null)
            cc.enabled = ccEnabled;

        playerController.ResetLook();
    }

    private IEnumerator EndingRoutine()
    {
        // 손에 든 책 등장(몸에 들려 시야 하단에 위치). 페이드인으로 드러나고, 이후 카메라가 숙여 응시.
        if (heldBook != null)
            heldBook.SetActive(true);

        if (whiteout != null)
            whiteout.alpha = 0f;

        // CorridorDoor의 페이드인이 복도+책을 드러낼 때까지 대기.
        yield return Wait(revealDelay);

        // 책 응시 연출.
        if (cameraTransform != null)
        {
            yield return Wait(preGazeHold);

            Quaternion forwardRot = cameraTransform.localRotation;
            Quaternion gazeRot = Quaternion.Euler(gazePitchDegrees, 0f, 0f);

            yield return TiltCamera(forwardRot, gazeRot, tiltDuration); // 책 내려다보기
            yield return Wait(gazeHold);
            yield return TiltCamera(gazeRot, forwardRot, tiltDuration); // 다시 정면
            yield return Wait(preWalkHold);
        }

        // 출구 문 쪽으로 천천히 전진하며 화이트아웃.
        yield return WalkToExit();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 흰 화면 위 자막 먼저, 버튼은 ClearScreenController가 지연 노출.
        clearScreen?.Show();
    }

    // 출구 문 향해 전진 + 보빙 + 후반 화이트아웃. 사실감 위해 ease-in-out.
    private IEnumerator WalkToExit()
    {
        Vector3 camBaseLocal = cameraTransform != null ? cameraTransform.localPosition : Vector3.zero;
        float startAlpha = whiteout != null ? whiteout.alpha : 0f;

        Vector3 startPos = Vector3.zero;
        Vector3 endPos = Vector3.zero;
        bool canWalk = playerController != null;
        if (canWalk)
        {
            startPos = playerController.transform.position;
            Vector3 dir = playerController.transform.forward;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            endPos = startPos + dir * walkDistance;
        }

        float elapsed = 0f;
        while (elapsed < walkDuration)
        {
            float t = elapsed / walkDuration;

            if (canWalk)
            {
                float moveT = Mathf.SmoothStep(0f, 1f, t); // 가속/감속
                playerController.transform.position = Vector3.Lerp(startPos, endPos, moveT);
            }

            if (cameraTransform != null)
            {
                // 걷는 상하/좌우 흔들림.
                float bobY = Mathf.Sin(t * walkBobFrequency * Mathf.PI * 2f) * walkBobAmount;
                float swayX = Mathf.Cos(t * walkBobFrequency * Mathf.PI) * walkBobAmount * 0.5f;
                cameraTransform.localPosition = camBaseLocal + new Vector3(swayX, bobY, 0f);
            }

            if (whiteout != null)
            {
                float wt = Mathf.InverseLerp(whiteoutStartFraction, 1f, t);
                whiteout.alpha = Mathf.SmoothStep(startAlpha, 1f, wt);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (canWalk)
            playerController.transform.position = endPos;
        if (cameraTransform != null)
            cameraTransform.localPosition = camBaseLocal;
        if (whiteout != null)
            whiteout.alpha = 1f;
    }

    // 타임스케일 무관 대기 (unscaledDeltaTime 누적).
    private IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // 부드러운 카메라 틸트 (ease-in-out).
    private IEnumerator TiltCamera(Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            cameraTransform.localRotation = Quaternion.Slerp(from, to, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cameraTransform.localRotation = to;
    }
}
