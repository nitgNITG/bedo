using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Free Mode-only PV characteristic calculation display.  It is intentionally
/// separate from PVGuidedDataBinder: it reads the completed Free Lab rheostat
/// group and produces readings only while the Free Lab power is operating.
/// </summary>
[DisallowMultipleComponent]
public sealed class FreeLabPVCharacteristicBinder : MonoBehaviour
{
    /// <summary>
    /// Lets legacy graph data sources recognise that this Free Mode binder is
    /// currently responsible for the on-screen PV readings.
    /// </summary>
    public static bool HasActiveDisplayOwner { get; private set; }

    [Header("Free Lab")]
    [SerializeField] private FreeLabManager freeLabManager;

    [Header("Intensity")]
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private float sliderMinimum = 0f;
    [SerializeField] private float sliderMaximum = 100f;
    [SerializeField] private float goAtSliderMinimum = 0f;
    [SerializeField] private float goAtSliderMaximum = 1f;
    [SerializeField] private float defaultGo = 1f;

    [Header("Optional angles")]
    [SerializeField] private PivotValueRotator tiltRotator;
    [SerializeField] private PivotValueRotator azimuthRotator;
    [SerializeField] private PivotValueRotator sunRotator;

    [Header("Displays")]
    [SerializeField] private TMP_Text voltageText;
    [SerializeField] private TMP_Text currentText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private Text legacyVoltageText;
    [SerializeField] private Text legacyCurrentText;
    [SerializeField] private Text legacyPowerText;

    [Header("Array configuration")]
    [SerializeField] private bool isSeries;
    [SerializeField] private bool isParallel;

    private static readonly float[] ResistanceSteps = { 13f, 20f, 40f, 60f, 80f, 100f };
    private static readonly float[] VoValues = { 22f, 28.6f, 35.2f, 41.8f, 48.4f, 55f };

    private bool lastPowerState;
    private float lastResistance = float.MinValue;

    private void Awake()
    {
        if (freeLabManager == null)
            freeLabManager = FreeLabManager.Instance;
    }

    private void OnEnable()
    {
        HasActiveDisplayOwner = true;

        if (intensitySlider != null)
            intensitySlider.onValueChanged.AddListener(OnInputChanged);

        Subscribe(tiltRotator);
        Subscribe(azimuthRotator);
        Subscribe(sunRotator);
        FreeLabRheostatDataAdapter.ActiveResistanceChanged += OnActiveResistanceChanged;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        HasActiveDisplayOwner = false;

        if (intensitySlider != null)
            intensitySlider.onValueChanged.RemoveListener(OnInputChanged);

        Unsubscribe(tiltRotator);
        Unsubscribe(azimuthRotator);
        Unsubscribe(sunRotator);
        FreeLabRheostatDataAdapter.ActiveResistanceChanged -= OnActiveResistanceChanged;
    }

    private void Update()
    {
        if (freeLabManager == null)
            freeLabManager = FreeLabManager.Instance;

        bool powered = freeLabManager != null && freeLabManager.IsSystemOperating();
        float resistance = FreeLabRheostatDataAdapter.ActiveResistanceOhms;
        if (powered != lastPowerState || !Mathf.Approximately(resistance, lastResistance))
        {
            lastPowerState = powered;
            lastResistance = resistance;
            RefreshDisplay();
        }
    }

    public void UseActiveFreeLabRheostat() => RefreshDisplay();

    public void SetArrayConfiguration(bool series, bool parallel)
    {
        isSeries = series;
        isParallel = parallel;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (!TryComputePoint(out float voltage, out float current, out float power))
        {
            SetOutput(0f, 0f, 0f);
            return;
        }

        SetOutput(voltage, current, power);
    }

    public bool TryComputePoint(out float voltage, out float current, out float power)
    {
        voltage = 0f;
        current = 0f;
        power = 0f;

        if (freeLabManager == null)
            freeLabManager = FreeLabManager.Instance;

        // A physical rheostat must have its complete cable group connected,
        // and Free Lab's shared main power must be operating.
        if (freeLabManager == null || !freeLabManager.IsSystemOperating() ||
            !freeLabManager.IsSolarPanelSourceOn() ||
            !FreeLabRheostatDataAdapter.HasActiveSource)
            return false;

        float resistance = Mathf.Max(0.001f,
            FreeLabRheostatDataAdapter.ActiveResistanceOhms);
        float G = GetGo() * 1000f;
        float tilt = GetAngle(tiltRotator);
        float azimuth = GetAngle(azimuthRotator);
        float sun = GetAngle(sunRotator);
        float GT = Mathf.Max(0f, G * Mathf.Cos(Mathf.Deg2Rad * (tilt - azimuth)) *
            Mathf.Cos(Mathf.Deg2Rad * sun));

        if (GT <= 0.1f)
            return false;

        float V = GetVo(resistance) * (1f - Mathf.Log(GT / 1000f));
        float I = (V / resistance * 2.5f) * GT / 1000f;

        if (isSeries) V *= 3f;
        if (isParallel) I *= 3f;

        voltage = V;
        current = I;
        power = V * I;
        return true;
    }

    private float GetGo()
    {
        float value = intensitySlider != null ? intensitySlider.value : defaultGo;
        return Mathf.Lerp(goAtSliderMinimum, goAtSliderMaximum,
            Mathf.InverseLerp(sliderMinimum, sliderMaximum, value));
    }

    private static float GetVo(float resistance)
    {
        if (resistance >= 100f)
            return 55f;

        int closest = 0;
        for (int i = 1; i < ResistanceSteps.Length; i++)
            if (Mathf.Abs(ResistanceSteps[i] - resistance) <
                Mathf.Abs(ResistanceSteps[closest] - resistance))
                closest = i;
        return VoValues[closest];
    }

    private static float GetAngle(PivotValueRotator rotator) =>
        rotator == null ? 0f : Mathf.Abs(rotator.ReportedValue);

    private void SetOutput(float voltage, float current, float power)
    {
        SetText(voltageText, legacyVoltageText, voltage.ToString("F2"));
        SetText(currentText, legacyCurrentText, current.ToString("F2"));
        SetText(powerText, legacyPowerText, power.ToString("F2"));
    }

    private static void SetText(TMP_Text tmp, Text legacy, string value)
    {
        if (tmp != null) tmp.text = value;
        if (legacy != null) legacy.text = value;
    }

    private void OnInputChanged(float _) => RefreshDisplay();
    private void OnActiveResistanceChanged(float _) => RefreshDisplay();
    private void OnAngleChanged(float _) => RefreshDisplay();
    private void Subscribe(PivotValueRotator rotator)
    {
        if (rotator != null) rotator.ReportedValueChanged += OnAngleChanged;
    }
    private void Unsubscribe(PivotValueRotator rotator)
    {
        if (rotator != null) rotator.ReportedValueChanged -= OnAngleChanged;
    }
}
