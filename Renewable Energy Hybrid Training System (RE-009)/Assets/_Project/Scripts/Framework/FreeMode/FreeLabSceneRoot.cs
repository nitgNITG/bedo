using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Marks the dedicated physical Free Lab hierarchy. It is inactive during
    /// Guided Mode and is activated by the shared lab manager in Free Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabSceneRoot : MonoBehaviour
    {
        [Tooltip("The physical experiment hierarchy used by this shared Free Lab. Leave empty to use the direct child named 'exp 6'.")]
        [SerializeField] private GameObject labContent;

        private void Start()
        {
            if (InteractionManager.Instance != null &&
                InteractionManager.Instance.Free)
            {
                ActivateForFreeMode();
            }
        }

        public void ActivateForFreeMode()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            GameObject content = labContent;
            if (content == null)
            {
                Transform exp6 = transform.Find("exp 6");
                if (exp6 != null)
                    content = exp6.gameObject;
            }

            // The Free Lab currently uses its separate copy of exp 6 as the
            // shared physical lab. It must be visible independently of the
            // Guided experiment selected before Free Mode was opened.
            if (content != null && !content.activeSelf)
                content.SetActive(true);
        }
    }
}
