using UnityEngine;
using TutorialFramework;

public class PanelToggleButton : MonoBehaviour
{
    public bool IsOpen => panel != null && panel.activeInHierarchy;

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Tooltip("Opening this panel closes the panels controlled by these buttons.")]
    [SerializeField] private PanelToggleButton[] exclusivePeers;

    [Header("Animation (Optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string pressedAnimation;
    [SerializeField] private string releasedAnimation;
    private void Start()
    {
        // Main Menu intentionally opens with its saved experiment-selection
        // page visible.  Do not apply the generic close-on-start rule there.
        if (InteractionManager.Instance != null &&
            InteractionManager.Instance.IsInMainMenuScene)
        {
            return;
        }


        if (panel != null)
            panel.SetActive(false);
    }



    // Call this from the toggle button OnClick
    public void TogglePanel()
    {
        if (panel == null)
            return;


        bool shouldOpen = !panel.activeSelf;
        if (shouldOpen && exclusivePeers != null)
        {
            foreach (PanelToggleButton peer in exclusivePeers)
            {
                if (peer != null && peer != this && peer.panel != panel)
                    peer.ClosePanel();
            }
        }

        SetPanelOpen(shouldOpen);
    }

    private void SetPanelOpen(bool isOpen)
    {
        panel.SetActive(isOpen);


        if (animator != null && animator.isActiveAndEnabled)
        {
            if (isOpen)
            {
                if (!string.IsNullOrEmpty(pressedAnimation))
                    animator.Play(pressedAnimation);
            }
            else
            {
                if (!string.IsNullOrEmpty(releasedAnimation))
                    animator.Play(releasedAnimation);
            }
        }
    }



    // Call this from the X button OnClick
    public void ClosePanel()
    {
        if (panel == null)
            return;


        SetPanelOpen(false);
    }
}
