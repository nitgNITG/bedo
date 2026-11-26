using System;
using TMPro;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>Connects the PV lab's authored rotators to the Hybrid array inputs.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class HybridPVInputs : MonoBehaviour
    {
        public HybridTrainingController guided, free;
        public PivotValueRotator[] dimming, tilt, azimuth, sun;
        public Interaction[] interactions;
        public GameObject[] popups;
        private bool synchronizing;
        private Action<int>[][] handlers;
        private PivotValueRotator[][] groups;

        private void Start()
        {
            groups = new[] { dimming, tilt, azimuth, sun };
            handlers = new Action<int>[groups.Length][];
            for (int g = 0; g < groups.Length; g++)
            {
                handlers[g] = new Action<int>[groups[g].Length];
                for (int i = 0; i < groups[g].Length; i++)
                {
                    int group = g, index = i;
                    handlers[g][i] = value => Changed(group, index);
                    if (groups[g][i]) groups[g][i].ValueChanged += handlers[g][i];
                }
            }
            ApplyInputs();
        }

        private void Changed(int group, int index)
        {
            if (synchronizing) return;
            synchronizing = true;
            int value = groups[group][index].CurrentValue;
            foreach (var rotator in groups[group])
            {
                if (!rotator) continue;
                int steps = Mathf.Abs(rotator.CurrentValue - value);
                for (int step = 0; step < steps; step++)
                {
                    if (rotator.CurrentValue < value) rotator.IncreaseValue();
                    else if (rotator.CurrentValue > value) rotator.DecreaseValue();
                }
            }
            synchronizing = false;
            ApplyInputs();
        }

        private void ApplyInputs()
        {
            // The PV reference uses internal signed steps; ReportedValue is absolute.
            // Keep signed direction here for cos(tilt - azimuth).
            float intensity = dimming[0].CurrentValue * .1f;
            float panelTilt = tilt[0].CurrentValue * 15f;
            float direction = azimuth[0].CurrentValue * 10f;
            float sunDirection = sun[0].CurrentValue * 10f;
            foreach (var system in new[] { guided, free })
            {
                if (!system) continue;
                system.irradiance = intensity;
                system.tilt = panelTilt;
                system.azimuth = direction;
                system.sun = sunDirection;
                if (system.isActiveAndEnabled) system.Refresh();
            }
        }

        public void TogglePopup(GameObject popup)
        {
            bool opening = popup && !popup.activeSelf;
            // Use the assigned Hybrid camera rather than the PV scene's side views.
            if (opening)
            {
                var canvas = popup.GetComponent<Canvas>();
                if (canvas && canvas.worldCamera) popup.transform.rotation = canvas.worldCamera.transform.rotation;
            }
            foreach (var other in popups) if (other) other.SetActive(opening && other == popup);
        }

        private void LateUpdate()
        {
            bool available = InteractionManager.Instance &&
                (InteractionManager.Instance.Guided || InteractionManager.Instance.Free);
            foreach (var interaction in interactions) if (interaction) interaction.Allow = available;
            if (!available) foreach (var popup in popups) if (popup) popup.SetActive(false);
        }

        private void OnDestroy()
        {
            if (groups == null || handlers == null) return;
            for (int g = 0; g < groups.Length; g++)
                for (int i = 0; i < groups[g].Length; i++)
                    if (groups[g][i]) groups[g][i].ValueChanged -= handlers[g][i];
        }
    }
}
