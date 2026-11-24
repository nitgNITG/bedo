using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Bedo.Hydro
{
    public sealed class HydroSpeedKnobClick : MonoBehaviour
    {
        public HydroSpeedInput input;
        public bool modeSelector;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private void OnMouseDown()
        {
            if (!input) return;
            // Physics raycasters also report this knob to the EventSystem.
            // Block canvas UI only, so the knob does not block its own click.
            if (EventSystem.current)
            {
                hits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                    { position = Input.mousePosition }, hits);
                foreach (var hit in hits) if (hit.module is GraphicRaycaster) return;
            }
            if (modeSelector) input.ToggleMode(); else input.ToggleKnob();
            TutorialFramework.SharedInteractionClickSound.Play();
        }
    }
}
