using Obvious.Soap;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
[RequireComponent(typeof(Animator))]
public class IndependentAnimatedToggle : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    /*
     WHY THIS EXISTS:
     - Unity UI “Highlighted/Selected” visuals come from focus/hover (EventSystem), not from Toggle.isOn.
       So clicking a different group can remove the "selected" look even though the toggle is still ON.
     - This script makes the persistent look come from Toggle.isOn (Selected trigger when ON).
     - It also avoids Unity ToggleGroup so groups don’t conflict; instead we notify IndependentToggleGroup.
     - Exposes OnValueChanged(bool) so you can hook your own events to this toggle.
    */

    [Header("Animator Triggers")]
    [SerializeField] private string normalTrigger = "Normal";
    [SerializeField] private string pressedTrigger = "Pressed";
    [SerializeField] private string highlightedTrigger = "Highlighted";
    [SerializeField] private string selectedTrigger = "Selected";
    [SerializeField] private string disabledTrigger = "Disabled";

    [Header("BoolReference (your existing type)")]
    [SerializeField] private BoolReference boolReference = new BoolReference(false, true);

    [Header("Group (auto-found if empty)")]
    [SerializeField] private IndependentToggleGroup groupOverride;

    [Header("Independent Toggle Event")]
    [SerializeField] private BoolEvent onValueChanged = new BoolEvent();
    public BoolEvent OnValueChanged => onValueChanged;

    [Serializable] public class BoolEvent : UnityEvent<bool> { }

    private Toggle toggle;
    private Animator animator;
    private IndependentToggleGroup group;

    private bool suppressToggleCallback;
    private bool pointerInside;
    private bool lastEmittedValue;

    public bool IsOn => toggle != null && toggle.isOn;

    private void Awake()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        toggle = GetComponent<Toggle>();
        animator = GetComponent<Animator>();

        // Not using Unity ToggleGroup/Selectable animation driving
        toggle.group = null;
        toggle.transition = Selectable.Transition.None;

        group = groupOverride != null ? groupOverride : GetComponentInParent<IndependentToggleGroup>();
        lastEmittedValue = toggle.isOn;
    }

    private void Start()
    {
        // Initial sync: BoolReference -> Toggle
        suppressToggleCallback = true;
        toggle.SetIsOnWithoutNotify(boolReference.Value);
        suppressToggleCallback = false;

        if (group != null)
        {
            group.Register(this);
            if (toggle.isOn)
                group.NotifyToggleOn(this, fromGroup: true);
        }

        RefreshVisualState();
        lastEmittedValue = toggle.isOn; // don't auto-fire on start
    }

    private void OnEnable()
    {
        group = groupOverride != null ? groupOverride : GetComponentInParent<IndependentToggleGroup>();
        if (group != null) group.Register(this);

        toggle.onValueChanged.AddListener(OnToggleValueChanged);

        // Direct BoolReference subscription (as you requested)
        boolReference.OnValueChanged += OnBoolReferenceChanged;

        // Sync on enable
        OnBoolReferenceChanged(boolReference.Value);

        RefreshVisualState();
    }

    private void OnDisable()
    {
        toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        boolReference.OnValueChanged -= OnBoolReferenceChanged;

        if (group != null) group.Unregister(this);
    }

    // ---- Called by the group to force ON/OFF (no Unity ToggleGroup) ----
    public void SetIsOnFromGroup(bool isOn)
    {
        suppressToggleCallback = true;
        toggle.SetIsOnWithoutNotify(isOn);
        suppressToggleCallback = false;

        if (boolReference.Value != isOn)
            boolReference.Value = isOn;

        RefreshVisualState();
        EmitIfChanged(isOn);
    }

    public void RefreshVisualState()
    {
        if (!toggle.interactable)
        {
            SetAnimatorTrigger(disabledTrigger);
            return;
        }

        // Persistent selected look when ON
        if (toggle.isOn)
        {
            SetAnimatorTrigger(selectedTrigger);
            return;
        }

        if (pointerInside)
        {
            SetAnimatorTrigger(highlightedTrigger);
            return;
        }

        // Optional focus look for keyboard/gamepad navigation
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            SetAnimatorTrigger(selectedTrigger);
            return;
        }

        SetAnimatorTrigger(normalTrigger);
    }

    // ---- User interaction: Toggle -> BoolReference + Group ----
    private void OnToggleValueChanged(bool isOn)
    {
        if (suppressToggleCallback) return;

        // ✅ FIX: Prevent the group from ending up with NONE selected when AllowSwitchOff == false.
        // We check "after this change, is there any other toggle ON?"
        if (!isOn && group != null && !group.AllowSwitchOff && !group.AnyOnExcept(this))
        {
            suppressToggleCallback = true;
            toggle.SetIsOnWithoutNotify(true);
            suppressToggleCallback = false;

            if (boolReference.Value != true)
                boolReference.Value = true;

            // Ensure group still considers this selected
            group.NotifyToggleOn(this, fromGroup: true);

            RefreshVisualState();
            EmitIfChanged(true);
            return;
        }

        // Update BoolReference
        if (boolReference.Value != isOn)
            boolReference.Value = isOn;

        if (group != null)
        {
            if (isOn) group.NotifyToggleOn(this);
            else group.NotifyToggleOff(this);
        }

        RefreshVisualState();
        EmitIfChanged(isOn);
    }

    // ---- Programmatic change: BoolReference -> Toggle (+ Group) ----
    private void OnBoolReferenceChanged(bool newValue)
    {
        if (toggle.isOn == newValue)
        {
            RefreshVisualState();
            return;
        }

        // If BoolReference tries to turn OFF the last one while AllowSwitchOff==false, block it too
        if (!newValue && group != null && !group.AllowSwitchOff && !group.AnyOnExcept(this))
        {
            if (boolReference.Value != true)
                boolReference.Value = true;

            RefreshVisualState();
            EmitIfChanged(true);
            return;
        }

        suppressToggleCallback = true;
        toggle.SetIsOnWithoutNotify(newValue);
        suppressToggleCallback = false;

        if (group != null)
        {
            if (newValue) group.NotifyToggleOn(this, fromGroup: true);
            else group.NotifyToggleOff(this);
        }

        RefreshVisualState();
        EmitIfChanged(newValue);
    }

    // ---- Pointer/selection visuals ----
    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (!toggle.isOn) SetAnimatorTrigger(highlightedTrigger);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        if (!toggle.isOn) SetAnimatorTrigger(normalTrigger);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!toggle.isOn && toggle.interactable)
            SetAnimatorTrigger(pressedTrigger);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!toggle.isOn && toggle.interactable)
            RefreshVisualState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!toggle.isOn) RefreshVisualState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!toggle.isOn) RefreshVisualState();
    }

    // ---- Animator helper ----
    private void SetAnimatorTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;

        animator.ResetTrigger(normalTrigger);
        animator.ResetTrigger(pressedTrigger);
        animator.ResetTrigger(highlightedTrigger);
        animator.ResetTrigger(selectedTrigger);
        animator.ResetTrigger(disabledTrigger);

        animator.SetTrigger(triggerName);
    }

    // ---- Event helper ----
    private void EmitIfChanged(bool value)
    {
        if (value == lastEmittedValue) return;
        lastEmittedValue = value;
        onValueChanged?.Invoke(value);
    }
}
