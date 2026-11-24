using UnityEngine;

namespace Bedo.Hydro
{
    public static class HydroExperimentTwoReadings
    {
        public const float BatteryVoltage = HydroFreeLabReadings.BatteryVoltage;
        public const float HalogenCurrent = HydroFreeLabReadings.DcHalogenCurrent;

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

        // The charge controller supplies the battery once the generator reaches battery voltage.
        public static Reading Read(float rpm, bool mainPower, bool generatorEnabled, bool loadEnabled)
        {
            if (!mainPower)
                return default;

            var generator = HydroExperimentOneReadings.ReadLoaded(generatorEnabled ? rpm : 0f, 4f);
            float loadCurrent = loadEnabled ? HalogenCurrent : 0f;
            float batteryCurrent = HydroFreeLabReadings.BatteryCurrent(HydroFreeLabReadings.ChargeSource(generator) - loadCurrent, false);
            return new Reading(generator, loadCurrent, batteryCurrent);
        }
    }
}
