using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using TutorialFramework;

public class SliderRangeChecker : MonoBehaviour
{
    // =====================================================
    // SLIDER
    // =====================================================

    [Header("Slider")]
    [SerializeField]
    private Slider slider;


    // =====================================================
    // VALUE DISPLAY
    // =====================================================

    [Header("Value Display")]
    [SerializeField]
    private TMP_Text valueText;

    [Header("Wind experiments (optional)")]
    [SerializeField] private WindTurbineSpeedController[] windSpeedControllers;
    [SerializeField] private Collider windSliderCollider;
    [SerializeField] private AudioClip windSliderClick;
    [Header("Wind speed popup")]
    [SerializeField] private CanvasGroup windSpeedPopup;
    [SerializeField] private SmoothCameraMovement turbineViewCamera;
    [SerializeField] private int turbineViewIndex = -1;
    private readonly List<PanelToggleButton> bottomPanels = new List<PanelToggleButton>();

    public void ToggleWindSpeedPopup()
    {
        if (windSpeedPopup == null) return;
        if (windSpeedPopup.alpha < .5f && bottomPanels.Exists(tab => tab != null && tab.IsOpen))
            return;
        SetWindSpeedPopup(windSpeedPopup.alpha < .5f);
        SharedInteractionClickSound.Play(windSliderClick);
    }

    public void CloseWindSpeedPopup()
    {
        SetWindSpeedPopup(false);
        SharedInteractionClickSound.Play(windSliderClick);
    }

    public void CloseWindSpeedPopupSilently()
    {
        SetWindSpeedPopup(false);
    }

    public void ViewWindTurbine()
    {
        if (turbineViewCamera != null && turbineViewIndex >= 0)
            turbineViewCamera.MoveToPosition(turbineViewIndex);
        SharedInteractionClickSound.Play(windSliderClick);
    }

    private void SetWindSpeedPopup(bool visible)
    {
        if (windSpeedPopup == null) return;
        // Keep the actual slider active so hiding the panel cannot reset the
        // authored guided unlock, speed, or maximum-reached event.
        windSpeedPopup.alpha = visible ? 1f : 0f;
        windSpeedPopup.interactable = visible;
        windSpeedPopup.blocksRaycasts = visible;
    }

    private WindTurbineSpeedController activeWindController;

    private bool IsWindSlider => windSpeedControllers != null && windSpeedControllers.Length > 0;

    private void RefreshWindSlider()
    {
        if (!IsWindSlider || slider == null) return;
        WindTurbineSpeedController next = null;
        foreach (var controller in windSpeedControllers)
            if (controller != null && controller.IsActiveSliderExperiment) { next = controller; break; }
        if (next != activeWindController)
        {
            activeWindController = next;
            // Range is authored once on the shared slider. Storyboard events own its collider.
            if (windSliderCollider != null) windSliderCollider.enabled = false;
            slider.SetValueWithoutNotify(next != null ? next.CurrentSpeed : 0f);
        }
        bool available = next != null && next.CanUseSpeedSlider;
        slider.interactable = available;
    }

    private void OnWindSliderChanged(float value)
    {
        RefreshWindSlider();
        if (activeWindController != null && slider.interactable)
        {
            float previousSpeed = activeWindController.CurrentSpeed;
            activeWindController.SetSliderSpeed(value);
            if (windSliderClick != null && !Mathf.Approximately(previousSpeed, activeWindController.CurrentSpeed))
                SharedInteractionClickSound.Play(windSliderClick);
        }
    }

    private void OnDisable()
    {
        if (IsWindSlider && slider != null) slider.onValueChanged.RemoveListener(OnWindSliderChanged);
    }


    // =====================================================
    // RANGE CHECK
    // =====================================================

    [System.Serializable]
    public class RangeCheck
    {
        [Header("Target")]
        public int targetValue;

        [Tooltip("Allowed distance from the target.")]
        public int range = 5;


        [Header("Required Time")]
        [Tooltip(
            "How long the slider must remain inside " +
            "the range before the event fires."
        )]
        public float requiredTime = 1f;


        [Header("Event")]
        public UnityEvent onReached;


        // Runtime
        [HideInInspector]
        public bool checking;

        [HideInInspector]
        public bool reached;

        [HideInInspector]
        public float timeInsideRange;
    }


    [Header("Range Checks")]
    [SerializeField]
    private List<RangeCheck> rangeChecks = new List<RangeCheck>();


    // =====================================================
    // RUNTIME
    // =====================================================

    private int activeCheckIndex = -1;

