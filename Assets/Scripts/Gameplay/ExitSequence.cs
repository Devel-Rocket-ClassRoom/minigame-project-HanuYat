using UnityEngine;

public class ExitSequence : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private ClearScreenController clearScreen;

    private bool cleared;

    public void TriggerClear()
    {
        if (cleared)
            return;
        cleared = true;

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        clearScreen?.Show();
    }
}
