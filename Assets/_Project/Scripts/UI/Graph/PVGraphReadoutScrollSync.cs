using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Keeps matching plotted rows aligned when any reading column scrolls.</summary>
[DisallowMultipleComponent]
public sealed class PVGraphReadoutScrollSync : MonoBehaviour
{
    [SerializeField] private ScrollRect dcVoltageScroll;
    [SerializeField] private ScrollRect dcCurrentScroll;
    [SerializeField] private ScrollRect additionalScroll;
    private readonly Dictionary<ScrollRect,UnityAction<Vector2>> listeners = new Dictionary<ScrollRect,UnityAction<Vector2>>();
    private bool synchronizing;

    public void Configure(ScrollRect voltage, ScrollRect current, ScrollRect third = null)
    {
        Unsubscribe();
        dcVoltageScroll = voltage; dcCurrentScroll = current; additionalScroll = third;
        if (isActiveAndEnabled) { Subscribe(); AlignToFirstRow(); }
    }
    private void OnEnable() { Subscribe(); AlignToFirstRow(); }
    private void OnDisable() => Unsubscribe();
    private void Subscribe()
    {
        foreach(var scroll in new[]{dcVoltageScroll,dcCurrentScroll,additionalScroll})
        {
            if(!scroll || listeners.ContainsKey(scroll)) continue;
            var source=scroll;
            UnityAction<Vector2> listener = _ => Sync(source);
            listeners.Add(scroll,listener); scroll.onValueChanged.AddListener(listener);
        }
    }
    private void Unsubscribe()
    {
        foreach(var item in listeners) if(item.Key) item.Key.onValueChanged.RemoveListener(item.Value);
        listeners.Clear();
    }
    private void Sync(ScrollRect source)
    {
        if(synchronizing || !source || !source.content) return;
        synchronizing=true;
        foreach(var destination in new[]{dcVoltageScroll,dcCurrentScroll,additionalScroll})
        {
            if(!destination || destination==source || !destination.content) continue;
            var position=destination.content.anchoredPosition;
            position.y=source.content.anchoredPosition.y;
            destination.content.anchoredPosition=position;
        }
        synchronizing=false;
    }
    private void AlignToFirstRow()
    {
        if(!dcVoltageScroll || !dcVoltageScroll.content) return;
        Canvas.ForceUpdateCanvases(); Sync(dcVoltageScroll);
    }
}
