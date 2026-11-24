using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;


public class PVCSVReader : MonoBehaviour
{
    [System.Serializable]
    public struct OffGridReading
    {
        public float batteryVoltage, batteryCurrent, batteryPower;
        public float dcLoadCurrent, dcLoadPower, acLoadVoltage, acLoadCurrent, acLoadPower;
        public float inverterCurrent, inverterPower, gridVoltage, gridCurrent, gridPower;
        public float availableChargeCurrent, dumpCurrent;
    }

    // EXP4 reads the off-grid columns of the source workbook. AC lamp display
    // current (P) and its DC-equivalent calculation current (Q) are distinct.
    public static bool TryCalculateOffGrid(TextAsset model, float sourceCurrent,
        int dcLoads, int acLoads, bool gridConnected, out OffGridReading reading, bool dcLED = false)
    {
        reading = default;
        if (model == null || float.IsNaN(sourceCurrent) || float.IsInfinity(sourceCurrent)) return false;
        string[] rows = model.text.Split('\n');
        if (rows.Length < 2) return false;
        string[] header = rows[0].Trim().Split(','), cells = rows[1].Trim().Split(',');
        if (cells.Length != header.Length) return false;
        var constants = new Dictionary<string, float>();
        for (int i = 0; i < header.Length; i++)
            if (float.TryParse(cells[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                constants[header[i]] = value;
        string[] required = { "DC_Load_Current_A", "DC_LED_Current_A", "AC_Lamp_Display_Current_A", "AC_Lamp_DC_Equivalent_A", "AC_Lamp_Voltage_V", "Grid_Charge_Current_A", "Battery_Voltage_V" };
        foreach (string name in required) if (!constants.ContainsKey(name)) return false;
        float busVoltage = constants["Battery_Voltage_V"], limit = constants["Grid_Charge_Current_A"];
        if (busVoltage <= 0 || limit <= 0 || constants["AC_Lamp_Voltage_V"] <= 0) return false;
        reading.batteryVoltage = busVoltage;
        reading.dcLoadCurrent = Mathf.Max(0, dcLoads) * constants[dcLED ? "DC_LED_Current_A" : "DC_Load_Current_A"];
        reading.dcLoadPower = busVoltage * reading.dcLoadCurrent;
        reading.acLoadVoltage = acLoads > 0 ? constants["AC_Lamp_Voltage_V"] : 0;
        reading.acLoadCurrent = Mathf.Max(0, acLoads) * constants["AC_Lamp_Display_Current_A"];
        reading.acLoadPower = reading.acLoadVoltage * reading.acLoadCurrent;
        reading.inverterCurrent = Mathf.Max(0, acLoads) * constants["AC_Lamp_DC_Equivalent_A"];
        reading.inverterPower = busVoltage * reading.inverterCurrent;
        float balance = sourceCurrent - reading.dcLoadCurrent - reading.inverterCurrent;
        reading.availableChargeCurrent = Mathf.Clamp(balance, 0, limit); // column S and its limits
        reading.batteryCurrent = gridConnected ? limit : Mathf.Min(balance, limit);
        reading.batteryPower = busVoltage * reading.batteryCurrent;
        reading.dumpCurrent = Mathf.Max(0, balance - limit);
        if (gridConnected)
        {
            // Columns W/X: DC deficit includes the 9 A grid battery charge.
            // Keep the sign: positive grid current means import.
            reading.gridVoltage = constants["AC_Lamp_Voltage_V"];
            reading.gridCurrent = -(balance - limit) * busVoltage / reading.gridVoltage;
            reading.gridPower = reading.gridVoltage * reading.gridCurrent;
        }
        return true;
    }

    public static bool TryCalculateWindGraph(TextAsset reference, float rpm, out float voltage)
    {
        voltage = 0f;
        if (reference == null || float.IsNaN(rpm) || float.IsInfinity(rpm)) return false;
        var samples = new SortedDictionary<float, float>();
        foreach (string row in reference.text.Split('\n').Skip(1))
        {
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cells = row.Trim().Split(',');
            if (cells.Length != 2 ||
                !float.TryParse(cells[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(cells[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y)) return false;
            samples[x] = y;
        }
        if (samples.Count < 2) return false;
        var points = samples.ToArray();
        int upper = 1;
        while (upper < points.Length - 1 && rpm > points[upper].Key) upper++;
        var a = points[upper - 1];
        var b = points[upper];
        // Preserve every reference point exactly. Between points interpolate;
        // outside the supplied range extend the nearest segment, with a zero floor.
        voltage = Mathf.Max(0f, Mathf.LerpUnclamped(a.Value, b.Value, (rpm - a.Key) / (b.Key - a.Key)));
        return true;
    }

    // Wind calculations are independent of the legacy PV calibration reader.
    [System.Serializable]
    public struct WindReading
    {
        public float rpm, resistance, acVoltage, acCurrent, acPower;
        public float dcVoltage, dcCurrent, dcPower;
    }

public static bool TryCalculateWind(TextAsset calibration, float rpm,
        float resistance, out WindReading reading)
    {
        reading = default;
        if (float.IsNaN(rpm) || float.IsInfinity(rpm) ||
            float.IsNaN(resistance) || float.IsInfinity(resistance) || resistance <= 0f)
            return false;

        // E1: use the physical generator/load equations directly. The prior
        // C1/C2 resistance table could make loaded DC voltage and power
        // exceed their physically possible source values.
        reading.rpm = Mathf.Clamp(rpm, 0f, 1000f);
        reading.resistance = resistance;
        reading.acVoltage = 0.055f * reading.rpm;
        reading.dcVoltage = reading.acVoltage * resistance / (resistance + 0.8f);
        reading.dcCurrent = reading.dcVoltage / resistance;
        reading.dcPower = reading.dcVoltage * reading.dcCurrent;
        reading.acCurrent = 1.2f * reading.dcCurrent;
        reading.acPower = reading.acVoltage * reading.acCurrent;
        return true;
    }

    public List<PVPoint> data = new List<PVPoint>();

    [Header("Mathematical model")]
    [Tooltip("The source workbook formula is Vo = 22 × C1.")]
    [SerializeField] private float openCircuitVoltageMultiplier = 22f;
    [Tooltip("The source workbook formula is ISC = V / R × 2.5.")]
    [SerializeField] private float shortCircuitCurrentMultiplier = 2.5f;


    /// <summary>
    /// Recreates the PV workbook's formulas. C1 comes from the CSV resistance
    /// calibration; values between authored resistances are interpolated and
    /// values above 100 ohm use the highest authored calibration (C1 = 2.5,
    /// hence Vo = 55 with the current workbook values).
    /// </summary>
    public bool Calculate(float intensity, float tilt, float azimuth,
        float sunAngle, float resistance, out PVPoint result)
    {
        result = new PVPoint
        {
            intensity = intensity,
            irradiance = 1000f * intensity,
            tilt = tilt,
            azimuth = azimuth,
            sunAngle = sunAngle,
            resistance = resistance
        };

        if (data == null || data.Count == 0)
            return false;

        result.constantC1 = GetConstantForResistance(resistance);
        result.openCircuitVoltage = openCircuitVoltageMultiplier * result.constantC1;
        result.adjustedIrradiance = Mathf.Max(0f,
            result.irradiance * Mathf.Cos((tilt - azimuth) * Mathf.Deg2Rad) *
            Mathf.Cos(sunAngle * Mathf.Deg2Rad));

        // ln(0) is undefined in the source formula. Represent a no-light
        // configuration as an inactive panel instead of producing NaN.
        if (result.adjustedIrradiance <= 0.0001f || resistance <= 0.0001f)
            return true;

        result.voltage = result.openCircuitVoltage *
            (1f - Mathf.Log(result.adjustedIrradiance / 1000f));
        result.shortCircuitCurrent = result.voltage / resistance *
            shortCircuitCurrentMultiplier;
        result.current = result.shortCircuitCurrent *
            result.adjustedIrradiance / 1000f;
        result.power = result.voltage * result.current;
        return true;
    }

    private float GetConstantForResistance(float resistance)
    {
        List<PVPoint> calibration = data
            .Where(point => point.resistance > 0f && point.constantC1 > 0f)
            .GroupBy(point => point.resistance)
            .Select(group => group.First())
            .OrderBy(point => point.resistance)
            .ToList();

        if (calibration.Count == 0)
            return 0f;
        if (resistance <= calibration[0].resistance)
            return calibration[0].constantC1;

        PVPoint highest = calibration[calibration.Count - 1];
        if (resistance >= highest.resistance)
            return highest.constantC1;

        for (int index = 1; index < calibration.Count; index++)
        {
            PVPoint upper = calibration[index];
            if (resistance > upper.resistance)
                continue;

            PVPoint lower = calibration[index - 1];
            return Mathf.Lerp(lower.constantC1, upper.constantC1,
                Mathf.InverseLerp(lower.resistance, upper.resistance, resistance));
        }

        return highest.constantC1;
    }
}
