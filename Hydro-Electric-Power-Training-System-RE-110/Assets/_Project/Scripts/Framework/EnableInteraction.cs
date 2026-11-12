using UnityEngine;

namespace TutorialFramework
{
    public class EnableInteraction : BaseInteraction
    {
        [Header("Interaction")]
        public Interaction interaction;


        [Header("Guided Marker")]
        public GameObject marker;


        [Header("Object To Enable")]
        public GameObject objectToEnable;



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



            if (objectToEnable != null)
                objectToEnable.SetActive(true);



            if (interaction != null)
                interaction.Disable();


            // INTENTIONALLY NO Complete()
        }



        public override void ResetInteraction()
        {
            base.ResetInteraction();


            if (marker != null)
                marker.SetActive(false);
        }
    }
}