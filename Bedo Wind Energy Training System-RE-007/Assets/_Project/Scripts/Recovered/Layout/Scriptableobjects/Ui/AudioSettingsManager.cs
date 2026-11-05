using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;
using Obvious.Soap;

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Audio Settings Manager")]
public sealed class AudioSettingsManager : MonoBehaviour
{
    [Serializable]
    public class SliderSetting
    {
        [Tooltip("UI slider controlling this mixer parameter.")]
        public Slider slider;

        [Tooltip("Unique id for PlayerPrefs (e.g., 'Music', 'SFX', 'Master').")]
        public string sliderTag;

        [Tooltip("Exposed AudioMixer parameter name (e.g., 'MusicVolume', 'MasterVolume').")]
        public string mixerParameter;

        [NonSerialized] public UnityAction<float> handler;
    }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Tooltip("Exposed parameter name for MASTER volume (e.g., 'MasterVolume').")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("Global Sound (BoolReference)")]
    [Tooltip("Controls mute/unmute of the master. On unmute we reapply current sliders first, then use the master slider's value.")]
    [SerializeField] private BoolReference isSoundOn;
    [SerializeField] private BoolReference isMusicOn;

    [Header("Sliders")]
    [SerializeField] private SliderSetting[] sliders;

    [Header("Persistence")]
    [Tooltip("Default volume (0–100) when no save exists yet.")]
    [Range(0, 100)][SerializeField] private float defaultVolumePercent = 100f;

    [Tooltip("Save PlayerPrefs immediately when a slider changes.")]
    [SerializeField] private bool saveImmediately = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;

    // UI slider config
    private const float SliderMin = 0f;
    private const float SliderMax = 100f;
    private const bool SliderWholeNumbers = true;

    // prevent -Infinity dB; ~ -80 dB
    private const float MinNormalized = 0.0001f;

    private BoolReference _underlyingVar;
    private bool _subscribed;

    // ---------- Unity lifecycle ----------

    private void OnEnable()
    {
        _underlyingVar = isSoundOn;
        if (_underlyingVar != null && !_subscribed)
        {
            _underlyingVar.OnValueChanged += OnSoundToggleChanged;
            _subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (_underlyingVar != null && _subscribed)
        {
            _underlyingVar.OnValueChanged -= OnSoundToggleChanged;
            _subscribed = false;
        }
        PlayerPrefs.Save();
    }

    private void Start()
    {
        if (mixer == null)
        {
            Debug.LogError("AudioSettingsManager: AudioMixer is not assigned.");
            return;
        }

        // 1) Initialize each slider from PlayerPrefs and apply to mixer
        if (sliders != null)
        {
            foreach (var s in sliders)
            {
                if (!IsValidSetting(s)) continue;

                s.slider.minValue = SliderMin;
                s.slider.maxValue = SliderMax;
                s.slider.wholeNumbers = SliderWholeNumbers;

                string key = PrefKey(s.sliderTag);
                float saved = Mathf.Clamp(
                    PlayerPrefs.GetFloat(key, defaultVolumePercent),
                    SliderMin, SliderMax
                );

                s.slider.SetValueWithoutNotify(saved);
                ApplyParameterVolume(saved, s.mixerParameter);

                s.handler = v => OnSliderChanged(v, s);
                s.slider.onValueChanged.AddListener(s.handler);

                if (verboseLogging) Debug.Log($"[Audio] Loaded {key}={saved} -> {s.mixerParameter}");
            }
        }

        // 2) Apply master state at startup
        ApplyMasterMute(IsSoundOn());
    }

    private void OnDestroy()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        if (sliders == null) return;
        foreach (var s in sliders)
        {
            if (s?.slider == null) continue;
            if (s.handler != null)
                s.slider.onValueChanged.RemoveListener(s.handler);
        }
    }

    // ---------- Events ----------

    private void OnSoundToggleChanged(bool isOn)
    {
        if (isOn)
        {
            // Reapply current per-parameter volumes from UI,
            // then lift master using the MASTER SLIDER value (not a constant).
            ReapplyAllVolumesFromUI();
            ApplyMasterMute(true);
        }
        else
        {
            // Mute master only; do not touch per-parameter values.
            ApplyMasterMute(false);
        }

        if (verboseLogging) Debug.Log($"[Audio] Master {(isOn ? "unmuted (reapplied sliders)" : "muted")}");
    }

    private void OnSliderChanged(float value, SliderSetting setting)
    {
        float clamped = Mathf.Clamp(value, SliderMin, SliderMax);
        PlayerPrefs.SetFloat(PrefKey(setting.sliderTag), clamped);
        if (saveImmediately) PlayerPrefs.Save();

        // Always apply per-parameter value even if muted,
        // so unmute reflects latest slider settings.
        ApplyParameterVolume(clamped, setting.mixerParameter);

        if (verboseLogging) Debug.Log($"[Audio] Saved {setting.sliderTag}={clamped} -> {setting.mixerParameter}");
    }

    // ---------- Core audio ops ----------

    private void ReapplyAllVolumesFromUI()
    {
        if (sliders == null) return;
        foreach (var s in sliders)
        {
            if (!IsValidSetting(s)) continue;
            ApplyParameterVolume(s.slider.value, s.mixerParameter);
        }
    }

    private void ApplyParameterVolume(float sliderValue, string mixerParameter)
    {
        if (mixer == null || string.IsNullOrEmpty(mixerParameter)) return;

        float normalized = Mathf.Clamp(sliderValue / SliderMax, MinNormalized, 1f);
        float dB = 20f * Mathf.Log10(normalized);

        if (!mixer.SetFloat(mixerParameter, dB))
        {
            Debug.LogWarning($"AudioSettingsManager: parameter '{mixerParameter}' not found in mixer.");
        }
    }

    /// <summary>
    /// Mutes or unmutes the master mixer parameter.
    /// On unmute, uses the MASTER SLIDER'S CURRENT VALUE (or saved/default if no slider).
    /// </summary>
    private void ApplyMasterMute(bool soundOn)
    {
        if (mixer == null || string.IsNullOrEmpty(masterVolumeParameter)) return;

        float dB;
        if (!soundOn)
        {
            // Hard mute
            dB = 20f * Mathf.Log10(MinNormalized);
        }
        else
        {
            // Use the master slider's current UI value (or saved/default)
            float masterPercent = GetMasterSliderPercent();
            float normalized = Mathf.Clamp(masterPercent / SliderMax, MinNormalized, 1f);
            dB = 20f * Mathf.Log10(normalized);
        }

        if (!mixer.SetFloat(masterVolumeParameter, dB))
        {
            Debug.LogWarning($"AudioSettingsManager: master parameter '{masterVolumeParameter}' not found in mixer.");
        }
    }

    /// <summary>
    /// Returns the current UI value (0–100) of the slider bound to masterVolumeParameter.
    /// If no slider is present, falls back to PlayerPrefs or defaultVolumePercent.
    /// </summary>
    private float GetMasterSliderPercent()
    {
        // Try to find an actual slider bound to masterVolumeParameter
        if (sliders != null)
        {
            foreach (var s in sliders)
            {
                if (s == null) continue;
                if (!string.IsNullOrEmpty(s.mixerParameter) &&
                    s.mixerParameter == masterVolumeParameter)
                {
                    if (s.slider != null)
                        return Mathf.Clamp(s.slider.value, SliderMin, SliderMax);

                    // Fallback: read what we saved for this tag
                    if (!string.IsNullOrEmpty(s.sliderTag))
                        return Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey(s.sliderTag), defaultVolumePercent), SliderMin, SliderMax);
                }
            }
        }

        // Last resort: return default
        return defaultVolumePercent;
    }

    // ---------- Utils ----------

    private bool IsSoundOn()
    {
        // If no BoolReference is assigned, treat as ON.
        return isSoundOn == null ? true : isSoundOn.Value;
    }

    private static string PrefKey(string tag) => $"Audio_{tag}_SliderValue";

    private static bool IsValidSetting(SliderSetting s)
    {
        if (s == null || s.slider == null) return false;

        if (string.IsNullOrWhiteSpace(s.sliderTag))
        {
            Debug.LogWarning("AudioSettingsManager: Missing sliderTag.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(s.mixerParameter))
        {
            Debug.LogWarning("AudioSettingsManager: Missing mixerParameter.");
            return false;
        }
        return true;
    }
    // SOAP: BoolReference exposes a BoolVariable when not using constant mode
    //private static BoolVariable GetUnderlyingVariable(BoolReference reference)
    //{
    //    if (reference == null) return null;
    //    try { return reference.Variable; } catch { return null; }
    //}
}
