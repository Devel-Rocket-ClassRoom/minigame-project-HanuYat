using System.Collections.Generic;
using UnityEngine;

public static class ResettableRegistry
{
    private static readonly HashSet<IResettable> items = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearOnPlayStart()
    {
        items.Clear();
    }

    public static void Register(IResettable r) => items.Add(r);

    public static void Unregister(IResettable r) => items.Remove(r);

    public static void ResetAll()
    {
        foreach (IResettable r in items)
        {
            r.ResetToDefault();
        }
    }
}
