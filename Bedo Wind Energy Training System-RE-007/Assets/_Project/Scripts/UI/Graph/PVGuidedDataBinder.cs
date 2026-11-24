using Obvious.Soap;
using TutorialFramework.Simulation;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TutorialFramework;

/// <summary>
/// Guided Mode PV data display. Computes V, I, P directly from equations —
/// no CSV lookup required.
/// </summary>
public class PVGuidedDataBinder : MonoBehaviour
{
    [Header("Wind EXP4 off-grid (optional)")]
    [SerializeField] private bool windOffGrid;
    [SerializeField] private TextAsset offGridModel;
    [SerializeField] private WireInteraction[] offGridBatteryWires, offGridDCLoadWires, offGridWindWires, offGridGridWires;
    [SerializeField] private WireInteraction[] offGridACLamp1Wires, offGridACLamp2Wires;
    [SerializeField] private TMP_Text[] offGridBatteryTexts, offGridDCLoadTexts, offGridACLoadTexts;
    [SerializeField] private TMP_Text[] offGridInverterTexts, offGridGridTexts, offGridDumpTexts;
    [SerializeField] private TMP_Text offGridChargeCurrentText;
    [SerializeField] private GameObject[] connectedLampLights;
    private WindSimulationData legacyWindState;
    private WindSimulationData WindState { get => windSpeedController && windSpeedController.SharedState ? windSpeedController.SharedState.Value : legacyWindState; set { if (windSpeedController && windSpeedController.SharedState) windSpeedController.SharedState.Value = value; else legacyWindState = value; } }
    private bool offGridSystemOn { get => WindState.offGridSystemOn; set { var next = WindState; next.offGridSystemOn = value; WindState = next; } }
    private bool offGridInverterOn { get => WindState.offGridInverterOn; set { var next = WindState; next.offGridInverterOn = value; WindState = next; } }
    public bool UsesWindOffGrid => windOffGrid;
    public PVCSVReader.OffGridReading OffGridReading { get => WindState.bus; private set { var next = WindState; next.bus = value; WindState = next; } }

