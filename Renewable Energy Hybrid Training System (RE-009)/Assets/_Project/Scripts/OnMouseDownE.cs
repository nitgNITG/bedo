using UnityEngine;
using UnityEngine.Events;
using TutorialFramework;

namespace Template
{
    public class OnMouseDownE : MonoBehaviour
    {
        public UnityEvent mouseDown;
        [SerializeField] private AudioClip mouseDownSound;

        private void OnMouseDown()
        {
            if (HybridPVPopupSurface.BlocksWorldClick()) return;
            if (mouseDownSound != null) SharedInteractionClickSound.Play(mouseDownSound);
            mouseDown.Invoke();
        }
    }
}
