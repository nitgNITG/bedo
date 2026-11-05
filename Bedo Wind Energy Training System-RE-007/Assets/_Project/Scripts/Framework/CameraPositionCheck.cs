using UnityEngine;

namespace TutorialFramework
{
    public class CameraPositionCheck : BaseInteraction
    {
        [Header("Target Camera Position")]
        public Transform targetPosition;


        [Header("Camera")]
        public Transform playerCamera;


        [Header("Tolerance")]
        public float positionTolerance = 0.5f;
        public float rotationTolerance = 20f;



        public override void Begin()
        {
            ResetInteraction();
        }



        private void Update()
        {
            if (completed)
                return;


            if (targetPosition == null ||
                playerCamera == null)
                return;



            float distance =
                Vector3.Distance(
                    playerCamera.position,
                    targetPosition.position
                );


            float angle =
                Quaternion.Angle(
                    playerCamera.rotation,
                    targetPosition.rotation
                );



            if (distance <= positionTolerance &&
               angle <= rotationTolerance)
            {
                Complete();
            }
        }
    }
}