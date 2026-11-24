using UnityEngine;

namespace Bedo.Hydro
{
    internal static class HydroFreeLabReadings
    {
        internal const float BatteryVoltage = 24f;
        internal const float GridVoltage = 220f;
        internal const float ChargeLimit = 9f;
        internal const float DcHalogenCurrent = 2f;
        internal const float DcLedCurrent = .2f;
        internal const float AcLampDisplayedCurrent = .2f;
        internal const float AcLampBatteryCurrent = 2f;

        internal static float ChargeSource(HydroExperimentOneReadings.Reading generator) =>
            generator.DcVoltage >= BatteryVoltage ? generator.DcCurrent : 0f;

        internal static float BatteryCurrent(float balance, bool grid) =>
            grid ? ChargeLimit : Mathf.Min(balance, ChargeLimit);

        internal static float DumpCurrent(float balance) =>
            Mathf.Max(0f, balance - ChargeLimit);

        internal static float GridCurrent(float balance) =>
            (Mathf.Max(0f, ChargeLimit - balance) - DumpCurrent(balance)) * BatteryVoltage / GridVoltage;
    }
}
