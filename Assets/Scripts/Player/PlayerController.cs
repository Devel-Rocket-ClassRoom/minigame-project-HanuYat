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

    public bool IsCrouching => isCrouching;

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
            isCrouching ? crouchSpeed
            : sprintAction.action.IsPressed() ? sprintSpeed
            : walkSpeed;

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
        float radius = controller.radius;
        Vector3 checkCenter = transform.position + Vector3.up * (standHeight - radius);
        Collider[] hits = Physics.OverlapSphere(
            checkCenter,
            radius,
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
