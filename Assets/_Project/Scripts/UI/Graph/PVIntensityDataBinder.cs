using System.Collections;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable bridge from a 0-100 Free Lab intensity slider to PVData.csv.
/// It updates the DC voltage, current, and power displays using the closest
/// supported CSV intensity row.
/// </summary>
public class PVIntensityDataBinder : MonoBehaviour
{
    [System.Serializable]
    public class PVSliderInput
    {
        [Tooltip("Leave unassigned to ignore this CSV field.")]
        public FreeLabSliderControl slider;
        [Tooltip("Optional alternative to a FreeLabSliderControl. Leave both fields empty to ignore this CSV field.")]
        public PivotValueRotator rotator;
        [Tooltip("Optional direct CSV source for the existing physical rheostat. It reports resistance in ohms without changing the Guided Mode controller.")]
        public FreeLabRheostatDataAdapter rheostatAdapter;
        public float sliderMinimum = 0f;
        public float sliderMaximum = 100f;
        public float csvMinimum;
        public float csvMaximum = 100f;
        [Tooltip("Use absolute values for both the control and the CSV angle column.")]
        public bool useAbsoluteValue;

        public bool IsAssigned => slider != null || rotator != null ||
            rheostatAdapter != null;

        public float GetCsvValue()
        {
            if (rheostatAdapter != null)
                return rheostatAdapter.ResistanceOhms;

            float sourceValue = slider != null
                ? slider.Value
                : rotator != null
                    ? rotator.ReportedValue
                    : sliderMinimum;

            if (!IsAssigned ||
                Mathf.Approximately(sliderMinimum, sliderMaximum))
            {
                return csvMinimum;
            }

            float normalized = Mathf.InverseLerp(
                sliderMinimum,
                sliderMaximum,
                sourceValue
            );
            float value = Mathf.Lerp(csvMinimum, csvMaximum, normalized);
            return useAbsoluteValue ? Mathf.Abs(value) : value;
        }
    }

    [Header("Sources")]
    [SerializeField] private PVCSVReader dataReader;

    [Header("Optional CSV Inputs")]
    [Tooltip("0-100 slider maps to CSV intensity 0.1-1.0.")]
    [SerializeField] private PVSliderInput intensity = new PVSliderInput
    {
        csvMinimum = 0.1f,
        csvMaximum = 1f
    };
    [Tooltip("Default mapping is 0-100 slider to 0-100 ohms. Adjust the ranges to match your rheostat slider.")]
    [SerializeField] private PVSliderInput rheostatResistance =
        new PVSliderInput();
    [Tooltip("Default mapping is 0-100 slider to 0-45 degrees.")]
    [SerializeField] private PVSliderInput tiltAngle = new PVSliderInput
    {
        csvMinimum = 0f,
        csvMaximum = 45f
    };
    [Tooltip("Default mapping is 0-100 slider to -40 to +40 degrees.")]
    [SerializeField] private PVSliderInput azimuthAngle = new PVSliderInput
    {
        csvMinimum = -40f,
        csvMaximum = 40f,
        useAbsoluteValue = true
    };
    [Tooltip("Default mapping is 0-100 slider to -40 to +40 degrees.")]
    [SerializeField] private PVSliderInput sunAngle = new PVSliderInput
    {
        csvMinimum = -40f,
        csvMaximum = 40f,
        useAbsoluteValue = true
    };

    [Header("TextMeshPro Displays")]
    [SerializeField] private TMP_Text voltageText;
    [SerializeField] private TMP_Text currentText;
    [SerializeField] private TMP_Text powerText;

    [Header("Legacy UI Text Displays (optional)")]
    [SerializeField] private Text legacyVoltageText;
    [SerializeField] private Text legacyCurrentText;
    [SerializeField] private Text legacyPowerText;

    private void OnEnable()
    {
        Subscribe(intensity);
        Subscribe(rheostatResistance);
        Subscribe(tiltAngle);
        Subscribe(azimuthAngle);
        Subscribe(sunAngle);
    }

    private void OnDisable()
    {
        Unsubscribe(intensity);
        Unsubscribe(rheostatResistance);
        Unsubscribe(tiltAngle);
        Unsubscribe(azimuthAngle);
        Unsubscribe(sunAngle);
    }

