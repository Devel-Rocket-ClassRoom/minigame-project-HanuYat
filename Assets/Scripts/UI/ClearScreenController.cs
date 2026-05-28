using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClearScreenController : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private Button restartButton;

    [SerializeField]
    private Button mainMenuButton;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu Scene";

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        restartButton?.onClick.AddListener(OnRestart);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    private void OnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
