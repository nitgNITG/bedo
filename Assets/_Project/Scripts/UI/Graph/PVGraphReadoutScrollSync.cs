using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the DC Voltage and DC Current point lists vertically aligned while
/// either list is dragged or scrolled.
/// </summary>
[DisallowMultipleComponent]
public sealed class PVGraphReadoutScrollSync : MonoBehaviour
{
    [SerializeField] private ScrollRect dcVoltageScroll;
    [SerializeField] private ScrollRect dcCurrentScroll;

    private bool synchronizing;

    public void Configure(ScrollRect voltage, ScrollRect current)
    {
        if (isActiveAndEnabled)
            Unsubscribe();

        dcVoltageScroll = voltage;
        dcCurrentScroll = current;

        if (isActiveAndEnabled)
        {
            Subscribe();
            AlignToFirstRow();
        }
    }

    private void OnEnable()
    {
        Subscribe();
        AlignToFirstRow();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (dcVoltageScroll != null)
            dcVoltageScroll.onValueChanged.AddListener(OnVoltageScrolled);
        if (dcCurrentScroll != null)
            dcCurrentScroll.onValueChanged.AddListener(OnCurrentScrolled);
    }

    private void Unsubscribe()
    {
        if (dcVoltageScroll != null)
            dcVoltageScroll.onValueChanged.RemoveListener(OnVoltageScrolled);
        if (dcCurrentScroll != null)
            dcCurrentScroll.onValueChanged.RemoveListener(OnCurrentScrolled);
    }

    private void OnVoltageScrolled(Vector2 _)
    {
        Sync(dcVoltageScroll, dcCurrentScroll);
    }

    private void OnCurrentScrolled(Vector2 _)
    {
        Sync(dcCurrentScroll, dcVoltageScroll);
    }

    private void Sync(ScrollRect source, ScrollRect destination)
    {
        if (synchronizing || source == null || destination == null ||
            source.content == null || destination.content == null)
            return;

        synchronizing = true;

        // A percentage position can put matching rows a few pixels apart if
        // the two contents have different calculated heights. Copying the
        // real content offset keeps each plotted pair on the same line.
        Vector2 destinationPosition = destination.content.anchoredPosition;
        destinationPosition.y = source.content.anchoredPosition.y;
        destination.content.anchoredPosition = destinationPosition;
        synchronizing = false;
    }

    private void AlignToFirstRow()
    {
        if (dcVoltageScroll == null || dcCurrentScroll == null ||
            dcVoltageScroll.content == null || dcCurrentScroll.content == null)
            return;

        Canvas.ForceUpdateCanvases();
        Vector2 currentPosition = dcCurrentScroll.content.anchoredPosition;
        currentPosition.y = dcVoltageScroll.content.anchoredPosition.y;
        dcCurrentScroll.content.anchoredPosition = currentPosition;
    }
}
