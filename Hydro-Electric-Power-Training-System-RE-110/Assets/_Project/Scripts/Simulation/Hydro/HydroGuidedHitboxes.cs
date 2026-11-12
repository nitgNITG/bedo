using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;

namespace Bedo.Hydro
{
    public sealed class HydroGuidedHitboxes : MonoBehaviour
    {
        [SerializeField] private StoryboardSteps storyboard;
        private readonly Dictionary<Collider, bool> original = new Dictionary<Collider, bool>();
        [SerializeField] private Interaction[] panelTargets;
        [SerializeField] private Interaction monitorGraphButton;
        private Interaction[] targets;

        private void LateUpdate()
        {
            var manager = InteractionManager.Instance;
            if (manager == null || !manager.Guided || !manager.IsExperimentActive(storyboard))
            {
                Restore();
                return;
            }

            if (targets == null)
            {
                var owned = new List<Interaction>(storyboard.GetComponentsInChildren<Interaction>(true));
                if (panelTargets != null)
                    owned.AddRange(panelTargets);
                targets = owned.ToArray();
            }

            foreach (var target in targets)
            {
                if (!target || target == monitorGraphButton)
                    continue;

                var owner = target.GetComponentInParent<BaseInteraction>();
                bool allow = owner != null && storyboard.IsCurrentGuidedInteraction(owner) && target.Allow;
                foreach (var hitbox in target.GetComponents<Collider>())
                {
                    if (!original.ContainsKey(hitbox))
                        original.Add(hitbox, hitbox.enabled);
                    hitbox.enabled = allow;
                }
            }
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            foreach (var entry in original)
                if (entry.Key)
                    entry.Key.enabled = entry.Value;
            original.Clear();
        }
    }
}