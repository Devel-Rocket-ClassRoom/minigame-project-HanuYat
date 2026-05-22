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

    private bool isReturning;

    private void OnEnable()
    {
        cancelAction.action.Enable();
        cancelAction.action.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        cancelAction.action.performed -= OnCancelPerformed;
        cancelAction.action.Disable();
    }

    private void OnCancelPerformed(InputAction.CallbackContext _)
    {
        if (isReturning)
            return;
        isReturning = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fadeController != null)
            fadeController.StartTransition(() => SceneManager.LoadScene(mainMenuSceneName));
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
