using System;
using UnityEngine;

public class AnomalyBirds : AnomalyEffectBase
{
    [SerializeField]
    private GameObject[] birdRoots;

    [SerializeField]
    private BirdWander[] birdWanders;

    [SerializeField]
    private BirdDiver birdDiver;

    public static event Action OnPlayerAttacked;

    public static void RaisePlayerAttacked() => OnPlayerAttacked?.Invoke();

    public bool IsArmed { get; private set; }

    private void Awake()
    {
        foreach (var root in birdRoots)
            if (root != null)
                root.SetActive(false);
        if (birdDiver != null)
            birdDiver.gameObject.SetActive(false);
    }

    public override void Activate()
    {
        IsArmed = true;
        foreach (var root in birdRoots)
            if (root != null)
                root.SetActive(true);
        foreach (var wander in birdWanders)
            if (wander != null)
                wander.StartWander();
        Debug.Log("[Anomaly] A13 AnomalyBirds activated");
    }

    public override void Deactivate()
    {
        IsArmed = false;
        foreach (var wander in birdWanders)
            if (wander != null)
                wander.StopWander();
        foreach (var root in birdRoots)
            if (root != null)
                root.SetActive(false);
        if (birdDiver != null)
            birdDiver.gameObject.SetActive(false);
    }
}
