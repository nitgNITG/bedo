using System;
using System.Linq;
using UnityEngine;
using TutorialFramework;

/// <summary>Scene bindings used only by the new hybrid guided experiment.</summary>
[DefaultExecutionOrder(220)]
public sealed class HybridGuidedBindings : MonoBehaviour
{
    public StoryboardSteps board;
    public HybridTrainingController system;
    public GameObject[] oldScreens;
    public GameObject notesGroup, subtitleGroup, monitors, graphPage;
    public Interaction graphButton,systemButton;
    public GameObject[] graphReadouts=Array.Empty<GameObject>();
    [HideInInspector] public bool alternatingWireClicksApplied;
    public WindMonitorTabs tabs;
    public Collider speedCollider;
    public CanvasGroup speedPopup;
    public Renderer[] resetCovers;
    public Renderer[] replacedVisuals;
    public Renderer[] powerOnIndicators=Array.Empty<Renderer>();
    public SingleClickInteraction speedWait;
    private bool emptyGraphInitialized;
    private WireInteraction[] wires;
    private SingleClickInteraction[] singles;
    public void Initialize()
    {
        emptyGraphInitialized=false;
        wires=board.GetComponentsInChildren<WireInteraction>(true);
        singles=board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().ToArray();
        foreach(var w in wires){w.wireObject.SetActive(false);w.plugA.SetActive(false);w.plugB.SetActive(false);w.ResetInteraction();}
        foreach(var s in singles)if(s.marker)s.marker.SetActive(false);
        foreach(var cover in resetCovers)if(cover)cover.enabled=true;
        foreach(var indicator in powerOnIndicators)if(indicator)indicator.enabled=false;
        foreach(var visual in replacedVisuals)if(visual)visual.enabled=false;
        if(speedCollider)speedCollider.enabled=false;
        if(speedPopup){speedPopup.alpha=0;speedPopup.interactable=false;speedPopup.blocksRaycasts=false;}
        if(notesGroup)notesGroup.SetActive(true);
        if(subtitleGroup)subtitleGroup.SetActive(true);
        foreach(var screen in oldScreens)if(screen)screen.SetActive(false);
        if(graphPage)graphPage.SetActive(false);
        foreach(var readout in graphReadouts)if(readout)readout.SetActive(system.recordGraphMeasurements);
        if(system.monitor)system.monitor.SetActive(true);
        system.ResetExperiment();
    }
    public void ShowSpeed()
    {
        if(speedCollider)speedCollider.enabled=true;
        if(speedPopup){speedPopup.alpha=1;speedPopup.interactable=true;speedPopup.blocksRaycasts=true;}
    }
    public void CompleteSpeed()
    {
        if(!board||!speedWait||!board.IsCurrentGuidedInteraction(speedWait)||speedWait.IsCompleted)return;
        if(speedWait.marker)speedWait.marker.SetActive(false);
        speedWait.ForceComplete();
    }
    public void ShowSystem()
    {
        if(speedPopup){speedPopup.alpha=0;speedPopup.interactable=false;speedPopup.blocksRaycasts=false;}
        if(tabs&&graphPage&&graphPage.activeSelf)tabs.ShowSystem();
        if(graphPage)graphPage.SetActive(false);
        if(monitors)monitors.SetActive(system.MainPowered);
        if(system.monitor)system.monitor.SetActive(true);
    }
    private void OnDisable(){emptyGraphInitialized=false;foreach(var readout in graphReadouts)if(readout)readout.SetActive(true);if(replacedVisuals!=null)foreach(var visual in replacedVisuals)if(visual)visual.enabled=true;}
    private void LateUpdate()
    {
        if(!InteractionManager.Instance||!InteractionManager.Instance.Guided||!InteractionManager.Instance.IsExperimentActive(board))return;
        if(!emptyGraphInitialized&&system.graph){system.graph.ShowVoltageGraph();emptyGraphInitialized=true;}
        foreach(var readout in graphReadouts)if(readout&&readout.activeSelf!=system.recordGraphMeasurements)readout.SetActive(system.recordGraphMeasurements);
        // Selection can toggle the experiment root while changing pages. Keep
        // stationary hardware covers hidden for as long as their guided copies own them.
        foreach(var visual in replacedVisuals)if(visual&&visual.enabled)visual.enabled=false;
        if(wires==null)wires=board.GetComponentsInChildren<WireInteraction>(true);
        if(singles==null)singles=board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().ToArray();
        foreach(var w in wires)foreach(var point in new[]{w.pointA,w.pointB})foreach(var c in point.GetComponents<Collider>())c.enabled=w.CanClickPoint(point);
        foreach(var s in singles)if(s.interaction)foreach(var c in s.interaction.GetComponents<Collider>())c.enabled=s.interaction.Allow&&board.IsCurrentGuidedInteraction(s)&&!s.IsCompleted;
        if(!system.MainPowered){if(monitors)monitors.SetActive(false);if(graphPage)graphPage.SetActive(false);}
        else if(monitors)monitors.SetActive(!graphPage||!graphPage.activeSelf);
        if(graphButton)graphButton.Allow=system.MainPowered&&graphPage&&!graphPage.activeSelf;
        if(systemButton)systemButton.Allow=system.MainPowered&&graphPage&&graphPage.activeSelf;
    }
}
