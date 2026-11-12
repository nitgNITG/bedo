using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ButtonPointerProxyExtender : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    [Tooltip("The Button component to forward events to.")]
    public Button targetButton;

    private GameObject Target => targetButton != null ? targetButton.gameObject : null;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Highlight state
        if (Target != null)
            ExecuteEvents.Execute<IPointerEnterHandler>(Target, eventData, ExecuteEvents.pointerEnterHandler);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Normal state
        if (Target != null)
            ExecuteEvents.Execute<IPointerExitHandler>(Target, eventData, ExecuteEvents.pointerExitHandler);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Pressed state
        if (Target != null)
            ExecuteEvents.Execute<IPointerDownHandler>(Target, eventData, ExecuteEvents.pointerDownHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Back to Highlighted on release
        if (Target != null)
            ExecuteEvents.Execute<IPointerUpHandler>(Target, eventData, ExecuteEvents.pointerUpHandler);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Trigger click callbacks
        if (targetButton != null)
            targetButton.onClick.Invoke();
    }
}