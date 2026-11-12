//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using TMPro;
//using UnityEditor.Localization.Plugins.XLIFF.V12;

namespace Michsky.UI.Shift
{
    public class QualityManager : MonoBehaviour
    {
        [Header("Audio")]
        public AudioMixer mixer;
        public SliderManager masterSlider;
        public SliderManager musicSlider;
        public SliderManager sfxSlider;

        public AudioSource SfxSource;
        public AudioSource MusicSource;

        [Header("Resolution")]
        public bool preferSelector = true;
        public HorizontalSelector resolutionSelector;
        public TMP_Dropdown resolutionDropdown;
        [System.Serializable]
        public class DynamicRes : UnityEvent<int> { }
        public DynamicRes clickEvent;

        Resolution[] resolutions;
        List<string> options = new List<string>();

        void Start()
        {
            //mixer.
            //mixer.outputAudioMixerGroup.
            Debug.Log("Master:"+ Mathf.Log10(PlayerPrefs.GetFloat(masterSlider.sliderTag + "SliderValue")));
            Debug.Log("Music:" + Mathf.Log10(PlayerPrefs.GetFloat(masterSlider.sliderTag + "SliderValue")));
            Debug.Log("SFX:" + Mathf.Log10(PlayerPrefs.GetFloat(masterSlider.sliderTag + "SliderValue")));
            if ( masterSlider != null ) mixer.SetFloat("Master", Mathf.Log10(PlayerPrefs.GetFloat(masterSlider.sliderTag + "SliderValue")) * 20);
           if (musicSlider != null) mixer.SetFloat("Music", Mathf.Log10(PlayerPrefs.GetFloat(musicSlider.sliderTag + "SliderValue")) * 20);
            if (sfxSlider != null) mixer.SetFloat("SFX", Mathf.Log10(PlayerPrefs.GetFloat(sfxSlider.sliderTag + "SliderValue")) * 20);

            if(resolutionSelector == null) { return; } 

            resolutions = Screen.resolutions;

            if (preferSelector == true)
            {
                if (resolutionDropdown != null) { resolutionDropdown.gameObject.SetActive(false); }
                if (resolutionSelector != null) { resolutionSelector.gameObject.SetActive(true); }
                else { return; }

                resolutionSelector.itemList.RemoveRange(0, resolutionSelector.itemList.Count);

                int currentResolutionIndex = -1;
                for (int i = 0; i < resolutions.Length; i++)
                {
#if UNITY_2022_2_OR_NEWER
                    string option = resolutions[i].width + "x" + resolutions[i].height + " " + resolutions[i].refreshRateRatio.numerator + "hz";
#else
                    string option = resolutions[i].width + "x" + resolutions[i].height + " " + resolutions[i].refreshRate + "hz";
#endif
                    options.Add(option);

                    resolutionSelector.CreateNewItem(options[i]);
                    // resolutionSelector.itemList[i].onValueChanged.AddListener(UpdateResolution);

#if UNITY_2022_2_OR_NEWER
                    if (resolutions[i].width == Screen.currentResolution.width
                        && resolutions[i].height == Screen.currentResolution.height
                        && resolutions[i].refreshRateRatio.numerator == Screen.currentResolution.refreshRateRatio.numerator)
#else
                    if (resolutions[i].width == Screen.currentResolution.width
                        && resolutions[i].height == Screen.currentResolution.height
                        && resolutions[i].refreshRate == Screen.currentResolution.refreshRate)
#endif
                    {
                        currentResolutionIndex = i;
                        resolutionSelector.index = currentResolutionIndex;
                    }
                }

                if (currentResolutionIndex == 0) { resolutionSelector.index = resolutionSelector.itemList.Count - 1; }
                resolutionSelector.UpdateUI();
            }

            else
            {
                if (resolutionSelector != null) { resolutionSelector.gameObject.SetActive(false); }
                if (resolutionDropdown != null) { resolutionDropdown.gameObject.SetActive(true); }
                else { return; }

                resolutionDropdown.ClearOptions();

                List<string> options = new List<string>();

                int currentResolutionIndex = 0;
                for (int i = 0; i < resolutions.Length; i++)
                {
#if UNITY_2022_2_OR_NEWER
                    string option = resolutions[i].width + "x" + resolutions[i].height + " " + resolutions[i].refreshRateRatio.numerator + "hz";
#else
                    string option = resolutions[i].width + "x" + resolutions[i].height + " " + resolutions[i].refreshRate + "hz";
#endif
                    options.Add(option);

#if UNITY_2022_2_OR_NEWER
                    if (resolutions[i].width == Screen.currentResolution.width
                        && resolutions[i].height == Screen.currentResolution.height
                        && resolutions[i].refreshRateRatio.numerator == Screen.currentResolution.refreshRateRatio.numerator)
#else
                    if (resolutions[i].width == Screen.currentResolution.width
                        && resolutions[i].height == Screen.currentResolution.height
                        && resolutions[i].refreshRate == Screen.currentResolution.refreshRate)
#endif
                    {
                        currentResolutionIndex = i;
                    }
                }

                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = currentResolutionIndex;
                resolutionDropdown.RefreshShownValue();
                resolutionDropdown.onValueChanged.RemoveAllListeners();
                resolutionDropdown.onValueChanged.AddListener(SetResolution);
            }
        }

