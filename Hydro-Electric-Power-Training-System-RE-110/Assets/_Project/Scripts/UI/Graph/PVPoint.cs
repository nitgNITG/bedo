using System;

[Serializable]
public class PVPoint
{
    public float intensity;
    public float irradiance;

    public float tilt;
    public float azimuth;
    public float sunAngle;

    public float resistance;

    // Calibration values from the mathematical model. The values below are
    // calculated live instead of being treated as fixed CSV output columns.
    public float constantC1;
    public float openCircuitVoltage;
    public float shortCircuitCurrent;
    public float adjustedIrradiance;

    public float voltage;
    public float current;
    public float power;
}
