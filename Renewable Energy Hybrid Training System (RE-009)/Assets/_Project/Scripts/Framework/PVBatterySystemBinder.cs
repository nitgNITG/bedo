using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// Standalone PV data display with battery and DC load simulation.
/// No rheostat — resistance is fixed at 100Ω.
/// </summary>
public class PVBatterySystemBinder : MonoBehaviour
{
    [Header("Intensity Slider")]
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private float sliderMinimum = 0f;
    [SerializeField] private float sliderMaximum = 100f;
    [SerializeField] private float goAtSliderMinimum = 0.1f;
    [SerializeField] private float goAtSliderMaximum = 1f;
    [SerializeField] private float defaultGo = 1f;

    [Header("Rotators")]
    [SerializeField] private PivotValueRotator tiltRotator;
    [SerializeField] private PivotValueRotator azimuthRotator;
    [SerializeField] private PivotValueRotator sunRotator;

    [Header("Solar Panel Display")]
    [SerializeField] private TMP_Text panelVoltageText;
    [SerializeField] private TMP_Text panelCurrentText;
    [SerializeField] private TMP_Text panelPowerText;

    [Header("Load Settings")]
    [SerializeField] private float loadVoltage = 12f;
    [SerializeField] private float loadCurrent = 0.2f;

    [Header("Load Display")]
    [SerializeField] private TMP_Text loadVoltageText;
    [SerializeField] private TMP_Text loadCurrentText;
    [SerializeField] private TMP_Text loadPowerText;

    [Header("Battery Settings")]
    [Tooltip("Maximum stored battery current in amps.")]
    [SerializeField] private float batteryMaxCharge = 9f;
    [Tooltip("Stored battery current changes at this fixed rate while charging or discharging.")]
    [FormerlySerializedAs("minBatteryPercentPerSecond")]
    [SerializeField, Min(0f)] private float batteryCurrentChangePerSecond = 0.1f;

    [Header("Battery Display")]
    [SerializeField] private TMP_Text batteryVoltageText;
    [SerializeField] private TMP_Text batteryCurrentText;
    [SerializeField] private TMP_Text batteryPowerText;
    [SerializeField] private TMP_Text batteryChargePercentText;

    [Header("Events")]
    public UnityEvent OnBatteryDepleted;

    // Vo lookup — fixed at 100Ω
    private const float FixedResistance = 100f;
    private const float FixedVo = 55f;

    private static readonly float[] ResistanceSteps = { 13f, 20f, 40f, 60f, 80f, 100f };
    private static readonly float[] VoValues = { 22f, 28.6f, 35.2f, 41.8f, 48.4f, 55f };

    private bool panelOn = false;
    private bool loadOn = false;

    // Read by the Free Mode lamp-visibility rule through the FreeLab
    // counterpart. This does not alter Guided Mode behaviour.
    public bool IsSolarPanelOn => panelOn;
    /// <summary>The current presently shown by the battery A DC display.</summary>
    public float DisplayedBatteryCurrent => batteryCharge;
    /// <summary>
    /// The current the battery must supply after the live solar input has
    /// been deducted from the active load. Zero means the panel covers it.
    /// </summary>
    public float RequiredBatteryCurrent { get; private set; }

    private float batteryCharge;
    private bool batteryChargeInitialized;
    private bool batteryDepletedEventFired = false;

    private void Start()
    {
        // Do not begin at a full battery. Its starting stored current is
        // established from the first live current balance after power begins.
        batteryCharge = 0f;
        batteryChargeInitialized = false;
        RefreshDisplay();
    }

    private void OnEnable()
    {
        if (intensitySlider != null)
            intensitySlider.onValueChanged.AddListener(OnInputChanged);

        Subscribe(tiltRotator);
        Subscribe(azimuthRotator);
        Subscribe(sunRotator);
    }

    private void OnDisable()
    {
        if (intensitySlider != null)
            intensitySlider.onValueChanged.RemoveListener(OnInputChanged);

        Unsubscribe(tiltRotator);
        Unsubscribe(azimuthRotator);
        Unsubscribe(sunRotator);
    }

