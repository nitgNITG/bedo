using TMPro;
using UnityEngine;

namespace Bedo.Hydro
{
    public sealed class HydroSystemMeters : MonoBehaviour
    {
        [SerializeField] private HydroGuidedSpeed speed;
        [SerializeField] private TMP_Text rpm;
        [SerializeField] private TMP_Text intensity;
        [SerializeField] private TMP_Text acVoltage;
        [SerializeField] private TMP_Text acCurrent;
        [SerializeField] private TMP_Text acPower;
        [SerializeField] private TMP_Text dcVoltage;
        [SerializeField] private TMP_Text dcCurrent;
        [SerializeField] private TMP_Text dcPower;

        private void LateUpdate()
        {
            if (!speed)
                return;
            var reading = speed.CurrentReading;
            Set(rpm, reading.Rpm);
            Set(intensity, reading.Rpm * 100f / HydroExperimentOneReadings.LoadedMaximumRpm);
            Set(acVoltage, reading.AcVoltage);
            Set(acCurrent, reading.AcCurrent);
            Set(acPower, reading.AcPower);
            Set(dcVoltage, reading.DcVoltage);
            Set(dcCurrent, reading.DcCurrent);
            Set(dcPower, reading.DcPower);
        }

        private static void Set(TMP_Text text, float value)
        {
            if (text)
                text.SetText("{0:2}", value);
        }
    }
}