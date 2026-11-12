using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Put on a collider in an authored spline hierarchy when the collider is
    /// below the logical FreeLabWire parent.
    /// </summary>
    public class FreeLabWireClickForwarder : MonoBehaviour
    {
        private bool removalRequested;

        private void OnEnable()
        {
            removalRequested = false;
        }

        private void OnMouseDown()
        {
            RequestRemoval();
        }

        // Some authored spline/collider combinations deliver the click on
        // mouse release instead of mouse down. Support both paths.
        private void OnMouseUpAsButton()
        {
            RequestRemoval();
        }

        private void RequestRemoval()
        {
            if (removalRequested)
                return;

            FreeLabWire wire = GetComponentInParent<FreeLabWire>();

            if (wire != null && wire.RemoveFromFreeLab())
            {
                removalRequested = true;
            }
        }
    }
}
