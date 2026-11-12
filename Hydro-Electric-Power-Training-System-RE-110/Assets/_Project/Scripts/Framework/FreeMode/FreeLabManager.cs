using System.Linq;
using TutorialFramework.Simulation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace TutorialFramework
{
    /// <summary>
    /// The shared physical laboratory for Free Mode.  This owns wiring and
    /// physical restrictions.  Experiment evaluators observe it independently.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabManager : MonoBehaviour
    {
        public static FreeLabManager Instance { get; private set; }

        [Header("Lab Objects")]
        [SerializeField]
        private List<FreeLabEndpoint> endpoints =
            new List<FreeLabEndpoint>();

        [SerializeField]
        private List<FreeLabControl> controls =
            new List<FreeLabControl>();

        [SerializeField]
        private List<FreeLabExperimentEvaluator> evaluators =
            new List<FreeLabExperimentEvaluator>();

        [SerializeField]
        private List<FreeLabSliderControl> sliders =
            new List<FreeLabSliderControl>();

        [SerializeField]
        private List<FreeLabThreeStateToggle> threeStateToggles =
            new List<FreeLabThreeStateToggle>();

        [SerializeField]
        private List<FreeLabCalculationStageRouter> calculationStageRouters =
            new List<FreeLabCalculationStageRouter>();

        [SerializeField]
        private List<FreeLabDevice> devices =
            new List<FreeLabDevice>();

        [SerializeField]
        private List<FreeLabDeviceManager> deviceManagers =
            new List<FreeLabDeviceManager>();

        [Header("Authored Wire Definitions")]
        [Tooltip("Assign authored FreeLabWireDefinition components here. Each one points to an existing wire parent/spline and its existing plugs.")]
        [SerializeField]
        private List<FreeLabWireDefinition> wireDefinitions =
            new List<FreeLabWireDefinition>();

        [SerializeField]
        private Transform wireContainer;

        [Header("Client Physical Restrictions")]
        [SerializeField]
        private bool preventWiringWhileOperating = true;

        [SerializeField]
        private string wiringWhileOperatingWarning =
            "Turn off the power first.";

        [SerializeField]
        private string powerBeforeConnectionWarning =
            "Connect the modules first.";

        [Header("Feedback")]
        [SerializeField]
        private UnityEvent<string> onWarning;

        [Header("Free Mode Warning Panel")]
        [Tooltip("Optional panel shown only for Free Mode warnings. It stays visible until CloseFreeModeWarning is called by its OK button.")]
        [SerializeField] private GameObject freeModeWarningPanel;
        [SerializeField] private TMP_Text freeModeWarningText;
        [SerializeField] private UnityEngine.UI.Text legacyFreeModeWarningText;

        private readonly List<FreeLabWire> legacyConnections =
            new List<FreeLabWire>();

        private readonly HashSet<FreeLabEndpoint> possibleConnectionHighlights =
            new HashSet<FreeLabEndpoint>();

        private FreeLabEndpoint selectedEndpoint;
        private FreeLabOperationalStateMachine operationalStateMachine;
        private bool initialized;
        private Bedo.Hydro.HydroFreeLabCircuits hydroCircuits;

        [SerializeField] private WindConnectionState connectionState;
        private IList<FreeLabWire> connections => connectionState ? connectionState : (IList<FreeLabWire>)legacyConnections;
        public int ConnectionCount => connections.Count;
        public IReadOnlyList<FreeLabWire> Connections => connectionState ? connectionState : (IReadOnlyList<FreeLabWire>)legacyConnections;

        public static FreeLabManager GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            foreach (FreeLabSceneRoot sceneRoot in
                Resources.FindObjectsOfTypeAll<FreeLabSceneRoot>())
            {
                if (sceneRoot.gameObject.scene.IsValid())
                    sceneRoot.ActivateForFreeMode();
            }

            FreeLabManager manager =
                FindFirstObjectByType<FreeLabManager>();

            if (manager != null)
                return manager;

            GameObject managerObject =
                new GameObject("Free Lab Manager");

            return managerObject.AddComponent<FreeLabManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (InteractionManager.Instance != null &&
                InteractionManager.Instance.Free)
            {
                InitializeFreeLab();
            }
        }

        public bool HandleInteraction(
            Interaction interaction,
            out bool invokeExistingClickEvent)
        {
            invokeExistingClickEvent = true;

            if (!IsFreeMode())
                return true;

            InitializeFreeLab();

            // Some physical controls use separate child objects for their
            // on/off visual states. Treat clicks on either child as a click
            // on the one parent FreeLabControl before considering endpoints.
            FreeLabControl control = interaction.GetComponent<FreeLabControl>();
            if (control == null)
                control = interaction.GetComponentInParent<FreeLabControl>();

            if (control != null)
            {
                invokeExistingClickEvent = false;
                return HandleControl(control);
            }

            FreeLabEndpoint endpoint =
                interaction.GetComponent<FreeLabEndpoint>();

            if (endpoint != null)
            {
                invokeExistingClickEvent = false;
                return HandleEndpoint(endpoint);
            }

            // An unregistered action is legitimate sandbox interaction. It is
            // neutral: execute its existing click event without evaluator noise.
            return true;
        }

        /// <summary>
        /// Assign this no-argument method to the warning panel's OK button.
        /// It hides the panel and returns the displayed operational state to
        /// the current physical Free Lab configuration.
        /// </summary>
        public void CloseFreeModeWarning()
        {
            if (freeModeWarningPanel != null)
                freeModeWarningPanel.SetActive(false);

            operationalStateMachine?.Evaluate(this);
        }

        public void InitializeFreeLab()
        {
            if (initialized)
                return;

            initialized = true;
            hydroCircuits = GetComponent<Bedo.Hydro.HydroFreeLabCircuits>();
            DiscoverSceneObjects();
            operationalStateMachine = GetComponent<
                FreeLabOperationalStateMachine>();
            if (operationalStateMachine == null)
                operationalStateMachine = gameObject.AddComponent<
                    FreeLabOperationalStateMachine>();

            foreach (FreeLabEndpoint endpoint in endpoints)
            {
                if (endpoint != null && endpoint.Interaction != null)
                    endpoint.Interaction.Enable();
            }

            foreach (FreeLabControl control in controls)
            {
                Interaction interaction = control != null
                    ? control.GetComponent<Interaction>()
                    : null;

                if (interaction != null)
                    interaction.Enable();
            }

            EvaluateAll();
        }

        public bool HasConnection(
            FreeLabEndpoint first,
            FreeLabEndpoint second)
        {
            foreach (FreeLabWire connection in connections)
            {
                if (connection == null)
                    continue;

                if ((connection.EndpointA == first &&
                     connection.EndpointB == second) ||
                    (connection.EndpointA == second &&
                     connection.EndpointB == first))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyConnection(FreeLabEndpoint endpoint)
        {
            if (endpoint == null)
                return false;

            foreach (FreeLabWire connection in connections)
            {
                if (connection != null && (connection.EndpointA == endpoint ||
                    connection.EndpointB == endpoint))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TrySetSliderValue(
            FreeLabSliderControl slider,
            float newValue)
        {
            if (slider == null)
                return false;

            InitializeFreeLab();
            slider.RefreshAvailability(this);

            if (!slider.IsAvailable &&
                slider.PreventAdjustmentWhileOperating &&
                IsSystemOperating())
            {
                slider.RestoreVisualValue();
                return false;
            }

            if (!slider.IsAvailable)
            {
                slider.RestoreVisualValue();
                return false;
            }

            slider.ApplyValue(newValue);
            return true;
        }

        public bool RemoveConnection(FreeLabWire connection)
        {
            if (!IsFreeMode() || connection == null)
                return false;

            if (preventWiringWhileOperating && IsSystemOperating() &&
                !CanModifyWhileOperating(
                    connection.EndpointA,
                    connection.EndpointB))
            {
                Warn(wiringWhileOperatingWarning);
                return false;
            }

            connections.Remove(connection);
            connection.gameObject.SetActive(false);
            RefreshEndpointConnectionState(connection.EndpointA);
            RefreshEndpointConnectionState(connection.EndpointB);
            EvaluateAll();
            return true;
        }

        private bool HandleEndpoint(FreeLabEndpoint endpoint)
        {
            if (selectedEndpoint == null)
            {
                selectedEndpoint = endpoint;
                selectedEndpoint.SetSelectionPlugVisible(true);
                ShowPossibleConnectionHighlights(selectedEndpoint);
                return true;
            }

            if (selectedEndpoint == endpoint)
            {
                selectedEndpoint.SetSelectionPlugVisible(false);
                ClearPossibleConnectionHighlights();
                selectedEndpoint = null;
                return true;
            }

            if (preventWiringWhileOperating && IsSystemOperating() &&
                !CanModifyWhileOperating(selectedEndpoint, endpoint))
            {
                selectedEndpoint.SetSelectionPlugVisible(false);
                endpoint.SetSelectionPlugVisible(false);
                ClearPossibleConnectionHighlights();
                selectedEndpoint = null;
                Warn(wiringWhileOperatingWarning);
                return false;
            }

            bool created = !HasConnection(selectedEndpoint, endpoint) &&
                CreateConnection(selectedEndpoint, endpoint);

            if (created)
            {
                // A completed wire owns both plugs until that exact wire is
                // removed. Clear the click-preview now; the allocated plugs
                // stay visible through their wire ownership. Leaving preview
                // enabled here would incorrectly show a second plug after
                // only the first connection.
                selectedEndpoint.SetSelectionPlugVisible(false);
                endpoint.SetSelectionPlugVisible(false);
            }
            else
            {
                selectedEndpoint.SetSelectionPlugVisible(false);
                endpoint.SetSelectionPlugVisible(false);
            }

            selectedEndpoint = null;
            ClearPossibleConnectionHighlights();
            EvaluateAll();
            return true;
        }

        private void ShowPossibleConnectionHighlights(FreeLabEndpoint source)
        {
            ClearPossibleConnectionHighlights();
            if (source == null)
                return;

            foreach (FreeLabWireDefinition definition in wireDefinitions)
            {
                if (definition == null || definition.wireParentTemplate == null)
                    continue;

                FreeLabEndpoint target = null;
                if (definition.endpointA == source)
                    target = definition.endpointB;
                else if (definition.endpointB == source)
                    target = definition.endpointA;

                if (target == null || target == source ||
                    HasConnection(source, target) ||
                    (hydroCircuits && !hydroCircuits.CanAdd(this, definition)))
                {
                    continue;
                }

                target.SetPossibleConnectionIndicatorVisible(true);
                possibleConnectionHighlights.Add(target);
            }
        }

        private void ClearPossibleConnectionHighlights()
        {
            foreach (FreeLabEndpoint endpoint in possibleConnectionHighlights)
            {
                if (endpoint != null)
                    endpoint.SetPossibleConnectionIndicatorVisible(false);
            }

            possibleConnectionHighlights.Clear();
        }

        private bool CanModifyWhileOperating(
            FreeLabEndpoint first,
            FreeLabEndpoint second)
        {
            FreeLabWireDefinition definition = FindDefinition(first, second);
            return definition != null && (hydroCircuits
                ? hydroCircuits.CanRemovePowered(definition)
                : definition.allowWhileOperating);
        }

        private bool HandleControl(FreeLabControl control)
        {
            bool hydroPower = hydroCircuits && hydroCircuits.AppliesTo(control);
            if (!control.Value && (hydroPower ? IsControlSafetyLocked(control) : !control.CanEnable(this)))
            {
                // "Connect the modules first" is specifically a wiring
                // message. A missing control, slider, or safety interlock
                // silently keeps the control off instead of mislabelling the
                // problem as incomplete wiring.
                if (control.HasMissingWiringRequirements(this))
                    Warn(powerBeforeConnectionWarning);
                return false;
            }

            if (!control.Value && hydroCircuits && hydroCircuits.AppliesTo(control) &&
                !hydroCircuits.TrySelectAtPowerOn(this))
            {
                Warn(powerBeforeConnectionWarning);
                return false;
            }

            control.Toggle();
            EvaluateAll();
            return true;
        }

        public bool IsSystemOperating()
        {
            foreach (FreeLabControl control in controls)
            {
                if (control != null &&
                    control.MarksSystemOperating &&
                    control.Value)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the state of Free Mode's physical solar-panel power control.
        /// This is deliberately separate from IsSystemOperating(): the main
        /// board can remain on while the panel itself is switched off.
        /// </summary>
        public bool IsSolarPanelSourceOn()
        {
            foreach (FreeLabControl control in controls)
            {
                if (control == null)
                    continue;

                string id = control.ControlId;
                if (!string.IsNullOrEmpty(id) &&
                    id.IndexOf("Solar Panel", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    id.IndexOf("On/Off", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return control.Value;
                }
            }

            return false;
        }

        /// <summary>
        /// A power control is unavailable while any assigned safety control
        /// (such as an emergency button or circuit breaker) remains off.
        /// </summary>
        public bool IsControlSafetyLocked(FreeLabControl powerControl)
        {
            if (powerControl == null)
                return false;

            foreach (FreeLabControl control in controls)
            {
                if (control != null && control.IsSafetyInterlock &&
                    !control.Value && control.LocksPowerControl(powerControl))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasMatchingExperimentConfiguration
        {
            get
            {
                foreach (FreeLabExperimentEvaluator evaluator in evaluators)
                {
                    // Index 0 is the evaluator's authored A / initial state.
                    // A complete module configuration begins at B (index 1).
                    if (evaluator != null && evaluator.CurrentStateIndex > 0)
                        return true;
                }

                return false;
            }
        }

        public FreeLabControl FindControlContaining(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            foreach (FreeLabControl control in controls)
            {
                if (control != null && control.ControlId.IndexOf(
                    text, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return control;
                }
            }

            return null;
        }

        private bool CreateConnection(
            FreeLabEndpoint first,
            FreeLabEndpoint second)
        {
            FreeLabWireDefinition definition =
                FindDefinition(first, second);

            if (definition == null ||
                definition.wireParentTemplate == null ||
                (hydroCircuits && !hydroCircuits.CanAdd(this, definition)))
            {
                // An un-authored endpoint pair is not one of the documented
                // warning cases. Handle it through the existing cancellation
                // path without displaying feedback.
                return false;
            }

            // Each authored connection is a unique physical wire in this
            // lab. Use that exact object directly instead of making a
            // runtime copy, so authored Inspector references stay stable.
            GameObject wireObject = definition.wireParentTemplate;

            // The copied template may still carry the old Guided component.
            // It must not receive clicks in the shared Free Mode lab.
            foreach (WireInteraction guidedWire in
                wireObject.GetComponentsInChildren<WireInteraction>(true))
            {
                guidedWire.enabled = false;
            }

            // A template can contain the old mouse bridge used by Guided
            // Mode. The shared Free Lab owns removal clicks instead.
            foreach (OnMouseDownEvent oldMouseBridge in
                wireObject.GetComponentsInChildren<OnMouseDownEvent>(true))
            {
                oldMouseBridge.enabled = false;
            }

            FreeLabWire wire =
                wireObject.GetComponent<FreeLabWire>();

            if (wire == null)
                wire = wireObject.AddComponent<FreeLabWire>();

            // The authored wire's spline often owns the collider rather than
            // the logical wire parent. Forward clicks from every child
            // collider to this exact connection for removal.
            foreach (Collider collider in
                wireObject.GetComponentsInChildren<Collider>(true))
            {
                if (collider.GetComponent<FreeLabWireClickForwarder>() == null)
                {
                    collider.gameObject.AddComponent<
                        FreeLabWireClickForwarder
                    >();
                }
            }

            if (!first.TryAcquireConnectionPlug(out GameObject firstPlug))
                return false;

            if (!second.TryAcquireConnectionPlug(out GameObject secondPlug))
            {
                first.ReleaseConnectionPlug(firstPlug);
                return false;
            }

            wire.Configure(this, first, second, firstPlug, secondPlug);
            wireObject.SetActive(true);
            connections.Add(wire);
            return true;
        }

        private FreeLabWireDefinition FindDefinition(
            FreeLabEndpoint first,
            FreeLabEndpoint second)
        {
            foreach (FreeLabWireDefinition definition in wireDefinitions)
            {
                if (definition == null ||
                    definition.endpointA == null ||
                    definition.endpointB == null)
                {
                    continue;
                }

                bool forward = SamePhysicalEndpoint(
                    definition.endpointA,
                    first) && SamePhysicalEndpoint(
                    definition.endpointB,
                    second);

                bool reverse = SamePhysicalEndpoint(
                    definition.endpointA,
                    second) && SamePhysicalEndpoint(
                    definition.endpointB,
                    first);

                if (forward || reverse)
                    return definition;
            }

            return null;
        }

        public void RefreshEvaluators()
        {
            EvaluateAll();
        }

        private void EvaluateAll()
        {
            // Resolve the selected rheostat before any calculator reads it.
            // This makes a newly completed (or removed) rheostat cable group
            // update the readings in the same interaction, without waiting
            // for the user to turn its knob.
            FreeLabRheostatDataAdapter.RefreshActiveSource();

            // Resolve alternative physical-device families first. This makes
            // their selection available before individual devices update
            // lights, events, and other visuals.
            foreach (FreeLabDeviceManager deviceManager in deviceManagers)
            {
                if (deviceManager != null)
                    deviceManager.Evaluate(this);
            }

            foreach (FreeLabDevice device in devices)
            {
                if (device != null)
                    device.Evaluate(this);
            }

            // Device families must resolve before scenario evaluators read
            // their required-device-group conditions.
            foreach (FreeLabExperimentEvaluator evaluator in evaluators)
            {
                if (evaluator != null)
                    evaluator.Evaluate(this);
            }

            foreach (FreeLabSliderControl slider in sliders)
            {
                if (slider != null)
                    slider.RefreshAvailability(this);
            }

            foreach (FreeLabThreeStateToggle toggle in threeStateToggles)
            {
                if (toggle != null)
                    toggle.Evaluate(this);
            }

            foreach (FreeLabCalculationStageRouter router in calculationStageRouters)
            {
                if (router != null) router.Evaluate(this);
            }

            operationalStateMachine?.Evaluate(this);
        }

        private void HideEndpointPlugIfUnused(FreeLabEndpoint endpoint)
        {
            if (endpoint == null)
                return;

            foreach (FreeLabWire connection in connections)
            {
                if (connection != null &&
                    (connection.EndpointA == endpoint ||
                     connection.EndpointB == endpoint))
                {
                    return;
                }
            }

            endpoint.SetSelectionPlugVisible(false);
        }

        private void RefreshEndpointConnectionState(FreeLabEndpoint endpoint)
        {
            if (endpoint == null)
                return;

            bool hasConnection = connections.Any(connection =>
                connection != null &&
                (connection.EndpointA == endpoint || connection.EndpointB == endpoint));

            if (!hasConnection)
                endpoint.SetSelectionPlugVisible(false);
        }

        private void DiscoverSceneObjects()
        {
            // Free Mode is a self-contained hierarchy. Never discover
            // objects or definitions from the original Guided experiments.
            // This used to traverse the complete hierarchy once for each
            // type below. A scene reload now performs one traversal and puts
            // each discovered component into the exact same list as before.
            MonoBehaviour[] components =
                GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour component in components)
            {
                if (component is FreeLabEndpoint endpoint)
                    AddUnique(endpoints, endpoint);
                else if (component is FreeLabControl control)
                    AddUnique(controls, control);
                else if (component is FreeLabExperimentEvaluator evaluator)
                    AddUnique(evaluators, evaluator);
                else if (component is FreeLabSliderControl slider)
                    AddUnique(sliders, slider);
                else if (component is FreeLabThreeStateToggle toggle)
                    AddUnique(threeStateToggles, toggle);
                else if (component is FreeLabCalculationStageRouter router)
                    AddUnique(calculationStageRouters, router);
                else if (component is FreeLabDevice device)
                    AddUnique(devices, device);
                else if (component is FreeLabDeviceManager deviceManager)
                    AddUnique(deviceManagers, deviceManager);
                else if (component is FreeLabWireDefinition wireDefinition)
                    AddUnique(wireDefinitions, wireDefinition);
            }
        }

        private void Warn(string message)
        {
            Debug.Log("FREE MODE: " + message);

            operationalStateMachine?.ReportWarning(message);

            bool hasAssignedFreeModePanel = freeModeWarningPanel != null &&
                (freeModeWarningText != null || legacyFreeModeWarningText != null);
            if (hasAssignedFreeModePanel)
            {
                if (freeModeWarningText != null)
                    freeModeWarningText.text = message;
                if (legacyFreeModeWarningText != null)
                    legacyFreeModeWarningText.text = message;
                freeModeWarningPanel.SetActive(true);
            }
            else if (WarningManager.Instance != null)
                WarningManager.Instance.ShowWarning(message);

            onWarning?.Invoke(message);
        }

        private static void AddUnique<T>(
            List<T> destination,
            T[] source)
            where T : Object
        {
            foreach (T item in source)
            {
                if (item != null && !destination.Contains(item))
                    destination.Add(item);
            }
        }

        private static void AddUnique<T>(List<T> destination, T item)
            where T : Object
        {
            if (item != null && !destination.Contains(item))
                destination.Add(item);
        }

        private static bool SamePhysicalEndpoint(
            FreeLabEndpoint first,
            FreeLabEndpoint second)
        {
            if (first == null || second == null)
                return false;

            if (first == second)
                return true;

            // Separate stacked bananas deliberately represent independent
            // sockets when one physical location needs more than one live
            // wire. Connections are therefore matched by their assigned
            // endpoint objects, never by coincident world position or name.
            return false;
        }

        private static bool IsFreeMode()
        {
            return InteractionManager.Instance != null &&
                InteractionManager.Instance.Free;
        }
    }
}
