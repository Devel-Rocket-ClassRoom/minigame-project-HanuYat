using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private Slider sensitivitySlider;

    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private Slider bgmVolumeSlider;

    [SerializeField]
    private Slider sfxVolumeSlider;

    private bool isSyncing;

    private void OnEnable()
    {
        ResetButtonAnimators();

        var settings = SettingsManager.Instance;
        if (settings == null)
        {
            Debug.LogError(
                "[SettingsPanelController] SettingsManager.Instance 없음 — 비활성.",
                this
            );
            return;
        }

        ConfigureSensitivitySlider();

        SyncSlidersFromSettings();

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        settings.OnMouseSensitivityChanged += OnSensitivityChangedExternal;
        settings.OnMasterVolumeChanged += OnMasterVolumeChangedExternal;
        settings.OnBgmVolumeChanged += OnBgmVolumeChangedExternal;
        settings.OnSfxVolumeChanged += OnSfxVolumeChangedExternal;
    }

    private void OnDisable()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

        var settings = SettingsManager.Instance;
        if (settings != null)
        {
            settings.OnMouseSensitivityChanged -= OnSensitivityChangedExternal;
            settings.OnMasterVolumeChanged -= OnMasterVolumeChangedExternal;
            settings.OnBgmVolumeChanged -= OnBgmVolumeChangedExternal;
            settings.OnSfxVolumeChanged -= OnSfxVolumeChangedExternal;
        }
    }

    private void ConfigureSensitivitySlider()
    {
        if (sensitivitySlider == null)
            return;
        sensitivitySlider.minValue = SettingsManager.MinMouseSensitivity;
        sensitivitySlider.maxValue = SettingsManager.MaxMouseSensitivity;
    }

    public void ClosePanel()
    {
        var root = panelRoot != null ? panelRoot : gameObject;
        root.SetActive(false);
    }

    private void ResetButtonAnimators()
    {
        var stateHash = Animator.StringToHash("Normal");
        foreach (var anim in GetComponentsInChildren<Animator>(true))
        {
            if (anim.runtimeAnimatorController == null)
                continue;
            if (!anim.HasState(0, stateHash))
                continue;
            anim.Play(stateHash, 0, 1f);
        }
    }

    private void SyncSlidersFromSettings()
    {
        var settings = SettingsManager.Instance;
        if (settings == null)
            return;
        isSyncing = true;
        if (sensitivitySlider != null)
            sensitivitySlider.value = settings.MouseSensitivity;
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = settings.MasterVolume;
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.value = settings.BgmVolume;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = settings.SfxVolume;
        isSyncing = false;
    }

    private void OnSensitivityChanged(float value)
    {
        if (isSyncing)
            return;
        SettingsManager.Instance?.SetMouseSensitivity(value);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (isSyncing)
            return;
        SettingsManager.Instance?.SetMasterVolume(value);
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (isSyncing)
            return;
        SettingsManager.Instance?.SetBgmVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (isSyncing)
            return;
        SettingsManager.Instance?.SetSfxVolume(value);
    }

    private void OnSensitivityChangedExternal(float value)
    {
        if (sensitivitySlider == null || Mathf.Approximately(sensitivitySlider.value, value))
            return;
        isSyncing = true;
        sensitivitySlider.value = value;
        isSyncing = false;
    }

    private void OnMasterVolumeChangedExternal(float value)
    {
        if (masterVolumeSlider == null || Mathf.Approximately(masterVolumeSlider.value, value))
            return;
        isSyncing = true;
        masterVolumeSlider.value = value;
        isSyncing = false;
    }

    private void OnBgmVolumeChangedExternal(float value)
    {
        if (bgmVolumeSlider == null || Mathf.Approximately(bgmVolumeSlider.value, value))
            return;
        isSyncing = true;
        bgmVolumeSlider.value = value;
        isSyncing = false;
    }

    private void OnSfxVolumeChangedExternal(float value)
    {
        if (sfxVolumeSlider == null || Mathf.Approximately(sfxVolumeSlider.value, value))
            return;
        isSyncing = true;
        sfxVolumeSlider.value = value;
        isSyncing = false;
    }
}
