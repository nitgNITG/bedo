using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Bedo.Hydro
{
    public sealed class HydroMpptGuided : MonoBehaviour
    {
        public StoryboardSteps storyboard;
        [SerializeField] private string speedStepId;
        public Slider slider;
        public CanvasGroup speedPanel;
        public TMP_Text speedText;
        public Button shortcut;
        public GameObject mpptScreen;
        public GameObject stepsPanel;
        public GameObject speedHighlight;
        private float chargeDisplay;
        public TMP_Text mpptText;
        public GameObject lamp;
        public Transform rotor;
        public TMP_Text[] monitorValues;
        public HydroGuidedMonitor graph;
        public bool IsPowered => powered;
        public bool GeneratorEnabled => generator;
        public HydroExperimentOneReadings.Reading GeneratorReading => HydroExperimentTwoReadings.Read(rpm, powered, generator, load).Generator;

        private HydroReferenceControls referenceControls;
        private bool powered, load, generator, speedUnlocked;
        private float rpm;
        public bool CanAdjustSpeed => speedUnlocked;

        private void OnEnable()
        {
            if (speedPanel) referenceControls = speedPanel.GetComponent<HydroReferenceControls>();
            if (referenceControls) referenceControls.enabled = false;
            if (slider)
            {
                slider.maxValue = 5;
                slider.wholeNumbers = false;
                slider.SetValueWithoutNotify(0);
                slider.interactable = true;
                slider.onValueChanged.AddListener(SetSpeed);
            }
            
        }

        private void OnDisable()
        {
            if (slider) slider.onValueChanged.RemoveListener(SetSpeed);
            if (stepsPanel) stepsPanel.SetActive(false);
            if (speedHighlight) speedHighlight.SetActive(false);
            chargeDisplay = 0f;
            if (shortcut) shortcut.onClick.RemoveListener(ToggleSpeed);
            if (mpptScreen) mpptScreen.SetActive(false);
            if (lamp) lamp.SetActive(false);
            if (graph) graph.ClearReadings();
            ShowSpeed(false);
            powered = load = generator = speedUnlocked = false;
            rpm = 0;
            if (referenceControls) referenceControls.enabled = true;
        }

        public void PowerOn() { powered = true; Refresh(); }
        public void EnableLoad() { load = true; Refresh(); }
        public void EnableGenerator() { generator = true; Refresh(); }
        public void UnlockSpeed() { speedUnlocked = true; Refresh(); }

        private void ToggleSpeed()
        {
            if (!speedUnlocked || !speedPanel) return;
            ShowSpeed(speedPanel.alpha < .5f);
        }

        private void ShowSpeed(bool visible)
        {
            if (!speedPanel) return;
            speedPanel.alpha = visible ? 1f : 0f;
            speedPanel.interactable = speedPanel.blocksRaycasts = visible;
            if (speedHighlight && visible) speedHighlight.SetActive(false);
        }

        private void SetSpeed(float value)
        {
            if (!speedUnlocked || (HydroSpeedInput.Instance ? !HydroSpeedInput.Instance.AcceptSpeedChange : !speedPanel.interactable)) return;
            rpm = Mathf.Clamp(value, 0, 5) * 200;
            Refresh();
            if (rpm == 1000 && storyboard.IsCurrentStep(speedStepId))
            {
                ShowSpeed(false);
                storyboard.GoNextStep();
            }
        }

        private void Update()
        {
            if (mpptScreen && mpptScreen.activeSelf && mpptText)
            {
                const float ratedCurrent = HydroFreeLabReadings.ChargeLimit;
                var battery = HydroExperimentTwoReadings.Read(rpm, powered, generator, load);
                chargeDisplay = Mathf.MoveTowards(chargeDisplay, Mathf.Clamp01(battery.BatteryCurrent / ratedCurrent) * 100f, (100f / ratedCurrent) * .1f * Time.deltaTime);
                mpptText.text = chargeDisplay.ToString("F2") + "%";
            }
            if (generator && rotor && rpm > 0)
                rotor.Rotate(Vector3.forward, rpm * 6f * Time.deltaTime, Space.Self);
        }

        private void Refresh()
        {
            var reading = HydroExperimentTwoReadings.Read(rpm, powered, generator, load);
            if (mpptScreen) mpptScreen.SetActive(powered && load);
            if (lamp) lamp.SetActive(powered && load);
            if (mpptText && powered && load) mpptText.text = chargeDisplay.ToString("F2") + "%";
            if (speedText) speedText.text = rpm.ToString();
            if (graph && powered && generator) graph.AddDcReading(reading.Generator.Rpm, reading.Generator.DcVoltage, reading.Generator.DcCurrent);
            if (monitorValues == null) return;
            var g = reading.Generator;
            float[] values = { rpm, g.AcVoltage, g.AcCurrent, g.AcPower, g.DcVoltage, g.DcCurrent, g.DcPower, powered ? HydroExperimentTwoReadings.BatteryVoltage : 0f, reading.BatteryCurrent, reading.BatteryPower, powered && load ? HydroExperimentTwoReadings.BatteryVoltage : 0f, reading.LoadCurrent, reading.LoadPower };
            for (int i = 0; i < Mathf.Min(values.Length, monitorValues.Length); i++)
                if (monitorValues[i]) monitorValues[i].text = values[i].ToString("0.00");
        }
    }
}
