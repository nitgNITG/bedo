using System;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Bedo.Hydro
{
    public sealed class HydroFreeLabMonitor : MonoBehaviour
    {
        [Serializable]
        public sealed class Page
        {
            public GameObject system;
            public GameObject graphs;
            public Interaction graphButton;
            public HydroGuidedMonitor chart;
            public TMP_Text[] meters;
        }

        [Serializable]
        public sealed class Lamp
        {
            public bool ac;
            public bool led;
            public FreeLabConnectionGroup[] paths;
            public GameObject on;
            public GameObject off;

            public bool Connected(FreeLabManager lab) => paths.Any(path => path && path.IsComplete(lab));
        }

        public HydroFreeLabCircuits circuits;
        public Page[] pages;
        public Lamp[] lamps;
        public GameObject monitors;
        public GameObject blackScreen;
        public Interaction systemButton;
        public WindMonitorTabs tabs;
        public FreeLabControl hydroEmergency, hydroBreaker, hydroPower, run, output, inverter, mppt;
        public FreeLabConnectionGroup generatorWiring, gridWiring, mpptWiring, inverterWiring;
        public Slider speed;
        public Button shortcut;
        public CanvasGroup speedPanel;
        public TMP_Text rpmLabel, mpptText;
        public GameObject mpptScreen;
        public Renderer mpptHousing;
        public GameObject hydroPowerGlow;
        public Transform rotor;
        public GameObject[] guidedControlVisuals;
        public GameObject[] passiveWindHardware;
        public Template.OnMouseDownE hydroOffButton;

        private FreeLabWindController labView;
        private bool graphShown;
        private int profile;
        private bool previousLoaded;
        private float lastRpm, lastVoltage, lastCurrent;

        public bool OwnsExporter(PVExperimentExporter exporter) => profile > 0 &&
            pages[profile - 1].graphs.GetComponentInChildren<PVExperimentExporter>(true) == exporter;

        public void Begin(FreeLabWindController view)
        {
            labView = view;
            foreach (var visual in guidedControlVisuals) visual.SetActive(false);
            foreach (var hardware in passiveWindHardware)
            {
                foreach (var collider in hardware.GetComponentsInChildren<BoxCollider>(true))
                    collider.enabled = false;
                hardware.SetActive(true);
            }
            if (passiveWindHardware.Length > 0)
                passiveWindHardware[0].transform.parent.gameObject.SetActive(true);
            hydroOffButton.mouseDown = new UnityEngine.Events.UnityEvent();
            hydroOffButton.mouseDown.AddListener(StopHydro);
            var reference = speedPanel.GetComponent<HydroReferenceControls>();
            if (reference) reference.enabled = false;
            speed.wholeNumbers = true;
            speed.minValue = 0;
            speed.SetValueWithoutNotify(0);
            shortcut.onClick.AddListener(ToggleSpeed);
            SetSpeedVisible(false);
            foreach (var page in pages)
            {
                page.graphButton.gameObject.SetActive(true);
                var guidedMeters = page.system.GetComponentInChildren<HydroSystemMeters>(true);
                if (guidedMeters) guidedMeters.enabled = false;
            }
            ConfigureFirstGraphButtons();
            ApplyPages();
        }

        private void ConfigureFirstGraphButtons()
        {
            var page = pages[0];
            foreach (var button in page.graphs.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "Voltage Graph Tab")
                {
                    button.onClick = new Button.ButtonClickedEvent();
                    button.onClick.AddListener(page.chart.ShowGraph);
                }
                else if (button.name == "AC-Speed Graph Tab")
                {
                    button.onClick = new Button.ButtonClickedEvent();
                    button.onClick.AddListener(page.chart.ShowCurrentGraph);
                }
            }
            foreach (var click in page.graphs.GetComponentsInChildren<Template.OnMouseDownE>(true))
            {
                if (click.name != "Clear") continue;
                click.mouseDown = new UnityEngine.Events.UnityEvent();
                click.mouseDown.AddListener(page.chart.ClearReadings);
            }
        }

        public bool TogglePage(bool showGraph)
        {
            if (profile == 0 || graphShown == showGraph) return false;
            graphShown = showGraph;
            ApplyPages();
            var page = pages[profile - 1];
            if (showGraph)
            {
                page.graphButton.PlaySound();
                page.chart.ShowGraph();
            }
            else systemButton.PlaySound();
            return true;
        }

        private void ApplyPages()
        {
            blackScreen.SetActive(profile == 0);
            monitors.SetActive(profile > 0 && !graphShown);
            systemButton.Allow = profile > 0 && graphShown;
            tabs.SetPageSelection(graphShown);
            for (int i = 0; i < pages.Length; i++)
            {
                bool active = i + 1 == profile;
                pages[i].system.SetActive(active);
                pages[i].graphs.SetActive(active && graphShown);
                pages[i].graphButton.Allow = active && !graphShown;
            }
        }

        private void StopHydro()
        {
            output.SetValue(false);
            run.SetValue(false);
            hydroPower.SetValue(false);
        }

        private void ToggleSpeed() => SetSpeedVisible(!speedPanel.interactable);

        private void SetSpeedVisible(bool visible)
        {
            speed.interactable = visible && profile > 0 && hydroPower.Value;
            speedPanel.alpha = visible ? 1 : 0;
            speedPanel.interactable = speedPanel.blocksRaycasts = visible;
        }

        public void Refresh()
        {
            int active = circuits.ActiveExperiment;
            if (profile != active)
            {
                if (profile > 0) pages[profile - 1].chart.ClearReadings();
                profile = active;
                graphShown = false;
                speed.SetValueWithoutNotify(0);
                SetSpeedVisible(false);
                lastRpm = -1;
                ApplyPages();
            }

            var lab = labView.lab;
            bool hydroOn = active > 0 && hydroEmergency.Value && hydroBreaker.Value && hydroPower.Value;
            if (!hydroOn) StopHydro();
            if (hydroPowerGlow && hydroPowerGlow.activeSelf != hydroOn)
                hydroPowerGlow.SetActive(hydroOn);
            if (active != 2 && mppt.Value) mppt.SetValue(false);
            if (active != 3 && inverter.Value) inverter.SetValue(false);
            bool running = hydroOn && run.Value;
            bool generating = running && output.Value && generatorWiring.IsComplete(lab);
            speed.interactable = hydroOn && speedPanel.interactable;

            var rheostat = labView.rheostats.FirstOrDefault(item => item.connections.IsComplete(lab));
            bool loaded = active != 1 || rheostat != null;
            speed.maxValue = loaded ? 5 : HydroExperimentOneReadings.UnloadedMaximumStop;
            if (previousLoaded != loaded)
            {
                previousLoaded = loaded;
                if (loaded && lastRpm > 0) speed.SetValueWithoutNotify(Mathf.Round(lastRpm / 200f));
            }

            int speedStop = Mathf.RoundToInt(speed.value);
            float requestedRpm = loaded ? speedStop * 200 : HydroExperimentOneReadings.ReadUnloaded(speedStop).Rpm;
            float resistance = active == 1 && rheostat != null ? rheostat.controller.CurrentResistance : 4f;
            var source = generating && !loaded ? HydroExperimentOneReadings.ReadUnloaded(speedStop) :
                HydroFreeLabReadings.Generator(generating ? requestedRpm : 0f, resistance);
            if (rpmLabel) rpmLabel.text = requestedRpm.ToString("0.##");
            if (running && rotor && requestedRpm > 0f)
                rotor.Rotate(Vector3.forward, requestedRpm * 6f * Time.deltaTime, Space.Self);

            bool mpptOn = active == 2 && mppt.Value && mpptWiring.IsComplete(lab);
            bool inverterOn = active == 3 && inverter.Value && inverterWiring.IsComplete(lab);
            bool grid = inverterOn && gridWiring.IsComplete(lab);
            float dcLoad = 0;
            bool acLoad = false;
            foreach (var lamp in lamps)
            {
                bool lit = (lamp.ac ? inverterOn : mpptOn || inverterOn) && lamp.Connected(lab);
                if (lamp.off) lamp.off.SetActive(!lit);
                if (lamp.on) lamp.on.SetActive(lit);
                if (!lit) continue;
                if (lamp.ac) acLoad = true;
                else dcLoad += lamp.led ? HydroFreeLabReadings.DcLedCurrent : HydroFreeLabReadings.DcHalogenCurrent;
            }

            float battery = mpptOn || inverterOn ? HydroFreeLabReadings.BatteryVoltage : 0;
            float batteryCurrent = battery > 0 ? HydroFreeLabReadings.BatteryCurrent(source.DcCurrent, dcLoad, acLoad, grid) : 0;
            float acVoltage = acLoad ? HydroFreeLabReadings.GridVoltage : 0;
            float acCurrent = acLoad ? HydroFreeLabReadings.AcLampDisplayedCurrent : 0;
            float gridVoltage = grid ? HydroFreeLabReadings.GridVoltage : 0;
            float gridCurrent = grid ? -batteryCurrent * battery / gridVoltage : 0;
            mpptScreen.SetActive(mpptOn);
            if (mpptText && mpptOn)
                mpptText.text = (Mathf.Clamp01(batteryCurrent / HydroFreeLabReadings.ChargeLimit) * 100f).ToString("F2") + "%";
            if (active == 0) return;

            var page = pages[active - 1];
            float loadVoltage = dcLoad > 0 ? battery : 0;
            float[] values = { source.Rpm, source.AcVoltage, source.AcCurrent, source.AcPower,
                source.DcVoltage, source.DcCurrent, source.DcPower, battery, batteryCurrent,
                battery * batteryCurrent, loadVoltage, dcLoad, loadVoltage * dcLoad,
                acVoltage, acCurrent, acVoltage * acCurrent, gridVoltage, gridCurrent, gridVoltage * gridCurrent };
            for (int i = 0; i < page.meters.Length && i < values.Length; i++)
                if (page.meters[i]) page.meters[i].text = values[i].ToString(i == 0 ? "0.##" : "F2");

            if (source.Rpm > 0 && (!Mathf.Approximately(lastRpm, source.Rpm) ||
                !Mathf.Approximately(lastVoltage, source.DcVoltage) || !Mathf.Approximately(lastCurrent, source.DcCurrent)))
            {
                page.chart.AddDcReading(source.Rpm, source.DcVoltage, source.DcCurrent);
                lastRpm = source.Rpm;
                lastVoltage = source.DcVoltage;
                lastCurrent = source.DcCurrent;
            }
        }

        private void OnDestroy()
        {
            if (shortcut) shortcut.onClick.RemoveListener(ToggleSpeed);
        }
    }
}
