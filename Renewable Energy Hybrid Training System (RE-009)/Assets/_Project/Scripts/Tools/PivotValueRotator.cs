using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class PivotValueRotator : MonoBehaviour
{
    // =====================================================
    // VALUE
    // =====================================================

    [Header("Value")]
    [SerializeField] private int currentValue = 0;
    [SerializeField] private int minValue = -1;
    [SerializeField] private int maxValue = 1;

    [Tooltip("The positive value reported for each internal step. This is separate from the visual Rotation Per Step.")]
    [SerializeField] private float reportedValuePerStep = 1f;

    public int CurrentValue => currentValue;
    public int AbsoluteValue => Mathf.Abs(currentValue);
    public event Action<int> ValueChanged;
    public float AngleValue => Mathf.Abs(currentValue * rotationPerStep);
    public event Action<float> AngleValueChanged;
    public float ReportedValue => Mathf.Abs(
        currentValue * reportedValuePerStep
    );
    public event Action<float> ReportedValueChanged;

    [Header("Value Display")]
    [Tooltip("Optional text display. The shown value is always positive.")]
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Text legacyValueText;


    // =====================================================
    // ROTATION
    // =====================================================

    [Header("Rotation")]

    [Tooltip("The point around which this object will rotate.")]
    [SerializeField] private Transform pivot;

    [Tooltip("The local axis around which the object rotates.")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

    [Tooltip("Degrees rotated for every increase/decrease.")]
    [SerializeField] private float rotationPerStep = 5f;

    [Tooltip(
        "Determines which direction IncreaseValue rotates. " +
        "DecreaseValue automatically rotates in the opposite direction."
    )]
    [SerializeField]
    private RotationDirection increaseDirection =
        RotationDirection.Positive;


    // =====================================================
    // TARGET VALUES
    // =====================================================

    [Header("Value Events")]

    [Tooltip(
        "Each entry can monitor a specific value. " +
        "Use StartCheckingIndex(index) to activate one."
    )]
    [SerializeField]
    private List<ValueEvent> valueEvents =
        new List<ValueEvent>();


    // =====================================================
    // INTERNAL
    // =====================================================

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private enum RotationAxis
    {
        X,
        Y,
        Z
    }

    private enum RotationDirection
    {
        Positive = 1,
        Negative = -1
    }


    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        ApplyRotation();
        UpdateValueDisplay();
    }


    // =====================================================
    // VALUE CONTROL
    // =====================================================

    public void IncreaseValue()
    {
        if (currentValue >= maxValue)
            return;

        currentValue++;

        RotateAroundPivot(
            rotationPerStep *
            (float)increaseDirection
        );

        CheckValueEvents();
        NotifyValueChanged();
    }


    public void DecreaseValue()
    {
        if (currentValue <= minValue)
            return;

        currentValue--;

        RotateAroundPivot(
            -rotationPerStep *
            (float)increaseDirection
        );

        CheckValueEvents();
        NotifyValueChanged();
    }


    // =====================================================
    // ROTATION
    // =====================================================

    private void RotateAroundPivot(float degrees)
    {
        if (pivot == null)
        {
            Debug.LogWarning(
                $"{name}: No pivot has been assigned."
            );

            return;
        }

        Vector3 axis = GetRotationAxis();

        transform.RotateAround(
            pivot.position,
            axis,
            degrees
        );
    }


    private Vector3 GetRotationAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return pivot.right;

            case RotationAxis.Y:
                return pivot.up;

            case RotationAxis.Z:
                return pivot.forward;
        }

        return pivot.up;
    }


    private void ApplyRotation()
    {
        if (pivot == null)
            return;

        /*
         * Reset to the original transform before calculating
         * the rotation based on the current value.
         */

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        int stepsFromZero = currentValue;

        float totalRotation =
            stepsFromZero *
            rotationPerStep *
            (float)increaseDirection;

        RotateAroundPivot(totalRotation);
    }


    // =====================================================
    // VALUE EVENTS
    // =====================================================

    private void CheckValueEvents()
    {
        for (int i = 0; i < valueEvents.Count; i++)
        {
            ValueEvent valueEvent = valueEvents[i];

            if (valueEvent == null)
                continue;

            if (!valueEvent.checking)
                continue;

            if (currentValue != valueEvent.value)
                continue;

            if (valueEvent.eventFired)
                continue;

            valueEvent.eventFired = true;

            valueEvent.onValueReached?.Invoke();

            Debug.Log(
                $"{name}: Reached monitored value " +
                $"{valueEvent.value} (Index {i})"
            );
        }
    }

    private void NotifyValueChanged()
    {
        UpdateValueDisplay();
        ValueChanged?.Invoke(AbsoluteValue);
        AngleValueChanged?.Invoke(AngleValue);
        ReportedValueChanged?.Invoke(ReportedValue);
    }

    private void UpdateValueDisplay()
    {
        string displayValue = ReportedValue.ToString("0.##");

        if (valueText != null)
            valueText.text = displayValue;

        if (legacyValueText != null)
            legacyValueText.text = displayValue;
    }


    // =====================================================
    // START CHECKING
    // =====================================================

    /// <summary>
    /// Starts checking the ValueEvent at the specified index.
    /// Only that specific entry will become active.
    /// </summary>
    public void StartCheckingIndex(int index)
    {
        if (index < 0 || index >= valueEvents.Count)
        {
            Debug.LogWarning(
                $"{name}: StartCheckingIndex received invalid index " +
                $"{index}. List count: {valueEvents.Count}"
            );

            return;
        }

        ValueEvent valueEvent = valueEvents[index];

        if (valueEvent == null)
        {
            Debug.LogWarning(
                $"{name}: ValueEvent at index " +
                $"{index} is null."
            );

            return;
        }

        valueEvent.checking = true;
        valueEvent.eventFired = false;

        Debug.Log(
            $"{name}: Started checking value event at index " +
            $"{index}, target value = {valueEvent.value}"
        );
    }


    /// <summary>
    /// Stops checking the ValueEvent at the specified index.
    /// </summary>
    public void StopCheckingIndex(int index)
    {
        if (index < 0 || index >= valueEvents.Count)
            return;

        if (valueEvents[index] == null)
            return;

        valueEvents[index].checking = false;
    }


    /// <summary>
    /// Resets the event-fired state for a specific index.
    /// </summary>
    public void ResetCheckingIndex(int index)
    {
        if (index < 0 || index >= valueEvents.Count)
            return;

        if (valueEvents[index] == null)
            return;

        valueEvents[index].eventFired = false;
    }


    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmos()
    {
        if (pivot == null)
            return;

        Vector3 axis = GetRotationAxis();

        // Pivot
        Gizmos.DrawSphere(
            pivot.position,
            0.025f
        );

        // Rotation axis / line
        float lineLength = 0.5f;

        Vector3 start =
            pivot.position -
            axis * lineLength;

        Vector3 end =
            pivot.position +
            axis * lineLength;

        Gizmos.DrawLine(
            start,
            end
        );

        // Direction indicator
        Gizmos.DrawLine(
            pivot.position,
            pivot.position + axis * 0.15f
        );
    }


    // =====================================================
    // SERIALIZABLE EVENT
    // =====================================================

    [Serializable]
    public class ValueEvent
    {
        [Tooltip("The value that this entry is looking for.")]
        public int value;

        [Tooltip(
            "This is activated by StartCheckingIndex(index)."
        )]
        [HideInInspector]
        public bool checking;

        [Tooltip(
            "Prevents the event from firing repeatedly while " +
            "the object remains at this value."
        )]
        [HideInInspector]
        public bool eventFired;

        public UnityEvent onValueReached;
    }
}
