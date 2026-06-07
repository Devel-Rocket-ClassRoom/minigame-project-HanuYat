using System.Diagnostics;
using Debug = UnityEngine.Debug;

// 이상현상 활성 로그 — 에디터에서만 출력된다.
// [Conditional("UNITY_EDITOR")] 이므로 릴리스(플레이어) 빌드에선 호출 자체가 컴파일 단계에서 스트립됨
// → Player.log로 "어떤 이상현상이 켜졌는지" 정답이 새지 않는다. (Exit 8류 탐지 메커니즘 스포일 방지)
public static class AnomalyLog
{
    [Conditional("UNITY_EDITOR")]
    public static void Activated(string label) => Debug.Log($"[Anomaly] {label} activated");
}