    private Coroutine checkCoroutine;


    private void OnEnable()
    {
        bottomPanels.Clear();
        // Close this popup directly whenever the student selects Main Settings,
        // Steps, or Experiments from the bottom navigation.
        foreach (var button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (button == null || button.gameObject.scene != gameObject.scene ||
                button.transform.parent == null || button.transform.parent.name != "Buttom Bar") continue;
            string buttonName = button.name.ToLowerInvariant();
            if (buttonName.Contains("main settings") || buttonName.Contains("steps") || buttonName.Contains("experiments"))
            {
                var tab = button.GetComponent<PanelToggleButton>();
                if (tab != null)
                    bottomPanels.Add(tab);
                button.onClick.RemoveListener(CloseWindSpeedPopupSilently);
                button.onClick.AddListener(CloseWindSpeedPopupSilently);
            }
        }

        if (IsWindSlider && slider != null)
        {
            slider.onValueChanged.RemoveListener(OnWindSliderChanged);
            slider.onValueChanged.AddListener(OnWindSliderChanged);
            RefreshWindSlider();
        }
        // The value can also be changed by another system (for example when
        // power is switched off). Keep the label correct even when that code
        // intentionally updates the Slider without raising On Value Changed.
        RefreshValueText();
    }

private void Update()
    {
        RefreshWindSlider();
        // A few of the training controls reset the slider silently so they do
        // not re-trigger their own input logic. The visual number must still
        // mirror the slider in both Guided and Free Mode.
        RefreshValueText();
    }




    // =====================================================
    // SLIDER VALUE CHANGED
    // =====================================================

    /// <summary>
    /// Call this from Slider -> On Value Changed (Int).
    /// The actual slider value is read directly from the Slider component.
    /// </summary>
    public void OnSliderValueChanged(int value)
    {
        if (slider == null)
        {
            Debug.LogWarning(
                "SliderRangeChecker: Slider reference is missing."
            );

            return;
        }

        RefreshValueText();
    }

    private void RefreshValueText()
    {
        if (slider == null || valueText == null)
            return;

        string shownValue = Mathf.RoundToInt(slider.value).ToString();
        if (valueText.text != shownValue)
            valueText.text = shownValue;
    }


    // =====================================================
    // START CHECK
    // =====================================================

    public void StartCheck(int index)
    {
        if (index < 0 || index >= rangeChecks.Count)
        {
            Debug.LogWarning(
                $"SliderRangeChecker: Invalid range check index {index}."
            );

            return;
        }


        // Stop previous check
        StopCurrentCheck();


        activeCheckIndex = index;

        RangeCheck check = rangeChecks[index];

        check.checking = true;
        check.reached = false;
        check.timeInsideRange = 0f;


        checkCoroutine = StartCoroutine(CheckRange(check));
    }


    // =====================================================
    // CHECK RANGE
    // =====================================================

    private IEnumerator CheckRange(RangeCheck check)
    {
        while (check.checking)
        {
            // Get the REAL current value from the Slider
            int currentValue = Mathf.RoundToInt(slider.value);


            int minimum = check.targetValue - check.range;
            int maximum = check.targetValue + check.range;


            // Are we inside the allowed range?
            if (currentValue >= minimum &&
                currentValue <= maximum)
            {
                check.timeInsideRange += Time.deltaTime;


                // Stayed inside long enough
                if (check.timeInsideRange >= check.requiredTime)
                {
                    check.reached = true;
                    check.checking = false;


                    // Run THIS item's event
                    check.onReached?.Invoke();


                    activeCheckIndex = -1;
                    checkCoroutine = null;

                    yield break;
                }
            }
            else
            {
                // Left the range.
                // Timer starts over.
                check.timeInsideRange = 0f;
            }


            yield return null;
        }
    }


    // =====================================================
    // STOP CURRENT CHECK
    // =====================================================

    public void StopCurrentCheck()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }


        if (activeCheckIndex >= 0 &&
            activeCheckIndex < rangeChecks.Count)
        {
            RangeCheck check = rangeChecks[activeCheckIndex];

            check.checking = false;
            check.timeInsideRange = 0f;
        }


        activeCheckIndex = -1;
    }


    // =====================================================
    // RESET CHECK
    // =====================================================

    public void ResetCurrentCheck()
    {
        if (activeCheckIndex < 0 ||
            activeCheckIndex >= rangeChecks.Count)
        {
            return;
        }


        int index = activeCheckIndex;

        StopCurrentCheck();

        StartCheck(index);
    }
}
