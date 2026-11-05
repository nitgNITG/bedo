using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Off-grid PV system with AC and DC loads, battery, and solar panel.
/// </summary>
public class PVOffGridSystemBinder : MonoBehaviour
{
    [Header("Inverter")]
    [SerializeField] private float inverterEfficiency = 1f;

    [Header("Voltage Settings")]
    [SerializeField] private float acVoltage = 22f;
    [SerializeField] private float dcVoltage = 24f;

    [Header("Intensity Slider")]
    [SerializeField] private UnityEngine.UI.Slider intensitySlider;
    [SerializeField] private float sliderMinimum = 0f;
    [SerializeField] private float sliderMaximum = 100f;
    [SerializeField] private float goAtSliderMinimum = 0f;
    [SerializeField] private float goAtSliderMaximum = 1f;
    [SerializeField] private float defaultGo = 1f;

    [Header("Rotators")]
    [SerializeField] private PivotValueRotator tiltRotator;
    [SerializeField] private PivotValueRotator azimuthRotator;
    [SerializeField] private PivotValueRotator sunRotator;

    // ── Solar Panel ──────────────────────────────────────────────────
    [Header("Solar Panel Display")]
    [SerializeField] private TMP_Text panelVoltageText;
    [SerializeField] private TMP_Text panelCurrentText;
    [SerializeField] private TMP_Text panelPowerText;

    // ── Off Grid source ─────────────────────────────────────────────
    [Header("Off Grid")]
    [FormerlySerializedAs("acVoltmeterCurrent")]
    [SerializeField] private float offGridCurrent = 0.1f;
    [FormerlySerializedAs("acVoltmeterVoltageText")]
    [SerializeField] private TMP_Text offGridVoltageText;
    [FormerlySerializedAs("acVoltmeterCurrentText")]
    [SerializeField] private TMP_Text offGridCurrentText;
    [FormerlySerializedAs("acVoltmeterPowerText")]
    [SerializeField] private TMP_Text offGridPowerText;

    // ── AC Load ──────────────────────────────────────────────────────
    [Header("AC Load")]
    [SerializeField] private float acLoadCurrentDisplay = 0.2f;
    [SerializeField] private float acLoadCurrentCalculation = 2f;
    [SerializeField] private TMP_Text acLoadVoltageText;
    [SerializeField] private TMP_Text acLoadCurrentText;
    [SerializeField] private TMP_Text acLoadPowerText;

    // ── DC Load 1 (Panel Load) ───────────────────────────────────────
    [Header("DC Load 1 — Panel Load")]
    [SerializeField] private float dcLoad1Current = 2f;
    [SerializeField] private TMP_Text dcLoad1VoltageText;
    [SerializeField] private TMP_Text dcLoad1CurrentText;
    [SerializeField] private TMP_Text dcLoad1PowerText;

    // ── DC Load 2 (Battery Load) ─────────────────────────────────────
    [Header("DC Load 2 — Battery Load")]
    [SerializeField] private float dcLoad2Current = 2f;
    [SerializeField] private TMP_Text dcLoad2VoltageText;
    [SerializeField] private TMP_Text dcLoad2CurrentText;
    [SerializeField] private TMP_Text dcLoad2PowerText;

    // ── Battery ──────────────────────────────────────────────────────
    [Header("Battery")]
    [Tooltip("Maximum stored battery current in amps. It is not added to the live current calculation.")]
    [SerializeField] private float batteryMaxCharge = 9f;
    [Tooltip("Stored battery current changes at this fixed rate while charging or discharging.")]
    [FormerlySerializedAs("minBatteryPercentPerSecond")]
    [SerializeField, Min(0f)] private float batteryCurrentChangePerSecond = 0.1f;
    [SerializeField] private TMP_Text batteryVoltageText;
    [SerializeField] private TMP_Text batteryCurrentText;
    [SerializeField] private TMP_Text batteryPowerText;
    [SerializeField] private TMP_Text batteryChargePercentText;
    [SerializeField] private GameObject chargingObject;
    [SerializeField] private GameObject dischargingObject;

    [Header("Events")]
    public UnityEvent OnBatteryDepleted;

    // ── Constants ────────────────────────────────────────────────────
    private const float FixedResistance = 100f;
    private const float FixedVo = 55f;

    private static readonly float[] ResistanceSteps = { 13f, 20f, 40f, 60f, 80f, 100f };
    private static readonly float[] VoValues = { 22f, 28.6f, 35.2f, 41.8f, 48.4f, 55f };

