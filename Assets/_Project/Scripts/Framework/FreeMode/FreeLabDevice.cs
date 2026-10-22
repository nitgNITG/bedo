using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    /// <summary>
    /// A physical device in the shared Free Lab. It becomes active only when
    /// its authored wires are connected and its chosen power condition is met.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabDevice : MonoBehaviour
    {
        [Header("Required Connections")]
        [Tooltip("Every authored wire in this list must be connected. Assign the wire objects themselves, not endpoint pairs.")]
        [SerializeField] private List<FreeLabWireDefinition> requiredWires =
            new List<FreeLabWireDefinition>();

        [Tooltip("Every assigned cable group must be complete. Use this when a device has a named multi-wire setup.")]
        [SerializeField] private List<FreeLabConnectionGroup> requiredConnectionGroups =
            new List<FreeLabConnectionGroup>();

        [Tooltip("Optional cable alternatives: at least one complete group is required. Use this only when this one device has more than one valid wiring method.")]
        [SerializeField] private List<FreeLabConnectionGroup> anyConnectionGroups =
            new List<FreeLabConnectionGroup>();

        [Header("Power")]
        [Tooltip("When on, this device requires the shared Free Lab system to be operating.")]
        [SerializeField] private bool requiresSystemOperating = true;

        [Tooltip("Optional additional power controls. Every assigned control must be on. Leave empty when the shared operating state is enough.")]
        [SerializeField] private List<FreeLabControl> requiredPowerControls =
            new List<FreeLabControl>();

        [Header("Visuals")]
        [Tooltip("Optional objects activated while this device is running.")]
        [SerializeField] private List<GameObject> objectsToTurnOn =
            new List<GameObject>();

        [Tooltip("Optional objects deactivated while this device is running.")]
        [SerializeField] private List<GameObject> objectsToTurnOff =
            new List<GameObject>();

        [Header("Events")]
        [SerializeField] private UnityEvent onDeviceStarted;
        [SerializeField] private UnityEvent onDeviceStopped;

        [Header("Runtime (Play Mode)")]
        [SerializeField] private bool isRunning;

        // A source-limited system may temporarily extinguish a lamp while
        // preserving its valid wiring and electrical load state.
        private bool visualPowerAvailable = true;

        // Set by FreeLabDeviceGroup. A device not assigned to a group remains
        // allowed to run; a device in an alternative group may run only when
        // it is the group's selected, connected option.
        private bool selectedByDeviceGroup = true;

        public bool IsRunning => isRunning;
        public bool IsVisuallyRunning => isRunning && visualPowerAvailable;
        /// <summary>
        /// Whether the shared Free Mode source currently permits this
        /// device's visual to illuminate. This is intentionally independent
        /// of alternative-device selection so an exact AC lamp path can make
        /// its own visual decision.
        /// </summary>
        public bool IsVisualPowerAvailable => visualPowerAvailable;
        public IReadOnlyList<GameObject> ObjectsToTurnOn => objectsToTurnOn;
        public IReadOnlyList<GameObject> ObjectsToTurnOff => objectsToTurnOff;

        /// <summary>True when this device's authored cable requirements are complete, before considering power.</summary>
        public bool IsConnected => HasRequiredConnections(FreeLabManager.Instance);

        /// <summary>
        /// Runtime coordination hook for a FreeLabDeviceGroup. This is not
        /// serialized so an authored device is never permanently disabled by
        /// a Play Mode selection.
        /// </summary>
        public void SetSelectedByDeviceGroup(bool selected)
        {
            selectedByDeviceGroup = selected;
        }

        /// <summary>
        /// Temporarily controls only this device's visual state. The authored
        /// wiring, actual running state, and events stay intact so a load does
        /// not disappear from the electrical calculation merely because its
        /// source cannot illuminate it.
        /// </summary>
        public void SetVisualPowerAvailable(bool available)
        {
            if (visualPowerAvailable == available)
                return;

            visualPowerAvailable = available;
            ApplyVisualState(IsVisuallyRunning);
        }

        /// <summary>Editor/setup helper. Replaces this device's authored wire list.</summary>
        public void ConfigureRequiredWires(IEnumerable<FreeLabWireDefinition> wires)
        {
            requiredWires = wires != null
                ? new List<FreeLabWireDefinition>(wires)
                : new List<FreeLabWireDefinition>();
        }

        /// <summary>Editor/setup helper. Replaces this device's optional valid wiring paths.</summary>
        public void ConfigureAlternativeConnectionGroups(
            IEnumerable<FreeLabConnectionGroup> groups)
        {
            anyConnectionGroups = groups != null
                ? new List<FreeLabConnectionGroup>(groups)
                : new List<FreeLabConnectionGroup>();
        }

        private void OnEnable()
        {
            Evaluate(FreeLabManager.Instance);
        }

        public void Evaluate(FreeLabManager lab)
        {
            if (lab == null)
                return;

            bool shouldRun = selectedByDeviceGroup &&
                HasRequiredConnections(lab) && HasRequiredPower(lab);
            if (isRunning == shouldRun)
            {
                ApplyVisualState(shouldRun && visualPowerAvailable);
                return;
            }

            SetRunning(shouldRun);
        }

        private bool HasRequiredConnections(FreeLabManager lab)
        {
            bool hasAnyRequirement = requiredWires.Count > 0 ||
                requiredConnectionGroups.Count > 0 || anyConnectionGroups.Count > 0;
            if (!hasAnyRequirement)
                return false;

            foreach (FreeLabWireDefinition wire in requiredWires)
            {
                if (wire == null || wire.endpointA == null || wire.endpointB == null ||
                    !lab.HasConnection(wire.endpointA, wire.endpointB))
                {
                    return false;
                }
            }

            foreach (FreeLabConnectionGroup group in requiredConnectionGroups)
            {
                if (group == null || !group.IsComplete(lab))
                    return false;
            }

            if (anyConnectionGroups.Count > 0)
            {
                bool anyComplete = false;
                foreach (FreeLabConnectionGroup group in anyConnectionGroups)
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

            return true;
        }

        private bool HasRequiredPower(FreeLabManager lab)
        {
            if (requiresSystemOperating && !lab.IsSystemOperating())
                return false;

            foreach (FreeLabControl control in requiredPowerControls)
            {
                if (control == null || !control.Value)
                    return false;
            }

            return true;
        }

        private void SetRunning(bool running)
        {
            if (isRunning == running)
                return;

            isRunning = running;
            ApplyVisualState(running && visualPowerAvailable);

            if (running)
                onDeviceStarted?.Invoke();
            else
                onDeviceStopped?.Invoke();
        }

        private void ApplyVisualState(bool running)
        {
            SetObjectsActive(objectsToTurnOn, running);
            SetObjectsActive(objectsToTurnOff, !running);
        }

        private static void SetObjectsActive(
            List<GameObject> objects,
            bool active)
        {
            if (objects == null)
                return;

            foreach (GameObject target in objects)
            {
                if (target != null)
                    target.SetActive(active);
            }
        }
    }
}
