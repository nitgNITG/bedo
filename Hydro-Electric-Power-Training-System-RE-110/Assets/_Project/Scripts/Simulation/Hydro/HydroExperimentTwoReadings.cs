using UnityEngine;

namespace Bedo.Hydro
{
    public static class HydroExperimentTwoReadings
    {
        public const float BatteryVoltage = 24f;
        public const float HalogenCurrent = 20.8f;

        public readonly struct Reading
        {
            public readonly HydroExperimentOneReadings.Reading Generator;
            public readonly float LoadCurrent;
            public readonly float BatteryCurrent;
            public float LoadPower => BatteryVoltage * LoadCurrent;
            public float BatteryPower => BatteryVoltage * BatteryCurrent;

            internal Reading(HydroExperimentOneReadings.Reading generator, float loadCurrent, float batteryCurrent)
            {
                Generator = generator;
                LoadCurrent = loadCurrent;
                BatteryCurrent = batteryCurrent;
            }
        }

        // Sheet1 rows 70-75. The single DC halogen case uses column N.
        public static Reading Read(float rpm, bool mainPower, bool generatorEnabled, bool loadEnabled)
        {
            if (!mainPower)
                return default;

            var generator = HydroExperimentOneReadings.ReadLoaded(generatorEnabled ? rpm : 0f, 4f);
            float loadCurrent = loadEnabled ? HalogenCurrent : 0f;
            float batteryCurrent = generator.DcCurrent - loadCurrent;
            return new Reading(generator, loadCurrent, batteryCurrent);
        }
    }
}
