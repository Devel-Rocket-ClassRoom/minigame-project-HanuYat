using System.Collections.Generic;
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

        AnomalyEffectBase picked;
        if (candidates.Count <= 1)
        {
            picked = candidates[0];
        }
        else
        {
            int index;
            int attempts = 0;
            do
            {
                index = Random.Range(0, candidates.Count);
                attempts++;
            } while (candidates[index] == previousAnomaly && attempts < candidates.Count);
            picked = candidates[index];
        }

        currentAnomaly = picked;
        currentAnomaly.Activate();
    }
}
