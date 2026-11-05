using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Copies the approved Exp 1 Notification (1) scroll-view layout to every other
/// notification below Popups > Notifications, while retaining each notification's
/// original Subtitle object and text.
/// </summary>
public static class NotificationScrollSetupUtility
{
    [MenuItem("Bedo/Notification Scrolls/Copy Exp 1 Notification 1 Layout To All %&n")]
    private static void CopyTemplateToAllNotifications()
    {
        var notificationsRoot = FindNotificationsRoot();
        var templateNotification = FindTemplateNotification(notificationsRoot);
        var templateScroll = templateNotification.GetComponentInChildren<ScrollRect>(true);

        if (templateScroll == null || templateScroll.content == null)
        {
            Debug.LogError("Popups > Notifications > Exp 1 Notifications > Notification (1) needs its configured Scroll View first.");
            return;
        }

        var updated = new List<string>();
        var skipped = new List<string>();
        var disabledBars = 0;

        foreach (var notification in notificationsRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!notification.name.StartsWith("Notification", StringComparison.OrdinalIgnoreCase))
                continue;

            var bar = FindDirectChild(notification, "Bar");
            if (bar != null && bar.gameObject.activeSelf)
            {
                Undo.RecordObject(bar.gameObject, "Disable notification bar");
                bar.gameObject.SetActive(false);
                EditorUtility.SetDirty(bar.gameObject);
                disabledBars++;
            }

            if (notification == templateNotification)
                continue;

            // Already-configured notifications no longer have Subtitle as a direct
            // child, so update their scrollbar setting before looking for a subtitle.
            var existingScroll = FindDirectChildScroll(notification);
            if (existingScroll != null)
            {
                EnsureAutoSize(existingScroll, true);
                skipped.Add(GetHierarchyPath(notification) + " (already has a Scroll View)");
                continue;
            }

            var subtitle = FindDirectChild(notification, "Subtitle");
            if (subtitle == null || subtitle.GetComponent<TMP_Text>() == null)
            {
                skipped.Add(GetHierarchyPath(notification) + " (no direct Subtitle TMP object)");
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(notification.gameObject, "Set up notification scroll view");

            var copiedScroll = UnityEngine.Object.Instantiate(templateScroll.gameObject).GetComponent<ScrollRect>();
            Undo.RegisterCreatedObjectUndo(copiedScroll.gameObject, "Copy notification scroll view");
            copiedScroll.name = "Scroll View";
            copiedScroll.transform.SetParent(notification, false);

            // Remove the template Subtitle and put this notification's original Subtitle
            // into the copied Content. No text is copied or replaced.
            var copiedSubtitle = FindDirectChild(copiedScroll.content, "Subtitle");
            if (copiedSubtitle != null)
                Undo.DestroyObjectImmediate(copiedSubtitle.gameObject);

            subtitle.SetParent(copiedScroll.content, false);
            subtitle.SetSiblingIndex(0);
            copiedScroll.verticalNormalizedPosition = 1f;
            EnsureAutoSize(copiedScroll, true);

            EditorUtility.SetDirty(notification.gameObject);
            EditorUtility.SetDirty(copiedScroll);
            EditorUtility.SetDirty(subtitle.gameObject);
            updated.Add(GetHierarchyPath(notification));
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log(
            $"Set up notification scroll views for {updated.Count} notification(s).\n"
            + (updated.Count > 0 ? string.Join("\n", updated) : string.Empty)
            + $"\n\nDisabled {disabledBars} notification Bar object(s)."
            + (skipped.Count > 0 ? "\n\nSkipped:\n" + string.Join("\n", skipped) : string.Empty));
    }

    [MenuItem("Bedo/Notification Scrolls/Replace All Scrollbars From Exp 1 Notification 1 %&m")]
    private static void ReplaceAllScrollbarsFromTemplate()
    {
        var notificationsRoot = FindNotificationsRoot();
        var templateNotification = FindTemplateNotification(notificationsRoot);
        var templateScroll = templateNotification.GetComponentInChildren<ScrollRect>(true);
        var templateScrollbar = templateScroll != null ? templateScroll.verticalScrollbar : null;

        if (templateScrollbar == null)
        {
            Debug.LogError("The template Notification (1) Scroll View has no assigned Vertical Scrollbar.");
            return;
        }

        var replaced = new List<string>();
        foreach (var notification in notificationsRoot.GetComponentsInChildren<Transform>(true))
        {
            if (notification == templateNotification ||
                !notification.name.StartsWith("Notification", StringComparison.OrdinalIgnoreCase))
                continue;

            var scroll = FindDirectChildScroll(notification);
            if (scroll == null)
                continue;

            Undo.RegisterFullObjectHierarchyUndo(notification.gameObject, "Replace notification scrollbar");

            if (scroll.verticalScrollbar != null)
                Undo.DestroyObjectImmediate(scroll.verticalScrollbar.gameObject);

            var copiedScrollbar = UnityEngine.Object.Instantiate(templateScrollbar.gameObject)
                .GetComponent<Scrollbar>();
            Undo.RegisterCreatedObjectUndo(copiedScrollbar.gameObject, "Copy notification scrollbar");
            copiedScrollbar.name = templateScrollbar.gameObject.name;
            copiedScrollbar.transform.SetParent(scroll.transform, false);
            copiedScrollbar.transform.SetSiblingIndex(Mathf.Min(templateScrollbar.transform.GetSiblingIndex(), scroll.transform.childCount - 1));

            scroll.verticalScrollbar = copiedScrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            EnsureAutoSize(scroll, true);

            EditorUtility.SetDirty(scroll);
            EditorUtility.SetDirty(copiedScrollbar);
            replaced.Add(GetHierarchyPath(notification));
        }

        // The template notification is also a notification scroll, so set its new
        // default-off option true without replacing its own scrollbar.
        EnsureAutoSize(templateScroll, true);
        EditorUtility.SetDirty(templateScroll);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Replaced vertical scrollbars on {replaced.Count} notification(s).\n" + string.Join("\n", replaced));
    }

    [MenuItem("Bedo/Notification Scrolls/Keep All Notification Scrollbars Visible %&v")]
    private static void KeepAllNotificationScrollbarsVisible()
    {
        var notificationsRoot = FindNotificationsRoot();
        var updated = 0;

        foreach (var notification in notificationsRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!notification.name.StartsWith("Notification", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var scroll in notification.GetComponentsInChildren<ScrollRect>(true))
            {
                EnsureAutoSize(scroll, true);
                scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                EditorUtility.SetDirty(scroll);
                updated++;
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Set Keep Vertical Scrollbar Visible on {updated} notification Scroll View(s).");
    }

    private static void EnsureAutoSize(ScrollRect scrollRect, bool keepVerticalScrollbarVisible)
    {
        var autoSize = scrollRect.GetComponent<AutoSizeSubtitleScrollContent>();
        if (autoSize == null)
            autoSize = Undo.AddComponent<AutoSizeSubtitleScrollContent>(scrollRect.gameObject);

        var serializedAutoSize = new SerializedObject(autoSize);
        var keepScrollbarProperty = serializedAutoSize.FindProperty("keepVerticalScrollbarVisible");
        if (keepScrollbarProperty != null)
        {
            keepScrollbarProperty.boolValue = keepVerticalScrollbarVisible;
            serializedAutoSize.ApplyModifiedProperties();
        }

        autoSize.SetKeepVerticalScrollbarVisible(keepVerticalScrollbarVisible);
        EditorUtility.SetDirty(autoSize);
    }

    private static Transform FindNotificationsRoot()
    {
        foreach (var popup in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (!popup.name.Equals("Popups", StringComparison.OrdinalIgnoreCase))
                continue;

            var notifications = FindDirectChild(popup, "Notifications");
            if (notifications != null)
                return notifications;
        }

        throw new InvalidOperationException("Could not find Popups > Notifications in the active scene.");
    }

    private static Transform FindTemplateNotification(Transform notificationsRoot)
    {
        foreach (var item in notificationsRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!item.name.Equals("Notification (1)", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = GetHierarchyPath(item);
            if (path.IndexOf("Exp 1 Notifications", StringComparison.OrdinalIgnoreCase) >= 0)
                return item;
        }

        throw new InvalidOperationException("Could not find Exp 1 Notifications > Notification (1).");
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private static ScrollRect FindDirectChildScroll(Transform parent)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var scroll = parent.GetChild(i).GetComponent<ScrollRect>();
            if (scroll != null)
                return scroll;
        }

        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        for (var current = transform.parent; current != null; current = current.parent)
            path = current.name + " > " + path;

        return path;
    }
}
