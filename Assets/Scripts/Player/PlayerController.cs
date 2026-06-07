using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField]
    private InputActionReference moveAction;

    [SerializeField]
    private InputActionReference lookAction;

    [SerializeField]
    private InputActionReference sprintAction;

    [SerializeField]
    private InputActionReference crouchAction;

    [Header("References")]
    [SerializeField]
    private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField]
    private float walkSpeed = 3.0f;

    [SerializeField]
    private float sprintSpeed = 5.5f;

    [SerializeField]
    private float crouchSpeed = 1.5f;

    [SerializeField]
    private float gravity = -9.81f;

    [Header("Look")]
    [SerializeField]
    private float lookSensitivity = 0.15f;

    [SerializeField]
    private float pitchMin = -89f;

    [SerializeField]
    private float pitchMax = 89f;

    [Header("Cursor")]
    [SerializeField]
    private bool lockCursor = true;

    [Header("Crouch")]
    [SerializeField]
    private float standHeight = 2.0f;

    [SerializeField]
    private float crouchHeight = 1.0f;

    [SerializeField]
    private float crouchTransitionSpeed = 8f;

    private CharacterController controller;
    private float pitch;
    private float yaw;
    private Vector3 verticalVelocity;
    private bool isCrouching;
    private float cameraStandLocalY;
    private float initialCenterY;
    private float initialHeight;
    private float speedMultiplier = 1f;

    public bool IsCrouching => isCrouching;

    // 외부(예: 물 이상현상)에서 이동속도 배율 제어. 1 = 정상.
    public void SetSpeedMultiplier(float value) => speedMultiplier = Mathf.Max(0f, value);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            Debug.LogError("[PlayerController] cameraTransform 미할당 — 컴포넌트 비활성화.", this);
            enabled = false;
            return;
        }
        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        cameraStandLocalY = cameraTransform.localPosition.y;
        initialCenterY = controller.center.y;
        initialHeight = controller.height;
    }

    private void OnEnable()
    {
        if (
            !TryEnable(moveAction, nameof(moveAction))
            || !TryEnable(lookAction, nameof(lookAction))
            || !TryEnable(sprintAction, nameof(sprintAction))
            || !TryEnable(crouchAction, nameof(crouchAction))
        )
        {
            enabled = false;
        }

        if (SettingsManager.Instance != null)
        {
            lookSensitivity = SettingsManager.Instance.MouseSensitivity;
            SettingsManager.Instance.OnMouseSensitivityChanged += OnSensitivityChanged;
        }
    }

    private void OnDisable()
    {
        TryDisable(moveAction);
        TryDisable(lookAction);
        TryDisable(sprintAction);
        TryDisable(crouchAction);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnMouseSensitivityChanged -= OnSensitivityChanged;
    }

    private void OnSensitivityChanged(float value)
    {
        lookSensitivity = value;
    }

    private bool TryEnable(InputActionReference reference, string fieldName)
    {
        if (reference == null || reference.action == null)
        {
            Debug.LogError($"[PlayerController] {fieldName} 미할당 — 비활성.", this);
            return false;
        }
        reference.action.Enable();
        return true;
    }

    private void TryDisable(InputActionReference reference)
    {
        if (reference != null && reference.action != null)
            reference.action.Disable();
    }

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (crouchAction.action.WasPressedThisFrame())
        {
            if (isCrouching)
            {
                if (CanStand())
                    isCrouching = false;
            }
            else
            {
                isCrouching = true;
            }
        }

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        if (!Mathf.Approximately(controller.height, targetHeight))
        {
            controller.height = Mathf.MoveTowards(
                controller.height,
                targetHeight,
                crouchTransitionSpeed * Time.deltaTime
            );
            controller.center = new Vector3(
                0f,
                initialCenterY - (initialHeight - controller.height) * 0.5f,
                0f
            );
        }

        Vector2 move = moveAction.action.ReadValue<Vector2>();
        Vector3 horizontal = transform.right * move.x + transform.forward * move.y;
        float speed =
            (
                isCrouching ? crouchSpeed
                : sprintAction.action.IsPressed() ? sprintSpeed
                : walkSpeed
            ) * speedMultiplier;

        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        Vector3 motion = horizontal * speed + verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    private void LateUpdate()
    {
        Vector2 look = lookAction.action.ReadValue<Vector2>();
        yaw += look.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, pitchMin, pitchMax);
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float crouchCameraLocalY = cameraStandLocalY - (standHeight - crouchHeight);
        float targetCameraY = isCrouching ? crouchCameraLocalY : cameraStandLocalY;
        Vector3 cameraLocalPos = cameraTransform.localPosition;
        float newCamY = Mathf.MoveTowards(
            cameraLocalPos.y,
            targetCameraY,
            crouchTransitionSpeed * Time.deltaTime
        );
        if (!Mathf.Approximately(cameraLocalPos.y, newCamY))
        {
            cameraLocalPos.y = newCamY;
            cameraTransform.localPosition = cameraLocalPos;
        }
    }

    [ContextMenu("Reset Look")]
    public void ResetLook()
    {
        pitch = 0f;
        yaw = transform.eulerAngles.y;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
    }

    private bool CanStand()
    {
        // 세울 머리 끝(standHeight) 바로 아래 작은 probe. 반경을 캡슐 반경의 절반으로 좁히고
        // probe 상단이 standHeight에 닿도록 center를 올려, 머리 높이 천장은 그대로 검출하되
        // 옆 벽은 오검출하지 않는다 — 벽은 항상 캡슐 반경 이상 떨어져 있어(침투 방지) 좁은 probe엔 안 닿음.
        // (PR#49: self 필터는 유지)
        float radius = controller.radius;
        float probeRadius = radius * 0.5f;
        Vector3 checkCenter = transform.position + Vector3.up * (standHeight - probeRadius);
        Collider[] hits = Physics.OverlapSphere(
            checkCenter,
            probeRadius,
            ~0,
            QueryTriggerInteraction.Ignore
        );
        foreach (Collider hit in hits)
        {
            // 플레이어 자신의 CharacterController 캡슐은 무시 (천장 등 외부 장애물만 검사).
            if (hit != controller)
                return false;
        }
        return true;
    }
}
