using System.Collections;
using UnityEngine;

public class AnomalyScaleChange : AnomalyEffectBase
{
    [SerializeField]
    private Transform[] targets;

    [SerializeField]
    private Vector3 scaleMultiplier = new Vector3(2f, 2f, 2f);

    [SerializeField]
    private float duration = 20f;

    private Vector3[] originalScales;
    private Vector3[] targetScales;
    private Coroutine scaleCoroutine;

    private void Awake()
    {
        originalScales = new Vector3[targets.Length];
        targetScales = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            originalScales[i] = targets[i].localScale;
            targetScales[i] = new Vector3(
                originalScales[i].x * scaleMultiplier.x,
                originalScales[i].y * scaleMultiplier.y,
                originalScales[i].z * scaleMultiplier.z
            );
        }
    }

    public override void Activate()
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(targetScales));
        Debug.Log("[Anomaly] A06 AnomalyScaleChange activated");
    }

    public override void Deactivate()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            targets[i].localScale = originalScales[i];
        }
    }

    private IEnumerator ScaleTo(Vector3[] to)
    {
        Vector3[] from = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            from[i] = targets[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                    continue;
                targets[i].localScale = Vector3.Lerp(from[i], to[i], t);
            }
            yield return null;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            targets[i].localScale = to[i];
        }
        scaleCoroutine = null;
    }
}
