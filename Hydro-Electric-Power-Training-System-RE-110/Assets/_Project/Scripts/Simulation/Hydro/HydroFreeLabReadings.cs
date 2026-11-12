using UnityEngine;

namespace Bedo.Hydro
{
    internal static class HydroFreeLabReadings
    {
        internal const float BatteryVoltage = 24f;
        internal const float GridVoltage = 220f;
        internal const float ChargeLimit = 40f;
        internal const float DcHalogenCurrent = 20.8f;
        internal const float DcLedCurrent = 2.1f;
        // Sheet1 specifies one AC lamp model. Apply it to either AC lamp type.
        internal const float AcLampDisplayedCurrent = 2.27f;
        internal const float AcLampBatteryCurrent = 20.8f;

        internal static HydroExperimentOneReadings.Reading Generator(float rpm, float resistance)
        {
            if (resistance <= 100f)
                return HydroExperimentOneReadings.ReadLoaded(rpm, resistance);

            // Keep the last measured coefficients and use the connected resistance in Ohm's law.
            var boundary = HydroExperimentOneReadings.ReadLoaded(rpm, 100f);
            return new HydroExperimentOneReadings.Reading(boundary.Rpm, boundary.AcVoltage,
                boundary.AcVoltage / resistance, boundary.DcVoltage, boundary.DcCurrent * 100f / resistance);
        }

        internal static float BatteryCurrent(float generatorCurrent, float dcLoad, bool acLoad, bool grid)
        {
            float balance = generatorCurrent - dcLoad - (acLoad ? AcLampBatteryCurrent : 0f);
            return grid ? balance - ChargeLimit : Mathf.Min(balance, ChargeLimit);
        }
    }
}
