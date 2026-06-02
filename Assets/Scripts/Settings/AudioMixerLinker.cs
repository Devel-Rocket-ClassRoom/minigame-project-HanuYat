using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// SettingsManager 볼륨 이벤트를 AudioMixer exposed parameter로 연결한다.
/// 씬마다 사용하는 AudioMixer가 다를 수 있으므로 파라미터 이름을 Inspector에서 지정.
/// </summary>
public class AudioMixerLinker : MonoBehaviour
{
    [SerializeField]
    private AudioMixer mixer;

    [Header("Exposed Parameter Names")]
    [SerializeField]
    private string masterParam = "Master";

    [SerializeField]
    private string bgmParam = "Music";

    [SerializeField]
    private string sfxParam = "SFX";

    private void Start()
    {
        if (mixer == null)
        {
            Debug.LogWarning("[AudioMixerLinker] mixer not assigned.");
            return;
        }

        // Start()에서 실행 — 이 시점엔 SettingsManager.Awake() 완료 보장.
        if (SettingsManager.Instance != null)
        {
            Apply(masterParam, SettingsManager.Instance.MasterVolume);
            Apply(bgmParam, SettingsManager.Instance.BgmVolume);
            Apply(sfxParam, SettingsManager.Instance.SfxVolume);
        }
        else
        {
            Debug.LogWarning(
                "[AudioMixerLinker] SettingsManager.Instance null — 볼륨 초기화 스킵."
            );
        }

        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (SettingsManager.Instance == null)
            return;
        SettingsManager.Instance.OnMasterVolumeChanged += OnMasterChanged;
        SettingsManager.Instance.OnBgmVolumeChanged += OnBgmChanged;
        SettingsManager.Instance.OnSfxVolumeChanged += OnSfxChanged;
    }

    private void UnsubscribeEvents()
    {
        if (SettingsManager.Instance == null)
            return;
        SettingsManager.Instance.OnMasterVolumeChanged -= OnMasterChanged;
        SettingsManager.Instance.OnBgmVolumeChanged -= OnBgmChanged;
        SettingsManager.Instance.OnSfxVolumeChanged -= OnSfxChanged;
    }

    private void OnMasterChanged(float value) => Apply(masterParam, value);

    private void OnBgmChanged(float value) => Apply(bgmParam, value);

    private void OnSfxChanged(float value) => Apply(sfxParam, value);

    private void Apply(string param, float linear)
    {
        if (string.IsNullOrEmpty(param))
            return;
        // linear 0~1 → dB. 0이면 -80dB(묵음).
        float db = linear <= 0f ? -80f : Mathf.Log10(linear) * 20f;
        mixer.SetFloat(param, db);
    }
}
