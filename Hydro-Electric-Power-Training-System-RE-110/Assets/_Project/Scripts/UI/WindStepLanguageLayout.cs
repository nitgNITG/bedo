using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies the authored language layout without replacing the lesson content.</summary>
public sealed class WindStepLanguageLayout : MonoBehaviour
{
    [SerializeField] private ScrollRect view;
    [SerializeField] private ScrollRect englishTemplate;
    [SerializeField] private ScrollRect arabicTemplate;
    [SerializeField] private RectTransform subtitleNumber;

    private bool appliedArabic;

    private void OnEnable() => Apply();

    private void LateUpdate()
    {
        if (appliedArabic != WindLocalization.Arabic)
            Apply();
    }

    private void Apply()
    {
        appliedArabic = WindLocalization.Arabic;
        var template = appliedArabic ? arabicTemplate : englishTemplate;
        if (template == null || view == null)
            return;

        CopyRect((RectTransform)template.transform, (RectTransform)view.transform);
        CopyRect(template.viewport, view.viewport);

        if (template.verticalScrollbar != null && view.verticalScrollbar != null)
        {
            CopyScrollbarLayout((RectTransform)template.verticalScrollbar.transform,
                (RectTransform)view.verticalScrollbar.transform);
        }

        if (subtitleNumber != null)
        {
            var sourceNumber = template.transform.parent.Find("Index Number") as RectTransform;
            CopyRect(sourceNumber, subtitleNumber);
        }

        // Content size and scroll position remain owned by the live list or subtitle.
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)view.transform);
    }

    private static void CopyScrollbarLayout(RectTransform source, RectTransform target)
    {
        CopyRect(source, target);
        var sourceImage = source.GetComponent<Image>();
        var targetImage = target.GetComponent<Image>();
        if (sourceImage != null && targetImage != null)
        {
            targetImage.sprite = sourceImage.sprite;
            targetImage.color = sourceImage.color;
            targetImage.type = sourceImage.type;
        }

        foreach (Transform child in source)
        {
            if (child is RectTransform sourceRect && target.Find(child.name) is RectTransform targetRect)
                CopyScrollbarLayout(sourceRect, targetRect);
        }
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
            return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
    }
}