    // ── State ────────────────────────────────────────────────────────
    private bool panelOn = false;
    [FormerlySerializedAs("acVoltmeterOn")]
    private bool offGridOn = false;
    private bool acLoadOn = false;
    private bool dcLoad1On = false;
    private bool dcLoad2On = false;

    private float batteryCharge;
    private bool batteryChargeInitialized;
    private bool batteryDepletedEventFired = false;

    /// <summary>Live source state exposed for Free Mode-only physical rules.</summary>
    public bool IsSolarPanelOn => panelOn;
    /// <summary>Positive means charging; negative means battery discharge.</summary>
    public float LiveBatteryCurrent { get; private set; }
    /// <summary>The current presently shown by the battery A DC display.</summary>
    public float DisplayedBatteryCurrent => batteryCharge;
    /// <summary>
    /// The current the battery must supply after solar and off-grid input
    /// have both been deducted from the active load.
    /// </summary>
    public float RequiredBatteryCurrent { get; private set; }

    // ── Unity ────────────────────────────────────────────────────────
    private void Start()
    {
        batteryCharge = 0f;
        batteryChargeInitialized = false;
        SetGameObject(chargingObject, false);
        SetGameObject(dischargingObject, false);
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
        float panelI = panelOn ? ComputePanelCurrent() : 0f;
        float offGridI = offGridOn ? offGridCurrent : 0f;

        // DC Load 1 and DC Load 2 are alternate readouts for one physical load.
        // It must be counted once even if both display flags are enabled.
        bool dcLoadOn = dcLoad1On || dcLoad2On;
        float dcLoadCurrent = dcLoadOn ? Mathf.Max(dcLoad1Current, dcLoad2Current) : 0f;
        float acLoadCurrent = acLoadOn ? acLoadCurrentCalculation : 0f;
        float totalLoadCurrent = dcLoadCurrent + acLoadCurrent;
        RequiredBatteryCurrent = Mathf.Max(0f,
            totalLoadCurrent - panelI - offGridI);

        // Live current balance: sources charge the battery; active loads draw from it.
        // Negative means battery discharge. Positive means battery charging.
        float batteryI = panelI + offGridI - totalLoadCurrent;
        LiveBatteryCurrent = batteryI;

        if (panelOn || offGridOn || dcLoadOn || acLoadOn)
            InitializeBatteryChargeFromCurrent(batteryI);

        if (!Mathf.Approximately(batteryI, 0f))
        {
            ApplyBatteryChargeRate(batteryI);
            if (batteryI < 0f)
                CheckBatteryDepleted(batteryI);
        }

        UpdateBatteryObjects(batteryI);
        RefreshDisplay(panelI, batteryI, totalLoadCurrent);
    }

    private void ApplyBatteryChargeRate(float batteryCurrent)
    {
        if (Mathf.Approximately(batteryCurrent, 0f) || batteryMaxCharge <= 0f)
            return;

        // The displayed A DC is the changing battery current. Charging rises
        // toward 9 A; discharging falls only to the calculated negative
        // current balance.
        float targetCurrent = batteryCurrent > 0f
            ? batteryMaxCharge
            : Mathf.Max(-batteryMaxCharge, batteryCurrent);
        batteryCharge = Mathf.MoveTowards(batteryCharge, targetCurrent,
            batteryCurrentChangePerSecond * Time.deltaTime);
    }

    private void InitializeBatteryChargeFromCurrent(float batteryCurrent)
    {
        if (batteryChargeInitialized)
            return;

        batteryChargeInitialized = true;
        batteryCharge = batteryCurrent > 0f
            ? Mathf.Clamp(batteryCurrent, 0f, batteryMaxCharge)
            : 0f;
    }

    // ── On/Off Methods ───────────────────────────────────────────────
    public void TurnPanelOn() => panelOn = true;
    public void TurnPanelOff() => panelOn = false;
    public void TurnOffGridOn() => offGridOn = true;
    public void TurnOffGridOff() => offGridOn = false;

    // Keep existing UnityEvents valid while scene events are moved to the Off Grid names.
    public void TurnAcVoltmeterOn() => TurnOffGridOn();
    public void TurnAcVoltmeterOff() => TurnOffGridOff();
    public void TurnAcLoadOn() => acLoadOn = true;
    public void TurnAcLoadOff() => acLoadOn = false;
    public void TurnDcLoad1On() => dcLoad1On = true;
    public void TurnDcLoad1Off() => dcLoad1On = false;
    public void TurnDcLoad2On() => dcLoad2On = true;
    public void TurnDcLoad2Off() => dcLoad2On = false;