    private static bool Connected(WireInteraction[] wires)
    {
        if (wires == null || wires.Length == 0) return false;
        // StoryboardSteps resets IsCompleted when leaving each guided row.
        // The connected cable and its two inserted plugs remain in place.
        // Check their local state, including when a step parent is inactive.
        foreach (var wire in wires)
            if (wire == null || wire.wireObject == null || !wire.wireObject.activeSelf ||
                wire.plugA == null || !wire.plugA.activeSelf || wire.plugB == null || !wire.plugB.activeSelf) return false;
        return true;
    }
    public void TurnOffGridSystemOn() { offGridSystemOn = true; RefreshDisplay(); }
    public void TurnOffGridInverterOn() { offGridInverterOn = true; RefreshDisplay(); }
    public void TurnOffGridInverterOff() { offGridInverterOn = false; RefreshDisplay(); }
    public void ResetConnectedLampLights()
    {
        if (connectedLampLights != null) foreach (var lamp in connectedLampLights) if (lamp != null) lamp.SetActive(false);
    }
    public void TurnOffGridSystemOff()
    {
        offGridSystemOn = offGridInverterOn = false;
        if (windSpeedController != null) windSpeedController.TurnOff();
        ResetConnectedLampLights(); HidePowerAnalyzer(); RefreshDisplay();
    }
    private void RefreshOffGridDisplay(PVCSVReader.WindReading source)
    {
        bool powered = offGridSystemOn && offGridInverterOn && Connected(offGridBatteryWires);
        int dcLoads = powered && Connected(offGridDCLoadWires) ? 1 : 0;
        int acLoads = powered ? (Connected(offGridACLamp1Wires) ? 1 : 0) + (Connected(offGridACLamp2Wires) ? 1 : 0) : 0;
        bool generating = powered && windSpeedController.IsTurbineRunning && Connected(offGridWindWires);
        bool grid = powered && Connected(offGridGridWires);
        // EXP4 connects the LED module; use column M (0.2 A), rather than
        // column L's 2 A halogen example. AC calculation still uses column Q.
        // E2: a controller cannot charge a 24 V battery from a lower DC source.
        PVCSVReader.TryCalculateOffGrid(offGridModel,
            generating && source.dcVoltage >= 24f ? source.dcCurrent : 0f,
            dcLoads, acLoads, grid, out var result, true);
        if (!powered) result = default;
        OffGridReading = result;
        // Generator/rectifier can run while inverter is off; its readings still
        // come from the turbine's own power state, not the battery.
        SetOutput(source.dcVoltage.ToString("F2"), source.dcCurrent.ToString("F2"), source.dcPower.ToString("F2"));
        PWMTriplet(offGridBatteryTexts, result.batteryVoltage, result.batteryCurrent, result.batteryPower);
        PWMTriplet(offGridDCLoadTexts, dcLoads > 0 ? result.batteryVoltage : 0, result.dcLoadCurrent, result.dcLoadPower);
        PWMTriplet(offGridACLoadTexts, result.acLoadVoltage, result.acLoadCurrent, result.acLoadPower);
        PWMTriplet(offGridInverterTexts, acLoads > 0 ? result.batteryVoltage : 0, result.inverterCurrent, result.inverterPower);
        PWMTriplet(offGridGridTexts, result.gridVoltage, result.gridCurrent, result.gridPower);
        PWMTriplet(offGridDumpTexts, result.dumpCurrent > 0 ? result.batteryVoltage : 0, result.dumpCurrent, result.dumpCurrent * result.batteryVoltage);
        if (offGridChargeCurrentText != null) offGridChargeCurrentText.text = result.availableChargeCurrent.ToString("F2");
    }
    [Header("Wind EXP2 PWM (optional; disabled for EXP1)")]
    [SerializeField] private bool windPWM;
    [SerializeField] private TMP_Text[] pwmBatteryTexts;
    [SerializeField] private TMP_Text[] pwmLoadTexts;
    [SerializeField] private TMP_Text[] pwmDumpTexts;
    [SerializeField] private TMP_Text[] pwmUnusedTexts;
    [SerializeField] private TMP_Text pwmBatteryScreenText;
    [SerializeField] private GameObject pwmBatteryScreen;
    [SerializeField] private Slider windSpeedSlider;
    [SerializeField] private float pwmLoadCurrent = 2f;
    [SerializeField] private float pwmBatteryVoltage = 24f;
    [SerializeField] private float pwmMaximumChargeCurrent = 9f;
    [SerializeField] private float pwmScreenChangePerSecond = .1f;
    private bool pwmSystemOn { get => WindState.pwmSystemOn; set { var next = WindState; next.pwmSystemOn = value; WindState = next; } }
    private bool pwmLoadOn { get => WindState.pwmLoadOn; set { var next = WindState; next.pwmLoadOn = value; WindState = next; } }
    private bool pwmOutputOn { get => WindState.pwmOutputOn; set { var next = WindState; next.pwmOutputOn = value; WindState = next; } }
    [Header("SOAP charger parameters")]
    [SerializeField] private FloatReference pwmLoadCurrentReference = new FloatReference();
    private float PWMLoadCurrent => WindSoapReference.Read(pwmLoadCurrentReference, pwmLoadCurrent);
    [SerializeField] private FloatReference pwmBatteryVoltageReference = new FloatReference();
    private float PWMBatteryVoltage => WindSoapReference.Read(pwmBatteryVoltageReference, pwmBatteryVoltage);
    [SerializeField] private FloatReference pwmMaximumChargeCurrentReference = new FloatReference();
    private float PWMMaximumChargeCurrent => WindSoapReference.Read(pwmMaximumChargeCurrentReference, pwmMaximumChargeCurrent);
    [SerializeField] private FloatReference pwmScreenChangePerSecondReference = new FloatReference();
    private float PWMScreenChangePerSecond => WindSoapReference.Read(pwmScreenChangePerSecondReference, pwmScreenChangePerSecond);
    private float pwmScreenCurrent;
    public float PWMBatteryCurrent { get => WindState.batteryCurrent; private set { var next = WindState; next.batteryCurrent = value; WindState = next; } }
    public float PWMDumpCurrent { get => WindState.dumpCurrent; private set { var next = WindState; next.dumpCurrent = value; WindState = next; } }
    public bool UsesWindPWM => windPWM;
    public void TurnPWMSystemOn() { pwmSystemOn = true; RefreshDisplay(); }
    public void TurnPWMSystemOff()
    {
        pwmSystemOn = pwmLoadOn = pwmOutputOn = false;
        pwmScreenCurrent = 0;
        if (windSpeedController != null) windSpeedController.TurnOff();
        HidePowerAnalyzer();
        if (pwmBatteryScreen != null) pwmBatteryScreen.SetActive(false);
        RefreshDisplay();
    }
    public void TurnPWMLoadOn()
    {
        pwmLoadOn = true;
        if (pwmBatteryScreen != null) pwmBatteryScreen.SetActive(true);
        RefreshDisplay();
    }
    public void TurnPWMLoadOff() { pwmLoadOn = false; RefreshDisplay(); }
    public void EnablePWMOutput() { pwmOutputOn = true; ShowPowerAnalyzer(); RefreshDisplay(); }
    public void DisablePWMOutput() { pwmOutputOn = false; HidePowerAnalyzer(); RefreshDisplay(); }
    public void SetWindRPM(float rpm) { if (windSpeedController != null) windSpeedController.SetSpeed(rpm); RefreshDisplay(); }
    private static void PWMTriplet(TMP_Text[] texts, float voltage, float current, float power)
    {
        if (texts == null) return;
        float[] values = { voltage, current, power };
        for (int i = 0; i < Mathf.Min(3, texts.Length); i++)
            if (texts[i] != null) texts[i].text = values[i].ToString("F2");
    }
    private void RefreshPWMDisplay(PVCSVReader.WindReading reading)
    {
        bool generating = pwmSystemOn && pwmOutputOn && windSpeedController.IsTurbineRunning;
        // E2: below the battery voltage, the source supplies no charging current.
        float sourceCurrent = generating && reading.dcVoltage >= PWMBatteryVoltage
            ? reading.dcCurrent : 0f;
        float loadCurrent = pwmSystemOn && pwmLoadOn ? PWMLoadCurrent : 0;
        PWMBatteryCurrent = pwmSystemOn ? Mathf.Min(sourceCurrent - loadCurrent, PWMMaximumChargeCurrent) : 0;
        PWMDumpCurrent = pwmSystemOn ? Mathf.Max(0, sourceCurrent - loadCurrent - PWMMaximumChargeCurrent) : 0;
        SetOutput((generating ? reading.dcVoltage : 0).ToString("F2"), sourceCurrent.ToString("F2"), (generating ? reading.dcPower : 0).ToString("F2"));
        PWMTriplet(pwmBatteryTexts, pwmSystemOn ? PWMBatteryVoltage : 0, PWMBatteryCurrent, PWMBatteryVoltage * PWMBatteryCurrent);
        PWMTriplet(pwmLoadTexts, loadCurrent > 0 ? PWMBatteryVoltage : 0, loadCurrent, PWMBatteryVoltage * loadCurrent);
        PWMTriplet(pwmDumpTexts, PWMDumpCurrent > 0 ? PWMBatteryVoltage : 0, PWMDumpCurrent, PWMBatteryVoltage * PWMDumpCurrent);
        if (pwmUnusedTexts != null) foreach (var text in pwmUnusedTexts) if (text != null) text.text = "0";
        if (pwmBatteryScreenText != null)
            pwmBatteryScreenText.text = (PWMMaximumChargeCurrent > 0 ? Mathf.Max(0, pwmScreenCurrent) / PWMMaximumChargeCurrent * 100 : 0).ToString("F2") + "%";
    }
    [Header("EXP1 wind model (optional)")]
    [SerializeField] private WindTurbineSpeedController windSpeedController;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text acVoltageText;
    [SerializeField] private TMP_Text acCurrentText;
    [SerializeField] private TMP_Text acPowerText;
    [SerializeField] private GameObject powerAnalyzerCanvas;
    [SerializeField] private TMP_Text powerAnalyzerText;

