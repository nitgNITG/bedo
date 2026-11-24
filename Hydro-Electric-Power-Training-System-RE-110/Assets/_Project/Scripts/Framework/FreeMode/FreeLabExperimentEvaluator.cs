using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    /// <summary>
    /// One passive Phase 2 scenario evaluator.  It never blocks an action or
    /// displays a warning; it only reports which configuration state matches.
    /// </summary>
    public class FreeLabExperimentEvaluator : MonoBehaviour
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
            public FreeLabDeviceGroup deviceGroup;
        }

        [Serializable]
        public class ConfigurationState
        {
            public string stateName = "State";
            public List<ConnectionRequirement> requiredConnections =
                new List<ConnectionRequirement>();
            [Tooltip("Every assigned cable group must be complete.")]
            public List<FreeLabConnectionGroup> requiredConnectionGroups =
                new List<FreeLabConnectionGroup>();
            [Tooltip("Optional alternatives: at least one complete group is required.")]
            public List<FreeLabConnectionGroup> anyConnectionGroups =
                new List<FreeLabConnectionGroup>();
            [Tooltip("Every row is required, but each row accepts any one of its cable-group alternatives. Use this for independent lamp/device substitutions.")]
            public List<FreeLabConnectionGroupAlternativeRequirement> requiredAlternativeConnectionGroups =
                new List<FreeLabConnectionGroupAlternativeRequirement>();
            [Tooltip("Every assigned device group must have one connected device.")]
            public List<DeviceGroupRequirement> requiredDeviceGroups =
                new List<DeviceGroupRequirement>();
            [Tooltip("Optional device alternatives: at least one group must have a connected device.")]
            public List<DeviceGroupRequirement> anyDeviceGroups =
                new List<DeviceGroupRequirement>();
            public List<ControlRequirement> requiredControls =
                new List<ControlRequirement>();
            public List<SliderRequirement> requiredSliders =
                new List<SliderRequirement>();

            [Tooltip("Invoked only when this configuration becomes the currently matched state.")]
            public UnityEvent onEntered;
        }

        [SerializeField]
        private string experimentId = "Experiment";

        [Tooltip("Ordered from the least complete state to the most complete state. The last matching state is reported.")]
        [SerializeField]
        private List<ConfigurationState> states =
            new List<ConfigurationState>();

        [SerializeField]
        private UnityEvent<string> onStateChanged;

        [Header("Runtime State (Play Mode)")]
        [SerializeField]
        private int currentStateIndex = -1;

        [SerializeField]
        private string currentStateName = "No matching state";

        public string ExperimentId => experimentId;
        public int CurrentStateIndex => currentStateIndex;
        public string CurrentStateName => currentStateName;

        public void Evaluate(FreeLabManager lab)
        {
            int bestMatch = -1;

            for (int i = 0; i < states.Count; i++)
            {
                if (Matches(lab, states[i]))
                    bestMatch = i;
            }

            string nextName = bestMatch >= 0
                ? states[bestMatch].stateName
                : "No matching state";

            if (bestMatch == currentStateIndex &&
                nextName == currentStateName)
            {
                return;
            }

            currentStateIndex = bestMatch;
            currentStateName = nextName;
            Debug.Log(
                "FREE MODE " + ExperimentId + " → " +
                currentStateName,
                this
            );
            if (bestMatch >= 0)
                states[bestMatch].onEntered?.Invoke();
            if (bestMatch >= 0 &&
                states[bestMatch].stateName == "B - Modules Connected")
            {
                string trackingId = HydroStudentTracking.FreeExperimentId(ExperimentId);
                if (HydroStudentTracking.IsActiveExperiment(trackingId))
                {
                    HydroStudentTracking.CompleteStep(1);
                    HydroStudentTracking.CompleteExperiment();
                }
            }
            onStateChanged?.Invoke(currentStateName);
        }

        private static bool Matches(
            FreeLabManager lab,
            ConfigurationState state)
        {
            foreach (ConnectionRequirement requirement in state.requiredConnections)
            {
                if (requirement.endpointA == null ||
                    requirement.endpointB == null ||
                    !lab.HasConnection(
                        requirement.endpointA,
                        requirement.endpointB))
                {
                    return false;
                }
            }

            foreach (FreeLabConnectionGroup group in
                state.requiredConnectionGroups)
            {
                if (group == null || !group.IsComplete(lab))
                    return false;
            }

            if (state.anyConnectionGroups.Count > 0)
            {
                bool anyComplete = false;
                foreach (FreeLabConnectionGroup group in
                    state.anyConnectionGroups)
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
                state.requiredAlternativeConnectionGroups)
            {
                if (requirement == null || !requirement.IsComplete(lab))
                    return false;
            }

            foreach (DeviceGroupRequirement requirement in state.requiredDeviceGroups)
            {
                if (requirement == null || requirement.deviceGroup == null ||
                    !requirement.deviceGroup.IsComplete)
                    return false;
            }

            if (state.anyDeviceGroups.Count > 0 &&
                !state.anyDeviceGroups.Exists(requirement =>
                    requirement != null && requirement.deviceGroup != null &&
                    requirement.deviceGroup.IsComplete))
            {
                return false;
            }

            foreach (ControlRequirement requirement in state.requiredControls)
            {
                if (requirement.control == null ||
                    requirement.control.Value != requirement.requiredValue)
                {
                    return false;
                }
            }

            foreach (SliderRequirement requirement in state.requiredSliders)
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
    }
}
