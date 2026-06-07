using UnityEngine;

// A09 정적 NPC: 평소 비활성인 NPC(Humanoid idle)를 복도 끝에 등장시킨다.
// 단순 SetActive 토글 — Animator 가 기본 Idle 상태를 재생한다.
public class AnomalyNPC : AnomalyEffectBase
{
    [SerializeField]
    private GameObject npc;

    private void Awake()
    {
        // 씬에서 실수로 활성 상태로 뒀을 때를 대비한 안전 초기화
        if (npc != null)
            npc.SetActive(false);
    }

    public override void Activate()
    {
        if (npc != null)
            npc.SetActive(true);
        AnomalyLog.Activated("A09 AnomalyNPC");
    }

    public override void Deactivate()
    {
        if (npc != null)
            npc.SetActive(false);
    }
}
