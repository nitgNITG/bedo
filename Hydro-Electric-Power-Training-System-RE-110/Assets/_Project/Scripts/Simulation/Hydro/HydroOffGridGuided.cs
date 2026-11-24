using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Bedo.Hydro
{
    public sealed class HydroOffGridGuided : MonoBehaviour
    {
        public StoryboardSteps storyboard;
        [SerializeField] private string speedStepId;
        public Slider slider;
        public Button shortcut;
        public CanvasGroup panel;
        public TMP_Text speedText;
        public GameObject speedHighlight;
        public Transform rotor;
        public GameObject dcLamp, acLamp;
        [SerializeField] private GameObject dcLampOff, acLampOff;
        public TMP_Text[] meters;
        [SerializeField] private HydroGuidedMonitor graph;
        private HydroReferenceControls referenceControls;
        private bool inverter, acLoad, generator, grid, speedUnlocked, advanced;
        private float rpm;
        public bool GeneratorEnabled => generator;
        public HydroExperimentOneReadings.Reading GeneratorReading => HydroExperimentOneReadings.ReadLoaded(generator ? rpm : 0f, 4f);
        public bool CanAdjustSpeed => speedUnlocked;

        private void OnEnable()
        {
            referenceControls = panel.GetComponent<HydroReferenceControls>();
            if (referenceControls) referenceControls.enabled = false;
            slider.minValue = 0;
            slider.maxValue = 5;
            slider.wholeNumbers = false;
            slider.interactable = true;
            slider.SetValueWithoutNotify(0);
            slider.onValueChanged.AddListener(SetSpeed);

            ShowSpeed(false);
            Refresh();
        }

        private void OnDisable()
        {
            if (slider) slider.onValueChanged.RemoveListener(SetSpeed);
            if (shortcut) shortcut.onClick.RemoveListener(ToggleSpeed);
            if (referenceControls) referenceControls.enabled = true;
            SetLamp(dcLamp, dcLampOff, false);
            SetLamp(acLamp, acLampOff, false);
            ShowSpeed(false);
            inverter = acLoad = generator = grid = speedUnlocked = advanced = false;
            rpm = 0;
            if (graph) graph.ClearReadings();
        }

        public void PowerInverter() { inverter = true; Refresh(); }
        public void ConnectAcLoad() { acLoad = true; Refresh(); }
        public void EnableGenerator() { generator = true; Refresh(); }
        public void ConnectGrid() { grid = true; Refresh(); }
        public void UnlockSpeed() { speedUnlocked = true; if (speedHighlight) speedHighlight.SetActive(true); }

        private void ToggleSpeed()
        {
            if (speedUnlocked) ShowSpeed(panel.alpha < .5f);
        }

        private void ShowSpeed(bool visible)
        {
            if (panel)
            {
                panel.alpha = visible ? 1f : 0f;
                panel.interactable = panel.blocksRaycasts = visible;
            }
            if (speedHighlight) speedHighlight.SetActive(false);
        }

        private void SetSpeed(float stop)
        {
            if (!speedUnlocked || (HydroSpeedInput.Instance ? !HydroSpeedInput.Instance.AcceptSpeedChange : !panel.interactable)) return;
            rpm = Mathf.Clamp(stop, 0, 5) * 200;
            Refresh();
            if (graph && generator && rpm > 0)
            {
                var reading = HydroExperimentOneReadings.ReadLoaded(rpm, 4f);
                graph.AddDcReading(reading.Rpm, reading.DcVoltage, reading.DcCurrent);
            }
            if (rpm != 1000 || advanced || !storyboard.IsCurrentStep(speedStepId)) return;
            advanced = true;
            ShowSpeed(false);
            storyboard.GoNextStep();
        }

        private void Update()
        {
            if (generator && rotor && rpm > 0)
                rotor.Rotate(Vector3.forward, rpm * 6f * Time.deltaTime, Space.Self);
        }

        private static void SetLamp(GameObject on, GameObject off, bool powered)
        {
            if (off) off.SetActive(!powered);
            if (on) on.SetActive(powered);
        }

        private void Refresh()
        {
            var source = HydroExperimentOneReadings.ReadLoaded(generator ? rpm : 0f, 4f);
            float dcLoadCurrent = inverter ? HydroFreeLabReadings.DcLedCurrent : 0f;
            float acLoadInput = inverter && acLoad ? HydroFreeLabReadings.AcLampBatteryCurrent : 0f;
            float acLoadVoltage = inverter && acLoad ? HydroFreeLabReadings.GridVoltage : 0f;
            float acLoadCurrent = inverter && acLoad ? HydroFreeLabReadings.AcLampDisplayedCurrent : 0f;
            float batteryVoltage = inverter ? HydroFreeLabReadings.BatteryVoltage : 0f;
            float balance = HydroFreeLabReadings.ChargeSource(source) - dcLoadCurrent - acLoadInput;
            float batteryCurrent = inverter ? HydroFreeLabReadings.BatteryCurrent(balance, grid) : 0f;
            float gridVoltage = grid ? HydroFreeLabReadings.GridVoltage : 0f;
            float gridCurrent = grid ? HydroFreeLabReadings.GridCurrent(balance) : 0f;
            SetLamp(dcLamp, dcLampOff, inverter);
            SetLamp(acLamp, acLampOff, inverter && acLoad);
            if (speedText) speedText.text = rpm.ToString();
            float[] values = { rpm, source.AcVoltage, source.AcCurrent, source.AcPower, source.DcVoltage, source.DcCurrent, source.DcPower,
                batteryVoltage, batteryCurrent, batteryVoltage * batteryCurrent, batteryVoltage, dcLoadCurrent, batteryVoltage * dcLoadCurrent,
                acLoadVoltage, acLoadCurrent, acLoadVoltage * acLoadCurrent, gridVoltage, gridCurrent, gridVoltage * gridCurrent };
            for (int i = 0; i < values.Length && i < meters.Length; i++)
                if (meters[i]) meters[i].text = values[i].ToString(i == 0 ? "0" : "0.00");
        }
    }
}
