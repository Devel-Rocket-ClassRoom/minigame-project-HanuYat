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

    [Header("SFX (선택)")]
    // ESC 토글음 전용. 패널 버튼 클릭음은 ButtonManager가 별도 처리(중복 방지 위해 버튼 Resume엔 미적용).
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip toggleClip;

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
        // 페이드 전환 / Ghost·Bird·Exit 시퀀스 중 = player 비활성 → pause 차단 (resume은 허용).
        if (!isPaused && playerController != null && !playerController.enabled)
            return;
        TogglePause();
    }

    private void TogglePause()
    {
        // ESC 경로 전용 토글음 — 버튼 클릭 Resume은 ButtonManager가 따로 울려 중복 방지.
        if (audioSource != null && toggleClip != null)
            audioSource.PlayOneShot(toggleClip);

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
