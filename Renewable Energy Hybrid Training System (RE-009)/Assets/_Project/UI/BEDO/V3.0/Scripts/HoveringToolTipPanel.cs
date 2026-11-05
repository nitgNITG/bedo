using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using TMPro;
using UnityEngine.InputSystem;

namespace Bedo.Recovered.UI
{
public class HoveringToolTipPanel : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If false, component references will be found at runtime via hierarchy paths.")]
    [SerializeField] private bool useCustomResources = false;
    [SerializeField] private bool sharpAnimations = false;

    [Header("Resources")]
    [SerializeField] private Transform tooltipWindow;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] Animator mWindowAnimator;

    [Header("Events & Data")]
    [Tooltip("Event to listen for and raise tooltip descriptions.")]
    [SerializeField] private ScriptableEventTooltipComponentDescription OnTooltipComponentDescription;

    [Tooltip("Event to listen for and raise tooltip descriptions.")]
    [SerializeField] private BoolVariable IsToolTipAllowedToShow;

    [Tooltip("BoolVariable tracking whether the tooltip is currently shown.")]
    [SerializeField] private BoolVariable IsToolTipShown;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        canvasGroup = GetComponent<CanvasGroup>();
        if (!useCustomResources)
            FindReferences();

        //// Always keep panel active; hide via canvas group
        //if (canvasGroup != null)
        //{
        //    canvasGroup.alpha = 0f;
        //    canvasGroup.blocksRaycasts = false;
        //}

        if (IsToolTipShown != null)
            IsToolTipShown.Value = false;
    }

    private void OnEnable()
    {
        OnTooltipComponentDescription.OnRaised += HandleTooltipActivated;

        IsToolTipShown.OnValueChanged += IsToolTipShown_OnValueChanged;
    }



    private void OnDisable()
    {
        OnTooltipComponentDescription.OnRaised -= HandleTooltipActivated;
        IsToolTipShown.OnValueChanged -= IsToolTipShown_OnValueChanged;
    }

    private void Update()
    {
        // Follow the cursor position using the new Input System
        if (Mouse.current != null)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            transform.position = pos;
        }
    }

    /// <summary>
    /// Finds child UI elements by path if not set in inspector.
    /// </summary>
    private void FindReferences()
    {
        Transform root = transform;
        tooltipWindow = root.Find("Tooltip window");
        if (tooltipWindow == null)
        {
            Debug.LogError("HoveringToolTipPanel: 'Tooltip window' not found.");
            return;
        }

        var title = tooltipWindow.Find("Title");
        if (title != null)
            titleText = title.GetComponent<TextMeshProUGUI>();

        var desc = tooltipWindow.Find("Description");
        if (desc != null)
            descriptionText = desc.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Handles incoming tooltip requests. Hides or shows based on data.
    /// </summary>
    private void HandleTooltipActivated(TooltipComponentDescription data)
    {
        if (data == null || !IsToolTipAllowedToShow.Value)
        {
            IsToolTipShown.Value = false;
            return;
        }

        titleText.text = data.GetTitle();
        descriptionText.text = data.GetDescription();
        IsToolTipShown.Value = true;
    }

    /// <summary>
    /// Shows the tooltip by adjusting canvas group.
    /// </summary>
    private void ShowTooltip()
    {
        ModalWindowIn();
    }

    /// <summary>
    /// Hides the tooltip by adjusting canvas group.
    /// </summary>
    public void HideTooltip()
    {
        ModalWindowOut();
    }


    public void ModalWindowIn()
    {
        StopCoroutine("DisableWindow");
        tooltipWindow.gameObject.SetActive(true);
        if (sharpAnimations == false)
            mWindowAnimator.CrossFade("Window In", 0.02f);
        else
            mWindowAnimator.Play("Window In");
    }

    public void ModalWindowOut()
    {
        if (sharpAnimations == false)
            mWindowAnimator.CrossFade("Window Out", 0.02f);
        else
            mWindowAnimator.Play("Window Out");
        StartCoroutine("DisableWindow");
    }

    IEnumerator DisableWindow()
    {
        yield return new WaitForSeconds(0.5f);
        tooltipWindow.gameObject.SetActive(false);
    }

    private void IsToolTipShown_OnValueChanged(bool val)
    {
        if (val)
        {
            ShowTooltip();
        }
        else
        {
            HideTooltip();
        }
    }
}

}