    private void Update()
    {
        if (!panelOn && !loadOn)
        {
            RequiredBatteryCurrent = 0f;
            return;
        }

        float panelI = panelOn ? ComputePanelCurrent() : 0f;
        float actualLoadI = loadOn ? loadCurrent : 0f;
        RequiredBatteryCurrent = Mathf.Max(0f, actualLoadI - panelI);

        // Battery logic
        float batteryI = 0f;

        if (panelOn && loadOn)
        {
            float excess = panelI - actualLoadI;
            if (excess > 0f)
            {
                // Panel covers load, excess charges battery
                batteryI = excess;
            }
            else if (excess < 0f)
            {
                // Battery covers the gap
                batteryI = excess; // negative = discharging
            }
            // excess == 0: battery idle
        }
        else if (!panelOn && loadOn)
        {
            // Battery powers load entirely
            batteryI = -actualLoadI;
        }
        else if (panelOn && !loadOn)
        {
            // Panel charges battery
            batteryI = panelI;
        }

        InitializeBatteryChargeFromCurrent(batteryI);
        ApplyBatteryChargeRate(batteryI);
        if (batteryI < 0f)
            CheckBatteryDepleted(batteryI);
        RefreshDisplay();
    }

    /// <summary>
    /// Establishes the initial stored battery current once the system first has
    /// a live current balance. Positive charging current becomes the starting
    /// current (for example 1.20 A starts at 1.20 A). A discharge begins at
    /// zero, then moves down to its calculated negative current.
    /// </summary>
    private void InitializeBatteryChargeFromCurrent(float batteryCurrent)
    {
        if (batteryChargeInitialized)
            return;

        batteryChargeInitialized = true;
        batteryCharge = batteryCurrent > 0f
            ? Mathf.Clamp(batteryCurrent, 0f, batteryMaxCharge)
            : 0f;
    }

    private void ApplyBatteryChargeRate(float batteryCurrent)
    {
        if (Mathf.Approximately(batteryCurrent, 0f) || batteryMaxCharge <= 0f)
            return;

        // The displayed A DC is the stored battery current. A positive
        // balance fills it toward 9 A; a negative balance drains it only as
        // far as that calculated negative current.
        float targetCurrent = batteryCurrent > 0f
            ? batteryMaxCharge
            : Mathf.Max(-batteryMaxCharge, batteryCurrent);
        batteryCharge = Mathf.MoveTowards(batteryCharge, targetCurrent,
            batteryCurrentChangePerSecond * Time.deltaTime);
    }

    // ── Panel on/off ────────────────────────────────────────────────
    public void TurnPanelOn()
    {
        panelOn = true;
    }

    public void TurnPanelOff()
    {
        panelOn = false;
        RefreshDisplay();
    }

    // ── Load on/off ─────────────────────────────────────────────────
    public void TurnLoadOn()
    {
        loadOn = true;
    }

    public void TurnLoadOff()
    {
        loadOn = false;
        RefreshDisplay();
    }

    // ── Display ──────────────────────────────────────────────────────
    private void RefreshDisplay()
    {
        // Panel
        if (panelOn && TryComputePanel(out float pV, out float pI, out float pP))
        {
            SetText(panelVoltageText, pV.ToString("F2"));
            SetText(panelCurrentText, pI.ToString("F2"));
            SetText(panelPowerText, pP.ToString("F2"));
        }
        else
        {
            SetText(panelVoltageText, "0.00");
            SetText(panelCurrentText, "0.00");
            SetText(panelPowerText, "0.00");
        }

        // Load
        if (loadOn)
        {
            float loadP = loadVoltage * loadCurrent;
            SetText(loadVoltageText, loadVoltage.ToString("F2"));
            SetText(loadCurrentText, loadCurrent.ToString("F2"));
            SetText(loadPowerText, loadP.ToString("F2"));
        }
        else
        {
            SetText(loadVoltageText, "0.00");
            SetText(loadCurrentText, "0.00");
            SetText(loadPowerText, "0.00");
        }

        // Battery
        float batteryI = batteryCharge;
        float batteryP = loadVoltage * Mathf.Abs(batteryI);
        SetText(batteryVoltageText, loadVoltage.ToString("F2"));
        SetText(batteryCurrentText, batteryI.ToString("F2"));
        SetText(batteryPowerText, batteryP.ToString("F2"));
        float batteryPercent = batteryMaxCharge > 0f
            ? Mathf.Max(0f, batteryI) / batteryMaxCharge * 100f
            : 0f;
        SetText(batteryChargePercentText, batteryPercent.ToString("F2") + "%");
    }

