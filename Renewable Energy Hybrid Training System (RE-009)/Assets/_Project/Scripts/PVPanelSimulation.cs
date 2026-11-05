using UnityEngine;

namespace Template
{
    public class PVPanelSimulation : MonoBehaviour
    {
        [Header("PV Panel")]
        [SerializeField] private float openCircuitVoltage = 20f;
        [SerializeField] private float shortCircuitCurrent = 1.05f;

        [Header("Light")]
        [Range(0f, 1f)]
        [SerializeField] private float lightIntensity = 1f;

        [Header("Rheostat")]
        [SerializeField] private float rheostatResistance = 1000f;

        public float Voltage { get; private set; }
        public float Current { get; private set; }
        public float Power => Voltage * Current;

        public float LightIntensity
        {
            get => lightIntensity;
            set => lightIntensity = Mathf.Clamp01(value);
        }

        public float RheostatResistance
        {
            get => rheostatResistance;
            set => rheostatResistance = Mathf.Max(0.1f, value);
        }

        private void Update()
        {
            CalculateOutput();
        }

        private void CalculateOutput()
        {
            // Light affects the available current
            float maxCurrent = shortCircuitCurrent * lightIntensity;

            // Simple load calculation
            Current = maxCurrent *
                      (1f - Mathf.Exp(-rheostatResistance / 10f));

            // Voltage decreases as current demand increases
            float loadFactor = Current / Mathf.Max(maxCurrent, 0.001f);

            Voltage = openCircuitVoltage *
                      (1f - Mathf.Pow(loadFactor, 2f));

            Voltage = Mathf.Clamp(Voltage, 0f, openCircuitVoltage);
        }
    }
}