using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Bedo.Hydro
{
    public sealed class HydroReferenceControls : MonoBehaviour
    {
        [SerializeField] private StoryboardSteps storyboard;
        [SerializeField] private HydroGuidedSpeed speed;
        [SerializeField] private RheostatController rheostat;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private HydroGuidedControl unloadedStep;
        [SerializeField] private HydroGuidedControl loadedStep;
        [SerializeField] private SliderRangeChecker windPanel;
        [SerializeField] private Transform speedKnob;
        [SerializeField] private Interaction modeSelector;
        private Quaternion manualPose;
        private bool remote;
        public void ToggleHydroMode() { if (HydroSpeedInput.Instance) HydroSpeedInput.Instance.ToggleMode(); }
        public bool CanAdjustSpeed => wasAvailable;
        private Quaternion knobRest;
        private void Awake()
        {
            if (speedKnob) knobRest = speedKnob.localRotation;
            if (modeSelector) manualPose = modeSelector.transform.localRotation;
        }
        [SerializeField] private GameObject speedHighlight;
        [SerializeField] private TMP_Text[] shortcutLabels;
        private bool wasHydro;
        private bool wasAvailable;
        private bool speedUnlocked;

        public void ToggleSpeedPopup()
        {
            if (HydroSpeedInput.Instance) HydroSpeedInput.Instance.ToggleSoftware();
        }
        private float resistance = float.NaN;

        private void OnEnable() => slider.onValueChanged.AddListener(HandleSlider);
        private void OnDisable() => slider.onValueChanged.RemoveListener(HandleSlider);

        private void LateUpdate()
        {
            var manager = InteractionManager.Instance;
            bool active = manager && manager.Guided && manager.IsExperimentActive(storyboard);
            bool speedStep = active && (storyboard.IsCurrentGuidedInteraction(unloadedStep) ||
                                        storyboard.IsCurrentGuidedInteraction(loadedStep));
            if (!active || storyboard.CurrentStep < 0) speedUnlocked = false;
            if (speedStep) speedUnlocked = true;
            bool available = active && speedUnlocked;
            if (active != wasHydro)
            {
                foreach (var label in shortcutLabels) if (label) label.text = active ? "RPM" : "Wind";
                wasHydro = active;
            }
            if (speedHighlight && (active || wasAvailable)) speedHighlight.SetActive(speedStep && panel.alpha < .5f);
            if (available != wasAvailable)
            {
                panel.alpha = 0f;
                panel.interactable = false;
                panel.blocksRaycasts = false;
                wasAvailable = available;
            }
            slider.interactable = available;
            slider.maxValue = speed.IsLoaded
                ? HydroExperimentOneReadings.LoadedMaximumRpm / HydroExperimentOneReadings.LoadedSpeedIncrement
                : HydroExperimentOneReadings.UnloadedMaximumStop;
            slider.SetValueWithoutNotify(speed.SliderPosition);
            valueText.text = speed.CurrentReading.Rpm.ToString("0.##");
            if (active && speed.IsLoaded && !Mathf.Approximately(resistance, rheostat.CurrentResistance))
            {
                resistance = rheostat.CurrentResistance;
                speed.SetResistance(resistance);
            }
        }

        private void HandleSlider(float value)
        {
            if (HydroSpeedInput.Instance ? !HydroSpeedInput.Instance.AcceptSpeedChange : !slider.interactable)
                return;

            float previousRpm = speed.CurrentReading.Rpm;
            speed.SetSliderPosition(value);
            if (storyboard.IsCurrentGuidedInteraction(loadedStep) &&
                speed.CurrentReading.Rpm > previousRpm)
                loadedStep.Finish();
        }
    }
}