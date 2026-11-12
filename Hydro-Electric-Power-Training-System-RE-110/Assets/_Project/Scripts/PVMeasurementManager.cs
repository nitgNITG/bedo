using System.Collections.Generic;
using UnityEngine;

namespace Template
{
    public class PVMeasurementManager : MonoBehaviour
    {
        [SerializeField] private PVPanelSimulation pvPanel;

        public List<PVMeasurement> measurements = new();

        public void RecordMeasurement()
        {
            PVMeasurement measurement = new PVMeasurement(
                pvPanel.RheostatResistance,
                pvPanel.Voltage,
                pvPanel.Current
            );

            measurements.Add(measurement);

            Debug.Log(
                $"Voltage: {measurement.voltage:F2} V | " +
                $"Current: {measurement.current:F2} A | " +
                $"Power: {measurement.Power:F2} W"
            );
        }
    }
}