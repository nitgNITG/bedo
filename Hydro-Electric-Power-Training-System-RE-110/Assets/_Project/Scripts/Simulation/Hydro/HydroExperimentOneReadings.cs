using UnityEngine;

namespace Bedo.Hydro
{
    public static class HydroExperimentOneReadings
    {
        private static readonly float[] UnloadedRpm =
            { 0, 696.5f, 731.4f, 762, 785.4f, 791.4f, 806.2f, 819.9f, 844.3f, 864.9f, 881.8f, 906.5f, 934.7f, 965.8f, 990.2f };
        private static readonly float[] UnloadedDc =
            { 0, 38.43f, 40.28f, 42.12f, 43.36f, 43.81f, 44.63f, 45.58f, 46.91f, 47.93f, 49.16f, 50.47f, 51.81f, 53.86f, 55.17f };
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
            return new Reading(rpm, .055f * rpm, 0, UnloadedDc[index], 0);
        }

        // Linear interpolation preserves every measured calibration point exactly.
        public static Reading ReadUnloaded(float position)
        {
            position = Mathf.Clamp(position, 0, UnloadedMaximumStop);
            int lower = Mathf.FloorToInt(position);
            int upper = Mathf.Min(lower + 1, UnloadedMaximumStop);
            float fraction = position - lower;
            float rpm = Mathf.Lerp(UnloadedRpm[lower], UnloadedRpm[upper], fraction);
            return new Reading(rpm, .055f * rpm, 0,
                Mathf.Lerp(UnloadedDc[lower], UnloadedDc[upper], fraction), 0);
        }

        public static float UnloadedPositionAtRpm(float rpm)
        {
            rpm = Mathf.Clamp(rpm, 0, UnloadedRpm[UnloadedMaximumStop]);
            for (int upper = 1; upper <= UnloadedMaximumStop; upper++)
                if (rpm <= UnloadedRpm[upper])
                    return upper - 1 + Mathf.InverseLerp(UnloadedRpm[upper - 1], UnloadedRpm[upper], rpm);
            return UnloadedMaximumStop;
        }

        // Both rheostat leads must be connected before using this reading.
        public static Reading ReadLoaded(float rpm, float ohms)
        {
            rpm = Mathf.Clamp(rpm, 0, LoadedMaximumRpm);
            ohms = Mathf.Max(float.Epsilon, ohms);
            float ac = .055f * rpm;
            float dc = ac * (ohms / (ohms + .8f));
            float dcCurrent = ac / (ohms + .8f);
            return new Reading(rpm, ac, 1.2f * dcCurrent, dc, dcCurrent);
        }
    }
}