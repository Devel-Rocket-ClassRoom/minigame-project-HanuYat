using UnityEngine;

public class AnomalyTransformChange : AnomalyEffectBase
{
    [SerializeField]
    private Transform[] targets;

    [SerializeField]
    private Vector3 positionOffset = new Vector3(0.3f, 0f, 0f);

    [SerializeField]
    private Vector3 rotationOffset = new Vector3(0f, 15f, 0f);

    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;

    private void Awake()
    {
        originalPositions = new Vector3[targets.Length];
        originalRotations = new Quaternion[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            originalPositions[i] = targets[i].localPosition;
            originalRotations[i] = targets[i].localRotation;
        }
    }

    public override void Activate()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            targets[i].localPosition = originalPositions[i] + positionOffset;
            targets[i].localRotation = originalRotations[i] * Quaternion.Euler(rotationOffset);
        }
        Debug.Log("[Anomaly] A05 AnomalyTransformChange activated");
    }

    public override void Deactivate()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;
            targets[i].localPosition = originalPositions[i];
            targets[i].localRotation = originalRotations[i];
        }
    }
}
