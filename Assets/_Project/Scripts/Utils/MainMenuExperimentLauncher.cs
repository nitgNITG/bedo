using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight Main Menu replacement for the old scene-local experiment
/// button events.  It keeps only an experiment ID and display name, then asks
/// the persistent InteractionManager to load the complete PV Lab scene.
/// </summary>
[RequireComponent(typeof(Button))]
public class MainMenuExperimentLauncher : MonoBehaviour
{
    [SerializeField] private string storyboardId;
    [SerializeField] private string experimentName;


    public void Configure(string id, string displayName)
    {
        storyboardId = id;
        experimentName = displayName;
    }


    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Launch);
    }


    private void Launch()
    {
        InteractionManager manager = InteractionManager.Instance;

        if (manager == null || string.IsNullOrWhiteSpace(storyboardId))
            return;


        manager.SetActiveStoryboard(storyboardId);
        manager.FirstTimeRun = true;


        if (!string.IsNullOrWhiteSpace(experimentName))
            manager.SetExperimentName(experimentName);


        manager.LoadSelectedExperimentScene();
    }
}
