using UnityEngine;

public class ExitSequence : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private ClearScreenController clearScreen;

    // 마지막 슬롯 점화 — TriggerClear는 페이드 미드포인트(화면 암전) 시점이라 시각 효과는 미미.
    // 상태 정합성 유지 목적. (goal 값 의존 없이 전 슬롯 점화)
    [SerializeField]
    private FlameCounterDisplay flameDisplay;

    private bool cleared;

    public void TriggerClear()
    {
        if (cleared)
            return;
        cleared = true;

        flameDisplay?.LightAll();

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        clearScreen?.Show();
    }
}
