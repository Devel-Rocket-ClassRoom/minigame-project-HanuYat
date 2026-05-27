using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour
{
    [SerializeField]
    private string mainMenuSceneName = "MainMenu Scene";

    [SerializeField]
    private InputActionReference cancelAction;

    [SerializeField]
    private FadeController fadeController;

    [SerializeField]
    private PlayerController playerController;

    private bool isReturning;

    private void OnEnable()
    {
        if (cancelAction == null || cancelAction.action == null)
        {
            Debug.LogError("[GameSceneController] cancelAction 미할당 — 비활성.", this);
            enabled = false;
            return;
        }
        cancelAction.action.Enable();
        cancelAction.action.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.performed -= OnCancelPerformed;
            cancelAction.action.Disable();
        }
    }

    private void OnCancelPerformed(InputAction.CallbackContext _)
    {
        if (isReturning)
            return;
        isReturning = true;

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fadeController != null)
            fadeController.StartTransition(() => SceneManager.LoadScene(mainMenuSceneName));
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
