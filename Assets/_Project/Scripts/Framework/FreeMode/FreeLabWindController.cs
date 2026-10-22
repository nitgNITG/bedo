using TutorialFramework.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TutorialFramework
{
    /// <summary>Wind-only Free Lab configuration and calculations, using the existing shared wiring framework.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class FreeLabWindController : MonoBehaviour
    {
        [Serializable] public class Socket
        {
            public FreeLabEndpoint endpoint;
            public GameObject[] plugs;
        }
        [Serializable] public class Control
        {
            public FreeLabControl control;
            public SingleClickInteraction authoredInteraction;
            public Animator animator;
            public string onAnimation, offAnimation;
            public Renderer pressedVisual;
        }
        [Serializable] public class Rheostat
        {
            public RheostatController controller;
            public GameObject adjustmentCanvas;
            public FreeLabConnectionGroup connections;
            public float maximumOhms;
        }
        [Serializable] public class Lamp
        {
            public string name;
            public bool ac, led;
            public FreeLabConnectionGroup[] paths;
            public GameObject light;
            public bool Connected(FreeLabManager lab) => paths != null && paths.Any(p => p && p.IsComplete(lab));
        }
        [Serializable] public class Profile
        {
            public int experiment;
            public FreeLabConnectionGroup[] required;
            public FreeLabConnectionGroupAlternativeRequirement[] alternatives;
            public bool Complete(FreeLabManager lab) => required != null && required.Length > 0 &&
                required.All(g => g && g.IsComplete(lab)) &&
                (alternatives == null || alternatives.All(a => a != null && a.IsComplete(lab)));
        }
        [Serializable] public class Page
        {
            public GameObject root, chargerScreen;
            public TMP_Text speed, chargerText;
            public TMP_Text[] ac, dc, battery, load, dump, acLoad, grid, inverter;
        }
        public static FreeLabWindController Instance { get; private set; }
        public FreeLabManager lab;
        public Transform endpointsRoot, wiresRoot, controlsRoot;
        public GameObject[] guidedRoots;
        public Socket[] sockets;
        public Control[] controls;
        public Rheostat[] rheostats;
        public Lamp[] lamps;
        public Profile[] profiles;
        public Page[] pages;
        public FreeLabControl mainPower, mainEmergency, mainBreaker, windPower, windEmergency, windBreaker, inverterPower, analyzerMode;
        public FreeLabConnectionGroup generatorConnections, pwmBattery, mpptBattery, inverterBattery, gridConnections;
        public FreeLabWireDefinition[] definitions;
        public TextAsset sourceModel, controllerModel, offGridModel;
        public Slider speedSlider;
        public TMP_Text speedSliderText, powerAnalyzerText;
        public Collider speedCollider;
        public GameObject monitors, graphPage, blackScreen, analyzer, windGlow;
        public Interaction graphButton, systemButton;
        public Transform rotor;
        public float rotorVisualMultiplier = .05f;
        public AudioClip windSound;
        public PVGraphController graph;
        public PVExperimentExporter exporter;
        [Header("SOAP simulation context")]
        [SerializeField] private WindRuntimeState runtimeState;
        [SerializeField] private WindSimulationSettings simulationSettings;
        private WindSimulationData legacyState = new WindSimulationData { activeExperiment = 1 };
        private WindSimulationData State { get => runtimeState ? runtimeState.Value : legacyState; set { if (runtimeState) runtimeState.Value = value; else legacyState = value; } }
        public WindRuntimeState SharedState => runtimeState;
        private float MaximumRPM => simulationSettings ? simulationSettings.maximumRPM.Value : 1000;
        private float SliderIncrement => simulationSettings ? simulationSettings.sliderIncrement.Value : 200;
        public int ActiveExperiment { get => State.activeExperiment; private set { var next = State; next.activeExperiment = value; State = next; } }
        public float RPM { get => State.requestedRPM; private set { var next = State; next.requestedRPM = value; State = next; } }
        public PVCSVReader.WindReading Reading { get => State.source; private set { var next = State; next.source = value; State = next; } }
        public PVCSVReader.OffGridReading OffGridReading { get => State.bus; private set { var next = State; next.bus = value; State = next; } }
        public float ActiveResistance { get => State.activeResistance; private set { var next = State; next.activeResistance = value; State = next; } }
        public bool IsReady { get; private set; }
        private static void UseOneClickHandler(GameObject target)
        {
            foreach (var duplicate in target.GetComponents<OnMouseDownEvent>().Skip(1)) Destroy(duplicate);
        }
        private bool graphShown;
        private AudioSource audioSource;
        private readonly Dictionary<FreeLabControl, bool> previousControls = new Dictionary<FreeLabControl, bool>();
        private readonly Dictionary<string, float> constants = new Dictionary<string, float>();
        private float lastRPM = -1, lastVoltage = -1;
        private int lastExperiment = -1;

        private void Start()
        {
            if (InteractionManager.Instance == null || !InteractionManager.Instance.Free)
            { gameObject.SetActive(false); return; }
            Instance = this;
            foreach (var root in guidedRoots) if (root) root.SetActive(false);
            foreach (var socket in sockets)
            {
                var endpoint = socket.endpoint;
                endpoint.transform.SetParent(endpointsRoot, true);
                endpoint.gameObject.SetActive(true);
                UseOneClickHandler(endpoint.gameObject);
                endpoint.Interaction.Allow = true;
                foreach (var collider in endpoint.GetComponents<Collider>()) collider.enabled = true;
                // Keep exactly one clickable physical banana. Additional plug visuals are slots, not duplicate sockets.
                endpoint.ClearAdditionalConnectionPlugs();
                for (int i = 0; i < socket.plugs.Length; i++)
                {
                    var plug = socket.plugs[i];
                    if (!plug) continue;
                    plug.transform.SetParent(endpoint.transform, true);
                    if (i == 0) endpoint.SetSelectionPlug(plug);
                    else
                    {
                        var first = socket.plugs[0].transform;
                        plug.transform.localPosition = first.localPosition + new Vector3(0, 0, -.12f * i);
                        plug.transform.localRotation = first.localRotation;
                        plug.transform.localScale = first.localScale;
                        endpoint.AddAdditionalConnectionPlug(plug);
                    }
                    plug.SetActive(false);
                }
            }
            foreach (var definition in definitions)
            {
                if (!definition || !definition.wireParentTemplate) continue;
                definition.wireParentTemplate.transform.SetParent(wiresRoot, true);
                definition.wireParentTemplate.SetActive(false);
            }
            foreach (var c in controls)
            {
                // Animation clips store local positions in the authored step's
                // coordinate system. Preserve that frame when sharing the control.
                var authoredParent = c.control.transform.parent;
                var frame = new GameObject(c.control.ControlId + " frame").transform;
                frame.SetParent(authoredParent, false);
                frame.SetParent(controlsRoot, true);
                c.control.transform.SetParent(frame, true);
                if (c.pressedVisual && !c.pressedVisual.transform.IsChildOf(c.control.transform))
                {
                    c.pressedVisual.transform.SetParent(frame, true);
                    c.pressedVisual.gameObject.SetActive(true);
                }
                if (c.control == mainPower)
                {
                    var offButton = authoredParent.Find("Off_button004 (1)");
                    if (offButton)
                    {
                        offButton.SetParent(frame, true);
                        offButton.gameObject.SetActive(true);
                        var click = offButton.GetComponent<Template.OnMouseDownE>();
                        if (click)
                        {
                            click.mouseDown = new UnityEvent();
                            click.mouseDown.AddListener(() => { mainPower.SetValue(false); Refresh(); });
                        }
                    }
                }
                c.control.gameObject.SetActive(true);
                foreach (var collider in frame.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
                UseOneClickHandler(c.control.gameObject);
                if (c.authoredInteraction) c.authoredInteraction.enabled = false;
                c.control.GetComponent<Interaction>().Allow = true;
                c.control.SetValue(false);
            }
            foreach (var r in rheostats)
            {
                r.controller.transform.SetParent(controlsRoot, true);
                r.controller.gameObject.SetActive(true);
                UseOneClickHandler(r.controller.gameObject);
                r.controller.ConfigureFreeModeRange(r.maximumOhms, 4f, r.maximumOhms / 5f);
                r.controller.OnMinimumReached = new UnityEvent();
                var single = r.controller.GetComponent<SingleClickInteraction>();
                if (single) single.enabled = false;
                foreach (var collider in r.controller.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
                var interaction = r.controller.GetComponent<Interaction>();
                if (interaction)
                {
                    interaction.Allow = true;
                    interaction.onClicked = new UnityEvent();
                    if (r.adjustmentCanvas) interaction.onClicked.AddListener(() => ShowRheostat(r));
                }
                if (r.adjustmentCanvas)
                {
                    r.adjustmentCanvas.transform.SetParent(controlsRoot, true);
                    r.adjustmentCanvas.SetActive(false);
                    foreach (var button in r.adjustmentCanvas.GetComponentsInChildren<Button>(true))
                    {
                        button.onClick = new Button.ButtonClickedEvent();
                        if (button.name == "Plus") button.onClick.AddListener(r.controller.IncreaseValue);
                        else if (button.name == "Minus") button.onClick.AddListener(r.controller.DecreaseValue);
                    }
                }
            }
            if (windGlow && windPower) windGlow.transform.SetParent(windPower.transform.parent, true);
            foreach (var item in GetComponentsInChildren<FreeLabEndpoint>(true)) item.SetPossibleConnectionIndicatorVisible(false);
            if (speedSlider)
            {
                var checker = speedSlider.GetComponent<SliderRangeChecker>();
                if (checker) checker.enabled = false;
                speedSlider.onValueChanged = new Slider.SliderEvent();
                speedSlider.minValue = 0; speedSlider.maxValue = MaximumRPM;
                speedSlider.SetValueWithoutNotify(0);
                speedSlider.onValueChanged.AddListener(SetRPM);
            }
            if (graphButton) { graphButton.transform.SetParent(controlsRoot, true); graphButton.gameObject.SetActive(true); }
            ReadConstants(offGridModel);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = windSound; audioSource.loop = true; audioSource.playOnAwake = false;
            lab.InitializeFreeLab();
            IsReady = true;
            Refresh();
        }

        private void ShowRheostat(Rheostat selected)
        {
            foreach (var r in rheostats) if (r.adjustmentCanvas) r.adjustmentCanvas.SetActive(r == selected);
        }

        private void ReadConstants(TextAsset asset)
        {
            if (!asset) return;
            var lines = asset.text.Split('\n');
            var names = lines[0].Trim().Split(','); var values = lines[1].Trim().Split(',');
            for (int i = 0; i < Math.Min(names.Length, values.Length); i++)
                if (float.TryParse(values[i], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float value)) constants[names[i]] = value;
        }
        private float Constant(string name, float fallback) => constants.TryGetValue(name, out var value) ? value : fallback;
        public void SetRPM(float value)
        {
            RPM = Mathf.Clamp(Mathf.Round(value / SliderIncrement) * SliderIncrement, 0, MaximumRPM);
            if (speedSlider) speedSlider.SetValueWithoutNotify(RPM);
            Refresh();
        }
        private bool On(FreeLabControl control) => control && control.Value;
        private bool Connected(FreeLabConnectionGroup group) => group && group.IsComplete(lab);
        public bool HasCompleteConfiguration => profiles != null && profiles.Any(p => p.Complete(lab));
        public bool TryGetWindReading(out PVCSVReader.WindReading reading) { reading = Reading; return IsReady; }
        private void Update() { if (IsReady) Refresh(); }
        private void LateUpdate()
        {
            if (!IsReady || !rotor || Reading.rpm <= 0) return;
            rotor.Rotate(Vector3.right, Reading.rpm * 6f * (simulationSettings ? simulationSettings.visualRotorMultiplier.Value : rotorVisualMultiplier) * Time.deltaTime, Space.Self);
        }
        public void ShowGraph()
        {
            if (!IsReady || !On(mainPower) || graphShown) return;
            graphShown = true; ApplyPages(); graphButton?.PlaySound();
        }
        public void ShowSystem()
        {
            if (!IsReady || !On(mainPower) || !graphShown) return;
            graphShown = false; ApplyPages(); systemButton?.PlaySound();
        }
        private void ApplyPages()
        {
            bool powered = On(mainPower);
            if (blackScreen) blackScreen.SetActive(!powered);
            if (monitors) monitors.SetActive(powered && !graphShown);
            if (graphPage) graphPage.SetActive(powered && graphShown);
            if (graphButton) graphButton.Allow = powered && !graphShown;
            if (systemButton) systemButton.Allow = powered && graphShown;
            for (int i = 0; i < pages.Length; i++) if (pages[i].root) pages[i].root.SetActive(i == ActiveExperiment - 1);
        }
        public void Refresh()
        {
            if (!IsReady) return;
            foreach (var c in controls)
            {
                bool value = c.control.Value;
                if (!previousControls.TryGetValue(c.control, out var previous) || previous != value)
                {
                    previousControls[c.control] = value;
                    // These are the authored off-state covers. Guided mode hides
                    // their renderers when the corresponding switch is turned on.
                    if (c.pressedVisual) c.pressedVisual.enabled = !value;
                    if (c.animator)
                    {
                        string animation = value ? c.onAnimation : c.offAnimation;
                        if (!string.IsNullOrEmpty(animation) && c.animator.HasState(0, Animator.StringToHash(animation)))
                            c.animator.Play(animation, 0, value ? 0f : 1f);
                    }
                }
            }
            bool powered = On(mainPower) && On(mainEmergency) && On(mainBreaker);
            if (!powered && On(mainPower)) mainPower.SetValue(false);
            bool generating = powered && On(windPower) && On(windEmergency) && On(windBreaker) && Connected(generatorConnections);
            if (!powered && On(windPower)) windPower.SetValue(false);
            if (!powered && On(inverterPower)) inverterPower.SetValue(false);
            if (!powered && On(analyzerMode)) analyzerMode.SetValue(false);
            ActiveExperiment = profiles.Where(p => p.Complete(lab)).Select(p => p.experiment).DefaultIfEmpty(1).Max();
            var connectedRheostat = rheostats.FirstOrDefault(r => Connected(r.connections));
            ActiveResistance = ActiveExperiment == 1 && connectedRheostat != null ? connectedRheostat.controller.CurrentResistance : 4f;
            if (ActiveExperiment == 1 && connectedRheostat == null) generating = false;
            // The CSV calibrates 4–100 ohms. The existing reader clamps its coefficients
            // outside that range, while current still uses the actual 150/800-ohm resistance.
            PVCSVReader.TryCalculateWind(ActiveExperiment == 1 ? sourceModel : controllerModel,
                generating ? RPM : 0, Mathf.Max(4, ActiveResistance), out var reading);
            Reading = reading;
            float dcLoad = 0, acDisplay = 0, acEquivalent = 0;
            bool batteryBus = powered && (ActiveExperiment == 2 ? Connected(pwmBattery) :
                ActiveExperiment == 3 ? Connected(mpptBattery) : ActiveExperiment == 4 && Connected(inverterBattery) && On(inverterPower));
            foreach (var lamp in lamps)
            {
                bool connected = lamp.Connected(lab);
                bool lit = batteryBus && connected && (!lamp.ac || ActiveExperiment == 4);
                if (lamp.light) lamp.light.SetActive(lit);
                if (!lit) continue;
                if (!lamp.ac) dcLoad += Constant(lamp.led ? "DC_LED_Current_A" : "DC_Load_Current_A", lamp.led ? .2f : 2);
                else
                {
                    acDisplay += Constant("AC_Lamp_Display_Current_A", .2f);
                    acEquivalent += lamp.led ? .2f : Constant("AC_Lamp_DC_Equivalent_A", 2);
                }
            }
            float bus = Constant("Battery_Voltage_V", 24), limit = Constant("Grid_Charge_Current_A", 9), acVoltage = Constant("AC_Lamp_Voltage_V", 220);
            bool grid = batteryBus && ActiveExperiment == 4 && Connected(gridConnections);
            float balance = reading.dcCurrent - dcLoad - acEquivalent;
            var result = new PVCSVReader.OffGridReading();
            if (batteryBus)
            {
                result.batteryVoltage = bus;
                result.batteryCurrent = grid ? limit : Mathf.Min(balance, limit);
                result.batteryPower = bus * result.batteryCurrent;
                result.dcLoadCurrent = dcLoad; result.dcLoadPower = bus * dcLoad;
                result.acLoadVoltage = acDisplay > 0 ? acVoltage : 0; result.acLoadCurrent = acDisplay; result.acLoadPower = result.acLoadVoltage * acDisplay;
                result.inverterCurrent = acEquivalent; result.inverterPower = bus * acEquivalent;
                result.availableChargeCurrent = Mathf.Clamp(balance, 0, limit);
                result.dumpCurrent = Mathf.Max(0, balance - limit);
                if (grid) { result.gridVoltage = acVoltage; result.gridCurrent = -(balance - limit) * bus / acVoltage; result.gridPower = acVoltage * result.gridCurrent; }
            }
            OffGridReading = result;
            for (int i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                Triplet(page.ac, reading.acVoltage, reading.acCurrent, reading.acPower);
                Triplet(page.dc, reading.dcVoltage, reading.dcCurrent, reading.dcPower);
                Triplet(page.battery, result.batteryVoltage, result.batteryCurrent, result.batteryPower);
                Triplet(page.load, dcLoad > 0 ? bus : 0, dcLoad, dcLoad * bus);
                Triplet(page.dump, result.dumpCurrent > 0 ? bus : 0, result.dumpCurrent, result.dumpCurrent * bus);
                Triplet(page.acLoad, result.acLoadVoltage, result.acLoadCurrent, result.acLoadPower);
                Triplet(page.grid, result.gridVoltage, result.gridCurrent, result.gridPower);
                Triplet(page.inverter, acEquivalent > 0 ? bus : 0, acEquivalent, acEquivalent * bus);
                Text(page.speed, reading.rpm, "0.##");
                bool screen = batteryBus && ActiveExperiment == i + 1;
                if (page.chargerScreen) page.chargerScreen.SetActive(screen);
                if (page.chargerText) page.chargerText.text = (screen ? Mathf.Max(0, result.batteryCurrent) / limit * 100 : 0).ToString("F2") + "%";
            }
            if (speedSliderText) speedSliderText.text = RPM.ToString("0");
            if (speedSlider) speedSlider.interactable = powered && On(windPower);
            if (speedCollider) speedCollider.enabled = powered && On(windPower);
            if (windGlow) windGlow.SetActive(generating);
            if (analyzer) analyzer.SetActive(powered && On(analyzerMode));
            if (powerAnalyzerText) powerAnalyzerText.text = $"{reading.acPower:F2} W\n0.00 VAr\n{Mathf.Abs(reading.acVoltage * reading.acCurrent):F2} VA";
            if (audioSource)
            {
                bool audible = generating && RPM > 0;
                audioSource.pitch = Mathf.Lerp(.6f, 1.2f, RPM / MaximumRPM);
                if (audible && !audioSource.isPlaying) audioSource.Play();
                else if (!audible && audioSource.isPlaying) audioSource.Stop();
            }
            ApplyPages();
            if (graph && (lastExperiment != ActiveExperiment || !Mathf.Approximately(lastRPM, reading.rpm) || !Mathf.Approximately(lastVoltage, reading.dcVoltage)))
            {
                if (lastExperiment != ActiveExperiment) graph.ClearRuntimeGraph();
                lastExperiment = ActiveExperiment; lastRPM = reading.rpm; lastVoltage = reading.dcVoltage;
                graph.UpdateWindMeasurement();
            }
        }
        private static void Text(TMP_Text text, float value, string format = "F2") { if (text) text.text = value.ToString(format); }
        private static void Triplet(TMP_Text[] texts, float v, float a, float w)
        { if (texts == null) return; float[] values = { v, a, w }; for (int i = 0; i < Math.Min(3, texts.Length); i++) Text(texts[i], values[i]); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
