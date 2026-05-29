using UnityEngine;

// 현재 턴의 책 회수 여부를 보유하는 단일 진실원천(게이트가 읽는 표면).
// 상태는 BookPickup 정적 이벤트를 구독해서만 갱신한다.
public class TurnObjective : MonoBehaviour
{
    public static TurnObjective Instance { get; private set; }

    public bool IsBookCollected { get; private set; }

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

    private void OnEnable()
    {
        BookPickup.OnCollected += HandleCollected;
        BookPickup.OnReset += HandleReset;
    }

    private void OnDisable()
    {
        BookPickup.OnCollected -= HandleCollected;
        BookPickup.OnReset -= HandleReset;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void HandleCollected()
    {
        IsBookCollected = true;
    }

    private void HandleReset()
    {
        IsBookCollected = false;
    }
}
