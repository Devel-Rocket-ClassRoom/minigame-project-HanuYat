using UnityEngine;

public class AnomalyRemoveObject : AnomalyEffectBase
{
    [SerializeField]
    private GameObject[] targets;

    private void Awake()
    {
        foreach (GameObject t in targets)
            if (t != null)
                t.SetActive(true);
    }

    public override void Activate()
    {
        foreach (GameObject t in targets)
            if (t != null)
                t.SetActive(false);
        AnomalyLog.Activated("A02 AnomalyRemoveObject");
    }

    public override void Deactivate()
    {
        foreach (GameObject t in targets)
            if (t != null)
                t.SetActive(true);
    }
}
