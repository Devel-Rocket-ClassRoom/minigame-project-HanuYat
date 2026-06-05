using System.Collections.Generic;
using UnityEngine;

public static class ResettableRegistry
{
    private static readonly HashSet<IResettable> items = new();
    private static readonly List<IResettable> resetBuffer = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearOnPlayStart()
    {
        items.Clear();
        resetBuffer.Clear();
    }

    public static void Register(IResettable r) => items.Add(r);

    public static void Unregister(IResettable r) => items.Remove(r);

    public static void ResetAll()
    {
        resetBuffer.Clear();
        resetBuffer.AddRange(items);
        // 한 항목의 ResetToDefault throw가 나머지 리셋을 중단시키고 호출자(FadeController
        // OnMidpoint)로 전파돼 전역 soft-lock 되는 것 방지 — 항목별 격리.
        foreach (IResettable r in resetBuffer)
        {
            try
            {
                r.ResetToDefault();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
