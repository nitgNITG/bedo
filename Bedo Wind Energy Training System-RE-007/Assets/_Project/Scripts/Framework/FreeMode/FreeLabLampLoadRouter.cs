using System;
using System.Reflection;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Free Mode-only load selector.  It changes the calculation inputs on a
    /// FreeLabPVOffGridBinder according to the currently connected lamp
    /// alternative, without modifying or attaching to Guided Mode binders.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabLampLoadRouter : MonoBehaviour
    {
        [Header("Free Mode References")]
        [SerializeField] private FreeLabDeviceGroup dcLampAlternatives;
        [SerializeField] private FreeLabDeviceGroup acLampAlternatives;
        [SerializeField] private FreeLabPVOffGridBinder offGridBinder;

        [Header("DC Lamp Load (A)")]
        [SerializeField] private float dcLedLoadCurrent = 0.2f;
        [SerializeField] private float dcHalogenLoadCurrent = 2f;

        [Header("AC Lamp Load (A)")]
        [Tooltip("The AC LED display and calculation are both 0.2 A.")]
        [SerializeField] private float acLedDisplayCurrent = 0.2f;
        [SerializeField] private float acLedCalculationCurrent = 0.2f;
        [Tooltip("The AC halogen meter displays 0.2 A, while its battery calculation uses 2 A.")]
        [SerializeField] private float acHalogenDisplayCurrent = 0.2f;
        [SerializeField] private float acHalogenCalculationCurrent = 2f;

        private static readonly BindingFlags Fields = BindingFlags.Instance |
            BindingFlags.NonPublic;
        private FieldInfo dcLoadOne;
        private FieldInfo dcLoadTwo;
        private FieldInfo dcLoadOneOn;
        private FieldInfo dcLoadTwoOn;
        private FieldInfo acLoadOn;
        private FieldInfo acDisplay;
        private FieldInfo acCalculation;
        private FieldInfo batteryLoadCurrent;
        private FieldInfo batteryLoadOn;
        // A fresh Free Lab has no connected lamp, so start at zero and log
        // only real add/remove changes rather than an initial 0 A message.
        private float lastDcLoad;
        private float lastAcLoad;
        private float lastBatteryLoad;
        private FreeLabDevice lastSelectedDcLamp;
        private bool lastSelectedDcLampRunning;

        /// <summary>Editor setup helper for the Free Lab scene only.</summary>
        public void Configure(FreeLabDeviceGroup dc, FreeLabDeviceGroup ac,
            FreeLabPVOffGridBinder binder)
        {
            dcLampAlternatives = dc;
            acLampAlternatives = ac;
            offGridBinder = binder;
        }

        private void Awake() { CacheFields(); }

        private void Update()
        {
            // This router belongs exclusively to the shared Free Lab.  Its
            // binders are also present in the scene while Guided Mode is
            // active, so never synchronize Free Lab lamp state into them
            // outside Free Mode.
            if (InteractionManager.Instance != null && !InteractionManager.Instance.Free)
                return;

            CacheFields();
            ApplyDcLoad();
            ApplyAcLoad();
            ApplyBatteryStageLoad();
            SyncOffGridLampRunningStates();
            LogLoadsIfChanged();
        }

        private void CacheFields()
        {
            // The battery-charging calculation screen can be active while the
            // off-grid screen is disabled. Cache its inputs independently so
            // Free Mode load routing and diagnostics still work in that case.
            if (batteryLoadCurrent == null)
            {
                batteryLoadCurrent = typeof(PVBatterySystemBinder).GetField(
                    "loadCurrent", Fields);
                batteryLoadOn = typeof(PVBatterySystemBinder).GetField(
                    "loadOn", Fields);
            }

            if (offGridBinder == null || dcLoadOne != null)
                return;

            Type baseType = typeof(PVOffGridSystemBinder);
            dcLoadOne = baseType.GetField("dcLoad1Current", Fields);
            dcLoadTwo = baseType.GetField("dcLoad2Current", Fields);
            dcLoadOneOn = baseType.GetField("dcLoad1On", Fields);
            dcLoadTwoOn = baseType.GetField("dcLoad2On", Fields);
            acLoadOn = baseType.GetField("acLoadOn", Fields);
            acDisplay = baseType.GetField("acLoadCurrentDisplay", Fields);
            acCalculation = baseType.GetField("acLoadCurrentCalculation", Fields);
        }

        private void ApplyDcLoad()
        {
            float current = GetSelectedDcLoadCurrent();
            Set(dcLoadOne, current);
            Set(dcLoadTwo, current);
        }

        private void ApplyBatteryStageLoad()
        {
            if (batteryLoadCurrent == null || batteryLoadOn == null)
                return;

            FreeLabDevice selected = dcLampAlternatives != null
                ? dcLampAlternatives.ConnectedDevice : null;
            float current = GetSelectedDcLoadCurrent();
            bool loadIsRunning = selected != null && selected.IsRunning;

            foreach (FreeLabPVBatteryBinder binder in
                FindObjectsByType<FreeLabPVBatteryBinder>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (binder == null || !binder.isActiveAndEnabled)
                    continue;

                batteryLoadCurrent.SetValue(binder, current);
                batteryLoadOn.SetValue(binder, loadIsRunning);
            }
        }

        private float GetSelectedDcLoadCurrent()
        {
            FreeLabDevice selected = dcLampAlternatives != null
                ? dcLampAlternatives.ConnectedDevice : null;
            if (selected == null)
                return 0f;

            return selected.gameObject.name.IndexOf("Halogen",
                StringComparison.OrdinalIgnoreCase) >= 0
                ? dcHalogenLoadCurrent : dcLedLoadCurrent;
        }

        private void ApplyAcLoad()
        {
            FreeLabDevice selected = acLampAlternatives != null
                ? acLampAlternatives.ConnectedDevice : null;
            float displayCurrent = 0f;
            float calculationCurrent = 0f;
            if (selected != null)
            {
                bool halogen = selected.gameObject.name.IndexOf("Halogen",
                    StringComparison.OrdinalIgnoreCase) >= 0;
                displayCurrent = halogen
                    ? acHalogenDisplayCurrent : acLedDisplayCurrent;
                calculationCurrent = halogen
                    ? acHalogenCalculationCurrent : acLedCalculationCurrent;
            }

            Set(acDisplay, displayCurrent);
            Set(acCalculation, calculationCurrent);
        }

        /// <summary>
        /// The physical Free Lab device is the authoritative source for
        /// whether a lamp is running.  Keep the off-grid calculation binder
        /// aligned with it every frame.  This also neutralizes old serialized
        /// device events that clear a different DC-load flag than the one
        /// they set, which otherwise leaves a phantom load and makes the
        /// battery-negative visual cutoff fire at the wrong time.
        /// </summary>
        private void SyncOffGridLampRunningStates()
        {
            if (offGridBinder == null)
                return;

            FreeLabDevice selectedDc = dcLampAlternatives != null
                ? dcLampAlternatives.ConnectedDevice : null;
            FreeLabDevice selectedAc = acLampAlternatives != null
                ? acLampAlternatives.ConnectedDevice : null;

            bool dcRunning = selectedDc != null && selectedDc.IsRunning;
            bool acRunning = selectedAc != null && selectedAc.IsRunning;

            // The two DC fields are alternative displays for one physical
            // load, so only one flag may represent the active Free Lab lamp.
            Set(dcLoadOneOn, dcRunning);
            Set(dcLoadTwoOn, false);
            Set(acLoadOn, acRunning);
        }

        private void Set(FieldInfo field, float value)
        {
            if (field != null && offGridBinder != null)
                field.SetValue(offGridBinder, value);
        }

        private void Set(FieldInfo field, bool value)
        {
            if (field != null && offGridBinder != null)
                field.SetValue(offGridBinder, value);
        }

        private void LogLoadsIfChanged()
        {
            float dc = dcLoadOne != null && offGridBinder != null
                ? (float)dcLoadOne.GetValue(offGridBinder) : 0f;
            float ac = acCalculation != null && offGridBinder != null
                ? (float)acCalculation.GetValue(offGridBinder) : 0f;

            // Exp. 7's Free Mode screen uses FreeLabPVBatteryBinder rather
            // than the off-grid binder. Read its actual calculation input as
            // well, so the console does not report a misleading zero there.
            float batteryStageDc = GetActiveBatteryStageLoad();
            FreeLabDevice selectedDcLamp = dcLampAlternatives != null
                ? dcLampAlternatives.ConnectedDevice : null;
            bool selectedDcLampRunning = selectedDcLamp != null &&
                selectedDcLamp.IsRunning;

            if (Mathf.Approximately(lastDcLoad, dc) &&
                Mathf.Approximately(lastAcLoad, ac) &&
                Mathf.Approximately(lastBatteryLoad, batteryStageDc) &&
                lastSelectedDcLamp == selectedDcLamp &&
                lastSelectedDcLampRunning == selectedDcLampRunning)
                return;

            lastDcLoad = dc;
            lastAcLoad = ac;
            lastBatteryLoad = batteryStageDc;
            lastSelectedDcLamp = selectedDcLamp;
            lastSelectedDcLampRunning = selectedDcLampRunning;
            string lampState = selectedDcLamp == null ? "no DC lamp selected" :
                selectedDcLamp.gameObject.name +
                (selectedDcLampRunning ? " (running)" : " (connected, power off)");
            Debug.Log("FREE MODE calculation lamp load — " + lampState +
                ". Battery stage DC: " +
                batteryStageDc.ToString("0.##") + " A, Off-grid DC: " +
                dc.ToString("0.##") + " A, Off-grid AC: " +
                ac.ToString("0.##") + " A", this);
        }

        private float GetActiveBatteryStageLoad()
        {
            if (batteryLoadCurrent == null || batteryLoadOn == null)
                return 0f;

            FreeLabPVBatteryBinder[] binders =
                FindObjectsByType<FreeLabPVBatteryBinder>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (FreeLabPVBatteryBinder binder in binders)
            {
                if (binder != null && binder.isActiveAndEnabled &&
                    (bool)batteryLoadOn.GetValue(binder))
                {
                    return (float)batteryLoadCurrent.GetValue(binder);
                }
            }

            return 0f;
        }
    }
}
