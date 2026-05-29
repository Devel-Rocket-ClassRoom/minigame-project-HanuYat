using System.Collections;
using Michsky.UI.Dark;
using UnityEngine;

// Dark - Complete Horror UI 에셋의 Modal Window를 감싸는 얇은 래퍼.
// 잠금 힌트(문)와 어둠 힌트(책)는 각각 별도의 Modal GameObject를 쓴다 — 메시지는 각 모달의 description에 사전 세팅.
// 이 컴포넌트는 Modal 루트가 아닌 항상 활성인 오브젝트(예: Canvas)에 둔다 —
// ModalWindowManager가 Out 후 GameObject를 SetActive(false) 하므로, 같은 곳에 두면 코루틴이 멈춘다.
public class HintMessage : MonoBehaviour
{
    public static HintMessage Instance { get; private set; }

    [SerializeField]
    private ModalWindowManager lockedModal;

    [SerializeField]
    private ModalWindowManager darkModal;

    // 책 획득 알림 — 배경 없이 텍스트만, 작게.
    [SerializeField]
    private ModalWindowManager pickupModal;

    [SerializeField]
    private float visibleDuration = 2f;

    private Coroutine hideCoroutine;
    private ModalWindowManager active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 문 잠김(책 미회수) 힌트
    public void ShowLocked()
    {
        ShowModal(lockedModal);
    }

    // 어두움(불 꺼짐) 힌트
    public void ShowDark()
    {
        ShowModal(darkModal);
    }

    // 책 획득 알림
    public void ShowPickup()
    {
        ShowModal(pickupModal);
    }

    private void ShowModal(ModalWindowManager modal)
    {
        if (modal == null)
            return;

        // 다른 힌트가 떠 있으면 먼저 닫는다.
        if (active != null && active != modal)
            active.ModalWindowOut();

        active = modal;
        modal.ModalWindowIn();

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(modal));
    }

    private IEnumerator HideAfterDelay(ModalWindowManager modal)
    {
        yield return new WaitForSecondsRealtime(visibleDuration);
        modal.ModalWindowOut();
        if (active == modal)
            active = null;
        hideCoroutine = null;
    }
}