    private IEnumerator Start()
    {
        // PVCSVReader loads in Start as well. Wait until all Start calls for
        // this frame complete before reading its data.
        yield return null;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        // In Free Mode, the live PV characteristic binder uses the currently
        // connected rheostat and its actual present resistance. This older
        // CSV bridge remains available to the graph, but must not overwrite
        // that calculated display with a default CSV point.
        if (IsFreeLabDisplayOwnerActive())
            return;

        if (dataReader == null ||
            dataReader.data == null || dataReader.data.Count == 0)
        {
            SetZeroOutput();
            return;
        }

        UpdateFromSlider(0f);
    }

    public void UpdateFromSlider(float sliderPercent)
    {
        if (IsFreeLabDisplayOwnerActive())
            return;

        if (!TryGetCurrentPoint(out PVPoint point))
        {
            SetZeroOutput();
            return;
        }

        SetText(voltageText, legacyVoltageText, point.voltage.ToString("F2"));
        SetText(currentText, legacyCurrentText, point.current.ToString("F2"));
        SetText(powerText, legacyPowerText, point.power.ToString("F2"));
    }

    /// <summary>Returns exactly the point currently shown by this Free Mode display.</summary>
    public bool TryGetCurrentPoint(out PVPoint point)
    {
        return TryGetCurrentPoint(rheostatResistance.GetCsvValue(), out point);
    }

    /// <summary>
    /// Calculates a point using the supplied physical rheostat resistance.
    /// This is used by the graph so every rheostat turn produces its own
    /// measurement instead of selecting one unchanged nearby CSV row.
    /// </summary>
    public bool TryGetCurrentPoint(float resistanceOhms, out PVPoint point)
    {
        point = null;
        if (dataReader == null || dataReader.data == null || dataReader.data.Count == 0)
            return false;

        float intensityValue = GetIntensityValue();
        float tiltValue = tiltAngle.GetCsvValue();
        float azimuthValue = azimuthAngle.GetCsvValue();
        float sunValue = sunAngle.GetCsvValue();

        return dataReader.Calculate(
            intensityValue,
            tiltValue,
            azimuthValue,
            sunValue,
            resistanceOhms,
            out point);
    }

    private float GetIntensityValue()
    {
        // Zero on the physical intensity control means no irradiance, rather
        // than the first non-zero CSV calibration row.
        if (intensity != null && intensity.slider != null &&
            Mathf.Approximately(intensity.slider.Value, intensity.sliderMinimum))
        {
            return 0f;
        }

        return intensity != null ? intensity.GetCsvValue() : 0f;
    }

    private static bool IsFreeLabDisplayOwnerActive()
    {
        return InteractionManager.Instance != null &&
            InteractionManager.Instance.Free &&
            FreeLabPVCharacteristicBinder.HasActiveDisplayOwner;
    }

    private void SetZeroOutput()
    {
        SetText(voltageText, legacyVoltageText, "0.00");
        SetText(currentText, legacyCurrentText, "0.00");
        SetText(powerText, legacyPowerText, "0.00");
    }

    private static void SetText(
        TMP_Text tmpText,
        Text legacyText,
        string value)
    {
        if (tmpText != null)
            tmpText.text = value;

        if (legacyText != null)
            legacyText.text = value;
    }

    private void Subscribe(PVSliderInput input)
    {
        if (input != null && input.slider != null)
            input.slider.ValueChanged += UpdateFromSlider;

        if (input != null && input.rotator != null)
            input.rotator.ReportedValueChanged += UpdateFromRotator;

        if (input != null && input.rheostatAdapter != null)
            FreeLabRheostatDataAdapter.ActiveResistanceChanged +=
                UpdateFromRheostat;
    }

    private void Unsubscribe(PVSliderInput input)
    {
        if (input != null && input.slider != null)
            input.slider.ValueChanged -= UpdateFromSlider;

        if (input != null && input.rotator != null)
            input.rotator.ReportedValueChanged -= UpdateFromRotator;

        if (input != null && input.rheostatAdapter != null)
            FreeLabRheostatDataAdapter.ActiveResistanceChanged -=
                UpdateFromRheostat;
    }

    private void UpdateFromRotator(float angle)
    {
        RefreshDisplay();
    }

    private void UpdateFromRheostat(float resistance)
    {
        RefreshDisplay();
    }

    private static float Difference(float csvValue, PVSliderInput input)
    {
        if (input == null || !input.IsAssigned)
            return 0f;

        float range = Mathf.Max(
            0.0001f,
            Mathf.Abs(input.csvMaximum - input.csvMinimum)
        );
        float candidateValue = input.useAbsoluteValue
            ? Mathf.Abs(csvValue)
            : csvValue;
        return Mathf.Abs(candidateValue - input.GetCsvValue()) / range;
    }
}
