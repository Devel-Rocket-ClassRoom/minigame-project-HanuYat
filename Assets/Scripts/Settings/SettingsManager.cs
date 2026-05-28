using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KeyMouseSensitivity = "Settings.MouseSensitivity";
    private const string KeyMasterVolume = "Settings.MasterVolume";
    private const string KeyBgmVolume = "Settings.BgmVolume";
    private const string KeySfxVolume = "Settings.SfxVolume";

    public const float DefaultMouseSensitivity = 0.15f;
    public const float DefaultVolume = 0.8f;

    public const float MinMouseSensitivity = 0.05f;
    public const float MaxMouseSensitivity = 1.0f;

    public float MouseSensitivity { get; private set; }
    public float MasterVolume { get; private set; }
    public float BgmVolume { get; private set; }
    public float SfxVolume { get; private set; }

    public event Action<float> OnMouseSensitivityChanged;
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBgmVolumeChanged;
    public event Action<float> OnSfxVolumeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Load()
    {
        MouseSensitivity = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultVolume);
        BgmVolume = PlayerPrefs.GetFloat(KeyBgmVolume, DefaultVolume);
        SfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, DefaultVolume);
    }

    public void SetMouseSensitivity(float value)
    {
        value = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
        if (Mathf.Approximately(MouseSensitivity, value))
            return;
        MouseSensitivity = value;
        PlayerPrefs.SetFloat(KeyMouseSensitivity, value);
        PlayerPrefs.Save();
        OnMouseSensitivityChanged?.Invoke(value);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(MasterVolume, value))
            return;
        MasterVolume = value;
        PlayerPrefs.SetFloat(KeyMasterVolume, value);
        PlayerPrefs.Save();
        OnMasterVolumeChanged?.Invoke(value);
    }

    public void SetBgmVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(BgmVolume, value))
            return;
        BgmVolume = value;
        PlayerPrefs.SetFloat(KeyBgmVolume, value);
        PlayerPrefs.Save();
        OnBgmVolumeChanged?.Invoke(value);
    }

    public void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(SfxVolume, value))
            return;
        SfxVolume = value;
        PlayerPrefs.SetFloat(KeySfxVolume, value);
        PlayerPrefs.Save();
        OnSfxVolumeChanged?.Invoke(value);
    }
}
