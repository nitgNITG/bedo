using TMPro;
using TutorialFramework;
using UnityEngine;

namespace Bedo.Hydro
{
    // Provisional hydraulic demonstration, not a calibrated device characteristic.
    [DefaultExecutionOrder(1100)]
    public sealed class HydroHydraulicReadout : MonoBehaviour
    {
        public HydroSpeedInput input;
        public HydroGuidedSpeed experimentOne;
        public TMP_Text flow, pressure, summary;
        [Min(1f)] public float referenceRpm = 1000f;
        [Min(0f)] public float referenceFlowLitresPerMinute = 20f;
        [Min(0f)] public float referencePressureBar = 30f;
        public float Flow { get; private set; }
        public float Pressure { get; private set; }
        public float HydraulicPower { get; private set; }
        public float Efficiency { get; private set; }

        private void LateUpdate()
        {
            if (!input) return;
            var manager = InteractionManager.Instance;
            HydroExperimentOneReadings.Reading reading = default;
            if (manager && manager.Free && input.freeLab)
                reading = input.freeLab.GeneratorReading;
            else if (manager && manager.Guided)
            {
                if (input.experimentTwo && manager.IsExperimentActive(input.experimentTwo.storyboard))
                    reading = input.experimentTwo.GeneratorReading;
                else if (input.experimentThree && manager.IsExperimentActive(input.experimentThree.storyboard))
                    reading = input.experimentThree.GeneratorReading;
                else if (experimentOne) reading = experimentOne.CurrentReading;
            }
            float fraction = Mathf.Max(0f, reading.Rpm) / Mathf.Max(1f, referenceRpm);
            Flow = referenceFlowLitresPerMinute * fraction;
            Pressure = referencePressureBar * fraction;
            HydraulicPower = Pressure * Flow * (100000f / 60000f);
            Efficiency = HydraulicPower > 0f ? reading.DcPower / HydraulicPower : 0f;
            if (flow) flow.text = Flow.ToString("F2");
            if (pressure) pressure.text = Pressure.ToString("F2");
            if (summary)
                summary.text = "Estimated model | Hydraulic: " + HydraulicPower.ToString("F1") +
                    " W | Efficiency: " + (Efficiency * 100f).ToString("F1") +
                    "%\nAssumed at " + referenceRpm.ToString("0.##") + " RPM: " +
                    referenceFlowLitresPerMinute.ToString("0.##") + " L/min, " +
                    referencePressureBar.ToString("0.##") + " bar";
        }
    }
}
