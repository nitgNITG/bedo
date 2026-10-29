using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Michsky.UI.Shift
{
    public class SliderManager : MonoBehaviour
    {
        [Header("Resources")]
        public TextMeshProUGUI valueText;

        [Header("Saving")]
        public bool enableSaving = false;
        public string sliderTag = "Tag Text";
        public float defaultValue = 1;

        [Header("Settings")]
        public bool usePercent = false;
        public bool showValue = true;
        public bool useRoundValue = false;

        Slider mainSlider;
        float saveValue;

        void Awake()
        {
            mainSlider = GetComponent<Slider>();
        }

        void OnEnable()
        {
            // Guided and Free Mode panels are enabled dynamically. Refresh their
            // layout on the next frame so the value label starts aligned.
            StartCoroutine(RefreshInitialLayout());
        }

        void Start()
        {
            if (mainSlider == null)
                mainSlider = GetComponent<Slider>();

            if (showValue == false && valueText != null)
                valueText.enabled = false;

            if (enableSaving == true)
            {
                if (PlayerPrefs.HasKey(sliderTag + "SliderValue") == false)
                    saveValue = defaultValue;
                else
                    saveValue = PlayerPrefs.GetFloat(sliderTag + "SliderValue");

                mainSlider.value = saveValue;

                mainSlider.onValueChanged.AddListener(delegate
                {
                    saveValue = mainSlider.value;
                    PlayerPrefs.SetFloat(sliderTag + "SliderValue", saveValue);
                });
            }

            RefreshValueText();
        }

        void Update()
        {
            RefreshValueText();
        }

        void RefreshValueText()
        {
            if (mainSlider == null || valueText == null)
                return;

            if (useRoundValue == true)
            {
                if (usePercent == true)
                    valueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString() + "%";
                else
                    valueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString();
            }

            else
            {
                if (usePercent == true)
                    valueText.text = mainSlider.value.ToString("F2") + "%";
                else
                    valueText.text = mainSlider.value.ToString("F2");
            }
        }

        IEnumerator RefreshInitialLayout()
        {
            yield return null;

            if (mainSlider == null)
                mainSlider = GetComponent<Slider>();

            if (mainSlider == null)
                yield break;

            Canvas.ForceUpdateCanvases();

            RectTransform sliderRect = transform as RectTransform;
            if (sliderRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(sliderRect);

            if (valueText != null)
            {
                RectTransform textParent = valueText.rectTransform.parent as RectTransform;
                if (textParent != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(textParent);

                valueText.ForceMeshUpdate();
            }

            RefreshValueText();
        }
    }
}
