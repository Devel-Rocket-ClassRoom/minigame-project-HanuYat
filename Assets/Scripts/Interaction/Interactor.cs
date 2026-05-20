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

    private void OnEnable()
    {
        interactAction.action.Enable();
        interactAction.action.started += OnInteractPressed;
    }

    private void OnDisable()
    {
        interactAction.action.started -= OnInteractPressed;
        interactAction.action.Disable();
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (
            Physics.Raycast(
                transform.position,
                transform.forward,
                out RaycastHit hit,
                maxDistance,
                interactableLayer
            )
        )
        {
            hit.collider.GetComponentInParent<IInteractable>()?.Interact();
        }
    }
}
