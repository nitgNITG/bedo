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
    private bool unlocked, graphShown;
    private int lastStep = -1;

    private void LateUpdate()
    {
        RefreshExperiment();
        if (active == null) return;
        if (unlocked) active.graphButton.Allow = !graphShown;
        if (systemButton != null) systemButton.Allow = unlocked && graphShown;
    }

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
            unlocked = next != null && next.firstGraphInteraction == null;
            graphShown = false;
            if (systemButton != null) systemButton.Allow = false;
            if (next != null)
            {
                monitors.SetActive(true);
                graphPage.SetActive(false);
                // Begin() owns the first guided unlock; do not enable it here.
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
        if (active == null || clickedButton != active.graphButton || graphShown) return;
        if (!unlocked)
        {
            if (!clickedButton.Allow) return;
            // The initial click still completes precisely the authored graph step.
            clickedButton.Click();
            unlocked = true;
            lastStep = active.storyboard.CurrentStep;
        }
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
        if (active == null || !unlocked || !graphShown || systemButton == null || !systemButton.Allow) return;
        systemButton.PlaySound();
        monitors.SetActive(true);
        graphPage.SetActive(false);
        graphShown = false;
        SetPageSelection(false);
        systemButton.Allow = false;
        active.graphButton.Allow = true;
    }
}
