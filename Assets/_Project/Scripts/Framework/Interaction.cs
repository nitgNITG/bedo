using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    public class Interaction : MonoBehaviour
    {

        public InteractionType interactionType;


        [SerializeField]
        private bool allow;


        public bool Allow
        {
            get => allow;
            set => allow = value;
        }



        public UnityEvent onClicked;
        [SerializeField] private AudioClip interactionSound;
        public void PlaySound() => SharedInteractionClickSound.Play(interactionSound);



        public void Click()
        {
            if (!allow)
                return;

            var manager = InteractionManager.Instance;
            if (manager != null && manager.Guided)
            {
                var wire = GetComponentInParent<WireInteraction>();
                if (wire != null && !wire.CanClickPoint(this)) return;
                // Sandbox endpoints never execute their legacy click events in Guided Mode.
                if (wire == null && GetComponent<FreeLabEndpoint>() != null) return;
            }

            PlaySound();


            // Free Mode has one shared physical lab. It intentionally bypasses
            // StoryboardSteps/freeSteps and routes every action through the
            // central sandbox resolver instead.
            if (InteractionManager.Instance != null &&
                InteractionManager.Instance.Free)
            {
                bool invokeExistingClickEvent;

                if (!FreeLabManager.GetOrCreate().HandleInteraction(
                    this,
                    out invokeExistingClickEvent))
                {
                    return;
                }

                if (invokeExistingClickEvent)
                    onClicked?.Invoke();

                return;
            }


            BaseInteraction baseInteraction = GetComponentInParent<BaseInteraction>();
            StoryboardSteps storyboard = baseInteraction != null
                ? baseInteraction.GetComponentInParent<StoryboardSteps>() : null;
            if (storyboard != null && !storyboard.CanExecuteInteraction(baseInteraction))
                return;

            onClicked?.Invoke();


            InteractionManager.Instance
                .ProcessInteraction(this);
        }


        public void Enable()
        {
            allow = true;
        }



        public void Disable()
        {
            allow = false;
        }

    }

    /// <summary>
    /// Uses the single AudioSource on the active SoundManager object for all
    /// interaction clicks. The source is found only when an interaction is
    /// clicked, then cached until its scene object is destroyed.
    /// </summary>
    internal static class SharedInteractionClickSound
    {
        private const string SoundManagerName = "SoundManager";

        private static AudioSource cachedSource;

        public static void Play(AudioClip overrideClip = null)
        {
            if (cachedSource == null)
            {
                GameObject soundManager = GameObject.Find(SoundManagerName);
                if (soundManager == null)
                    return;

                cachedSource = soundManager.GetComponent<AudioSource>();
            }

            if (cachedSource == null) return;
            AudioClip clip = overrideClip != null ? overrideClip : cachedSource.clip;
            if (clip != null) cachedSource.PlayOneShot(clip);
        }
    }
}
