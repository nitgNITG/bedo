using UnityEngine;
namespace TutorialFramework
{
    public sealed class HybridPVPopup : MonoBehaviour
    {
        public HybridPVInputs inputs;
        public GameObject popup;
        public void Toggle() { if (inputs) inputs.TogglePopup(popup); }
    }
}
