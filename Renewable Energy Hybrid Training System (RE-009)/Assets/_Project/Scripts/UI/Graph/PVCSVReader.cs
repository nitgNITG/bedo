using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
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

    // Wind calibration is opt-in. The existing PV reader and Free Mode data
    // continue to use PVData.csv and Calculate exactly as before.
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
        if (calibration == null || float.IsNaN(rpm) || float.IsInfinity(rpm) ||
            float.IsNaN(resistance) || float.IsInfinity(resistance) || resistance <= 0f)
            return false;

        var factors = new SortedDictionary<float, Vector2>();
        using (var reader = new StringReader(calibration.text))
        {
            reader.ReadLine(); // R, C1, C2, followed by workbook reference values.
            string row;
            while ((row = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(row)) continue;
                string[] cells = row.Split(',');
                if (cells.Length < 3 ||
                    !float.TryParse(cells[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ||
                    !float.TryParse(cells[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float c1) ||
                    !float.TryParse(cells[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float c2) ||
                    r <= 0f || c1 <= 0f || c2 <= 0f)
                    return false;
                factors[r] = new Vector2(c1, c2);
            }
        }
        if (factors.Count == 0) return false;

        // Interpolate only the source calibration factors, then evaluate the
        // equations at the exact RPM and load. Do not interpolate power values.
        var lower = factors.First();
        var upper = factors.Last();
        foreach (var pair in factors)
        {
            if (pair.Key <= resistance) lower = pair;
            if (pair.Key >= resistance) { upper = pair; break; }
        }
        Vector2 constants = lower.Key == upper.Key ? lower.Value :
            Vector2.Lerp(lower.Value, upper.Value,
                Mathf.InverseLerp(lower.Key, upper.Key, resistance));
        reading.rpm = Mathf.Clamp(rpm, 0f, 1000f);
        reading.resistance = resistance;
        reading.acVoltage = 0.055f * reading.rpm;
        reading.acCurrent = reading.acVoltage / resistance;
        reading.acPower = reading.acVoltage * reading.acCurrent;
        reading.dcVoltage = reading.acVoltage / 1.2f * constants.x;
        reading.dcCurrent = reading.dcVoltage / (resistance * constants.y);
        reading.dcPower = reading.dcVoltage * reading.dcCurrent;
        return true;
    }

    public List<PVPoint> data = new List<PVPoint>();

    [Header("Mathematical model")]
    [Tooltip("The source workbook formula is Vo = 22 × C1.")]
    [SerializeField] private float openCircuitVoltageMultiplier = 22f;
    [Tooltip("The source workbook formula is ISC = V / R × 2.5.")]
    [SerializeField] private float shortCircuitCurrentMultiplier = 2.5f;


    private void Start()
    {
        LoadCSV();
    }



    private void LoadCSV()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "PVData.csv"
        );


        if (!File.Exists(path))
        {
            Debug.LogError(
                "CSV not found: " + path
            );

            return;
        }


        string[] lines = File.ReadAllLines(path);



        // Skip:
        // Line 0 = headers
        // Line 1 = descriptions
        // Start from line 2

        for (int i = 2; i < lines.Length; i++)
        {

            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;


            string[] values =
                lines[i].Split(',');



            // Need at least 13 columns
            if (values.Length < 13)
                continue;



            PVPoint point =
                new PVPoint();



            point.intensity =
                Parse(values[0]);


            point.irradiance =
                Parse(values[1]);


            point.tilt =
                Parse(values[2]);


            point.azimuth =
                Parse(values[3]);


            point.sunAngle =
                Parse(values[4]);



            point.resistance =
                Parse(values[6]);

            point.constantC1 = Parse(values[7]);
            point.openCircuitVoltage = Parse(values[8]);
            point.adjustedIrradiance = Parse(values[5]);
            point.shortCircuitCurrent = Parse(values[10]);

            // Register the calibration before evaluating this row.
            data.Add(point);

            // DC Voltage, DC Current and Power are formula columns in the
            // workbook, so calculate them rather than trusting stale exports.
            Calculate(point.intensity, point.tilt, point.azimuth, point.sunAngle,
                point.resistance, out PVPoint calculated);
            point.voltage = calculated.voltage;
            point.current = calculated.current;
            point.power = calculated.power;
        }


        Debug.Log(
            "PV rows loaded: "
            + data.Count
        );

        Debug.Log(
    "First Point: " +
    data[0].voltage +
    "V  " +
    data[0].current +
    "A"
);
    }




    float Parse(string value)
    {
        float result;


        float.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out result
        );


        return result;
    }

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
