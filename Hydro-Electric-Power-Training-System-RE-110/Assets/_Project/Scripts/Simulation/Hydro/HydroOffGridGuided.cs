using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Bedo.Hydro
{
    public sealed class HydroOffGridGuided : MonoBehaviour
    {
        public StoryboardSteps storyboard;
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
        private int rpm;

        private void OnEnable()
        {
            referenceControls = panel.GetComponent<HydroReferenceControls>();
            if (referenceControls) referenceControls.enabled = false;
            slider.minValue = 0;
            slider.maxValue = 5;
            slider.wholeNumbers = true;
            slider.interactable = true;
            slider.SetValueWithoutNotify(0);
            slider.onValueChanged.AddListener(SetSpeed);
            shortcut.onClick.AddListener(ToggleSpeed);
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
            if (!speedUnlocked || !panel.interactable) return;
            rpm = Mathf.Clamp(Mathf.RoundToInt(stop), 0, 5) * 200;
            Refresh();
            if (graph && generator && rpm > 0)
            {
                float dcVoltage = rpm * .4f / 1.2f;
                graph.AddDcReading(rpm, dcVoltage, dcVoltage / 4f);
            }
            if (rpm != 1000 || advanced) return;
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
            // Hydro workbook, Sheet1 rows 70–76. Preserve the printed load
            // constants and the subtraction in column W.
            float acVoltage = generator ? rpm * .4f : 0;
            float acCurrent = acVoltage / 4f;
            float dcVoltage = acVoltage / 1.2f;
            float dcCurrent = dcVoltage / 4f;
            float dcLoadCurrent = inverter ? 20.8f : 0;
            float acLoadInput = inverter && acLoad ? 20.8f : 0;
            float acLoadVoltage = inverter && acLoad ? 220f : 0;
            float acLoadCurrent = inverter && acLoad ? 2.27f : 0;
            float batteryVoltage = inverter ? 24f : 0;
            float balance = dcCurrent - dcLoadCurrent - acLoadInput;
            float batteryCurrent = !inverter ? 0 : grid ? balance - 40f : Mathf.Min(balance, 40f);
            float gridCurrent = grid ? -batteryCurrent * 24f / 220f : 0;
            SetLamp(dcLamp, dcLampOff, inverter);
            SetLamp(acLamp, acLampOff, inverter && acLoad);
            if (speedText) speedText.text = rpm.ToString();
            float[] values = { rpm, acVoltage, acCurrent, acVoltage * acCurrent, dcVoltage, dcCurrent, dcVoltage * dcCurrent,
                batteryVoltage, batteryCurrent, batteryVoltage * batteryCurrent, inverter ? 24f : 0, dcLoadCurrent, 24f * dcLoadCurrent,
                acLoadVoltage, acLoadCurrent, acLoadVoltage * acLoadCurrent, grid ? 220f : 0, gridCurrent, 220f * gridCurrent };
            for (int i = 0; i < values.Length && i < meters.Length; i++)
                if (meters[i]) meters[i].text = values[i].ToString(i == 0 ? "0" : "0.00");
        }
    }
}
