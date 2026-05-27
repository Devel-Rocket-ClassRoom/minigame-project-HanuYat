using UnityEngine;

// 머티리얼 스왑은 sharedMaterial 참조 재할당으로 처리한다.
// sharedMaterial 의 method 호출 (SetColor/EnableKeyword 등)은 원본 asset 을 영구 변형하므로 금지.
// PR #40 에서 같은 이유로 ClassroomLightSwitch 의 MPB 시도가 롤백됨.
//
// swapInstances 는 anomalyMaterial 의 복제본을 renderer 마다 1개씩 미리 생성해 두는 캐시.
// 사이클 반복해도 추가 인스턴스 생성 안됨 → material leak 회피.
// OnDestroy 에서 우리 인스턴스만 파괴 (원본 anomalyMaterial asset 은 안건드림).
public class AnomalyMaterialSwap : AnomalyEffectBase
{
    [SerializeField]
    private Renderer[] targetRenderers;

    [SerializeField]
    private Material anomalyMaterial;

    private Material[] originalMaterials;
    private Material[] swapInstances;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            return;

        originalMaterials = new Material[targetRenderers.Length];
        swapInstances = new Material[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;
            originalMaterials[i] = targetRenderers[i].sharedMaterial;

            if (anomalyMaterial != null)
                swapInstances[i] = new Material(anomalyMaterial);
        }
    }

    public override void Activate()
    {
        if (anomalyMaterial == null || swapInstances == null)
            return;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null || swapInstances[i] == null)
                continue;
            targetRenderers[i].sharedMaterial = swapInstances[i];
        }
        Debug.Log("[Anomaly] A08 AnomalyMaterialSwap activated");
    }

    public override void Deactivate()
    {
        if (originalMaterials == null)
            return;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null || originalMaterials[i] == null)
                continue;
            targetRenderers[i].sharedMaterial = originalMaterials[i];
        }
    }

    private void OnDestroy()
    {
        if (swapInstances == null)
            return;
        foreach (Material m in swapInstances)
        {
            if (m != null)
                Destroy(m);
        }
    }
}
