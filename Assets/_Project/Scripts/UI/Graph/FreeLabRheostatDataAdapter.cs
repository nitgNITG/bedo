using System;
using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;

/// <summary>
/// Free Mode-only reader for the existing Guided Mode RheostatController.
/// It never changes the controller; it only reports the matching resistance
/// whenever its existing value changes.
/// </summary>
[DisallowMultipleComponent]
public class FreeLabRheostatDataAdapter : MonoBehaviour
{
    [SerializeField] private RheostatController rheostat;

    [Header("Free Lab Source Selection")]
    [Tooltip("This rheostat supplies the data only while this complete Free Lab cable group is connected.")]
    [SerializeField] private FreeLabConnectionGroup connectionGroup;

    [Tooltip("Reference only for future CSV data. It does not change the existing rheostat controller range.")]
    [SerializeField] private float physicalMaximumResistance = 800f;

    [Header("CSV Resistance Values (highest controller click to lowest)")]
    [Tooltip("All three rheostats use the current CSV values for now. Replace this list when the client supplies data for this specific module.")]
    [SerializeField] private List<float> csvResistanceValues =
        new List<float> { 100f, 80f, 60f, 40f, 20f, 13f };

    private static readonly List<FreeLabRheostatDataAdapter> Adapters =
        new List<FreeLabRheostatDataAdapter>();
    private static FreeLabRheostatDataAdapter activeAdapter;

    public float ResistanceOhms => activeAdapter != null
        ? activeAdapter.CurrentResistance
        : CurrentResistance;
    public float PhysicalMaximumResistance => physicalMaximumResistance;
    public bool IsActiveSource => activeAdapter == this;
    public static bool HasActiveSource => activeAdapter != null;
    public static float ActiveResistanceOhms => activeAdapter != null
        ? activeAdapter.CurrentResistance
        : 0f;

    /// <summary>
    /// True only for the physical rheostat whose complete Free Lab cable
    /// group is currently connected. Used to prevent disconnected modules
    /// from submitting duplicate graph readings.
    /// </summary>
    public static bool IsActiveRheostat(RheostatController candidate)
    {
        return activeAdapter != null && activeAdapter.rheostat == candidate;
    }

    public event Action<float> ResistanceChanged;
    public static event Action<float> ActiveResistanceChanged;

    private float lastControllerValue = float.MinValue;
    private float lastPublishedResistance = float.NaN;

    private void Awake()
    {
        if (rheostat == null)
            rheostat = GetComponent<RheostatController>();

        if (rheostat == null)
            rheostat = GetComponentInParent<RheostatController>();
    }

    private void OnEnable()
    {
        if (!Adapters.Contains(this))
            Adapters.Add(this);
        RefreshActiveSource();
    }

    private void OnDisable()
    {
        Adapters.Remove(this);
        RefreshActiveSource();
    }

    private void Update()
    {
        RefreshActiveSource();

        // NEW
        if (rheostat != null && !Mathf.Approximately(rheostat.CurrentResistance, lastControllerValue))
        {
            lastControllerValue = rheostat.CurrentResistance;
            ResistanceChanged?.Invoke(CurrentResistance);
        }

        if (activeAdapter == this && !Mathf.Approximately(
            lastPublishedResistance, CurrentResistance))
        {
            lastPublishedResistance = CurrentResistance;
            ActiveResistanceChanged?.Invoke(CurrentResistance);
        }
    }

    public void ConfigureConnectionGroup(
        FreeLabConnectionGroup group,
        float maximumResistance)
    {
        connectionGroup = group;
        physicalMaximumResistance = maximumResistance;
    }

    // NEW
    private float CurrentResistance =>
        rheostat != null ? rheostat.CurrentResistance : 0f;

    /// <summary>
    /// Re-evaluates the connected physical rheostat immediately. Free Lab
    /// calls this after every wire change, before its calculation binders
    /// refresh their displays.
    /// </summary>
    public static void RefreshActiveSource()
    {
        FreeLabRheostatDataAdapter previous = activeAdapter;
        activeAdapter = null;

        FreeLabManager lab = FreeLabManager.Instance;
        if (lab != null)
        {
            foreach (FreeLabRheostatDataAdapter adapter in Adapters)
            {
                if (adapter != null && adapter.connectionGroup != null &&
                    adapter.connectionGroup.IsComplete(lab))
                {
                    activeAdapter = adapter;
                    break;
                }
            }
        }

        if (previous != activeAdapter)
        {
            if (activeAdapter != null)
                activeAdapter.lastPublishedResistance =
                    activeAdapter.CurrentResistance;

            ActiveResistanceChanged?.Invoke(
                ActiveResistanceOhms);
        }
    }
}
