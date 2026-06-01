using UnityEngine;

// 종이 위 도깨비불(귀신불) 진행도 트래커. 화면 TMP 카운터를 대체하는 디제틱 표시.
// 슬롯마다 불꽃 인스턴스를 생성해 두고, Render(current)로 앞 current개만 점화한다.
// 종이가 비활성으로 시작하므로 Awake 미실행 상태에서 호출될 수 있다 → Render는 가드 필수.
public class FlameCounterDisplay : MonoBehaviour
{
    [SerializeField]
    private Transform[] slots;

    [SerializeField]
    private GameObject flamePrefab;

    // 불꽃 인스턴스 로컬 스케일 — 슬롯 스케일과 분리(슬롯엔 항상 보이는 마커가 따로 들어감).
    [SerializeField]
    private float flameLocalScale = 0.035f;

    // 불꽃 인스턴스 로컬 위치 오프셋 — 마커/종이 표면에 맞춰 미세조정(주로 z 깊이).
    // 불꽃은 플레이 모드에서만 생성되므로 에디터에서 이 값으로 위치를 조정한다.
    [SerializeField]
    private Vector3 flameLocalOffset = Vector3.zero;

    [Header("SFX (선택)")]
    [SerializeField]
    private AudioSource igniteSfx;

    [SerializeField]
    private AudioSource extinguishSfx;

    private GameObject[] flames;
    private bool initialized;
    private int lastCount;

    private void Awake()
    {
        if (slots == null || flamePrefab == null)
            return;

        flames = new GameObject[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            GameObject flame = Instantiate(flamePrefab, slots[i]);
            flame.transform.localPosition = flameLocalOffset;
            flame.transform.localRotation = Quaternion.identity;
            flame.transform.localScale = Vector3.one * flameLocalScale;

            // 파티클 시뮬레이션을 Local로 강제 — 토치 프리팹 기본값 World는
            // 슬롯 스케일을 줄여도 상승 속도/거리를 월드 단위로 유지해 가늘고 긴 기둥이 된다.
            // Local이면 트랜스폼 스케일에 맞춰 작은 도깨비불로 수렴.
            foreach (ParticleSystem ps in flame.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }

            flame.SetActive(false);
            flames[i] = flame;
        }

        initialized = true;
        lastCount = 0;
    }

    // 전 슬롯 점화 (클리어 시점 등). goal 값 의존 없이 슬롯 수 기준.
    public void LightAll()
    {
        if (!initialized || flames == null)
            return;
        Render(flames.Length);
    }

    // 앞 current개 슬롯 점화, 나머지 소멸. 새로 켜진 슬롯/전체 소멸에만 SFX.
    public void Render(int current)
    {
        // 종이 미등장(비활성)·미초기화 상태 가드 — NRE 방지.
        if (!initialized || flames == null)
            return;

        current = Mathf.Clamp(current, 0, flames.Length);

        for (int i = 0; i < flames.Length; i++)
        {
            if (flames[i] != null)
                flames[i].SetActive(i < current);
        }

        bool newlyLit = current > lastCount;
        bool allOut = current == 0 && lastCount > 0;

        if (newlyLit && igniteSfx != null)
            igniteSfx.Play();
        else if (allOut && extinguishSfx != null)
            extinguishSfx.Play();
        // 초기 0-렌더(lastCount==0 && current==0)는 위 두 조건 모두 거짓 → 무음.

        lastCount = current;
    }
}
