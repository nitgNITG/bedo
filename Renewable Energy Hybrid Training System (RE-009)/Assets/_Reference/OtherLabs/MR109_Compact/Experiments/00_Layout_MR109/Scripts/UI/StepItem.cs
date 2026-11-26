using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI IndexTMP;
    [SerializeField] Image NotifyIcon;
    [SerializeField] TextMeshProUGUI DescriptionTMP;
    private RectTransform row;
    private string measuredText;
    private float measuredWidth;
    private float baseRowHeight;
    private float baseDescriptionHeight;
    private float fixedFontSize;
    private bool usesWindLayout;
    private bool rightToLeft;
    private bool refreshLanguageLayout;
    public bool IsDescription(TMP_Text text) => text == DescriptionTMP;

    public void InitItem(string indexStr, string descriptionStr)
    {
        row = (RectTransform)transform;
        usesWindLayout = GetComponentInParent<WindStepLanguageLayout>() != null;
        baseRowHeight = row.rect.height;
        baseDescriptionHeight = DescriptionTMP.rectTransform.rect.height;
        fixedFontSize = DescriptionTMP.enableAutoSizing ? DescriptionTMP.fontSizeMax : DescriptionTMP.fontSize;
        DescriptionTMP.enableAutoSizing = false;
        DescriptionTMP.fontSize = fixedFontSize;

        if (indexStr == "*")
        {
            NotifyIcon.gameObject.SetActive(true);
            IndexTMP.gameObject.SetActive(false);
        }
        else
        {
            NotifyIcon.gameObject.SetActive(false);
            
            IndexTMP.gameObject.SetActive(!string.IsNullOrEmpty(indexStr));

            IndexTMP.text = indexStr;
        }

        DescriptionTMP.text = descriptionStr;
        refreshLanguageLayout = true;
    }


    private static void MirrorPosition(RectTransform rect)
    {
        var position = rect.anchoredPosition;
        position.x = -position.x;
        rect.anchoredPosition = position;
    }

    private void LateUpdate()
    {
        if (row == null) return;

        var description = DescriptionTMP.rectTransform;
        bool useRightSide = usesWindLayout && WindLocalization.Arabic;
        if (rightToLeft != useRightSide)
        {
            rightToLeft = useRightSide;
            refreshLanguageLayout = true;
            MirrorPosition(IndexTMP.rectTransform);
            MirrorPosition(NotifyIcon.rectTransform);
            MirrorPosition(description);
            var margin = DescriptionTMP.margin;
            DescriptionTMP.margin = new Vector4(margin.z, margin.y, margin.x, margin.w);
        }
        if (refreshLanguageLayout)
        {
            refreshLanguageLayout = false;
            WindLocalization.RefreshTextLayout(DescriptionTMP);
        }
        var margins = DescriptionTMP.margin;
        float width = description.rect.width - margins.x - margins.z;
        if (width <= 0f || (DescriptionTMP.text == measuredText && Mathf.Approximately(width, measuredWidth)))
            return;

        measuredText = DescriptionTMP.text;
        measuredWidth = width;
        float textHeight = DescriptionTMP.GetPreferredValues(measuredText, width, Mathf.Infinity).y;
        float extraHeight = Mathf.Max(0f, Mathf.Ceil(textHeight + margins.y + margins.w - baseDescriptionHeight));
        row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseRowHeight + extraHeight);
        description.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseDescriptionHeight + extraHeight);
        LayoutRebuilder.MarkLayoutForRebuild(row);
    }
}
