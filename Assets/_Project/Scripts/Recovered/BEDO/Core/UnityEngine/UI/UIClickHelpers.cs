// UIHelpers.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem;
#endif

public static class UIClickHelpers 
{
    /// <summary>
    /// Generic check: is current pointer (mouse/touch) over UI?
    /// Works with the New Input System: checks mouse deviceId, enhanced touches, and falls back to a screen-ray test.
    /// </summary>
    public static bool IsPointerOverUI()
    {
        // No EventSystem -> no UI
        if (EventSystem.current == null) return false;

        // 1) If the new Input System is present, try device-based checks first
#if ENABLE_INPUT_SYSTEM && UNITY_INPUT_SYSTEM
        // Mouse deviceId
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (EventSystem.current.IsPointerOverGameObject(mouse.deviceId)) return true;
        }

        // EnhancedTouch: check all active touches' touchIds
        if (EnhancedTouchSupport.enabled)
        {
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (EventSystem.current.IsPointerOverGameObject(touch.touchId)) return true;
            }
        }
#endif

        // 2) Fallback: use current pointer position (works for mouse and touch)
        var pos = GetCurrentScreenPosition();
        if (pos.HasValue && IsScreenPositionOverUI(pos.Value)) return true;

        // not over UI
        return false;
    }

    /// <summary>
    /// Raycasts the UI (EventSystem/GraphicRaycasters) at the given screen position.
    /// </summary>
    public static bool IsScreenPositionOverUI(Vector2 screenPos)
    {
        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // If any result corresponds to a Canvas/Graphic, treat it as UI.
        foreach (var r in results)
        {
            // If it has a RectTransform, it's almost certainly uGUI
            if (r.gameObject != null && r.gameObject.GetComponent<RectTransform>() != null)
                return true;

            // Or if the raycaster module is a GraphicRaycaster
            if (r.module != null && r.module is GraphicRaycaster)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Try to get a sensible "current" pointer position:
    /// - Mouse.current.position
    /// - Touch primary or first active touch
    /// - or null if nothing available
    /// </summary>
    private static Vector2? GetCurrentScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM && UNITY_INPUT_SYSTEM
        if (Pointer.current != null)
        {
            // Pointer.current covers mouse, pen, touch pointer devices; .position is Vector2Control
            return Pointer.current.position.ReadValue();
        }

        // If enhanced touch is enabled and there's an active touch, return its position
        if (EnhancedTouchSupport.enabled && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            var t = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
            return t.screenPosition;
        }
#endif

        // Last-resort: try UnityEngine.Input (legacy) — useful if project has both systems
#if UNITY_2019_1_OR_NEWER
        if (Input.mousePresent) return (Vector2)Input.mousePosition;
        if (Input.touchCount > 0) return Input.touches[0].position;
#endif

        return null;
    }
}