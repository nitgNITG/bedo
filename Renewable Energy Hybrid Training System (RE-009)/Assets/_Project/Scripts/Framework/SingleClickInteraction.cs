using UnityEngine;

namespace TutorialFramework
{
    public class SingleClickInteraction : BaseInteraction
    {
        [Header("Interaction")]
        public Interaction interaction;


        [Header("Guided Marker")]
        public GameObject marker;

        [Header("Option")]
        public bool IsNextStep = true;


        [Header("Animation (Optional)")]
        public Animator animator;

        public string animationName;



        private void Awake()
        {
            if (interaction == null)
                interaction = GetComponent<Interaction>();
        }



        public override void Begin()
        {
            ResetInteraction();


            if (interaction != null)
                interaction.Enable();


            if (marker != null)
            {
                marker.SetActive(
                    InteractionManager.Instance.Guided
                );
            }
        }



        public void Execute()
        {
            if (completed)
                return;



            if (marker != null)
                marker.SetActive(false);



            if (animator != null &&
                !string.IsNullOrEmpty(animationName))
            {
                animator.Play(animationName);
            }



            if (InteractionManager.Instance.Guided)
            {
                if (interaction != null)
                    interaction.Disable();
            }


            if (IsNextStep) {
                Complete();

            }

        }



        public override void ResetInteraction()
        {
            base.ResetInteraction();


            if (marker != null)
                marker.SetActive(false);
        }
    }
}