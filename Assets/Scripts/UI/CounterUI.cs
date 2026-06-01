using UnityEngine;

// TMP 카운터 로직 셸 — 렌더는 FlameCounterDisplay에 위임.
// JudgementSystem 표면(Goal 게터, UpdateCounter(int)) 유지.
public class CounterUI : MonoBehaviour
{
    [SerializeField]
    private int goal = 8;

    [SerializeField]
    private FlameCounterDisplay flameDisplay;

    public int Goal => goal;

    private int current;

    public void UpdateCounter(int newValue)
    {
        current = Mathf.Max(0, newValue);
        flameDisplay?.Render(current);
    }

    [ContextMenu("Test +1")]
    private void TestIncrement() => UpdateCounter(current + 1);

    [ContextMenu("Test Reset")]
    private void TestReset() => UpdateCounter(0);
}
