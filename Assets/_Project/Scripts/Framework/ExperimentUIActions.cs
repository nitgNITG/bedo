using UnityEngine;
using TutorialFramework;

public class ExperimentUIActions : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InteractionManager interactionManager;

    private void Awake()
    {
        GetInteractionManager();
    }

    public void GetInteractionManager()
    {
        if (interactionManager == null)
        {
            GameObject manager = GameObject.Find("InteractionManager");

            if (manager != null)
            {
                interactionManager = manager.GetComponent<InteractionManager>();
            }

            if (interactionManager == null)
            {
                Debug.LogError("InteractionManager not found.");
            }
        }
    }

    /// <summary>
    /// Restarts the current experiment.
    /// </summary>
    public void RestartExperiment()
    {
        GetInteractionManager();

        if (interactionManager != null)
        {
            interactionManager.RestartScenario();
        }
    }

    /// <summary>
    /// Returns to the experiment main menu (intro panel).
    /// </summary>
    public void ReturnToMainMenu()
    {
        GetInteractionManager();

        if (interactionManager != null)
        {
            interactionManager.ReturnToExperimentMenu();
        }
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
