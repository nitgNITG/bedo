using TutorialFramework;
using UnityEngine;

namespace Bedo.Hydro
{
    // The graph page dispatches its tabs and Clear to the controller that owns
    // the displayed Guided or Free Mode readings.
    public sealed class HydroGraphAutoOpen : MonoBehaviour
    {
        [SerializeField] private HydroGuidedMonitor monitor;
        [SerializeField] private HydroFreeLabMonitor freeLabMonitor;
        [SerializeField] private int freePageIndex;
        private HydroGuidedMonitor ActiveMonitor =>
            InteractionManager.Instance && InteractionManager.Instance.Free && freeLabMonitor
                ? freeLabMonitor.pages[freePageIndex].chart : monitor;

        private void OnEnable() { if (ActiveMonitor) ActiveMonitor.OpenGraph(); }
        public void ShowVoltageGraph() { if (ActiveMonitor) ActiveMonitor.ShowGraph(); }
        public void ShowCurrentGraph() { if (ActiveMonitor) ActiveMonitor.ShowCurrentGraph(); }
        public void ClearReadings() { if (ActiveMonitor) ActiveMonitor.ClearReadings(); }
    }
}
