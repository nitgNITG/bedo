using UnityEngine;

namespace TutorialFramework
{
    public class WireRemoveInteraction : BaseInteraction
    {
        [SerializeField] private GameObject wireObject;


        public override void Begin()
        {
            // Nothing required
        }


        public void RemoveWire()
        {
            if (!completed)
                return;


            if (wireObject != null)
                wireObject.SetActive(false);


            ForceUndo();
        }
    }
}