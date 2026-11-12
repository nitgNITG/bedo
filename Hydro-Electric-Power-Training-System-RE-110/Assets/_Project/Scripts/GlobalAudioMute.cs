using UnityEngine;

public class GlobalAudioMute : MonoBehaviour
{
    [Header("Animation (Optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string mutedAnimation = "Pressed";
    [SerializeField] private string unmutedAnimation = "Released";

    private bool muted;

    private void Start()
    {
        muted = AudioListener.pause;
        UpdateVisual();
    }

    public void ToggleMute()
    {
        muted = !muted;

        AudioListener.pause = muted;

        UpdateVisual();
    }

    public void Mute()
    {
        muted = true;

        AudioListener.pause = true;

        UpdateVisual();
    }

    public void Unmute()
    {
        muted = false;

        AudioListener.pause = false;

        UpdateVisual();
    }


    private void UpdateVisual()
    {
        if (animator == null)
            return;

        if (muted)
        {
            if (!string.IsNullOrEmpty(mutedAnimation))
                animator.Play(mutedAnimation);
        }
        else
        {
            if (!string.IsNullOrEmpty(unmutedAnimation))
                animator.Play(unmutedAnimation);
        }
    }
}