using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnomalyManager : MonoBehaviour
{
    public static AnomalyManager Instance { get; private set; }

    [SerializeField, Range(0f, 1f)]
    private float anomalyProbability = 0.5f;

    [SerializeField]
    private List<AnomalyEffectBase> candidates = new();

    private AnomalyEffectBase currentAnomaly;
    private readonly HashSet<AnomalyEffectBase> recentlyUsed = new();

    public bool IsAnomalyActive => currentAnomaly != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Refresh()
    {
        currentAnomaly?.Deactivate();
        AnomalyEffectBase lastPicked = currentAnomaly;
        currentAnomaly = null;

        if (candidates.Count == 0 || Random.value >= anomalyProbability)
            return;

        List<AnomalyEffectBase> pool = candidates.Where(c => !recentlyUsed.Contains(c)).ToList();
        if (pool.Count == 0)
        {
            // 사이클 완료 — 직전 발생 1개만 유지해 경계에서 즉시 재출현 방지
            recentlyUsed.Clear();
            if (lastPicked != null)
                recentlyUsed.Add(lastPicked);
            pool = candidates.Where(c => !recentlyUsed.Contains(c)).ToList();
            if (pool.Count == 0)
                pool = candidates;
        }
        AnomalyEffectBase picked = pool[Random.Range(0, pool.Count)];

        currentAnomaly = picked;
        recentlyUsed.Add(picked);
        currentAnomaly.Activate();
    }
}
