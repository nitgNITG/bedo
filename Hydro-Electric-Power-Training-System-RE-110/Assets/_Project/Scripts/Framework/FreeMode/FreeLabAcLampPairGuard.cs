using System;
using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Free Mode-only last check for AC lamp visuals. A lamp may light only
    /// when its own inlet pair and its own L/N pair are all connected. This
    /// prevents a valid LED L/N pair from accidentally lighting a halogen
    /// lamp (and vice versa), including when older generic AC-load events run.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FreeLabAcLampPairGuard : MonoBehaviour
    {
        [Serializable]
        public sealed class LampPath
        {
            [Tooltip("The Free Lab device representing this exact physical lamp.")]
            public FreeLabDevice device;
            [Tooltip("The lamp's illuminated visual.")]
            public GameObject onVisual;
            [Tooltip("The lamp's unlit visual.")]
            public GameObject offVisual;
            [Tooltip("All four wires are required: the lamp's red/black inlet and its matching L/N pair.")]
            public List<FreeLabWireDefinition> requiredWires =
                new List<FreeLabWireDefinition>();
        }

        [SerializeField] private List<LampPath> lampPaths = new List<LampPath>();

        /// <summary>Editor setup helper for the Free Lab scene.</summary>
        public void Configure(IEnumerable<LampPath> paths)
        {
            lampPaths = paths != null
                ? new List<LampPath>(paths)
                : new List<LampPath>();
        }

        /// <summary>
        /// Returns true only when the user has started an Exp. 9 AC-lamp
        /// setup but has mixed lamp families or lamp numbers. It deliberately
        /// does not block unrelated experiments that have no AC lamp cables.
        /// </summary>
        public bool BlocksPowerOn(FreeLabManager lab)
        {
            if (lab == null || !HasAnyLampWireConnected(lab))
                return false;

            foreach (LampPath path in lampPaths)
            {
                if (path != null && HasAllRequiredWires(lab, path.requiredWires))
                    return false;
            }

            return true;
        }

        private void LateUpdate()
        {
            // Guided Mode retains its existing visual control completely.
            if (InteractionManager.Instance == null || !InteractionManager.Instance.Free)
                return;

            FreeLabManager lab = FreeLabManager.Instance;
            if (lab == null)
                return;

            ApplyFreeModeLampSolarRule();

            bool operating = lab.IsSystemOperating();
            foreach (LampPath path in lampPaths)
            {
                if (path == null)
                    continue;

                // The device is the source of truth for its own authored
                // visuals. This also repairs old scenes where identically
                // named light objects caused a serialized name lookup to pick
                // a different lamp's visual.
                if (path.device != null)
                {
                    if (path.device.ObjectsToTurnOn.Count > 0)
                        path.onVisual = path.device.ObjectsToTurnOn[0];
                    if (path.device.ObjectsToTurnOff.Count > 0)
                        path.offVisual = path.device.ObjectsToTurnOff[0];
                }

                // The four authored wires identify this exact physical lamp.
                // Do not additionally require the shared alternative-device
                // group to select it: that group may select Lamp 1 first,
                // even when this valid Lamp 2 path is the one the user has
                // completed. Source availability is still honoured.
                bool complete = operating && HasAllRequiredWires(lab, path.requiredWires) &&
                    (path.device == null || path.device.IsVisualPowerAvailable);
                SetActive(path.onVisual, complete);
                SetActive(path.offVisual, !complete);
            }
        }

        private void ApplyFreeModeLampSolarRule()
        {
            FreeLabManager lab = GetComponentInParent<FreeLabManager>();
            FreeLabCalculationStageRouter stageRouter = lab
                ?.GetComponentInChildren<FreeLabCalculationStageRouter>(true);
            FreeLabPVOffGridBinder binder = lab
                ?.GetComponentInChildren<FreeLabPVOffGridBinder>(true);

            // Free Mode lamps for the battery/PWM/MPPT and Off-Grid paths
            // need enough current to meet the active load. The missing
            // amount is load minus every live source; the displayed battery
            // A DC must be at least that amount. This is recalculated by the
            // active binder every frame, including source/load changes.
            // Stage 1 keeps its pre-existing behaviour.
            bool hasBatteryLimitedLampStage = false;
            bool hasActiveBatteryBinder = false;
            float displayedBatteryCurrent = 0f;
            float requiredBatteryCurrent = 0f;
            if (stageRouter != null && stageRouter.ActiveStage == 2)
            {
                hasBatteryLimitedLampStage = true;
                foreach (FreeLabPVBatteryBinder batteryBinder in
                    lab.GetComponentsInChildren<FreeLabPVBatteryBinder>(true))
                {
                    if (batteryBinder != null && batteryBinder.isActiveAndEnabled)
                    {
                        hasActiveBatteryBinder = true;
                        displayedBatteryCurrent =
                            batteryBinder.DisplayedBatteryCurrent;
                        requiredBatteryCurrent =
                            batteryBinder.RequiredBatteryCurrent;
                        break;
                    }
                }
            }
            else if (stageRouter != null && stageRouter.ActiveStage == 3)
            {
                hasBatteryLimitedLampStage = true;
                if (binder != null && binder.isActiveAndEnabled)
                {
                    hasActiveBatteryBinder = true;
                    displayedBatteryCurrent = binder.DisplayedBatteryCurrent;
                    requiredBatteryCurrent = binder.RequiredBatteryCurrent;
                }
            }

            bool lampVisualPowerAvailable = !hasBatteryLimitedLampStage ||
                (hasActiveBatteryBinder && lab != null && lab.IsSystemOperating() &&
                    displayedBatteryCurrent >= requiredBatteryCurrent);

            FreeLabDeviceManager deviceManager = GetComponent<FreeLabDeviceManager>();
            if (deviceManager == null)
                return;

            foreach (FreeLabDeviceGroup group in deviceManager.DeviceGroups)
            {
                // Only lamp modules are affected. Other connected stage-three
                // devices, such as the inverter, remain visually untouched.
                if (group == null || group.GroupId.IndexOf("Lamp",
                        StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                foreach (FreeLabDevice device in group.Devices)
                    if (device != null)
                        device.SetVisualPowerAvailable(lampVisualPowerAvailable);
            }
        }

        private static bool HasAllRequiredWires(
            FreeLabManager lab, List<FreeLabWireDefinition> wires)
        {
            if (wires == null || wires.Count == 0)
                return false;

            foreach (FreeLabWireDefinition wire in wires)
            {
                if (wire == null || wire.endpointA == null || wire.endpointB == null ||
                    !lab.HasConnection(wire.endpointA, wire.endpointB))
                    return false;
            }

            return true;
        }

        private bool HasAnyLampWireConnected(FreeLabManager lab)
        {
            foreach (LampPath path in lampPaths)
            {
                if (path?.requiredWires == null)
                    continue;

                foreach (FreeLabWireDefinition wire in path.requiredWires)
                {
                    if (wire != null && wire.endpointA != null && wire.endpointB != null &&
                        lab.HasConnection(wire.endpointA, wire.endpointB))
                        return true;
                }
            }

            return false;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
