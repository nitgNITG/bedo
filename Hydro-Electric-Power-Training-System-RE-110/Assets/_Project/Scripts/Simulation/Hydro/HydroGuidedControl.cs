using TutorialFramework;
using UnityEngine;
using UnityEngine.Events;

namespace Bedo.Hydro
{
    public sealed class HydroGuidedControl : BaseInteraction
    {
        [SerializeField] private Interaction clickTarget;
        [SerializeField] private GameObject marker;
        [SerializeField] private UnityEvent onBegin;
        [SerializeField] private UnityEvent onClick;
        [SerializeField] private bool advanceOnClick = true;

        [SerializeField] private Transform releasedEmergencyVisual;
        private bool emergencyReleased;

        public override void Begin()
        {
            ResetInteraction();
            clickTarget.Enable();
            SetCollider(true);
            if (marker) marker.SetActive(true);
            onBegin?.Invoke();
        }

        public void Click()
        {
            if (IsCompleted)
                return;
            if (releasedEmergencyVisual && !emergencyReleased)
            {
                releasedEmergencyVisual.position += new Vector3(0f, .65f, -.75f).normalized * .18f;
                emergencyReleased = true;
            }
            onClick?.Invoke();
            if (advanceOnClick)
                Finish();
        }

        public void Finish()
        {
            clickTarget.Disable();
            SetCollider(false);
            if (marker) marker.SetActive(false);
            ForceComplete();
        }

        private void Awake() => SetCollider(false);

        private void SetCollider(bool enabled)
        {
            if (TryGetComponent<BoxCollider>(out var hitbox))
                hitbox.enabled = enabled;
        }

        public override void ResetInteraction()
        {
            base.ResetInteraction();
            if (clickTarget) clickTarget.Disable();
            SetCollider(false);
            if (marker) marker.SetActive(false);
        }
    }
}