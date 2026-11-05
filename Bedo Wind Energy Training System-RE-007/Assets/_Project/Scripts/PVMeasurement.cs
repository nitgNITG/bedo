using System;
using UnityEngine;

namespace Template
{
    [Serializable]
    public class PVMeasurement
    {
        public float resistance;
        public float voltage;
        public float current;

        public float Power => voltage * current;

        public PVMeasurement(
            float resistance,
            float voltage,
            float current)
        {
            this.resistance = resistance;
            this.voltage = voltage;
            this.current = current;
        }
    }
}