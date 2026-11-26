using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace TutorialFramework
{
    [RequireComponent(typeof(Canvas))]
    [DefaultExecutionOrder(11000)]
    public sealed class HybridPVPopupSurface : MonoBehaviour
    {
        private static readonly HashSet<HybridPVPopupSurface> visible = new HashSet<HybridPVPopupSurface>();
        private Canvas canvas;
        private Graphic[] graphics;

        private void OnEnable()
        {
            canvas = GetComponent<Canvas>();
            graphics = GetComponentsInChildren<Graphic>(true);
            visible.Add(this);
            FaceCamera();
        }
        private void OnDisable() { visible.Remove(this); }
        private void LateUpdate() { FaceCamera(); }
        private void FaceCamera()
        {
            if (!canvas || !canvas.worldCamera) return;
            var camera = canvas.worldCamera.transform;
            var direction = transform.position - camera.position;
            if (direction.sqrMagnitude > .0001f)
                transform.rotation = Quaternion.LookRotation(direction, camera.up);
        }
        public static bool BlocksWorldClick()
        {
            if (Mouse.current == null) return false;
            return BlocksWorldClickAt(Mouse.current.position.ReadValue());
        }
        public static bool BlocksWorldClickAt(Vector2 position)
        {
            foreach (var surface in visible)
            {
                if (!surface || !surface.canvas || !surface.canvas.isActiveAndEnabled) continue;
                foreach (var graphic in surface.graphics)
                    if (graphic && graphic.isActiveAndEnabled && graphic.raycastTarget &&
                        RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, position, surface.canvas.worldCamera))
                        return true;
            }
            return false;
        }
    }
}
