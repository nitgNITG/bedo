using Obvious.Soap;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WHY THIS EXISTS:
/// - Unity's built-in ToggleGroup + UI "Selected/Highlighted" visuals are focus/hover-based.
///   When you click elsewhere, the previous toggle can lose its "selected look".
/// - This group exists to enforce "single selection inside THIS group only" without Unity ToggleGroup,
///   so multiple groups won't interfere with each other.
/// - AllowSwitchOff is a BoolReference so you can drive it from your variable system.
/// </summary>
public class IndependentToggleGroup : MonoBehaviour
{
    [Tooltip("If false, this group will always keep at least one toggle ON.")]
    [SerializeField] private BoolReference allowSwitchOff = new BoolReference(false, false);

    private readonly List<IndependentAnimatedToggle> toggles = new();

    public IndependentAnimatedToggle Current { get; private set; }
    public bool AllowSwitchOff => allowSwitchOff.Value;

    public void Register(IndependentAnimatedToggle t)
    {
        if (t == null || toggles.Contains(t)) return;
        toggles.Add(t);

        // If a toggle is already ON while registering, enforce single selection.
        if (t.IsOn)
            NotifyToggleOn(t, fromGroup: true);
    }

    public void Unregister(IndependentAnimatedToggle t)
    {
        if (t == null) return;
        toggles.Remove(t);
        if (Current == t) Current = null;
    }

    /// <summary>
    /// Called by a toggle when it becomes ON. Turns OFF other toggles in THIS group only.
    /// </summary>
    public void NotifyToggleOn(IndependentAnimatedToggle onToggle, bool fromGroup = false)
    {
        if (onToggle == null) return;

        Current = onToggle;

        for (int i = 0; i < toggles.Count; i++)
        {
            var t = toggles[i];
            if (t == null || t == onToggle) continue;

            if (t.IsOn)
                t.SetIsOnFromGroup(false);
        }

        onToggle.RefreshVisualState();
    }

    public void NotifyToggleOff(IndependentAnimatedToggle offToggle)
    {
        if (offToggle != null && Current == offToggle)
            Current = null;
    }

    /// <summary>
    /// True if ANY other toggle (besides 'except') is currently ON.
    /// Used to prevent ending up with "none selected" when AllowSwitchOff == false.
    /// </summary>
    public bool AnyOnExcept(IndependentAnimatedToggle except)
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            var t = toggles[i];
            if (t == null || t == except) continue;
            if (t.IsOn) return true;
        }
        return false;
    }
}
