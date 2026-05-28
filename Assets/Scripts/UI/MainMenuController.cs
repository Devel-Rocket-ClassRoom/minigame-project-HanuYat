using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "Game Scene";

    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private FadeController fadeController;

    private bool isStarting;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnStartClicked()
    {
        if (isStarting)
            return;
        isStarting = true;

        if (fadeController != null)
            fadeController.StartTransition(() => SceneManager.LoadScene(gameSceneName));
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void OnSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
