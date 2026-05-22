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
    private AnomalyEffectBase previousAnomaly;

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
        previousAnomaly = currentAnomaly;
        currentAnomaly = null;

        if (candidates.Count == 0 || Random.value >= anomalyProbability)
            return;

        List<AnomalyEffectBase> pool = candidates.Where(c => c != previousAnomaly).ToList();
        if (pool.Count == 0)
            pool = candidates;
        AnomalyEffectBase picked = pool[Random.Range(0, pool.Count)];

        currentAnomaly = picked;
        currentAnomaly.Activate();
    }
}
