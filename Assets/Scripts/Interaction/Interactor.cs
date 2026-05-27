using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [SerializeField]
    private InputActionReference interactAction;

    [SerializeField]
    private float maxDistance = 1.5f;

    [SerializeField]
    private LayerMask interactableLayer;

    private InteractableOutline currentHover;
    private IInteractable currentInteractable;

    private void OnEnable()
    {
        if (interactAction == null || interactAction.action == null)
        {
            Debug.LogError("[Interactor] interactAction 미할당 — 비활성.", this);
            enabled = false;
            return;
        }
        interactAction.action.Enable();
        interactAction.action.started += OnInteractPressed;
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.started -= OnInteractPressed;
            interactAction.action.Disable();
        }
        ClearHover();
    }

    private void Update()
    {
        InteractableOutline hoverHit = null;
        IInteractable interactHit = null;

        // 벽 등 모든 콜라이더에 대해 Raycast — 가장 가까운 것에 닿게.
        // 닿은 오브젝트가 Interactable 레이어일 때만 hover/interact 활성.
        if (
            Physics.Raycast(
                transform.position,
                transform.forward,
                out RaycastHit hit,
                maxDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            int hitLayerBit = 1 << hit.collider.gameObject.layer;
            if ((hitLayerBit & interactableLayer.value) != 0)
            {
                hoverHit = hit.collider.GetComponentInParent<InteractableOutline>();
                interactHit = hit.collider.GetComponentInParent<IInteractable>();
            }
        }

        if (hoverHit != currentHover)
        {
            if (currentHover != null)
            {
                currentHover.SetHovered(false);
            }
            if (hoverHit != null)
            {
                hoverHit.SetHovered(true);
            }
            currentHover = hoverHit;
        }

        currentInteractable = interactHit;
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        currentInteractable?.Interact();
    }

    private void ClearHover()
    {
        if (currentHover != null)
        {
            currentHover.SetHovered(false);
        }
        currentHover = null;
        currentInteractable = null;
    }
}
