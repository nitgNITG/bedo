using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time scene setup for Main UI > Popups. Keeps the authored notification
/// scale, positions it at the top of the popup region, and reserves the lower
/// area for the current instruction/subtitle panel.
/// </summary>
public static class NotificationPopupLayoutUtility
{
    [MenuItem("Bedo/UI/Stack Notifications Above Instructions")]
    private static void StackNotificationsAboveInstructions()
    {
        Transform mainUi = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => NameIs(transform, "Main UI"));
        Transform popups = FindDirectChild(mainUi, "Popups");
        Transform notifications = FindDirectChild(popups, "Notifications");
        Transform instructions = FindDirectChild(popups, "Instructions");

        if (popups == null || notifications == null || instructions == null)
        {
            Debug.LogError("Notification layout: Main UI > Popups > Notifications and Instructions were not found.");
            return;
        }

        RectTransform popupRect = popups as RectTransform;
        RectTransform notificationRect = notifications as RectTransform;
        RectTransform instructionRect = instructions as RectTransform;
        VerticalLayoutGroup layout = popups.GetComponent<VerticalLayoutGroup>();
        if (popupRect == null || notificationRect == null || instructionRect == null || layout == null)
        {
            Debug.LogError("Notification layout: expected RectTransforms and a VerticalLayoutGroup on Popups.");
            return;
        }

        Undo.RecordObjects(new UnityEngine.Object[]
        {
            popupRect, notificationRect, layout
        }, "Stack Notifications Above Instructions");

        LayoutElement element = notifications.GetComponent<LayoutElement>();
        if (element == null)
            element = Undo.AddComponent<LayoutElement>(notifications.gameObject);
        element.ignoreLayout = true;

        // The notification keeps its existing 1.2468 visual scale. Layout
        // therefore must not reserve only its unscaled RectTransform height.
        notificationRect.anchorMin = new Vector2(0.5f, 1f);
        notificationRect.anchorMax = new Vector2(0.5f, 1f);
        notificationRect.pivot = new Vector2(0.5f, 1f);
        notificationRect.anchoredPosition = Vector2.zero;

        const float spacing = 8f;
        float notificationVisualHeight = notificationRect.rect.height *
            Mathf.Abs(notificationRect.localScale.y);
        float requiredHeight = notificationVisualHeight +
            instructionRect.rect.height + spacing;
        popupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            requiredHeight);

        // Instructions are now the only layout-managed direct child. Keep
        // their authored height and align them at the bottom of Popups.
        layout.childForceExpandHeight = false;
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.LowerCenter;

        EditorUtility.SetDirty(popups.gameObject);
        EditorUtility.SetDirty(notifications.gameObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Notifications now stack above Instructions without overlap.");
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (NameIs(child, name))
                return child;
        }
        return null;
    }

    private static bool NameIs(Transform transform, string expected) =>
        transform != null && string.Equals(transform.name.Trim(), expected,
            StringComparison.OrdinalIgnoreCase);
}
