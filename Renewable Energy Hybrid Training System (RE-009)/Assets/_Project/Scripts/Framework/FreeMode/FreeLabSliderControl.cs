using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace TutorialFramework
{
    /// <summary>
    /// Shared numeric control for Free Mode, such as a potentiometer,
    /// brightness/intensity setting, or a UI slider. Assign a Unity UI Slider
    /// when available, or call SetValue from an existing 3D control script.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabSliderControl : MonoBehaviour
    {
        [Serializable]
        public class AdjustmentConfiguration
        {
            public List<FreeLabControl.ConnectionRequirement>
                requiredConnections =
                    new List<FreeLabControl.ConnectionRequirement>();
            [Tooltip("Every assigned cable group must be complete.")]
            public List<FreeLabConnectionGroup> requiredConnectionGroups =
                new List<FreeLabConnectionGroup>();
            [Tooltip("Optional alternatives: at least one complete group is required.")]
            public List<FreeLabConnectionGroup> anyConnectionGroups =
                new List<FreeLabConnectionGroup>();
            public List<FreeLabControl.ControlRequirement>
                requiredControls =
                    new List<FreeLabControl.ControlRequirement>();
        }

        [SerializeField]
        private string controlId;

        [SerializeField]
        private Slider uiSlider;

        [SerializeField]
        private float value;

        [SerializeField]
        private UnityEvent<float> onValueChanged;

        [SerializeField]
        private float lockedValue;

        [SerializeField]
        private UnityEvent onLocked;

        [SerializeField]
        private UnityEvent onUnlocked;

        [Header("Adjustment Constraints")]
        [Tooltip("When enabled, the slider cannot be changed while a shared power control marks the system as operating.")]
        [SerializeField]
        private bool preventAdjustmentWhileOperating = true;

        [Tooltip("Optional prerequisite configurations for adjusting this slider. Every requirement in one configuration is required; any one configuration permits adjustment.")]
        [SerializeField]
        private List<AdjustmentConfiguration> adjustmentConfigurations =
            new List<AdjustmentConfiguration>();

        [Header("Runtime State (Play Mode)")]
        [SerializeField]
        private bool isAvailable;

        public string ControlId => string.IsNullOrWhiteSpace(controlId)
            ? gameObject.name
            : controlId;

        public float Value => value;
        public bool PreventAdjustmentWhileOperating =>
            preventAdjustmentWhileOperating;
        public bool IsAvailable => isAvailable;
        public event Action<float> ValueChanged;

        private void Awake()
        {
            if (uiSlider == null)
                uiSlider = GetComponent<Slider>();

            if (uiSlider != null)
            {
                value = uiSlider.value;
                uiSlider.onValueChanged.AddListener(SetValue);
            }
        }

        private void OnDestroy()
        {
            if (uiSlider != null)
                uiSlider.onValueChanged.RemoveListener(SetValue);
        }

        public void SetValue(float newValue)
        {
            if (FreeLabManager.Instance != null &&
                InteractionManager.Instance != null &&
                InteractionManager.Instance.Free)
            {
                FreeLabManager.Instance.TrySetSliderValue(this, newValue);
                return;
            }

            ApplyValue(newValue);
        }

        public bool CanAdjust(FreeLabManager lab)
        {
            if (adjustmentConfigurations.Count == 0)
                return true;

            foreach (AdjustmentConfiguration configuration in
                adjustmentConfigurations)
            {
                if (configuration != null && Matches(lab, configuration))
                    return true;
            }

            return false;
        }

        public void RefreshAvailability(FreeLabManager lab)
        {
            bool nextAvailable =
                (!preventAdjustmentWhileOperating ||
                 !lab.IsSystemOperating()) &&
                CanAdjust(lab);

            if (isAvailable != nextAvailable)
            {
                isAvailable = nextAvailable;

                if (isAvailable)
                    onUnlocked?.Invoke();
                else
                    onLocked?.Invoke();
            }

            if (uiSlider != null)
                uiSlider.interactable = isAvailable;

            if (!isAvailable)
                ForceLockedValue();
        }

        internal void ApplyValue(float newValue)
        {
            if (Mathf.Approximately(value, newValue))
                return;

            value = newValue;

            if (uiSlider != null &&
                !Mathf.Approximately(uiSlider.value, value))
            {
                uiSlider.SetValueWithoutNotify(value);
            }

            onValueChanged?.Invoke(value);
            ValueChanged?.Invoke(value);
            FreeLabManager.Instance?.RefreshEvaluators();
        }

        internal void RestoreVisualValue()
        {
            if (uiSlider != null)
                uiSlider.SetValueWithoutNotify(value);
        }

        private void ForceLockedValue()
        {
            if (Mathf.Approximately(value, lockedValue))
            {
                RestoreVisualValue();
                return;
            }

            value = lockedValue;

            if (uiSlider != null)
                uiSlider.SetValueWithoutNotify(value);

            onValueChanged?.Invoke(value);
            ValueChanged?.Invoke(value);
            FreeLabManager.Instance?.RefreshEvaluators();
        }

        private static bool Matches(
            FreeLabManager lab,
            AdjustmentConfiguration configuration)
        {
            foreach (FreeLabControl.ConnectionRequirement requirement in
                configuration.requiredConnections)
            {
                if (requirement == null ||
                    requirement.endpointA == null ||
                    requirement.endpointB == null ||
                    !lab.HasConnection(
                        requirement.endpointA,
                        requirement.endpointB))
                {
                    return false;
                }
            }

            foreach (FreeLabConnectionGroup group in
                configuration.requiredConnectionGroups)
            {
                if (group == null || !group.IsComplete(lab))
                    return false;
            }

            if (configuration.anyConnectionGroups.Count > 0)
            {
                bool anyComplete = false;
                foreach (FreeLabConnectionGroup group in
                    configuration.anyConnectionGroups)
                {
                    if (group != null && group.IsComplete(lab))
                    {
                        anyComplete = true;
                        break;
                    }
                }

                if (!anyComplete)
                    return false;
            }

            foreach (FreeLabControl.ControlRequirement requirement in
                configuration.requiredControls)
            {
                if (requirement == null ||
                    requirement.control == null ||
                    requirement.control.Value != requirement.requiredValue)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
