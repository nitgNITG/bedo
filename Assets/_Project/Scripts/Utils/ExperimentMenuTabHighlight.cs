using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle), typeof(Animator))]
public sealed class ExperimentMenuTabHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Toggle tab;
    private Animator tabAnimator;
    private bool pointerInside;

    private void OnEnable()
    {
        tab = GetComponent<Toggle>();
        tabAnimator = GetComponent<Animator>();
        tab.onValueChanged.AddListener(OnSelectionChanged);
        Refresh();
    }

    private void Start() => Refresh();

    private void OnDisable()
    {
        if (tab != null) tab.onValueChanged.RemoveListener(OnSelectionChanged);
        pointerInside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        Refresh();
    }

    private void OnSelectionChanged(bool selected) => Refresh();

    private void Refresh()
    {
        if (tabAnimator == null || !tabAnimator.isActiveAndEnabled) return;
        // Keep the selected tab highlighted after the pointer leaves it.
        string state = !tab.IsInteractable() ? "Disabled" :
            tab.isOn || pointerInside ? "Highlighted" : "Normal";
        tabAnimator.Play(state, 0, 0f);
    }
}
