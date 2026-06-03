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

    // 게임 시작 동기 자막 — 책을 학교에 두고 온 상황 전달, 1회.
    [SerializeField]
    private ModalWindowManager introModal;

    // 첫 턴 후진 차단 — "여긴 들어온 문이다, 출구로 나가자".
    [SerializeField]
    private ModalWindowManager entranceModal;

    // 교실 첫 입장 — 책상 위치 + 불 켜기 안내.
    [SerializeField]
    private ModalWindowManager classroomModal;

    // 첫 루프백 — "분명 나갔는데" → "왼쪽 벽에 종이가" 순차 표시.
    [SerializeField]
    private ModalWindowManager weirdModal;

    [SerializeField]
    private ModalWindowManager paperModal;

    // 기본 표시 시간. 아래 per-modal 값이 0 이하면 이 값 사용.
    [SerializeField]
    private float visibleDuration = 2f;

    [Header("모달별 표시 시간 (0 이하 = 기본값 사용)")]
    [SerializeField]
    private float lockedDuration;

    [SerializeField]
    private float darkDuration;

    [SerializeField]
    private float pickupDuration;

    [SerializeField]
    private float introDuration;

    [SerializeField]
    private float entranceDuration;

    [SerializeField]
    private float classroomDuration;

    [SerializeField]
    private float weirdDuration;

    [SerializeField]
    private float paperDuration;

    // 시퀀스 모달 사이 간격(아웃 애니메이션 여유).
    [SerializeField]
    private float sequenceGap = 0.4f;

    private Coroutine hideCoroutine;
    private Coroutine sequenceCoroutine;
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

    // 0 이하면 기본 visibleDuration 사용
    private float Dur(float d) => d > 0f ? d : visibleDuration;

    // 비활성 GO에 ModalWindowOut 호출 시 Dark UI가 inactive 오브젝트에서 코루틴을 시작하려다
    // 에러("Coroutine couldn't be started because the game object is inactive").
    // 다른 경로가 이미 모달을 닫아 비활성화했을 수 있으므로 활성일 때만 닫는다.
    private static void CloseModal(ModalWindowManager modal)
    {
        if (modal != null && modal.gameObject.activeInHierarchy)
            modal.ModalWindowOut();
    }

    // 문 잠김(책 미회수) 힌트
    public void ShowLocked()
    {
        ShowModal(lockedModal, Dur(lockedDuration));
    }

    // 어두움(불 꺼짐) 힌트
    public void ShowDark()
    {
        ShowModal(darkModal, Dur(darkDuration));
    }

    // 책 획득 알림
    public void ShowPickup()
    {
        ShowModal(pickupModal, Dur(pickupDuration));
    }

    // 게임 시작 동기 자막 (1회)
    public void ShowIntro()
    {
        ShowModal(introModal, Dur(introDuration));
    }

    // 첫 턴 후진 차단 — 들어온 문 안내
    public void ShowEntrance()
    {
        ShowModal(entranceModal, Dur(entranceDuration));
    }

    // 교실 첫 입장 — 책상 위치 + 불 켜기 안내
    public void ShowClassroom()
    {
        ShowModal(classroomModal, Dur(classroomDuration));
    }

    // 첫 루프백 — Weird → Paper 순차 표시 (왼쪽 벽 종이로 유도)
    public void ShowLoopbackSequence()
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        if (sequenceCoroutine != null)
            StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = StartCoroutine(LoopbackRoutine());
    }

    private IEnumerator LoopbackRoutine()
    {
        yield return ShowAndHold(weirdModal, Dur(weirdDuration));
        yield return new WaitForSecondsRealtime(sequenceGap);
        yield return ShowAndHold(paperModal, Dur(paperDuration));
        sequenceCoroutine = null;
    }

    private IEnumerator ShowAndHold(ModalWindowManager modal, float duration)
    {
        if (modal == null)
            yield break;
        active = modal;
        modal.ModalWindowIn();
        yield return new WaitForSecondsRealtime(duration);
        CloseModal(modal);
        if (active == modal)
            active = null;
    }

    private void ShowModal(ModalWindowManager modal, float duration)
    {
        if (modal == null)
            return;

        // 루프백 시퀀스(weird→paper) 진행 중이면 정지 — 잔존 시 신규 모달과 충돌.
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        // 다른 힌트가 떠 있으면 먼저 닫는다.
        if (active != null && active != modal)
            CloseModal(active);

        active = modal;
        modal.ModalWindowIn();

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(modal, duration));
    }

    private IEnumerator HideAfterDelay(ModalWindowManager modal, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        CloseModal(modal);
        if (active == modal)
            active = null;
        hideCoroutine = null;
    }
}
