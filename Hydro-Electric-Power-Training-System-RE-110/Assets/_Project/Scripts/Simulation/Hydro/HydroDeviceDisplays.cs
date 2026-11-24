using System;
using TMPro;
using TutorialFramework;
using UnityEngine;

namespace Bedo.Hydro
{
    [DefaultExecutionOrder(1000)]
    public sealed class HydroDeviceDisplays : MonoBehaviour
    {
        [Serializable]
        public sealed class GuidedSource
        {
            public StoryboardSteps board;
            public HydroGuidedSpeed speed;
            public HydroMpptGuided mppt;
            public HydroOffGridGuided offGrid;
            public Renderer mainPowerCover;
            public WireInteraction[] umdPower;
            public WireInteraction[] umdInputs;
        }
        public GuidedSource[] guided;
        public HydroFreeLabMonitor freeMonitor;
        public FreeLabControl mainPower;
        public FreeLabWireDefinition[] umdPowerWires;
        public FreeLabWireDefinition[] umdInputWires;
        public GameObject umdCanvas, turbineCanvas;
        public TMP_Text umdText, turbineText;

        private static bool Complete(WireInteraction[] wires)
        {
            if (wires == null || wires.Length == 0) return false;
            foreach (var wire in wires)
                if (!wire || !wire.wireObject || !wire.wireObject.activeInHierarchy) return false;
            return true;
        }
        private static bool Complete(FreeLabManager lab, FreeLabWireDefinition[] wires)
        {
            if (!lab || wires == null || wires.Length == 0) return false;
            foreach (var wire in wires)
                if (!wire || !lab.HasConnection(wire.endpointA, wire.endpointB)) return false;
            return true;
        }
        private void LateUpdate()
        {
            bool umdOn = false, turbineOn = false, inputConnected = false;
            HydroExperimentOneReadings.Reading reading = default;
            var manager = InteractionManager.Instance;
            if (manager && manager.Free && freeMonitor && freeMonitor.isActiveAndEnabled)
            {
                var lab = FreeLabManager.Instance;
                bool powered = mainPower && mainPower.Value;
                umdOn = powered && Complete(lab, umdPowerWires);
                turbineOn = powered && freeMonitor.CanAdjustSpeed && freeMonitor.output.Value;
                inputConnected = Complete(lab, umdInputWires);
                reading = freeMonitor.GeneratorReading;
            }
            else if (manager && manager.Guided)
                foreach (var source in guided)
                {
                    if (!source.board || !manager.IsExperimentActive(source.board)) continue;
                    bool powered = source.mainPowerCover && !source.mainPowerCover.enabled;
                    umdOn = powered && Complete(source.umdPower);
                    inputConnected = Complete(source.umdInputs);
                    if (source.offGrid && source.offGrid.isActiveAndEnabled)
                    {
                        turbineOn = powered && source.offGrid.GeneratorEnabled;
                        reading = source.offGrid.GeneratorReading;
                    }
                    else if (source.mppt && source.mppt.isActiveAndEnabled)
                    {
                        turbineOn = powered && source.mppt.GeneratorEnabled;
                        reading = source.mppt.GeneratorReading;
                    }
                    else if (source.speed)
                    {
                        turbineOn = powered && source.speed.OutputEnabled;
                        reading = source.speed.CurrentReading;
                    }
                    break;
                }
            if (umdCanvas.activeSelf != umdOn) umdCanvas.SetActive(umdOn);
            if (turbineCanvas.activeSelf != turbineOn) turbineCanvas.SetActive(turbineOn);
            if (umdOn) umdText.text = "<line-height=140%>" + Electrical(inputConnected ? reading : default);
            if (turbineOn)
                turbineText.text = "<line-height=140%>" + Row(reading.Rpm.ToString("0.##"), "RPM") + "\n" + Electrical(reading);
        }
        private static string Row(string value, string unit) =>
            "<mspace=0.52em>" + value + "</mspace><pos=75%>" + unit;
        private static string Electrical(HydroExperimentOneReadings.Reading reading) =>
            Row(reading.AcVoltage.ToString("F2"), "V") + "\n" +
            Row(reading.AcCurrent.ToString("F2"), "A") + "\n" +
            Row(reading.AcPower.ToString("F2"), "W");
    }
}
