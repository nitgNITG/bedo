using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Bedo.Hydro
{
    [DefaultExecutionOrder(500)]
    public sealed class HydroSpeedInput : MonoBehaviour
    {
        public static HydroSpeedInput Instance { get; private set; }
        public Slider slider;
        public CanvasGroup softwarePanel;
        public GameObject knobPanel;
        public Transform knob, modeKnob;
        public Collider knobCollider;
        public Button shortcut;
        public Button[] bottomButtons;
        public PanelToggleButton[] bottomPanels;
        public HydroReferenceControls experimentOne;
        public HydroMpptGuided experimentTwo;
        public HydroOffGridGuided experimentThree;
        public HydroFreeLabMonitor freeLab;
        public StoryboardSteps[] boards;
        public string[] speedStepIds;
        public GameObject pointer;
        public GameObject[] oldHighlights;
        public Collider[] oldKnobColliders;
        private Quaternion knobRest, modeRest;
        private Renderer knobSurface;
        private bool remote, changingFromKnob;
        public bool Remote => remote;
        public bool Available
        {
            get
            {
                var manager = InteractionManager.Instance;
                if (!manager) return false;
                if (manager.Free) return freeLab && freeLab.CanAdjustSpeed;
                if (experimentOne && experimentOne.isActiveAndEnabled && experimentOne.CanAdjustSpeed) return true;
                if (experimentTwo && experimentTwo.isActiveAndEnabled && manager.IsExperimentActive(experimentTwo.storyboard)) return experimentTwo.CanAdjustSpeed;
                if (experimentThree && experimentThree.isActiveAndEnabled && manager.IsExperimentActive(experimentThree.storyboard)) return experimentThree.CanAdjustSpeed;
                return false;
            }
        }
        public bool SoftwareAvailable => Available && remote && !Blocked();
        // Keep the original RPM scale and interpolate between calibrated unloaded points.
        private bool UsesUnloadedStops => slider.maxValue > HydroExperimentOneReadings.LoadedMaximumRpm / HydroExperimentOneReadings.LoadedSpeedIncrement;
        private float MaximumRpm => HydroExperimentOneReadings.LoadedMaximumRpm;
        private float RpmAtStop(float stop) => UsesUnloadedStops
            ? HydroExperimentOneReadings.ReadUnloaded(stop).Rpm
            : stop * HydroExperimentOneReadings.LoadedSpeedIncrement;
        public float SpeedPercentage => slider && MaximumRpm > 0
            ? RpmAtStop(slider.value) / MaximumRpm * 100f : 0f;
        public void SetPercentage(float percentage)
        {
            if (!SoftwareAvailable) return;
            float target = Mathf.Clamp01(percentage / 100f) * MaximumRpm;
            slider.value = UsesUnloadedStops
                ? HydroExperimentOneReadings.UnloadedPositionAtRpm(target)
                : target / HydroExperimentOneReadings.LoadedSpeedIncrement;
        }
        public bool AcceptSpeedChange => Available && (changingFromKnob || remote);
        private void Awake()
        {
            Instance = this;
            knobSurface = knob.GetComponent<Renderer>();
            knobRest = knob.localRotation;
            modeRest = modeKnob.localRotation;
            CloseControls();
        }
        private void OnEnable()
        {
            shortcut.onClick.AddListener(ToggleSoftware);
            foreach (var button in bottomButtons) if (button) button.onClick.AddListener(CloseControls);
        }
        private void OnDisable()
        {
            shortcut.onClick.RemoveListener(ToggleSoftware);
            foreach (var button in bottomButtons) if (button) button.onClick.RemoveListener(CloseControls);
            CloseControls();
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private bool Blocked()
        {
            foreach (var panel in bottomPanels) if (panel && panel.IsOpen) return true;
            return false;
        }
        public void ToggleMode()
        {
            if (!InteractionManager.Instance) return;
            remote = !remote;
            if (knobPanel) knobPanel.SetActive(false);
        }
        public void ToggleSoftware()
        {
            if (Blocked()) { SetSoftware(false); return; }
            SetSoftware(softwarePanel.alpha < .5f);
        }
        public void ToggleKnob()
        {
            if (remote || !Available || Blocked()) return;
            knobPanel.SetActive(!knobPanel.activeSelf);
        }
        public void Increase() => Change(1);
        public void Decrease() => Change(-1);
        private void Change(int direction)
        {
            if (remote || !Available || !knobPanel.activeInHierarchy || Blocked()) return;
            changingFromKnob = true;
            try { slider.value = Mathf.Clamp(slider.value + direction, slider.minValue, slider.maxValue); }
            finally { changingFromKnob = false; }
            SharedInteractionClickSound.Play();
        }
        public void CloseControls()
        {
            SetSoftware(false);
            if (knobPanel) knobPanel.SetActive(false);
        }
        private void SetSoftware(bool show)
        {
            if (!softwarePanel) return;
            softwarePanel.alpha = show ? 1f : 0f;
            softwarePanel.interactable = softwarePanel.blocksRaycasts = show;
        }
        private void LateUpdate()
        {
            bool available = Available;
            if (Blocked()) CloseControls();
            if ((!available || remote) && knobPanel.activeSelf) knobPanel.SetActive(false);
            slider.interactable = SoftwareAvailable;
            knobCollider.enabled = available && !remote;
            foreach (var old in oldKnobColliders) if (old) old.enabled = false;
            float fraction = SpeedPercentage / 100f;
            knob.localRotation = knobRest * Quaternion.AngleAxis(-90f * fraction, Vector3.up);
            modeKnob.localRotation = Quaternion.RotateTowards(modeKnob.localRotation,
                modeRest * Quaternion.AngleAxis(remote ? -90f : 0f, Vector3.up), 360f * Time.deltaTime);
            foreach (var old in oldHighlights) if (old && old.activeSelf) old.SetActive(false);
            bool showPointer = false;
            var manager = InteractionManager.Instance;
            if (manager && manager.Guided && available && !remote && !knobPanel.activeSelf && !Blocked())
                for (int i = 0; i < boards.Length; i++)
                    if (boards[i] && manager.IsExperimentActive(boards[i]) &&
                        boards[i].IsCurrentStep(speedStepIds[i])) { showPointer = true; break; }
            if (knobPanel.activeSelf) knobPanel.transform.position = knobSurface.bounds.center + new Vector3(0, .2f, -.6f);
            if (pointer && pointer.activeSelf != showPointer) pointer.SetActive(showPointer);
        }
    }
}
