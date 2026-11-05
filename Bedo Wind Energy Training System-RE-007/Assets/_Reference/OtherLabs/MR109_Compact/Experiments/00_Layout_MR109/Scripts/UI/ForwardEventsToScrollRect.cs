using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ForwardEventsToScrollRect : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerDownHandler
{
    [SerializeField] private Scrollbar directScrollbar;
    private float pressOffset;
    private ScrollRect parentScrollRect;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (directScrollbar == null || eventData.button != PointerEventData.InputButton.Left) return;
        pressOffset = 0f;
        var corners = new Vector3[4];
        directScrollbar.handleRect.GetWorldCorners(corners);
        Vector2 bottom = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, corners[0]);
        Vector2 top = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, corners[2]);
        if (eventData.position.x >= bottom.x && eventData.position.x <= top.x &&
            eventData.position.y >= bottom.y && eventData.position.y <= top.y)
            pressOffset = eventData.position.y - (bottom.y + top.y) * .5f;
        DragScrollbar(eventData);
    }

    private void DragScrollbar(PointerEventData eventData)
    {
        if (!directScrollbar.IsInteractable() || eventData.button != PointerEventData.InputButton.Left) return;
        var area = (RectTransform)directScrollbar.handleRect.parent;
        var corners = new Vector3[4];
        area.GetWorldCorners(corners);
        float bottom = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, corners[0]).y;
        float top = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, corners[2]).y;
        float handleHeight = (top - bottom) * directScrollbar.size;
        float range = top - bottom - handleHeight;
        if (range <= .01f) return;
        // Overlay canvases may sit behind the null-camera event ray; use their projected bounds.
        float value = Mathf.Clamp01((eventData.position.y - pressOffset - bottom - handleHeight * .5f) / range);
        directScrollbar.value = directScrollbar.direction == Scrollbar.Direction.TopToBottom ? 1f - value : value;
    }

    private void Start()
    {
        if (directScrollbar != null) return;
        // Find the parent's ScrollRect component
        parentScrollRect = GetComponentInParent<ScrollRect>();
        if (parentScrollRect == null)
        {
            Debug.LogError("Parent ScrollRect not found. Please ensure this object is a child of a ScrollRect.");
        }
    }

    // Forward the OnBeginDrag event to the parent ScrollRect
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (directScrollbar != null) return;
        if (parentScrollRect != null)
        {
            parentScrollRect.OnBeginDrag(eventData);
        }
    }

    // Forward the OnDrag event to the parent ScrollRect
    public void OnDrag(PointerEventData eventData)
    {
        if (directScrollbar != null) { DragScrollbar(eventData); return; }
        if (parentScrollRect != null)
        {
            parentScrollRect.OnDrag(eventData);
        }
    }

    // Forward the OnEndDrag event to the parent ScrollRect
    public void OnEndDrag(PointerEventData eventData)
    {
        if (directScrollbar != null) return;
        if (parentScrollRect != null)
        {
            parentScrollRect.OnEndDrag(eventData);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (parentScrollRect != null)
        {
            parentScrollRect.OnScroll(eventData);
        }
    }
}
