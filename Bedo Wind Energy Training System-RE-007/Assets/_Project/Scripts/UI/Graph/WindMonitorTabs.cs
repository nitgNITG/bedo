using System;
using TutorialFramework;
using UnityEngine;

/// <summary>Switches monitor pages without replaying completed guided interactions.</summary>
public sealed class WindMonitorTabs : MonoBehaviour
{
    [Serializable]
    private class ExperimentTab
    {
        public StoryboardSteps storyboard;
        public Interaction graphButton;
        public SingleClickInteraction firstGraphInteraction;
        public GameObject selectionTab;
    }

    [SerializeField] private GameObject monitors;
    [SerializeField] private GameObject graphPage;
    [SerializeField] private Interaction systemButton;
    [SerializeField] private ExperimentTab[] experiments;
    [SerializeField] private GameObject systemSelectionTab;

    private void Start() => SetPageSelection(false);

    public void SetPageSelection(bool showingGraph)
    {
        if (systemSelectionTab) systemSelectionTab.SetActive(!showingGraph);
        if (experiments == null) return;
        foreach (var entry in experiments)
            if (entry != null && entry.selectionTab) entry.selectionTab.SetActive(showingGraph);
    }
    private ExperimentTab active;
    private bool graphShown;
    private int lastStep = -1;

    private void LateUpdate()
    {
        RefreshExperiment();
        if (active == null) return;
        if (active.graphButton != null) active.graphButton.Allow = !graphShown || IsGraphInstruction;
        if (systemButton != null) systemButton.Allow = graphShown;
    }

    private bool IsGraphInstruction => active != null && active.firstGraphInteraction != null &&
        active.storyboard.IsCurrentGuidedInteraction(active.firstGraphInteraction);

    private void RefreshExperiment()
    {
        var manager = InteractionManager.Instance;
        ExperimentTab next = null;
        if (manager != null && manager.Guided && experiments != null)
            foreach (var entry in experiments)
                if (entry != null && entry.storyboard != null &&
                    entry.storyboard.gameObject.activeInHierarchy && manager.IsExperimentActive(entry.storyboard))
                { next = entry; break; }

        int step = next != null ? next.storyboard.CurrentStep : -1;
        if (next != active || (next != null && step < lastStep))
        {
            if (active != null && active.graphButton != null) active.graphButton.Allow = false;
            active = next;
            graphShown = false;
            if (systemButton != null) systemButton.Allow = false;
            if (next != null)
            {
                monitors.SetActive(true);
                graphPage.SetActive(false);
            }
        }
        lastStep = step;
        if (next != null) SetPageSelection(graphShown);
    }

    public void ShowGraph(Interaction clickedButton)
    {
        if (InteractionManager.Instance != null && InteractionManager.Instance.Free && FreeLabWindController.Instance != null)
        { FreeLabWindController.Instance.ShowGraph(); return; }
        RefreshExperiment();
        if (active == null || clickedButton != active.graphButton) return;
        if (IsGraphInstruction && clickedButton.Allow)
        {
            clickedButton.Click();
            lastStep = active.storyboard.CurrentStep;
        }
        else if (graphShown) return;
        else clickedButton.PlaySound();
        graphShown = true;
        SetPageSelection(true);
        graphPage.SetActive(true);
        monitors.SetActive(false);
        clickedButton.Allow = false;
        if (systemButton != null) systemButton.Allow = true;
    }

    public void ShowSystem()
    {
        if (InteractionManager.Instance != null && InteractionManager.Instance.Free && FreeLabWindController.Instance != null)
        { FreeLabWindController.Instance.ShowSystem(); return; }
        RefreshExperiment();
        if (active == null || !graphShown || systemButton == null) return;
        systemButton.PlaySound();
        monitors.SetActive(true);
        graphPage.SetActive(false);
        graphShown = false;
        SetPageSelection(false);
        systemButton.Allow = false;
        active.graphButton.Allow = true;
    }
}
