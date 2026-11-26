using TutorialFramework;
using UnityEngine;

// The same renderer-swap and soft-halo presentation as Hydro, bound to Hybrid controls.
[DefaultExecutionOrder(900)]
public sealed class HybridPowerButtonGlow : MonoBehaviour
{
    public StoryboardSteps storyboard;
    public BaseInteraction powerInteraction;
    public FreeLabControl freeControl;
    public Renderer halo;
    public Renderer[] offCovers;
    public Renderer[] onRenderers;
    private int powerStep = -1;
    private void Awake()
    {
        if (storyboard && powerInteraction)
            powerStep = storyboard.steps.FindIndex(s => s.interaction == powerInteraction);
        Refresh();
    }
    private void LateUpdate() => Refresh();
    private void OnDisable() { if (halo) halo.enabled = false; }
    private void Refresh()
    {
        var manager = InteractionManager.Instance;
        bool lit = manager && (manager.Free
            ? freeControl && freeControl.Value
            : manager.Guided && storyboard && manager.IsExperimentActive(storyboard) &&
              powerStep >= 0 && storyboard.CurrentStep > powerStep);
        if (halo) halo.enabled = lit;
        if (onRenderers != null) foreach (var r in onRenderers) if (r) r.enabled = lit;
        if (offCovers != null) foreach (var r in offCovers) if (r) r.enabled = !lit;
    }
}
