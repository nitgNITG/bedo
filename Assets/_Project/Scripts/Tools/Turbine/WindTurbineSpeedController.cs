using TutorialFramework.Simulation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TutorialFramework;

public class WindTurbineSpeedController : MonoBehaviour
{
    [Header("Speed (RPM)")]
    [SerializeField] private float minSpeed = 0f;
    [SerializeField] private float maxSpeed = 1000f;
    [SerializeField] private float speedStep = 100f;

    [Header("EXP1 wind model (optional)")]
    [SerializeField] private float initialSpeed = 0f;
    [SerializeField] private TextAsset windCalibration;
    [SerializeField] private RheostatController loadRheostat;
    [SerializeField] private float defaultResistance = 100f;
    [SerializeField] private PVGuidedDataBinder dataBinder;
    [SerializeField] private PVGraphController graphController;
    [Header("Guided speed slider (optional)")]
    [SerializeField] private Slider speedSlider;
    [SerializeField] private StoryboardSteps speedStoryboard;
    [SerializeField] private Collider speedSliderCollider;
    [SerializeField] private int speedStepIndex = -1;
    public float MaximumSpeed => MaxSpeed;
    public bool IsActiveSliderExperiment => speedStoryboard != null &&
        speedStoryboard.gameObject.activeInHierarchy && InteractionManager.Instance != null &&
        InteractionManager.Instance.Guided && InteractionManager.Instance.IsExperimentActive(speedStoryboard);
    private bool IsSpeedStep => speedStoryboard != null && speedStepIndex >= 0 &&
        speedStoryboard.CurrentStep == speedStepIndex;
    public bool CanUseSpeedSlider => IsActiveSliderExperiment &&
        speedSliderCollider != null && speedSliderCollider.enabled;
    public void SetSliderSpeed(float rpm)
    {
        if (!CanUseSpeedSlider) return;
        // Match the main mathematical tables in every wind experiment.
        rpm = Mathf.Clamp(Mathf.Round(rpm / SliderIncrement) * SliderIncrement, 0f, MaxSpeed);
        SetSpeed(rpm);
        if (speedSlider != null) speedSlider.SetValueWithoutNotify(CurrentSpeed);
    }
    [Header("EXP1 turbine rotor")]
    [SerializeField] private Transform turbineRotor;
    [SerializeField] private RotationAxis rotorAxis = RotationAxis.X;
    [SerializeField, Min(0f)] private float visualSpeedMultiplier = 0.05f;
    private Quaternion rotorBaseRotation;
    private float rotorAngle;
    private bool rotorInitialized;
    [Header("SOAP simulation context")]
    [SerializeField] private WindRuntimeState runtimeState;
    [SerializeField] private WindSimulationSettings simulationSettings;
    private WindSimulationData legacyState;
    public WindRuntimeState SharedState => runtimeState;
    private WindSimulationData State { get => runtimeState ? runtimeState.Value : legacyState; set { if (runtimeState) runtimeState.Value = value; else legacyState = value; } }
    private bool turbineRunning { get => State.turbineRunning; set { var next = State; next.turbineRunning = value; State = next; } }
    private float MinimumSpeed => simulationSettings ? simulationSettings.minimumRPM.Value : minSpeed;
    private float MaxSpeed => simulationSettings ? simulationSettings.maximumRPM.Value : maxSpeed;
    private float SpeedStep => simulationSettings ? simulationSettings.legacyClickIncrement.Value : speedStep;
    private float InitialSpeed => simulationSettings ? simulationSettings.initialRPM.Value : initialSpeed;
    private float VisualSpeedMultiplier => simulationSettings ? simulationSettings.visualRotorMultiplier.Value : visualSpeedMultiplier;
    private float SliderIncrement => simulationSettings ? simulationSettings.sliderIncrement.Value : 200f;
    [Header("Wind module System On indicator")]
    [SerializeField] private GameObject systemOnGlow;
    [SerializeField] private AudioSource windMillAudio;
    public bool IsTurbineRunning => turbineRunning;
    public Transform TurbineRotor => turbineRotor;
    public void ResolveTurbineRotor()
    {
        if (turbineRotor != null || !HasWindModel) return;
        foreach (var candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!candidate.gameObject.scene.IsValid() || candidate.name.Replace('.', '_').ToLowerInvariant() != "axis_circle_003") continue;
            for (Transform parent = candidate; parent != null; parent = parent.parent)
                if (parent.name == "Wind Lab") { turbineRotor = candidate; return; }
        }
    }


    public enum RotationAxis
    {
        X,
        Y,
        Z
    }


    [Header("Control Knob Rotation")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.X;
    [SerializeField] private float rotationPerClick = 5f;

    [Header("Events")]
    public UnityEvent<float> OnSpeedChanged;
    public UnityEvent OnMaximumReached;

    public float CurrentSpeed => initialized ? currentSpeed : Mathf.Clamp(InitialSpeed, MinimumSpeed, MaxSpeed);
    public bool HasWindModel => windCalibration != null;

    public bool TryGetReading(out PVCSVReader.WindReading reading)
    {
        float resistance = loadRheostat != null && loadRheostat.CurrentResistance > 0f
            ? loadRheostat.CurrentResistance : defaultResistance;
        return TryGetReading(resistance, out reading);
    }

    public bool TryGetReading(float resistance, out PVCSVReader.WindReading reading)
    {
        bool valid = PVCSVReader.TryCalculateWind(windCalibration, HasWindModel && !turbineRunning ? 0f : CurrentSpeed, resistance, out reading);
        if (runtimeState) { var next = State; next.source = reading; next.activeResistance = resistance; State = next; }
        return valid;
    }

    public void TurnOn()
    {
        if (turbineRunning) return;
        InitializeSpeed();
        turbineRunning = true;
        if (systemOnGlow != null) systemOnGlow.SetActive(true);
        ResolveTurbineRotor();
        ApplySpeed();
    }

    public void TurnOff()
    {
        turbineRunning = false;
        if (windMillAudio != null) windMillAudio.Stop();
        if (systemOnGlow != null) systemOnGlow.SetActive(false);
        if (dataBinder != null) dataBinder.RefreshDisplay();
    }

    // The meter binder lives under Steps and ticks even while the speed
    // interaction's own step object is inactive.
    public void TickRotor()
    {
        if (!HasWindModel || !turbineRunning) return;
        if (turbineRotor == null) return;
        Vector3 axis = rotorAxis == RotationAxis.X ? Vector3.right : rotorAxis == RotationAxis.Y ? Vector3.up : Vector3.forward;
        if (!rotorInitialized) { rotorBaseRotation = turbineRotor.localRotation; rotorInitialized = true; }
        rotorAngle = (rotorAngle + CurrentSpeed * 6f * VisualSpeedMultiplier * Time.deltaTime) % 360f;
        turbineRotor.localRotation = rotorBaseRotation * Quaternion.AngleAxis(rotorAngle, axis);
    }


    private float currentSpeed { get => State.requestedRPM; set { var next = State; next.requestedRPM = value; State = next; } }
    private bool maximumEventFired;
    private Quaternion initialRotation;
    private bool initialized;

    private void Awake()
    {
        InitializeSpeed();
        if (systemOnGlow != null) systemOnGlow.SetActive(turbineRunning);
    }

    private void InitializeSpeed()
    {
        if (initialized) return;
        initialRotation = transform.localRotation;
        currentSpeed = Mathf.Clamp(InitialSpeed, MinimumSpeed, MaxSpeed);
        if (HasWindModel && graphController != null && speedSlider == null)
        {
            var speeds = graphController.GetWindReferenceSpeeds();
            if (speeds.Length > 0) currentSpeed = speeds[0];
        }
        initialized = true;
    }

    private void Start()
    {
        maximumEventFired = false;

        SetRotation();
        OnSpeedChanged?.Invoke(currentSpeed);
        if (dataBinder != null) dataBinder.RefreshDisplay();
        CheckMaximum();
    }

    public void IncreaseValue()
    {
        InitializeSpeed();
        if (StepReferenceSpeed(true)) return;
        if (currentSpeed >= MaxSpeed)
            return;

        currentSpeed = Mathf.Min(currentSpeed + SpeedStep, MaxSpeed);
        ApplySpeed();
    }

    public void DecreaseValue()
    {
        InitializeSpeed();
        if (StepReferenceSpeed(false)) return;
        if (currentSpeed <= MinimumSpeed)
            return;

        currentSpeed = Mathf.Max(currentSpeed - SpeedStep, MinimumSpeed);
        ApplySpeed();
    }

    public void SetSpeed(float rpm)
    {
        InitializeSpeed();
        float next = Mathf.Clamp(rpm, MinimumSpeed, MaxSpeed);
        if (Mathf.Approximately(next, currentSpeed)) return;
        currentSpeed = next;
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        if (windMillAudio != null)
        {
            if (turbineRunning && CurrentSpeed > 0f)
            {
                windMillAudio.pitch = Mathf.Lerp(0.6f, 1.2f, Mathf.Clamp01(CurrentSpeed / MaxSpeed));
                if (!windMillAudio.isPlaying) windMillAudio.Play();
            }
            else windMillAudio.Stop();
        }
        SetRotation();
        OnSpeedChanged?.Invoke(currentSpeed);
        if (dataBinder != null) dataBinder.RefreshDisplay();
        if (graphController != null) graphController.UpdateWindMeasurement();
        CheckMaximum();
    }

    private bool StepReferenceSpeed(bool increase)
    {
        if (!HasWindModel || graphController == null) return false;
        var speeds = graphController.GetWindReferenceSpeeds();
        if (speeds.Length == 0) return false;
        float next = currentSpeed;
        if (increase)
        {
            foreach (float rpm in speeds) if (rpm > currentSpeed + .001f) { next = rpm; break; }
        }
        else
        {
            for (int i = speeds.Length - 1; i >= 0; i--) if (speeds[i] < currentSpeed - .001f) { next = speeds[i]; break; }
        }
        if (!Mathf.Approximately(next, currentSpeed)) { currentSpeed = next; ApplySpeed(); }
        return true;
    }

    private void CheckMaximum()
    {
        // Dragging after the authored speed step must never advance another row.
        if (speedSlider != null && (!IsActiveSliderExperiment || !IsSpeedStep)) return;
        if (currentSpeed >= MaxSpeed && !maximumEventFired)
        {
            maximumEventFired = true;
            OnMaximumReached?.Invoke();
            Debug.Log("Wind turbine has reached maximum speed!");
        }
    }

    private void SetRotation()
    {
        if (speedSlider != null) return;
        float steps = (currentSpeed - MinimumSpeed) / SpeedStep;
        float angle = steps * rotationPerClick;

        Vector3 axis;

        switch (rotationAxis)
        {
            case RotationAxis.Y:
                axis = Vector3.up;
                break;

            case RotationAxis.Z:
                axis = Vector3.forward;
                break;

            default:
                axis = Vector3.right;
                break;
        }

        transform.localRotation =
            initialRotation * Quaternion.AngleAxis(angle, axis);
    }

    private void OnValidate()
    {
        minSpeed = Mathf.Max(0f, minSpeed);
        maxSpeed = Mathf.Max(minSpeed, maxSpeed);
        speedStep = Mathf.Max(0.01f, speedStep);
    }
}
