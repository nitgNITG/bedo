using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    public class WireInteraction : BaseInteraction
    {
        [Header("Connection Points")]
        public Interaction pointA;
        public Interaction pointB;


        [Header("Wire Object")]
        public GameObject wireObject;


        [Header("Plugs")]
        public GameObject plugA;
        public GameObject plugB;


        [Header("Markers")]
        public GameObject markerA;
        public GameObject markerB;


        public UnityEvent onFirstPointClicked;


        private Interaction firstPoint;

        private bool waitingSecond;



        public override void Begin()
        {
            ResetInteraction();


            if (wireObject)
                wireObject.SetActive(false);


            DisablePlugs();


            DisablePoints();


            if (InteractionManager.Instance.Guided)
            {
                pointA.Enable();

                if (markerA)
                    markerA.SetActive(true);
            }
            else
            {
                pointA.Enable();
                pointB.Enable();
            }
        }





        public void PointClicked(Interaction point)
        {
            // If wire already exists, clicking either connector removes it
            if (completed)
            {
                if (point == pointA || point == pointB)
                {
                    RemoveWire();
                }

                return;
            }



            if (!waitingSecond)
            {
                firstPoint = point;

                waitingSecond = true;


                point.Disable();


                EnablePlug(point);



                onFirstPointClicked?.Invoke();



                if (InteractionManager.Instance.Guided)
                {
                    if (firstPoint == pointA)
                    {
                        pointB.Enable();


                        if (markerA)
                            markerA.SetActive(false);


                        if (markerB)
                            markerB.SetActive(true);
                    }
                }
            }
            else
            {
                bool valid;


                if (InteractionManager.Instance.Guided)
                {
                    valid =
                        firstPoint == pointA &&
                        point == pointB;
                }
                else
                {
                    valid =
                        (firstPoint == pointA &&
                         point == pointB)

                        ||

                        (firstPoint == pointB &&
                         point == pointA);
                }



                if (valid)
                {
                    EnablePlug(point);

                    Finish();
                }
            }
        }





        private void Finish()
        {
            // Keep points enabled so they can be clicked to remove the wire
            pointA.Enable();
            pointB.Enable();


            if (markerA)
                markerA.SetActive(false);


            if (markerB)
                markerB.SetActive(false);


            if (wireObject)
                wireObject.SetActive(true);


            waitingSecond = false;


            Complete();
        }





        private void RemoveWire()
        {
            if (wireObject)
                wireObject.SetActive(false);


            DisablePlugs();


            pointA.Enable();
            pointB.Enable();


            waitingSecond = false;


            completed = false;


            Debug.Log("Wire removed: " + name);


            Undo();
        }





        private void EnablePlug(Interaction point)
        {
            if (point == pointA && plugA)
                plugA.SetActive(true);


            if (point == pointB && plugB)
                plugB.SetActive(true);
        }





        private void DisablePlugs()
        {
            if (plugA)
                plugA.SetActive(false);


            if (plugB)
                plugB.SetActive(false);
        }





        private void DisablePoints()
        {
            pointA.Disable();
            pointB.Disable();
        }
    }
}