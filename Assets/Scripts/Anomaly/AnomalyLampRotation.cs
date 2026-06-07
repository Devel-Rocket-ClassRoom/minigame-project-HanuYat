using UnityEngine;

public class AnomalyLampRotation : AnomalyEffectBase
{
    [SerializeField]
    private Transform[] targets;

    private Quaternion[] originalRotations;

    private void Awake()
    {
        if (targets == null)
            targets = new Transform[0];
        originalRotations = new Quaternion[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            originalRotations[i] = targets[i].localRotation;
        }
    }

    public override void Activate()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            targets[i].localRotation = originalRotations[i] * Quaternion.Euler(0f, 0f, 90f);
        }
        Debug.Log("[Anomaly] LampRotation activated");
    }

    public override void Deactivate()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            targets[i].localRotation = originalRotations[i];
        }
    }
}
