using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
namespace Bedo.Core.Physics.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ClickableObjectOnClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UnityEvent onClick;
        public Action ActionOnClick;
        public bool IsDisabled { set; get; }


        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsDisabled)
            {
                if (UIClickHelpers.IsScreenPositionOverUI(eventData.position)) return;
                onClick?.Invoke();
                ActionOnClick?.Invoke();
            }

        }

        public void ForceClick()
        {
            OnPointerClick(null);
        }
    }
}