        public void UpdateResolution()
        {
            if (resolutionSelector == null) { return; }
            clickEvent.Invoke(resolutionSelector.index);
        }

        public void SetResolution(int resolutionIndex)
        {
            if (resolutionSelector == null) { return; }
            Screen.SetResolution(resolutions[resolutionIndex].width, resolutions[resolutionIndex].height, Screen.fullScreen);
        }

        public void AnisotrpicFilteringEnable()
        {
            if (resolutionSelector == null) { return; }
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        }

        public void AnisotrpicFilteringDisable()
        {
            if (resolutionSelector == null) { return; }
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }

        public void AntiAlisasingSet(int index)
        {
            if (resolutionSelector == null) { return; }
            // 0, 2, 4, 8 - Zero means off
            QualitySettings.antiAliasing = index;
        }

        public void VsyncSet(int index)
        {
            if (resolutionSelector == null) { return; }
            // 0, 1 - Zero means off
            QualitySettings.vSyncCount = index;
        }

        public void ShadowResolutionSet(int index)
        {
            if (resolutionSelector == null) { return; }
            if (index == 3)
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            else if (index == 2)
                QualitySettings.shadowResolution = ShadowResolution.High;
            else if (index == 1)
                QualitySettings.shadowResolution = ShadowResolution.Medium;
            else if (index == 0)
                QualitySettings.shadowResolution = ShadowResolution.Low;
        }

        public void ShadowsSet(int index)
        {
            if (resolutionSelector == null) { return; }
            if (index == 0)
                QualitySettings.shadows = ShadowQuality.Disable;
            else if (index == 1)
                QualitySettings.shadows = ShadowQuality.All;
        }

        public void ShadowsCascasedSet(int index)
        {
            if (resolutionSelector == null) { return; }
            //0 = No, 2 = Two, 4 = Four
            QualitySettings.shadowCascades = index;
        }

        public void TextureSet(int index)
        {
            if (resolutionSelector == null) { return; }
            // 0 = Full, 4 = Eight Resolution
#if UNITY_2022_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = index;
#else
            QualitySettings.masterTextureLimit = index;
#endif
        }

        public void SoftParticleSet(int index)
        {
            if (index == 0)
                QualitySettings.softParticles = false;
            else if (index == 1)
                QualitySettings.softParticles = true;
        }

        public void ReflectionSet(int index)
        {
            if (index == 0)
                QualitySettings.realtimeReflectionProbes = false;
            else if (index == 1)
                QualitySettings.realtimeReflectionProbes = true;
        }

        public void VolumeSetMaster(float volume)
        {
            SfxSource.volume = volume / 100;
            MusicSource.volume = volume/100;

            mixer.SetFloat("Master", Mathf.Log10(volume/100) * 20);
        }

        public void VolumeSetMusic(float volume)
        {
            mixer.SetFloat("Music", Mathf.Log10(volume / 100) * 20);
        }

        public void VolumeSetSFX(float volume)
        {
            mixer.SetFloat("SFX", Mathf.Log10(volume / 100) * 20);
        }

        public void SetOverallQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }

        public void WindowFullscreen()
        {
            Screen.fullScreen = true;
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }

        public void WindowBorderless()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        public void WindowWindowed()
        {
            Screen.fullScreen = false;
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }
}