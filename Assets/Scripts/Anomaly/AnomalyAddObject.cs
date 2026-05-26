using UnityEngine;

public class AnomalyAddObject : AnomalyEffectBase
{
    [SerializeField]
    private GameObject target;

    private void Awake()
    {
        // 씬에서 실수로 활성 상태로 뒀을 때를 대비한 안전 초기화
        if (target != null)
            target.SetActive(false);
    }

    public override void Activate()
    {
        if (target != null)
            target.SetActive(true);
        Debug.Log("[Anomaly] A01 AnomalyAddObject activated");
    }

    public override void Deactivate()
    {
        if (target != null)
            target.SetActive(false);
    }
}