    public void ShowPowerAnalyzer()
    {
        if (!UsesWindModel || powerAnalyzerCanvas == null) return;
        RefreshDisplay();
        powerAnalyzerCanvas.SetActive(true);
    }

    public void HidePowerAnalyzer()
    {
        if (powerAnalyzerCanvas != null) powerAnalyzerCanvas.SetActive(false);
    }
    public bool UsesWindModel => windSpeedController != null && windSpeedController.HasWindModel;
    public bool TryGetWindReading(out PVCSVReader.WindReading reading)
    {
        reading = default;
        return UsesWindModel && windSpeedController.TryGetReading(out reading);
    }
    [Header("Guided intensity slider")]
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private float sliderMinimum = 0f;
    [SerializeField] private float sliderMaximum = 100f;
    [SerializeField] private float goAtSliderMinimum = 0.1f;
    [SerializeField] private float goAtSliderMaximum = 1f;
    [SerializeField] private float defaultGo = 1f;

    [Header("Optional Guided controls")]
    [SerializeField] private RheostatController rheostat;
    [SerializeField] private PivotValueRotator tiltRotator;
    [SerializeField] private PivotValueRotator azimuthRotator;
    [SerializeField] private PivotValueRotator sunRotator;

    [Header("TextMeshPro displays")]
    [SerializeField] private TMP_Text voltageText;
    [SerializeField] private TMP_Text currentText;
    [SerializeField] private TMP_Text powerText;

