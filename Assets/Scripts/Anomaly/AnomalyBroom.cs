using UnityEngine;

// A11 동적 이상: 빗자루 자동 청소 on/off.
// Activate → BroomSweep 켜서 빗자루가 교실을 쓸며 배회.
// Deactivate → BroomSweep 끄고(agent disable 후) 원래 벽에 기댄 transform 복원.
// 루프 재추첨 시 잔상 방지를 위해 복원은 필수.
public class AnomalyBroom : AnomalyEffectBase
{
    [SerializeField]
    private BroomSweep broomSweep;

    private Transform broomTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Awake()
    {
        if (broomSweep != null)
        {
            broomTransform = broomSweep.transform;
            originalPosition = broomTransform.position;
            originalRotation = broomTransform.rotation;
            broomSweep.SetSweeping(false);
        }
    }

    public override void Activate()
    {
        if (broomSweep != null)
            broomSweep.SetSweeping(true);
        AnomalyLog.Activated("A11 AnomalyBroom");
    }

    public override void Deactivate()
    {
        if (broomSweep == null)
            return;
        broomSweep.SetSweeping(false); // 내부에서 agent.enabled=false
        // agent 비활성 후 하드 복원 (agent 켜진 채 복원하면 덮어써짐)
        if (broomTransform != null)
            broomTransform.SetPositionAndRotation(originalPosition, originalRotation);
    }
}
