using UnityEngine;

// 첫 루프백(OnCorridorEntered 첫 발화) 시점에 벽 종이를 활성화하고 구독을 끊는다.
// paperRoot는 씬에서 비활성으로 시작 — Awake/FlameCounterDisplay 인스턴스화는
// SetActive(true) 직후 동기 실행됨.
public class TutorialPaperReveal : MonoBehaviour
{
    // 종이가 실제로 등장한 순간 1회 발화 — 클리어 타이머 시작 신호.
    public static event System.Action OnRevealed;

    [SerializeField]
    private GameObject paperRoot;

    [SerializeField]
    private FlameCounterDisplay flameDisplay;

    [SerializeField]
    private JudgementSystem judgement;

    private void Awake()
    {
        // 에디터에서 종이를 켜둔 채 플레이를 시작해도, 시작 시 강제로 숨긴다.
        // 종이는 첫 루프백(OnCorridorEntered 첫 발화)에만 등장해야 한다.
        // (활성 상태로 시작되면 FlameCounterDisplay.Awake가 이미 불꽃을 생성하지만,
        //  여기서 비활성화해도 인스턴스는 보존 → 등장 시 재생성 없이 그대로 표시됨.)
        if (paperRoot != null && paperRoot.activeSelf)
            paperRoot.SetActive(false);
    }

    private void OnEnable()
    {
        CorridorDoor.OnCorridorEntered += HandleFirstEntry;
    }

    private void OnDisable()
    {
        CorridorDoor.OnCorridorEntered -= HandleFirstEntry;
    }

    private void HandleFirstEntry()
    {
        // 이후 발화엔 반응하지 않도록 즉시 해제.
        CorridorDoor.OnCorridorEntered -= HandleFirstEntry;

        if (paperRoot == null)
            return;

        paperRoot.SetActive(true);

        // 종이 등장 = 클리어 타이머 시작 시점.
        OnRevealed?.Invoke();

        // 종이 활성화 직후 FlameCounterDisplay.Awake가 동기 실행됨.
        // 현재 카운터(첫 턴 억제로 0)에 맞춰 동기화.
        if (flameDisplay != null && judgement != null)
            flameDisplay.Render(judgement.Current);

        // "분명 나갔는데" → "왼쪽 벽에 종이가" 순차 안내 → 종이로 유도.
        HintMessage.Instance?.ShowLoopbackSequence();
    }
}
