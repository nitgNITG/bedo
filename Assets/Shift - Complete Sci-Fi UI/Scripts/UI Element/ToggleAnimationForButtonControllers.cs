using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Obvious.Soap;

[RequireComponent(typeof(Toggle))]
[RequireComponent(typeof(Animator))]
public class ToggleAnimationForButtonControllers : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private BoolReference boolReference;
    // Exposed triggers with default values
    [SerializeField] private string normalTrigger = "Normal";
    [SerializeField] private string pressedTrigger = "Pressed";
    [SerializeField] private string highlightedTrigger = "Highlighted";
    [SerializeField] private string selectedTrigger = "Selected";

    // Reference to Scriptable BoolVariable
   

    private Toggle toggle;
    private Animator animator;

    private void Awake()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        toggle = GetComponent<Toggle>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (boolReference != null)
        {
            SetAnimatorTrigger(boolReference.Value ? pressedTrigger : normalTrigger);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the Toggle's onValueChanged event
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            // Sync initial state from toggle
            //OnToggleValueChanged(toggle.isOn);
        }

        // Subscribe to the ScriptableVariable's change
        if (boolReference != null)
        {
            boolReference.OnValueChanged += OnBoolReferenceChanged;
            // Sync initial state from variable
            OnBoolReferenceChanged(boolReference.Value);
        }
    }

    private void OnDisable()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);

        if (boolReference != null)
            boolReference.OnValueChanged -= OnBoolReferenceChanged;
    }

    // Helper method that resets all triggers, then sets the specified trigger.
    private void SetAnimatorTrigger(string triggerName)
    {
        if (animator == null)
            return;

        // Reset all triggers to avoid conflicts between animations.
        animator.ResetTrigger(normalTrigger);
        animator.ResetTrigger(pressedTrigger);
        animator.ResetTrigger(highlightedTrigger);
        animator.ResetTrigger(selectedTrigger);

        // Set the specified trigger.
        animator.SetTrigger(triggerName);
    }

    // Called when the toggle's value changes (user interaction).
    private void OnToggleValueChanged(bool isOn)
    {
        // Animation for toggle change
        SetAnimatorTrigger(isOn ? pressedTrigger : normalTrigger);

        // Update the ScriptableVariable
        if (boolReference != null)
            boolReference.Value = isOn;
    }

    // Called when the ScriptableVariable changes (programmatic or via this toggle).
    private void OnBoolReferenceChanged(bool newValue)
    {
        if (toggle == null)
            return;

        // Only update UI if out of sync
        if (toggle.isOn == newValue)
            return;

        // Update Toggle without invoking its onValueChanged callback
        toggle.SetIsOnWithoutNotify(newValue);

        // Sync animation state
        SetAnimatorTrigger(newValue ? pressedTrigger : normalTrigger);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (toggle != null && toggle.isOn)
            return;

        SetAnimatorTrigger(highlightedTrigger);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (toggle != null && toggle.isOn)
            return;

        SetAnimatorTrigger(normalTrigger);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (toggle != null && toggle.isOn)
            return;

        SetAnimatorTrigger(selectedTrigger);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (toggle != null && toggle.isOn)
            return;

        SetAnimatorTrigger(normalTrigger);
    }
}
