using UnityEngine;
using TutorialFramework;

namespace Bedo.Hydro
{
    // Uses Wind's halo material; reads existing controls without changing their state.
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class HydroPowerButtonGlow : MonoBehaviour
    {
        public FreeLabControl freeControl;
        public StoryboardSteps[] storyboards;
        public BaseInteraction powerInteraction;
        private MeshRenderer halo;
        private int[] powerSteps;
        private void Awake()
        {
            halo = GetComponent<MeshRenderer>();
            powerSteps = new int[storyboards == null ? 0 : storyboards.Length];
            for (int i = 0; i < powerSteps.Length; i++)
                powerSteps[i] = storyboards[i] ? storyboards[i].steps.FindIndex(s => s.interaction == powerInteraction) : -1;
            Refresh();
        }
        private void OnEnable() { if (halo) Refresh(); }
        private void LateUpdate() { Refresh(); }
        private void Refresh()
        {
            bool on = false;
            var manager = InteractionManager.Instance;
            if (manager && freeControl && manager.Free)
                on = freeControl.Value;
            else if (manager && manager.Guided && powerSteps != null)
                for (int i = 0; i < powerSteps.Length; i++)
                    if (powerSteps[i] >= 0 && storyboards[i] &&
                        manager.IsExperimentActive(storyboards[i]) &&
                        storyboards[i].CurrentStep > powerSteps[i])
                    { on = true; break; }
            halo.enabled = on;
        }
    }
}