    // ── Display ──────────────────────────────────────────────────────
    private void RefreshDisplay(float panelI = 0f, float batteryI = 0f, float dcDemand = 0f)
    {
        // Solar panel
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

        // Off Grid
        if (offGridOn)
        {
            SetText(offGridVoltageText, acVoltage.ToString("F2"));
            SetText(offGridCurrentText, offGridCurrent.ToString("F2"));
            SetText(offGridPowerText, (acVoltage * offGridCurrent).ToString("F2"));
        }
        else
        {
            SetText(offGridVoltageText, "0.00");
            SetText(offGridCurrentText, "0.00");
            SetText(offGridPowerText, "0.00");
        }

        // AC Load
        if (acLoadOn)
        {
            SetText(acLoadVoltageText, acVoltage.ToString("F2"));
            SetText(acLoadCurrentText, acLoadCurrentDisplay.ToString("F2"));
            SetText(acLoadPowerText, (acVoltage * acLoadCurrentDisplay).ToString("F2"));
        }
        else
        {
            SetText(acLoadVoltageText, "0.00");
            SetText(acLoadCurrentText, "0.00");
            SetText(acLoadPowerText, "0.00");
        }

        // DC Load 1 — shows when panel is the source (battery not discharging)
        bool panelIsSource = batteryI >= 0f && panelOn;
        if (dcLoad1On && panelIsSource)
        {
            SetText(dcLoad1VoltageText, dcVoltage.ToString("F2"));
            SetText(dcLoad1CurrentText, dcLoad1Current.ToString("F2"));
            SetText(dcLoad1PowerText, (dcVoltage * dcLoad1Current).ToString("F2"));
        }
        else
        {
            SetText(dcLoad1VoltageText, "0.00");
            SetText(dcLoad1CurrentText, "0.00");
            SetText(dcLoad1PowerText, "0.00");
        }

        // DC Load 2 — shows when battery is discharging
        bool batteryIsSource = batteryI < 0f;
        if (dcLoad2On && batteryIsSource)
        {
            SetText(dcLoad2VoltageText, dcVoltage.ToString("F2"));
            SetText(dcLoad2CurrentText, dcLoad2Current.ToString("F2"));
            SetText(dcLoad2PowerText, (dcVoltage * dcLoad2Current).ToString("F2"));
        }
        else
        {
            SetText(dcLoad2VoltageText, "0.00");
            SetText(dcLoad2CurrentText, "0.00");
            SetText(dcLoad2PowerText, "0.00");
        }

        // Battery
        float displayBatteryI = batteryCharge;
        float batteryP = dcVoltage * Mathf.Abs(displayBatteryI);

        SetText(batteryVoltageText, dcVoltage.ToString("F2"));
        SetText(batteryCurrentText, displayBatteryI.ToString("F2"));
        SetText(batteryPowerText, batteryP.ToString("F2"));
        float batteryPercent = batteryMaxCharge > 0f
            ? Mathf.Max(0f, displayBatteryI) / batteryMaxCharge * 100f
            : 0f;
        SetText(batteryChargePercentText, batteryPercent.ToString("F2") + "%");
    }

    // ── Equations ────────────────────────────────────────────────────
    private bool TryComputePanel(out float voltage, out float current, out float power)
    {
        voltage = 0f; current = 0f; power = 0f;

        float GT = ComputeGT();
        if (GT <= 0f) return false;

        // The off-grid experiment uses two series panel sections:
        // voltage doubles while the panel current remains that of one section.
        float sectionVoltage = FixedVo * (1f - Mathf.Log(GT / 1000f));
        float ISC = sectionVoltage / FixedResistance * 2.5f;
        float I = ISC * GT / 1000f;
        float V = sectionVoltage * 2f;

        voltage = V; current = I; power = V * I;
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

    private float GetGo()
    {
        float source = intensitySlider != null ? intensitySlider.value : defaultGo;
        float go = Mathf.Lerp(goAtSliderMinimum, goAtSliderMaximum,
            Mathf.InverseLerp(sliderMinimum, sliderMaximum, source));
        return intensitySlider != null && intensitySlider.value <= sliderMinimum ? 0f : go;
    }

    private static float GetAngle(PivotValueRotator r) =>
        r == null ? 0f : Mathf.Abs(r.ReportedValue);

    // ── Battery ──────────────────────────────────────────────────────
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

    private void UpdateBatteryObjects(float batteryI)
    {
        SetGameObject(chargingObject, batteryI > 0f);
        SetGameObject(dischargingObject, batteryI < 0f);
    }

    private static void SetGameObject(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // ── Helpers ──────────────────────────────────────────────────────
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