    // ── Equations ────────────────────────────────────────────────────
    private bool TryComputePanel(out float voltage, out float current, out float power)
    {
        voltage = 0f; current = 0f; power = 0f;

        float GT = ComputeGT();
        if (GT <= 0f) return false;

        // The battery-charging experiments use two series panel sections:
        // voltage doubles while the panel current remains that of one section.
        float sectionVoltage = FixedVo * Mathf.Max(0f, 1f + 0.055f * Mathf.Log(GT / 1000f));
        float ISC = sectionVoltage / FixedResistance * 2.5f;
        float I = ISC * GT / 1000f;
        float V = sectionVoltage * 2f;
        float P = V * I;

        voltage = V; current = I; power = P;
        return true;
    }

    private float ComputePanelCurrent()
    {
        TryComputePanel(out _, out float I, out _);
        return I;
    }

    private float ComputeGT()
    {
        float go = GetGo();
        float G = go * 1000f;
        float tilt = GetAngle(tiltRotator);
        float azimuth = GetAngle(azimuthRotator);
        float sun = GetAngle(sunRotator);
        return Mathf.Max(0f, G
            * Mathf.Cos(Mathf.Deg2Rad * (tilt - azimuth))
            * Mathf.Cos(Mathf.Deg2Rad * sun));
    }

    private float ComputeBatteryDisplayCurrent()
    {
        if (!panelOn && !loadOn) return 0f;

        float panelI = panelOn ? ComputePanelCurrent() : 0f;
        float actualLoadI = loadOn ? loadCurrent : 0f;

        if (panelOn && loadOn)
        {
            float excess = panelI - actualLoadI;
            if (excess > 0f) return excess;   // charging
            if (excess < 0f) return excess;   // discharging (negative)
            return 0f;                          // idle
        }

        if (!panelOn && loadOn) return -actualLoadI; // discharging
        if (panelOn && !loadOn) return panelI;      // charging

        return 0f;
    }

    private float GetGo()
    {
        float source = intensitySlider != null ? intensitySlider.value : defaultGo;
        return Mathf.Lerp(goAtSliderMinimum, goAtSliderMaximum,
            Mathf.InverseLerp(sliderMinimum, sliderMaximum, source));
    }

    private static float GetAngle(PivotValueRotator r) =>
        r == null ? 0f : Mathf.Abs(r.ReportedValue);

    private void CheckBatteryDepleted(float targetCurrent)
    {
        if (batteryCharge <= targetCurrent && !batteryDepletedEventFired)
        {
            batteryDepletedEventFired = true;
            OnBatteryDepleted?.Invoke();
        }

        if (batteryCharge > targetCurrent)
            batteryDepletedEventFired = false;
    }

    private static void SetText(TMP_Text tmp, string value)
    {
        if (tmp != null) tmp.text = value;
    }

    private void OnInputChanged(float _) => RefreshDisplay();
    private void OnAngleChanged(float _) => RefreshDisplay();

    private void Subscribe(PivotValueRotator r)
    {
        if (r != null) r.ReportedValueChanged += OnAngleChanged;
    }

    private void Unsubscribe(PivotValueRotator r)
    {
        if (r != null) r.ReportedValueChanged -= OnAngleChanged;
    }



}
