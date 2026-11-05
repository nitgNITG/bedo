using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [System.Serializable]
    public class VolumeControl
    {
        public string playerPrefsKey;

        public string mixerParameter;

        public Slider slider;

        public TMP_Text valueText;

        [Range(0, 100)]
        public float defaultVolume = 100f;
    }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Volumes")]
    [SerializeField] private VolumeControl master;

    [SerializeField] private VolumeControl music;

    [SerializeField] private VolumeControl ui;

    [SerializeField] private VolumeControl devices;


    private void Start()
    {
        Initialize(master);
        Initialize(music);
        Initialize(ui);
        Initialize(devices);
    }


    private void Initialize(VolumeControl control)
    {
        if (control == null || control.slider == null)
            return;

        control.slider.minValue = 0;
        control.slider.maxValue = 100;

        float value = PlayerPrefs.GetFloat(
            control.playerPrefsKey,
            control.defaultVolume
        );

        control.slider.SetValueWithoutNotify(value);

        ApplyVolume(control, value);

        control.slider.onValueChanged.RemoveAllListeners();

        control.slider.onValueChanged.AddListener(
            delegate
            {
                SliderChanged(control);
            });
    }


    private void SliderChanged(VolumeControl control)
    {
        float value = control.slider.value;

        ApplyVolume(control, value);

        PlayerPrefs.SetFloat(
            control.playerPrefsKey,
            value
        );

        PlayerPrefs.Save();
    }


    private void ApplyVolume(
        VolumeControl control,
        float sliderValue)
    {
        if (control.valueText != null)
        {
            control.valueText.text =
                Mathf.RoundToInt(sliderValue).ToString();
        }


        float normalized = sliderValue / 100f;

        float dB;

        if (normalized <= 0.0001f)
        {
            dB = -80f;
        }
        else
        {
            dB = Mathf.Log10(normalized) * 20f;
        }


        bool success = audioMixer.SetFloat(
            control.mixerParameter,
            dB
        );


        Debug.Log(
            control.mixerParameter +
            " = " +
            dB +
            " dB | Success: " +
            success
        );
    }
}