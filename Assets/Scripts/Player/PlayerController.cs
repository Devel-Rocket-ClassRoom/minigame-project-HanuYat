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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;
        pitch = cameraTransform != null ? cameraTransform.localEulerAngles.x : 0f;
        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        cameraStandLocalY = cameraTransform != null ? cameraTransform.localPosition.y : 1.6f;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        sprintAction.action.Enable();
        crouchAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        sprintAction.action.Disable();
        crouchAction.action.Disable();
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
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
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
        return !Physics.CheckSphere(checkCenter, radius, ~0, QueryTriggerInteraction.Ignore);
    }
}
