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
        foreach (IResettable r in resetBuffer)
            r.ResetToDefault();
    }
}
