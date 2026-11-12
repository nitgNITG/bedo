using UnityEngine;

namespace Bedo.Hydro
{
    public static class HydroExperimentOneReadings
    {
        private static readonly float[] UnloadedRpm =
            { 0, 696.5f, 731.4f, 762, 785.4f, 791.4f, 806.2f, 819.9f, 844.3f, 864.9f, 881.8f, 906.5f, 934.7f, 965.8f, 990.2f };
        private static readonly float[] UnloadedDc =
            { 0, 38.43f, 40.28f, 42.12f, 43.36f, 43.81f, 44.63f, 45.58f, 46.91f, 47.93f, 49.16f, 50.47f, 51.81f, 53.86f, 55.17f };
        private static readonly float[] Resistance = { 4, 20, 40, 60, 80, 100 };
        private static readonly float[] VoltageFactor = { 1, 1.3f, 1.6f, 1.9f, 2.2f, 2.5f };
        private static readonly float[] CurrentFactor = { 1, .26f, .16f, .13f, .13f, 3 };

        public const int LoadedMaximumRpm = 1000;
        public const int LoadedSpeedIncrement = 200;
        public static int UnloadedMaximumStop => UnloadedRpm.Length - 1;

        public readonly struct Reading
        {
            public readonly float Rpm, AcVoltage, AcCurrent, DcVoltage, DcCurrent;
            public float AcPower => AcVoltage * AcCurrent;
            public float DcPower => DcVoltage * DcCurrent;

            internal Reading(float rpm, float acVoltage, float acCurrent, float dcVoltage, float dcCurrent)
            {
                Rpm = rpm;
                AcVoltage = acVoltage;
                AcCurrent = acCurrent;
                DcVoltage = dcVoltage;
                DcCurrent = dcCurrent;
            }
        }

        // Sheet1 O3:P16, plus the stopped state.
        public static Reading ReadUnloaded(int stop)
        {
            int index = Mathf.Clamp(stop, 0, UnloadedMaximumStop);
            float rpm = UnloadedRpm[index];
            return new Reading(rpm, .4f * rpm, 0, UnloadedDc[index], 0);
        }

        // Sheet1 A3:K38. Both rheostat leads must be connected before using this reading.
        public static Reading ReadLoaded(float rpm, float ohms)
        {
            rpm = Mathf.Clamp(rpm, 0, LoadedMaximumRpm);
            ohms = Mathf.Clamp(ohms, Resistance[0], Resistance[Resistance.Length - 1]);
            int upper = 1;
            while (ohms > Resistance[upper])
                upper++;
            int lower = upper - 1;
            float blend = Mathf.InverseLerp(Resistance[lower], Resistance[upper], ohms);
            float ac = .4f * rpm;
            float dc = ac / 1.2f * Mathf.Lerp(VoltageFactor[lower], VoltageFactor[upper], blend);
            float dcCurrent = dc / (ohms * Mathf.Lerp(CurrentFactor[lower], CurrentFactor[upper], blend));
            return new Reading(rpm, ac, ac / ohms, dc, dcCurrent);
        }
    }
}