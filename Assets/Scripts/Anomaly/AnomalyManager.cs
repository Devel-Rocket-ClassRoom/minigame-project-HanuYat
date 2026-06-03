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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Refresh()
    {
        currentAnomaly?.Deactivate();
        AnomalyEffectBase lastPicked = currentAnomaly;
        currentAnomaly = null;

        // Inspector 빈 슬롯(null) 제거 — null 후보가 필터 통과 시 picked=null → Activate() NRE 방지.
        List<AnomalyEffectBase> valid = candidates.Where(c => c != null).ToList();
        if (valid.Count == 0 || Random.value >= anomalyProbability)
            return;

        List<AnomalyEffectBase> pool = valid.Where(c => !recentlyUsed.Contains(c)).ToList();
        if (pool.Count == 0)
        {
            // 사이클 완료 — 직전 발생 1개만 유지해 경계에서 즉시 재출현 방지
            recentlyUsed.Clear();
            if (lastPicked != null)
                recentlyUsed.Add(lastPicked);
            pool = valid.Where(c => !recentlyUsed.Contains(c)).ToList();
            if (pool.Count == 0)
                pool = valid;
        }
        AnomalyEffectBase picked = pool[Random.Range(0, pool.Count)];

        currentAnomaly = picked;
        recentlyUsed.Add(picked);
        currentAnomaly.Activate();
    }
}
