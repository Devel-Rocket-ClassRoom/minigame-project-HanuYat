using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField]
    private InputActionReference pauseAction;

    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private FadeController fadeController;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu Scene";

    private bool isPaused;
    private bool isReturningToMenu;

    private void OnEnable()
    {
        if (pauseAction == null || pauseAction.action == null)
        {
            Debug.LogError("[PauseController] pauseAction 미할당 — 비활성.", this);
            enabled = false;
            return;
        }
        pauseAction.action.Enable();
        pauseAction.action.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnPausePerformed(InputAction.CallbackContext _)
    {
        if (isReturningToMenu)
            return;
        TogglePause();
    }

    private void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (playerController != null)
            playerController.enabled = false;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        StartCoroutine(LockCursorNextFrame());
    }

    private System.Collections.IEnumerator LockCursorNextFrame()
    {
        yield return null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMainMenu()
    {
        if (isReturningToMenu)
            return;
        isReturningToMenu = true;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            playerController.enabled = false;

        if (fadeController != null)
            fadeController.StartTransition(() => SceneManager.LoadScene(mainMenuSceneName));
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
