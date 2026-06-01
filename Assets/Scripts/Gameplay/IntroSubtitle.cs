using System.Collections;
using Michsky.UI.Dark;
using UnityEngine;
using UnityEngine.InputSystem;

// 게임 씬 시작 시퀀스: 페이드인 → 조작키 안내 모달(잠시 뒤 "아무 키" 프롬프트 등장, 그때부터 입력 수락) → 인트로 자막.
public class IntroSubtitle : MonoBehaviour
{
    // 게임 씬 페이드인(약 1s) 끝난 뒤 시작.
    [SerializeField]
    private float delaySeconds = 1.2f;

    // 조작키 안내 모달(Dark UI). 비우면 바로 인트로 자막으로.
    [SerializeField]
    private ModalWindowManager controlsModal;

    // 깜빡이는 "아무 키나 눌러 계속" 프롬프트. promptDelay 뒤 활성화되며, 이때부터 입력 수락.
    // (진입 직후 키 연타로 즉시 스킵되는 것 방지)
    [SerializeField]
    private GameObject controlsPrompt;

    // 모달 표시 후 프롬프트 등장(=입력 수락 시작)까지 지연.
    [SerializeField]
    private float promptDelay = 1.2f;

    // 조작키 모달 닫은 뒤 인트로 자막까지 간격.
    [SerializeField]
    private float gapAfterControls = 0.3f;

    // 시작 시퀀스 동안 비활성화할 플레이어. 조작/시점 차단 + (PauseController가 player 비활성 시
    // ESC pause를 막으므로) 설정창 차단까지 겸함.
    [SerializeField]
    private PlayerController playerController;

    private IEnumerator Start()
    {
        // 페이드인~조작키 모달 동안 플레이어 조작 차단.
        if (playerController != null)
            playerController.enabled = false;

        yield return new WaitForSecondsRealtime(delaySeconds);

        if (controlsModal != null)
        {
            controlsModal.ModalWindowIn();

            // 프롬프트 숨기고 promptDelay 동안 입력 무시 → 연타 스킵 방지.
            if (controlsPrompt != null)
                controlsPrompt.SetActive(false);

            yield return new WaitForSecondsRealtime(promptDelay);

            // 프롬프트 등장 = 입력 수락 시작.
            if (controlsPrompt != null)
                controlsPrompt.SetActive(true);

            while (!AnyPressed())
                yield return null;

            controlsModal.ModalWindowOut();
            yield return new WaitForSecondsRealtime(gapAfterControls);
        }

        // 조작키 모달 종료 → 플레이어 조작 복원.
        if (playerController != null)
            playerController.enabled = true;

        HintMessage.Instance?.ShowIntro();
    }

    // 아무 키(ESC 포함) 또는 좌클릭으로 모달 넘김.
    // ESC를 눌러도 설정창은 안 뜸 — 이 시점 PlayerController가 비활성이라
    // PauseController가 ESC pause를 차단(player 복원은 모달 종료 후).
    private static bool AnyPressed()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }
}
