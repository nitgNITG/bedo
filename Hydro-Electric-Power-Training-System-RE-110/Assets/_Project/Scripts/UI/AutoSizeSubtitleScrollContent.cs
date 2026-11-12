using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sizes a subtitle ScrollRect's Content to the actual wrapped TMP text height.
/// Short text has no scroll range; longer text creates only the required range.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(ScrollRect))]
public sealed class AutoSizeSubtitleScrollContent : MonoBehaviour
{
    [SerializeField, Min(0f)] private float verticalPadding = 12f;
    [Tooltip("When enabled, keeps this Scroll Rect's assigned vertical scrollbar visible even when the text fits.")]
    [SerializeField] private bool keepVerticalScrollbarVisible = false;

    [SerializeField] private bool measureAtCurrentFontSize;
    [Header("Optional notification placement")]
    [SerializeField] private RectTransform notificationGroup;
    [SerializeField] private RectTransform instructionGroup;
    [SerializeField, Min(0f)] private float instructionGap = 16f;
    private readonly Vector3[] placementCorners = new Vector3[4];
    private ScrollRect scrollRect;
    private TextMeshProUGUI subtitleText;
    private string previousText;
    private float previousViewportWidth = -1f;

    private void OnEnable()
    {
        CacheReferences();
        RefreshSize(true);
        Canvas.willRenderCanvases += PlaceNotificationAboveInstruction;
    }

    private void OnDisable() => Canvas.willRenderCanvases -= PlaceNotificationAboveInstruction;

    private void PlaceNotificationAboveInstruction()
    {
        if (notificationGroup == null || instructionGroup == null || !notificationGroup.gameObject.activeInHierarchy)
            return;
        var parent = notificationGroup.parent as RectTransform;
        if (parent == null || !TryPanelBounds(instructionGroup, parent, out var instruction) ||
            !TryPanelBounds(notificationGroup, parent, out var notification)) return;

        // Move only the notification group. Measure the panel backgrounds rather
        // than the oversized transparent glow rectangles around their borders.
        Vector3 position = notificationGroup.localPosition;
        position.x += instruction.center.x - notification.center.x;
        position.y += instruction.max.y + instructionGap - notification.min.y;
        notificationGroup.localPosition = position;
    }

    private bool TryPanelBounds(RectTransform group, RectTransform relativeTo, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var graphic in group.GetComponentsInChildren<Graphic>(false))
        {
            string label = graphic.name.Trim();
            if (!graphic.enabled || graphic.color.a <= 0f || label != "Background") continue;
            graphic.rectTransform.GetWorldCorners(placementCorners);
            foreach (var corner in placementCorners)
            {
                Vector3 point = relativeTo.InverseTransformPoint(corner);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return found;
    }

    private void LateUpdate()
    {
        RefreshSize(false);
    }

    private void OnValidate()
    {
        CacheReferences();
        RefreshSize(true);
    }

    /// <summary>Editor setup helper for notification scroll views.</summary>
    public void SetKeepVerticalScrollbarVisible(bool value)
    {
        keepVerticalScrollbarVisible = value;
        CacheReferences();
        ApplyScrollbarVisibility();
    }

    private void CacheReferences()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (scrollRect != null && subtitleText == null)
        {
            subtitleText = scrollRect.content != null
                ? scrollRect.content.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
        }

        ApplyScrollbarVisibility();
    }

    private void ApplyScrollbarVisibility()
    {
        if (!keepVerticalScrollbarVisible || scrollRect == null || scrollRect.verticalScrollbar == null)
            return;

        scrollRect.verticalScrollbar.gameObject.SetActive(true);
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
    }

    private void RefreshSize(bool force)
    {
        if (scrollRect == null || scrollRect.content == null || subtitleText == null)
        {
            return;
        }

        var viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
        if (viewport == null)
        {
            return;
        }

        var viewportWidth = Mathf.Max(1f, viewport.rect.width);
        if (!force && !measureAtCurrentFontSize && previousText == subtitleText.text && Mathf.Approximately(previousViewportWidth, viewportWidth))
        {
            return;
        }

        if (measureAtCurrentFontSize)
        {
            // Stretching anchors alone retains the copied fixed width.
            scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);
            var rect = subtitleText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(-verticalPadding * 2f, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(0f, -verticalPadding);
            subtitleText.enableWordWrapping = true;
        }
        subtitleText.ForceMeshUpdate();
        var availableTextWidth = Mathf.Max(1f, viewportWidth - subtitleText.margin.x - subtitleText.margin.z - (measureAtCurrentFontSize ? verticalPadding * 2f : 0f));
        bool autoSize = subtitleText.enableAutoSizing;
        if (measureAtCurrentFontSize) subtitleText.enableAutoSizing = false;
        var textHeight = subtitleText.GetPreferredValues(subtitleText.text, availableTextWidth, 0f).y;
        if (measureAtCurrentFontSize) subtitleText.enableAutoSizing = autoSize;
        var requiredHeight = Mathf.Max(viewport.rect.height, textHeight + (verticalPadding * 2f));

        var content = scrollRect.content;
        // Objective cards use this component as their height authority.
        // A copied layout fitter must not restore the old, oversized content height.
        if (measureAtCurrentFontSize && Application.isPlaying)
        {
            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            var layout = content.GetComponent<LayoutGroup>();
            if (layout != null) layout.enabled = false;
            var textFitter = subtitleText.GetComponent<ContentSizeFitter>();
            if (textFitter != null) textFitter.enabled = false;
        }
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        if (measureAtCurrentFontSize) content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = new Vector2(0f, Mathf.Clamp(content.anchoredPosition.y, 0f, requiredHeight - viewport.rect.height));
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);

        var textRect = subtitleText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -verticalPadding);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight - (verticalPadding * 2f));

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        previousText = subtitleText.text;
        previousViewportWidth = viewportWidth;
    }
}
