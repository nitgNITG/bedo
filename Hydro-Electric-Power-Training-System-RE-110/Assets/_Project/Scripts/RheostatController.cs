using Obvious.Soap;
using TutorialFramework.Simulation;
using UnityEngine;
using UnityEngine.Events;

public class RheostatController : MonoBehaviour
{
    [Header("Resistance")]
    [SerializeField] private float maxResistance = 100f;
    [SerializeField] private float minResistance = 13f;
    [SerializeField] private float resistanceStep = 20f;

    [Header("Rotation")]
    [SerializeField] private float rotationPerClick = 5f;

    [Header("PV Graph")]
    [SerializeField] private PVGraphController pvGraphController;

    [Header("Events")]
    public UnityEvent OnMinimumReached;

    public float CurrentResistance => currentResistance;

    public void ConfigureFreeModeRange(float maximum, float minimum, float step)
    {
        if (TutorialFramework.InteractionManager.Instance == null ||
            !TutorialFramework.InteractionManager.Instance.Free) return;
        MaximumResistance = maximum;
        MinimumResistance = Mathf.Clamp(minimum, .01f, maximum);
        ResistanceStep = Mathf.Max(.01f, step);
        currentResistance = maximum;
        minimumEventFired = false;
        SetRotation();
    }

    [Header("SOAP state and parameters")]
    [SerializeField] private FloatReference resistanceState = new FloatReference(0, true);
    [SerializeField] private FloatReference maximumResistance = new FloatReference();
    [SerializeField] private FloatReference minimumResistance = new FloatReference();
    [SerializeField] private FloatReference resistanceIncrement = new FloatReference();
    [SerializeField] private FloatReference rotationIncrement = new FloatReference();
    private float currentResistance { get => resistanceState.Value; set => resistanceState.Value = value; }
    private float MaximumResistance { get => WindSoapReference.Read(maximumResistance, maxResistance); set => WindSoapReference.Write(maximumResistance, ref maxResistance, value); }
    private float MinimumResistance { get => WindSoapReference.Read(minimumResistance, minResistance); set => WindSoapReference.Write(minimumResistance, ref minResistance, value); }
    private float ResistanceStep { get => WindSoapReference.Read(resistanceIncrement, resistanceStep); set => WindSoapReference.Write(resistanceIncrement, ref resistanceStep, value); }
    private float RotationPerClick => WindSoapReference.Read(rotationIncrement, rotationPerClick);
    private bool minimumEventFired = false;

    private void Start()
    {
        currentResistance = MaximumResistance;
        minimumEventFired = false;
        SetRotation();
    }

    public void IncreaseValue()
    {
        if (currentResistance >= MaximumResistance)
            return;

        currentResistance = Mathf.Min(currentResistance + ResistanceStep, MaximumResistance);
        SetRotation();
        UpdateGraph();
        CheckMinimum();
    }

    public void DecreaseValue()
    {
        if (currentResistance <= MinimumResistance)
            return;

        currentResistance = Mathf.Max(currentResistance - ResistanceStep, MinimumResistance);
        SetRotation();
        UpdateGraph();
        CheckMinimum();
    }

    private void UpdateGraph()
    {
        if (pvGraphController != null && pvGraphController.isActiveAndEnabled)
            pvGraphController.UpdateMeasurement(currentResistance, this);
    }

    private void CheckMinimum()
    {
        if (currentResistance <= MinimumResistance && !minimumEventFired)
        {
            minimumEventFired = true;
            OnMinimumReached?.Invoke();
            Debug.Log("Rheostat has reached minimum resistance!");
        }
    }

    private void SetRotation()
    {
        float steps = (MaximumResistance - currentResistance) / ResistanceStep;
        float xRotation = steps * RotationPerClick;
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(xRotation, currentEuler.y, currentEuler.z);
    }
}
