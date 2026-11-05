using UnityEngine;

namespace TutorialFramework
{
    public class ExperimentSelector : MonoBehaviour
    {
        [Header("Experiments")]
        [Tooltip("Assign every experiment's StoryboardSteps here.")]
        [SerializeField]
        private StoryboardSteps[] experiments;


        private void Start()
        {
            RestoreSelectedExperiment();
        }


        // =====================================================
        // SELECT EXPERIMENT
        // =====================================================

        public void SelectExperiment(
            StoryboardSteps selectedExperiment
        )
        {
            if (selectedExperiment == null)
            {
                Debug.LogError(
                    "ExperimentSelector: Selected experiment is null."
                );

                return;
            }


            InteractionManager manager =
                InteractionManager.Instance;


            if (manager == null)
            {
                Debug.LogError(
                    "ExperimentSelector: InteractionManager not found."
                );

                return;
            }


            Debug.Log(
                "SELECTING EXPERIMENT: "
                + selectedExperiment.StoryboardID
            );


            // Tell the persistent manager which experiment
            // was selected.
            manager.SetActiveStoryboard(
                selectedExperiment
            );


            manager.FirstTimeRun = true;


            // The experiment-selection menu is now a separate, lightweight
            // scene.  Keep the selected storyboard name on InteractionManager
            // and move to the full lab once, instead of starting/restarting
            // the experiment inside the menu scene.
            if (manager.IsInMainMenuScene)
            {
                manager.LoadSelectedExperimentScene();
                return;
            }


            // Disable every experiment except the selected one.
            DisableAllExperiments();


            selectedExperiment.gameObject.SetActive(true);


            Debug.Log(
                "Selected experiment enabled: "
                + selectedExperiment.StoryboardID
            );


            // Start the experiment using the existing method.
            selectedExperiment.StartExperimentFromIntro();
        }


        // =====================================================
        // DISABLE ALL EXPERIMENTS
        // =====================================================

        private void DisableAllExperiments()
        {
            if (experiments == null)
                return;


            foreach (
                StoryboardSteps experiment
                in experiments)
            {
                if (experiment == null)
                    continue;


                experiment.gameObject.SetActive(false);
            }
        }


        // =====================================================
        // RESTORE AFTER SCENE RELOAD
        // =====================================================

        private void RestoreSelectedExperiment()
        {
            InteractionManager manager =
                InteractionManager.Instance;


            if (manager == null)
                return;

            if (manager.Free && FreeLabWindController.Instance != null)
            {
                DisableAllExperiments();
                // Circuit detection chooses the live monitor, while the menu's
                // selected experiment continues to own the title and instructions.
                if (experiments != null)
                    foreach (var experiment in experiments)
                    {
                        if (experiment == null) continue;
                        bool selected = experiment.StoryboardID == manager.ActiveStoryboardName;
                        experiment.SetSelectedExperimentObjectActive(selected);
                        if (selected) experiment.SetExperimentName(manager.ExperimentName);
                    }
                FreeLabManager.GetOrCreate().InitializeFreeLab();
                return;
            }


            if (!manager.HasActiveStoryboard())
            {
                Debug.Log(
                    "No experiment selected. "
                    + "Leaving experiments disabled."
                );

                return;
            }


            string selectedID =
                manager.ActiveStoryboardName;


            Debug.Log(
                "RESTORING EXPERIMENT: "
                + selectedID
            );


            StoryboardSteps selectedExperiment = null;


            if (experiments != null)
            {
                foreach (
                    StoryboardSteps experiment
                    in experiments)
                {
                    if (experiment == null)
                        continue;


                    if (experiment.StoryboardID ==
                        selectedID)
                    {
                        selectedExperiment =
                            experiment;

                        break;
                    }
                }
            }


            // First disable everything.
            DisableAllExperiments();


            if (selectedExperiment == null)
            {
                Debug.LogWarning(
                    "Could not find experiment with ID: "
                    + selectedID
                );

                return;
            }


            // Enable only the selected experiment.
            selectedExperiment.gameObject.SetActive(true);


            Debug.Log(
                "Restored experiment: "
                + selectedExperiment.StoryboardID
            );
        }
    }
}
