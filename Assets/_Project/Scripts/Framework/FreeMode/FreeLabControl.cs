using Obvious.Soap;
using TutorialFramework.Simulation;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    /// <summary>
    /// Shared physical switch/button state used by Free Mode evaluators.
    /// Attach this to the same GameObject as its Interaction.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabControl : MonoBehaviour
    {
        [Serializable]
        public class ConnectionRequirement
        {
            public FreeLabEndpoint endpointA;
            public FreeLabEndpoint endpointB;
        }

        [Serializable]
        public class ControlRequirement
        {
            public FreeLabControl control;
            public bool requiredValue = true;
        }

        [Serializable]
        public class SliderRequirement
        {
            public FreeLabSliderControl slider;
            public float minimumValue;
            public float maximumValue = 1f;
        }

        [Serializable]
        public class DeviceGroupRequirement
        {
            [Tooltip("A device group is complete when one of its assigned device alternatives has all required wires connected.")]
            public FreeLabDeviceGroup deviceGroup;
        }

        [Serializable]
        public class ActivationConfiguration
        {
            [Tooltip("Every pair in this configuration is required. Add another configuration for an alternative valid experiment setup.")]
            public List<ConnectionRequirement> requiredConnections =
                new List<ConnectionRequirement>();

            [Tooltip("Every assigned cable group must be complete. A group reads its endpoints from its authored cable definitions.")]
            public List<FreeLabConnectionGroup> requiredConnectionGroups =
                new List<FreeLabConnectionGroup>();

            [Tooltip("Optional alternatives: at least one complete group is required. Use this for 'any rheostat may be connected' rules.")]
            public List<FreeLabConnectionGroup> anyConnectionGroups =
                new List<FreeLabConnectionGroup>();

            [Tooltip("Every row is required, but each row accepts any one of its cable-group alternatives. Use separate rows for independent DC and AC lamp roles.")]
            public List<FreeLabConnectionGroupAlternativeRequirement> requiredAlternativeConnectionGroups =
                new List<FreeLabConnectionGroupAlternativeRequirement>();

            [Tooltip("Every assigned device group must have one connected device.")]
            public List<DeviceGroupRequirement> requiredDeviceGroups =
                new List<DeviceGroupRequirement>();

            [Tooltip("Optional device alternatives: at least one group must have a connected device.")]
            public List<DeviceGroupRequirement> anyDeviceGroups =
                new List<DeviceGroupRequirement>();

            [Tooltip("Every shared button, lever, or breaker condition in this configuration is required.")]
            public List<ControlRequirement> requiredControls =
                new List<ControlRequirement>();

            [Tooltip("Every slider/potentiometer range in this configuration is required.")]
            public List<SliderRequirement> requiredSliders =
                new List<SliderRequirement>();
        }

        [SerializeField]
        private string controlId;

        [SerializeField]
        private Interaction interaction;

        [SerializeField]
        private bool value;

        [Tooltip("Use for main/system power controls. Wiring is blocked while any of these controls are on.")]
        [SerializeField]
        private bool marksSystemOperating;

        [Tooltip("The client material requires a module connection before this control can be switched on.")]
        [SerializeField]
        private bool requiresAtLeastOneConnection;

        [Header("Safety Interlock")]
        [Tooltip("Optional power controls that must be shut off and kept unavailable while this safety control is off. Add Main Board On/Off here for an emergency button or circuit breaker.")]
        [SerializeField]
        private List<FreeLabControl> powerControlsToCutAndLock =
            new List<FreeLabControl>();

        [Header("Enable Requirements")]
        [Tooltip("Optional complete wiring configurations for turning this control on. Every pair in one configuration is required; any one complete configuration permits the action.")]
        [SerializeField]
        private List<ActivationConfiguration> enableConfigurations =
            new List<ActivationConfiguration>();

        [SerializeField]
        private UnityEvent onEnabled;

        [SerializeField]
        private UnityEvent onDisabled;

        public string ControlId => string.IsNullOrWhiteSpace(controlId)
            ? gameObject.name
            : controlId;

        [SerializeField] private BoolReference switchState = new BoolReference();
        public bool Value => WindSoapReference.Read(switchState, value);
        public bool MarksSystemOperating => marksSystemOperating;
        public bool RequiresAtLeastOneConnection => requiresAtLeastOneConnection;
        public bool IsSafetyInterlock => powerControlsToCutAndLock != null &&
            powerControlsToCutAndLock.Count > 0;

        public bool LocksPowerControl(FreeLabControl powerControl)
        {
            return powerControl != null && powerControlsToCutAndLock != null &&
                powerControlsToCutAndLock.Contains(powerControl);
        }

        public bool CanEnable(FreeLabManager lab)
        {
            if (lab != null && lab.IsControlSafetyLocked(this))
                return false;

            // AC lamp wiring is a physical, linked path: the red/black inlet
            // and L/N must belong to one exact lamp. This is Free Mode-only
            // because the guard exists only under the Free Lab device manager.
            if (marksSystemOperating && lab != null)
            {
                FreeLabAcLampPairGuard lampGuard =
                    lab.GetComponentInChildren<FreeLabAcLampPairGuard>(true);
                if (lampGuard != null && lampGuard.BlocksPowerOn(lab))
                    return false;
            }

            // Rheostats are physical adjustment controls, not switches that
            // need a completed experiment before they can be turned. Their
            // separate data adapter decides whether their value is currently
            // relevant to the shared CSV output.
            if (GetComponent<FreeLabRheostatDataAdapter>() != null)
                return true;

            if (enableConfigurations.Count == 0)
                return !requiresAtLeastOneConnection || lab.ConnectionCount > 0;

            foreach (ActivationConfiguration configuration in
                enableConfigurations)
            {
                if (configuration != null && Matches(lab, configuration))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True when no authored enable configuration has its physical cable
        /// requirements complete. Controls and sliders are deliberately not
        /// considered here: they must not produce a "Connect the modules"
        /// warning when they are the only reason activation is denied.
        /// </summary>
        public bool HasMissingWiringRequirements(FreeLabManager lab)
        {
            if (lab == null)
                return false;

            if (marksSystemOperating)
            {
                FreeLabAcLampPairGuard lampGuard =
                    lab.GetComponentInChildren<FreeLabAcLampPairGuard>(true);
                if (lampGuard != null && lampGuard.BlocksPowerOn(lab))
                    return true;
            }

            if (enableConfigurations.Count == 0)
                return requiresAtLeastOneConnection && lab.ConnectionCount == 0;

            foreach (ActivationConfiguration configuration in enableConfigurations)
            {
                if (configuration != null && MatchesWiringOnly(lab, configuration))
                    return false;
            }

            return true;
        }

        /// <summary>Checks one zero-based Enable Configurations element without changing this control.</summary>
        public bool MatchesEnableConfiguration(FreeLabManager lab, int configurationIndex)
        {
            return lab != null && configurationIndex >= 0 &&
                configurationIndex < enableConfigurations.Count &&
                enableConfigurations[configurationIndex] != null &&
                Matches(lab, enableConfigurations[configurationIndex]);
        }

        public string DescribeEnableConfiguration(FreeLabManager lab, int configurationIndex)
        {
            if (lab == null) return "Free Lab Manager is missing.";
            if (configurationIndex < 0 || configurationIndex >= enableConfigurations.Count)
                return $"Configuration {configurationIndex} does not exist.";

            ActivationConfiguration configuration = enableConfigurations[configurationIndex];
            var result = new System.Text.StringBuilder(
                $"Main Board Enable Configuration {configurationIndex}:\n");
            foreach (ConnectionRequirement requirement in configuration.requiredConnections)
            {
                bool connected = requirement != null && requirement.endpointA != null &&
                    requirement.endpointB != null && lab.HasConnection(requirement.endpointA, requirement.endpointB);
                string first = requirement?.endpointA != null ? requirement.endpointA.EndpointId : "<missing endpoint A>";
                string second = requirement?.endpointB != null ? requirement.endpointB.EndpointId : "<missing endpoint B>";
                result.AppendLine($"  {(connected ? "OK" : "MISSING")}: {first} <-> {second}");
            }
            foreach (FreeLabConnectionGroup group in configuration.requiredConnectionGroups)
                result.AppendLine($"  {(group != null && group.IsComplete(lab) ? "OK" : "MISSING")}: group {group?.name ?? "<null>"}");
            foreach (FreeLabConnectionGroupAlternativeRequirement requirement in configuration.requiredAlternativeConnectionGroups)
                result.AppendLine($"  {(requirement != null && requirement.IsComplete(lab) ? "OK" : "MISSING")}: one alternative cable group");
            foreach (ControlRequirement requirement in configuration.requiredControls)
                result.AppendLine($"  {(requirement != null && requirement.control != null && requirement.control.Value == requirement.requiredValue ? "OK" : "MISSING")}: control {requirement?.control?.ControlId ?? "<null>"}");
            return result.ToString();
        }

        private void Reset()
        {
            interaction = GetComponent<Interaction>();
        }

        private void Awake()
        {
            if (interaction == null)
                interaction = GetComponent<Interaction>();
        }

        public void SetValue(bool newValue)
        {
            if (Value == newValue)
                return;

            WindSoapReference.Write(switchState, ref value, newValue);

            if (Value)
                onEnabled?.Invoke();
            else
            {
                CutAndLockAssignedPowerControls();
                onDisabled?.Invoke();
            }
        }

        public void Toggle()
        {
            SetValue(!Value);
        }

        private void CutAndLockAssignedPowerControls()
        {
            if (powerControlsToCutAndLock == null)
                return;

            foreach (FreeLabControl powerControl in powerControlsToCutAndLock)
            {
                if (powerControl != null && powerControl != this)
                    powerControl.SetValue(false);
            }
        }

        private static bool Matches(
            FreeLabManager lab,
            ActivationConfiguration configuration)
        {
            foreach (ConnectionRequirement requirement in
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

            foreach (FreeLabConnectionGroupAlternativeRequirement requirement in
                configuration.requiredAlternativeConnectionGroups)
            {
                if (requirement == null || !requirement.IsComplete(lab))
                    return false;
            }

            foreach (DeviceGroupRequirement requirement in configuration.requiredDeviceGroups)
            {
                if (requirement == null || requirement.deviceGroup == null ||
                    !requirement.deviceGroup.IsComplete)
                    return false;
            }

            if (configuration.anyDeviceGroups.Count > 0 &&
                !configuration.anyDeviceGroups.Exists(requirement =>
                    requirement != null && requirement.deviceGroup != null &&
                    requirement.deviceGroup.IsComplete))
            {
                return false;
            }

            foreach (ControlRequirement requirement in
                configuration.requiredControls)
            {
                if (requirement == null ||
                    requirement.control == null ||
                    requirement.control.Value != requirement.requiredValue)
                {
                    return false;
                }
            }

            foreach (SliderRequirement requirement in
                configuration.requiredSliders)
            {
                if (requirement == null ||
                    requirement.slider == null ||
                    requirement.slider.Value < requirement.minimumValue ||
                    requirement.slider.Value > requirement.maximumValue)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesWiringOnly(
            FreeLabManager lab,
            ActivationConfiguration configuration)
        {
            foreach (ConnectionRequirement requirement in configuration.requiredConnections)
            {
                if (requirement == null || requirement.endpointA == null ||
                    requirement.endpointB == null ||
                    !lab.HasConnection(requirement.endpointA, requirement.endpointB))
                    return false;
            }

            foreach (FreeLabConnectionGroup group in configuration.requiredConnectionGroups)
            {
                if (group == null || !group.IsComplete(lab))
                    return false;
            }

            if (configuration.anyConnectionGroups.Count > 0 &&
                !configuration.anyConnectionGroups.Exists(group =>
                    group != null && group.IsComplete(lab)))
                return false;

            foreach (FreeLabConnectionGroupAlternativeRequirement requirement in
                configuration.requiredAlternativeConnectionGroups)
            {
                if (requirement == null || !requirement.IsComplete(lab))
                    return false;
            }

            foreach (DeviceGroupRequirement requirement in configuration.requiredDeviceGroups)
            {
                if (requirement == null || requirement.deviceGroup == null ||
                    !requirement.deviceGroup.IsComplete)
                    return false;
            }

            return configuration.anyDeviceGroups.Count == 0 ||
                configuration.anyDeviceGroups.Exists(requirement =>
                    requirement != null && requirement.deviceGroup != null &&
                    requirement.deviceGroup.IsComplete);
        }
    }
}
