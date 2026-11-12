using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip")]
    public string title;
    [TextArea]
    public string description;



    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HoveringToolTipPanel.Instance != null)
        {
            HoveringToolTipPanel.Instance.Show(
                title,
                description
            );
        }
        else
        {
            Debug.LogError("Tooltip Panel Instance is NULL");
        }
    }



    public void OnPointerExit(PointerEventData eventData)
    {
        if (HoveringToolTipPanel.Instance != null)
        {
            HoveringToolTipPanel.Instance.Hide();
        }
    }
}