    [Header("Legacy UI Text displays (optional)")]
    [SerializeField] private Text legacyVoltageText;
    [SerializeField] private Text legacyCurrentText;
    [SerializeField] private Text legacyPowerText;


    [Header("Configuration")]
    [SerializeField] private bool isSeries = false;
    [SerializeField] private bool isParallel = false;



    // Vo lookup keyed by resistance
    private static readonly float[] ResistanceSteps = { 13f, 20f, 40f, 60f, 80f, 100f };
    private static readonly float[] VoValues = { 22f, 28.6f, 35.2f, 41.8f, 48.4f, 55f };

    private float graphResistance = -1f;
    private float lastRheostatResistance = float.MinValue;

    private void OnEnable()
    {
        ResetConnectedLampLights();
        if (windOffGrid) TurnOffGridSystemOff();
        if (windPWM) TurnPWMSystemOff();
        if (windPWM && windSpeedSlider != null) windSpeedSlider.onValueChanged.AddListener(SetWindRPM);
        if (UsesWindModel) { HidePowerAnalyzer(); windSpeedController.TurnOff(); }
        if (intensitySlider != null)
            intensitySlider.onValueChanged.AddListener(OnIntensityChanged);

        Subscribe(tiltRotator);
        Subscribe(azimuthRotator);
        Subscribe(sunRotator);
    }

    private void OnDisable()
    {
        ResetConnectedLampLights();
        if (windOffGrid) TurnOffGridSystemOff();
        if (windPWM) TurnPWMSystemOff();
        if (windPWM && windSpeedSlider != null) windSpeedSlider.onValueChanged.RemoveListener(SetWindRPM);
        if (UsesWindModel) { HidePowerAnalyzer(); windSpeedController.TurnOff(); }
        if (intensitySlider != null)
            intensitySlider.onValueChanged.RemoveListener(OnIntensityChanged);

        Unsubscribe(tiltRotator);
        Unsubscribe(azimuthRotator);
        Unsubscribe(sunRotator);
    }

    private IEnumerator Start()
    {
        yield return null;
        RefreshDisplay();
    }

    private void LateUpdate()
    {
        if (UsesWindModel) windSpeedController.TickRotor();
    }

    private void Update()
    {
        if (windOffGrid) { RefreshDisplay(); return; }
        if (windPWM)
        {
            pwmScreenCurrent = Mathf.MoveTowards(pwmScreenCurrent, PWMBatteryCurrent, PWMScreenChangePerSecond * Time.deltaTime);
            RefreshDisplay();
            return;
        }

        if (rheostat == null)
            return;

        if (Mathf.Approximately(rheostat.CurrentResistance, lastRheostatResistance))
            return;

        lastRheostatResistance = rheostat.CurrentResistance;

        // Only refresh from rheostat if the graph is not driving resistance
        if (graphResistance <= 0f)
            RefreshDisplay();
    }

    /// <summary>Called by PVGraphController when its rheostat moves.</summary>
    public void SetGraphResistance(float resistance)
    {
        graphResistance = resistance;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (UsesWindModel)
        {
            if (!TryGetWindReading(out var reading)) return;
            if (windOffGrid) RefreshOffGridDisplay(reading);
            else if (windPWM) RefreshPWMDisplay(reading);
            else
            SetOutput(reading.dcVoltage.ToString("F2"), reading.dcCurrent.ToString("F2"), reading.dcPower.ToString("F2"));
            if (speedText != null) speedText.text = reading.rpm.ToString("0.##");
            if (acVoltageText != null) acVoltageText.text = reading.acVoltage.ToString("F2");
            if (acCurrentText != null) acCurrentText.text = reading.acCurrent.ToString("F2");
            if (acPowerText != null) acPowerText.text = reading.acPower.ToString("F2");
            if (powerAnalyzerText != null)
            {
                // The wind model uses P = V * I (unity power factor), so Q = 0 and S = |P|.
                float apparentPower = Mathf.Abs(reading.acVoltage * reading.acCurrent);
                powerAnalyzerText.text = FormatPowerAnalyzer(reading.acPower, apparentPower);
            }
            return;
        }
        if (!TryComputePoint(out float voltage, out float current, out float power))
        {
            SetOutput("0.00", "0.00", "0.00");
            return;
        }

        SetOutput(voltage.ToString("F2"), current.ToString("F2"), power.ToString("F2"));
    }

    // Fixed unit column matches the physical analyzer LCD; equations stay unchanged.
public static string FormatPowerAnalyzer(float activePower, float apparentPower) =>
        $"<line-height=140%><mspace=0.12>{activePower:F2}</mspace><pos=75%>V\n" +
        $"<mspace=0.12>{0f:F2}</mspace><pos=75%>A\n" +
        $"<mspace=0.12>{apparentPower:F2}</mspace><pos=75%>W";

    /// <summary>Called by the Free Mode calculation router from its authored wire sets.</summary>
    public void SetArrayConfiguration(bool series, bool parallel)
    {
        isSeries = series;
        isParallel = parallel;
    }

    /// <summary>
    /// Computes V, I, P from equations. Returns false if GT is zero or inputs are invalid.
    /// </summary>
    public bool TryComputePoint(out float voltage, out float current, out float power)
    {
        voltage = 0f;
        current = 0f;
        power = 0f;

        if (UsesWindModel)
        {
            if (!TryGetWindReading(out var reading)) return false;
            voltage = reading.dcVoltage;
            current = reading.dcCurrent;
            power = reading.dcPower;
            return true;
        }

        float go = GetGo();
        float resistance = GetResistance();
        float tilt = GetAngle(tiltRotator);
        float azimuth = GetAngle(azimuthRotator);
        float sun = GetAngle(sunRotator);

        float G = go * 1000f;
        float GT = Mathf.Max(0f, G
            * Mathf.Cos(Mathf.Deg2Rad * (tilt - azimuth))
            * Mathf.Cos(Mathf.Deg2Rad * sun));

        if (GT <= 0.1f)
            return false;

        float Vo = GetVo(resistance);
        float V = Vo * (1f - Mathf.Log(GT / 1000f));
        float ISC = V / resistance * 2.5f;
        float I = ISC * GT / 1000f;

        if (isSeries) V *= 3f;
        if (isParallel) I *= 3f;

        float P = V * I;

        voltage = V;
        current = I;
        power = P;
        return true;
    }
    private float GetGo()
    {
        float source = intensitySlider != null ? intensitySlider.value : defaultGo;
        return Mathf.Lerp(goAtSliderMinimum, goAtSliderMaximum,
            Mathf.InverseLerp(sliderMinimum, sliderMaximum, source));
    }

    private float GetResistance()
    {
        if (graphResistance > 0f)
            return graphResistance;

        if (rheostat != null)
            return rheostat.CurrentResistance;

        return 100f;
    }

    /// <summary>Finds the closest Vo for the given resistance.</summary>
    private static float GetVo(float resistance)
    {
        int best = 0;
        float bestDiff = Mathf.Abs(ResistanceSteps[0] - resistance);
        for (int i = 1; i < ResistanceSteps.Length; i++)
        {
            float diff = Mathf.Abs(ResistanceSteps[i] - resistance);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        return VoValues[best];
    }

    private static float GetAngle(PivotValueRotator rotator) =>
        rotator == null ? 0f : Mathf.Abs(rotator.ReportedValue);

    private void SetOutput(string voltage, string current, string power)
    {
        SetText(voltageText, legacyVoltageText, voltage);
        SetText(currentText, legacyCurrentText, current);
        SetText(powerText, legacyPowerText, power);
    }

    private static void SetText(TMP_Text tmp, Text legacy, string value)
    {
        if (tmp != null) tmp.text = value;
        if (legacy != null) legacy.text = value;
    }

    private void OnIntensityChanged(float _) => RefreshDisplay();
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
