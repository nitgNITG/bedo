using System.Collections;
using System.Collections.Generic;
using TMPro;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;


namespace TutorialFramework
{
    public class StoryboardSteps : MonoBehaviour
    {
        // =====================================================
        // EXPERIMENT ID
        // =====================================================

        [Header("Experiment")]
        [Tooltip(
            "Unique name used by InteractionManager to identify "
            + "this experiment."
        )]
        [SerializeField]
        private string storyboardID = "";

        [Header("Selected Experiment Object")]
        [Tooltip("Optional object kept on while this experiment is the selected experiment, in Guided or Free Mode.")]
        [SerializeField] private GameObject selectedExperimentObject;


        /*
         * Public read-only access for InteractionManager.
         */

        public string StoryboardID
        {
            get
            {
                if (string.IsNullOrWhiteSpace(storyboardID))
                {
                    return gameObject.name;
                }

                return storyboardID;
            }
        }


        // =====================================================
        // GUIDED STEP
        // =====================================================

        [System.Serializable]
        public class Step
        {
            [Header("Step Type")]
            public StepType stepType =
                StepType.Interaction;


            [Header("Interaction")]
            public BaseInteraction interaction;


            [Header("Delay Step")]
            public float delayDuration = 0f;


            [Header("Guided UI")]
            public GameObject uiObject;


            [Header("Notes")]
            public GameObject notesObject;


            [Header("Audio")]
            public AudioClip audioClip;


            [Header("Events")]
            public UnityEvent onStepStart;
            public UnityEvent onStepEnd;
        }


        // =====================================================
        // FREE MODE STRUCTURE
        // =====================================================

        [System.Serializable]
        public class FreeStep
        {
            public enum CompletionMode
            {
                InteractionBased,
                ManualAdvance,
                Delay
            }


            [Header("Completion")]
            public CompletionMode completionMode =
                CompletionMode.InteractionBased;


            [Header("Delay Settings")]
            public float delayDuration = 3f;


            [Header("Required Interactions")]
            public List<BaseInteraction> interactions =
                new List<BaseInteraction>();


            [Header("Wrong Interaction Messages")]
            public List<WrongInteractionMessage>
                wrongInteractions =
                new List<WrongInteractionMessage>();


            [Header("Events")]
            public UnityEvent onStepStart;
            public UnityEvent onStepEnd;
        }


        [System.Serializable]
        public class WrongInteractionMessage
        {
            public BaseInteraction interaction;


            [TextArea(2, 5)]
            public string warningMessage;
        }


        // =====================================================
        // GUIDED MODE STEPS
        // =====================================================

        [Header("Guided Storyboard")]
        public List<Step> steps =
            new List<Step>();


        // =====================================================
        // FREE MODE STEPS
        // =====================================================

        [Header("Free Mode Storyboard")]
        public List<FreeStep> freeSteps =
            new List<FreeStep>();


        [Header("Audio")]
        public AudioSource audioSource;


        [Header("Settings")]
        [Tooltip(
            "Delay between finishing one step and starting the next."
        )]
        public float delayBetweenSteps = 0f;


        // =====================================================
        // RUNTIME GUIDED
        // =====================================================

        [Header("Runtime Guided")]
        [SerializeField]
        private int currentStep = -1;


        public int CurrentStep =>
            currentStep;


        // =====================================================
        // FREE MODE WARNING
        // =====================================================

        [Header("Free Mode Warning")]
        [SerializeField]
        private GameObject warningObject;


        [SerializeField]
        private TMP_Text warningText;


        [SerializeField]
        private float warningDuration = 3f;


        private Coroutine warningRoutine;


        private List<UnityAction>
            activeWarningListeners =
            new List<UnityAction>();


        // =====================================================
        // FREE MODE RUNTIME
        // =====================================================

        private int currentFreeStep = 0;


        // =====================================================
        // SETTING
        // =====================================================

        [Header("Setting")]
        [SerializeField]
        private bool RunOnStart = true;


        public GameObject HideBackgroundScreen;


        // =====================================================
        // START FROM INTRO OBJECTS
        // =====================================================

        [Header("Start From Intro Objects")]
        [Tooltip(
            "Objects belonging to THIS experiment that should "
            + "be enabled when this experiment is selected."
        )]
        [SerializeField]
        private List<GameObject> startFromIntroObjects =
            new List<GameObject>();


        // =====================================================
        // EXPERIMENT TITLE
        // =====================================================

        [Header("Experiment Title")]
        [SerializeField]
        private TMP_Text experimentTitleText;


        // =====================================================
        // INTRO
        // =====================================================

        [Header("Intro")]
        [SerializeField]
        private GameObject introPanel;


        [SerializeField]
        public InteractionManager interactionManager;


        // =====================================================
        // MODE OBJECTS
        // =====================================================

        [Header("Mode Objects")]
        [SerializeField]
        private GameObject[] guidedOnlyObjects;


        [SerializeField]
        private GameObject[] freeOnlyObjects;


        // =====================================================
        // START
        // =====================================================

        private void Start()
        {
            interactionManager =
                GameObject.Find("InteractionManager")
                .GetComponent<InteractionManager>();


            UpdateExperimentTitle();

            // The active storyboard name survives a Guided/Free scene reload.
            // Each storyboard restores only its own selected-experiment object.
            SetSelectedExperimentObjectActive(
                interactionManager != null && interactionManager.IsExperimentActive(this));


            UpdateModeObjects();


            // =====================================================
            // DEBUG ACTIVE STORYBOARD ID
            // =====================================================

            Debug.Log(
                "===== STORYBOARD START CHECK =====\n" +
                "Storyboard GameObject: " + gameObject.name + "\n" +
                "StoryboardID: " + StoryboardID + "\n" +
                "Manager ActiveStoryboardName: " +
                    interactionManager.ActiveStoryboardName + "\n" +
                "IDs Match: " +
                    interactionManager.IsExperimentActive(this) + "\n" +
                "FirstTimeRun: " +
                    interactionManager.FirstTimeRun + "\n" +
                "=================================="
            );


            if (!interactionManager.FirstTimeRun)
            {
                if (introPanel != null)
                {
                    introPanel.SetActive(true);


                    if (HideBackgroundScreen != null)
                        HideBackgroundScreen.SetActive(true);
                }


                return;
            }


            // =====================================================
            // ONLY START THE ACTIVE EXPERIMENT
            // =====================================================

            if (!interactionManager.IsExperimentActive(this))
            {
                Debug.Log(
                    "Storyboard NOT started because it is not active: "
                    + StoryboardID
                );

                return;
            }


            StartStoryboard();
        }        // =====================================================
        // SELECT EXPERIMENT
        // =====================================================

        /*
         * This is what you connect the experiment button to.
         *
         * Button
         *   -> OnClick
         *   -> StoryboardSteps
         *   -> SelectExperiment()
         *
         * It tells InteractionManager which experiment
         * has priority.
         */

        public void SelectExperiment()
        {
            if (interactionManager == null)
            {
                interactionManager =
                    InteractionManager.Instance;
            }


            if (interactionManager == null)
            {
                Debug.LogError(
                    "InteractionManager not found."
                );

                return;
            }


            Debug.Log(
                "Selecting experiment: "
                + StoryboardID
            );


            interactionManager.SetActiveStoryboard(
                this
            );
        }


        // =====================================================
        // CAN EXECUTE INTERACTION
        // =====================================================

        public bool IsCurrentGuidedInteraction(BaseInteraction interaction)
        {
            return currentStep >= 0 && currentStep < steps.Count &&
                steps[currentStep] != null && steps[currentStep].stepType == StepType.Interaction &&
                steps[currentStep].interaction == interaction;
        }

        public bool CanExecuteInteraction(
            BaseInteraction interaction
        )
        {
            // Reusable single controls keep their authored Allow behavior. Wires
            // must belong to the current guided step, not merely an enabled banana.
            if (InteractionManager.Instance.Guided)
                return !(interaction is WireInteraction) || IsCurrentGuidedInteraction(interaction);


            if (currentFreeStep >= freeSteps.Count)
                return false;


            FreeStep step =
                freeSteps[currentFreeStep];


            // Correct interaction for this step
            if (step.interactions.Contains(
                interaction))
            {
                return true;
            }


            // Check if it has a custom warning
            foreach (
                WrongInteractionMessage wrong
                in step.wrongInteractions)
            {
                if (wrong.interaction == interaction)
                {
                    ShowWarning(
                        wrong.warningMessage
                    );

                    return false;
                }
            }


            // Generic wrong interaction
            ShowWarning(
                "This action cannot be performed now."
            );


            return false;
        }


        // =====================================================
        // INTRO
        // =====================================================

        public void StartExperimentFromIntro()
        {
            if (interactionManager == null)
            {
                interactionManager =
                    InteractionManager.Instance;
            }


            if (interactionManager == null)
            {
                Debug.LogError(
                    "InteractionManager not found."
                );

                return;
            }


            Debug.Log(
                "Starting experiment from intro: "
                + StoryboardID
            );


            // =================================================
            // ENABLE ONLY THIS EXPERIMENT'S OBJECTS
            // =================================================

            EnableStartFromIntroObjects();


            // =================================================
            // CHECK IF AN EXPERIMENT IS ALREADY ACTIVE
            // =================================================

            if (interactionManager.HasActiveStoryboard())
            {
                Debug.Log(
                    "An experiment is already active. "
                    + "Switching to: "
                    + StoryboardID
                );


                // Make THIS storyboard the active experiment
                interactionManager.SetActiveStoryboard(
                    this
                );


                // Restart the scenario so the selected
                // experiment starts from a clean state.
                interactionManager.RestartScenario();


                return;
            }


            // =================================================
            // NORMAL FIRST EXPERIMENT START
            // =================================================

            interactionManager.SetActiveStoryboard(
                this
            );


            interactionManager.FirstTimeRun = true;


            if (introPanel != null)
                introPanel.SetActive(false);


            if (HideBackgroundScreen != null)
                HideBackgroundScreen.SetActive(false);


            StartStoryboard();
        }

        /// <summary>
        /// Called by InteractionManager whenever the selected experiment changes.
        /// This is deliberately independent of Guided/Free mode.
        /// </summary>
        public void SetSelectedExperimentObjectActive(bool active)
        {
            if (selectedExperimentObject != null)
                selectedExperimentObject.SetActive(active);
        }


        // =====================================================
        // ENABLE THIS EXPERIMENT'S START OBJECTS
        // =====================================================

        private void EnableStartFromIntroObjects()
        {
            if (startFromIntroObjects == null)
                return;


            foreach (GameObject obj in startFromIntroObjects)
            {
                if (obj == null)
                    continue;


                obj.SetActive(true);


                Debug.Log(
                    "Enabled experiment object: "
                    + obj.name
                );
            }
        }


        // =====================================================
        // EXPERIMENT NAME
        // =====================================================

        public void SetExperimentName(string experimentName)
        {
            // Menu buttons survive scene reloads through their scene-local
            // storyboard; always use the persistent manager for the title.
            interactionManager = InteractionManager.Instance;
            if (interactionManager == null)
                return;


            interactionManager.SetExperimentName(
                experimentName
            );

            UpdateExperimentTitle();
        }


        // =====================================================
        // UPDATE EXPERIMENT TITLE
        // =====================================================

        private void UpdateExperimentTitle()
        {
            if (interactionManager == null)
                return;


            if (experimentTitleText != null)
            {
                experimentTitleText.text =
                    interactionManager.ExperimentName;
            }
        }


        // =====================================================
        // START STORYBOARD
        // =====================================================

        public void StartStoryboard()
        {
            currentStep = -1;


            if (InteractionManager.Instance.Guided)
            {
                GoNextStep();
            }
            else
            {
                // Free Mode is a shared lab, not this storyboard's old
                // sequential freeSteps list. Guided Mode remains unchanged.
                FreeLabManager.GetOrCreate().InitializeFreeLab();
            }
        }


        // =====================================================
        // CURRENT FREE INTERACTION
        // =====================================================

        public bool IsCurrentStepInteraction(
            BaseInteraction interaction
        )
        {
            if (currentFreeStep >= freeSteps.Count)
                return false;


            return freeSteps[currentFreeStep]
                .interactions
                .Contains(interaction);
        }


        // =====================================================
        // WARNING
        // =====================================================

        public bool TryGetWarning(
            BaseInteraction interaction,
            out string message
        )
        {
            message = "";


            if (currentFreeStep >= freeSteps.Count)
                return false;


            foreach (
                WrongInteractionMessage wrong
                in freeSteps[currentFreeStep]
                    .wrongInteractions)
            {
                if (wrong.interaction == interaction)
                {
                    message =
                        wrong.warningMessage;

                    return true;
                }
            }


            return false;
        }


        // =====================================================
        // FREE MODE INITIALIZATION
        // =====================================================

        private void StartFreeMode()
        {
            currentFreeStep = 0;


            DisableAllFreeInteractions();


            EnableCurrentFreeStepInteractions();


            Debug.Log(
                "Free Mode Started"
            );


            StartFreeStep();
        }


        // =====================================================
        // START FREE STEP
        // =====================================================

        private void StartFreeStep()
        {
            ClearWarningListeners();


            if (currentFreeStep >= freeSteps.Count)
            {
                Debug.Log(
                    "Free Mode Finished"
                );

                return;
            }


            FreeStep step =
                freeSteps[currentFreeStep];


            step.onStepStart?.Invoke();


            Debug.Log(
                "Starting Free Step: "
                + currentFreeStep
            );


            foreach (
                BaseInteraction interaction
                in step.interactions)
            {
                if (interaction == null)
                    continue;


                interaction.onCompleted
                    .RemoveListener(
                        () => FreeInteractionCompleted(
                            interaction)
                    );


                interaction.onCompleted
                    .AddListener(
                        () => FreeInteractionCompleted(
                            interaction)
                    );


                interaction.onUndone
                    .RemoveListener(
                        () => FreeInteractionUndone(
                            interaction)
                    );


                interaction.onUndone
                    .AddListener(
                        () => FreeInteractionUndone(
                            interaction)
                    );


                interaction.Begin();
            }


            // Delay step
            if (step.completionMode ==
                FreeStep.CompletionMode.Delay)
            {
                StartCoroutine(
                    DelayFreeStep(
                        step.delayDuration
                    )
                );
            }


            // Wrong interaction warnings
            foreach (
                WrongInteractionMessage wrong
                in step.wrongInteractions)
            {
                if (wrong.interaction == null)
                    continue;


                UnityAction warningAction =
                    () => ShowWarning(
                        wrong.warningMessage
                    );


                wrong.interaction
                    .onCompleted
                    .AddListener(
                        warningAction
                    );


                activeWarningListeners
                    .Add(warningAction);
            }
        }


        // =====================================================
        // CLEAR WARNING LISTENERS
        // =====================================================

        private void ClearWarningListeners()
        {
            if (activeWarningListeners.Count == 0)
                return;


            foreach (FreeStep step in freeSteps)
            {
                foreach (
                    WrongInteractionMessage wrong
                    in step.wrongInteractions)
                {
                    if (wrong.interaction == null)
                        continue;


                    foreach (
                        UnityAction listener
                        in activeWarningListeners)
                    {
                        wrong.interaction
                            .onCompleted
                            .RemoveListener(
                                listener
                            );
                    }
                }
            }


            activeWarningListeners.Clear();
        }


        // =====================================================
        // COMPLETE FREE STEP
        // =====================================================

        private void CompleteFreeStep()
        {
            if (currentFreeStep >= freeSteps.Count)
                return;


            FreeStep step =
                freeSteps[currentFreeStep];


            step.onStepEnd?.Invoke();


            ClearWarningListeners();


            Debug.Log(
                "Free Step Ended: "
                + currentFreeStep
            );


            currentFreeStep++;


            EnableCurrentFreeStepInteractions();


            StartFreeStep();
        }


        // =====================================================
        // ADVANCE STEP
        // =====================================================

        public void AdvanceStep()
        {
            if (InteractionManager.Instance.Guided)
            {
                GoNextStep();
            }
            else
            {
                GoNextFreeStep();
            }
        }


        // =====================================================
        // FREE INTERACTION UNDONE
        // =====================================================

        private void FreeInteractionUndone(
            BaseInteraction interaction
        )
        {
            Debug.Log(
                "Free Interaction Undone: "
                + interaction.name
            );


            if (freeSteps[currentFreeStep]
                .interactions
                .Contains(interaction))
            {
                Debug.Log(
                    "Current step reopened because of: "
                    + interaction.name
                );
            }


            CheckFreeStepCompletion();
        }


        // =====================================================
        // DISABLE ALL FREE INTERACTIONS
        // =====================================================

        private void DisableAllFreeInteractions()
        {
            foreach (FreeStep step in freeSteps)
            {
                foreach (
                    BaseInteraction interaction
                    in step.interactions)
                {
                    if (interaction == null)
                        continue;


                    Interaction click =
                        interaction
                            .GetComponentInChildren<
                                Interaction>();


                    if (click != null)
                        click.Disable();
                }


                foreach (
                    WrongInteractionMessage wrong
                    in step.wrongInteractions)
                {
                    if (wrong.interaction == null)
                        continue;


                    Interaction click =
                        wrong.interaction
                            .GetComponentInChildren<
                                Interaction>();


                    if (click != null)
                        click.Disable();
                }
            }
        }


        // =====================================================
        // ENABLE CURRENT FREE STEP
        // =====================================================

        private void EnableCurrentFreeStepInteractions()
        {
            if (currentFreeStep >= freeSteps.Count)
                return;


            FreeStep step =
                freeSteps[currentFreeStep];


            foreach (
                BaseInteraction interaction
                in step.interactions)
            {
                if (interaction == null)
                    continue;


                Interaction click =
                    interaction
                        .GetComponentInChildren<
                            Interaction>();


                if (click != null)
                    click.Enable();
            }


            foreach (
                WrongInteractionMessage wrong
                in step.wrongInteractions)
            {
                if (wrong.interaction == null)
                    continue;


                Interaction click =
                    wrong.interaction
                        .GetComponentInChildren<
                            Interaction>();


                if (click != null)
                    click.Enable();
            }
        }


        // =====================================================
        // MODE OBJECTS
        // =====================================================

        private void UpdateModeObjects()
        {
            if (guidedOnlyObjects != null)
            {
                foreach (
                    GameObject obj
                    in guidedOnlyObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(
                            InteractionManager.Instance
                                .Guided
                        );
                    }
                }
            }


            if (freeOnlyObjects != null)
            {
                foreach (
                    GameObject obj
                    in freeOnlyObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(
                            InteractionManager.Instance
                                .Free
                        );
                    }
                }
            }
        }


        // =====================================================
        // DELAY FREE STEP
        // =====================================================

        private IEnumerator DelayFreeStep(
            float duration
        )
        {
            yield return new WaitForSeconds(
                duration
            );


            Debug.Log(
                "Delay Free Step Completed: "
                + currentFreeStep
            );


            CompleteFreeStep();
        }


        // =====================================================
        // FREE INTERACTION COMPLETED
        // =====================================================

        private void FreeInteractionCompleted(
            BaseInteraction interaction
        )
        {
            Debug.Log(
                "Completed: "
                + interaction.name
            );


            CheckFreeStepCompletion();
        }


        // =====================================================
        // CHECK FREE STEP COMPLETION
        // =====================================================

        private void CheckFreeStepCompletion()
        {
            if (currentFreeStep >= freeSteps.Count)
                return;


            FreeStep step =
                freeSteps[currentFreeStep];


            if (step.completionMode !=
                FreeStep.CompletionMode.InteractionBased)
            {
                return;
            }


            foreach (
                BaseInteraction interaction
                in step.interactions)
            {
                if (interaction == null)
                    continue;


                if (!interaction.IsCompleted)
                    return;
            }


            Debug.Log(
                "Free Step Completed: "
                + currentFreeStep
            );


            CompleteFreeStep();
        }


        // =====================================================
        // SHOW WARNING
        // =====================================================

        public void ShowWarning(
            string message
        )
        {
            if (warningObject == null ||
                warningText == null)
                return;


            if (warningRoutine != null)
                StopCoroutine(
                    warningRoutine
                );


            warningText.text =
                message;


            warningObject.SetActive(
                true
            );


            warningRoutine =
                StartCoroutine(
                    HideWarning()
                );
        }


        // =====================================================
        // HIDE WARNING
        // =====================================================

        private IEnumerator HideWarning()
        {
            yield return new WaitForSeconds(
                warningDuration
            );


            warningObject.SetActive(
                false
            );
        }


        // =====================================================
        // MANUAL FREE STEP
        // =====================================================

        public void GoNextFreeStep()
        {
            if (InteractionManager.Instance.Guided)
                return;


            if (currentFreeStep >= freeSteps.Count)
                return;


            Debug.Log(
                "Manual Free Step Completed: "
                + currentFreeStep
            );


            CompleteFreeStep();
        }


        // =====================================================
        // GUIDED SYSTEM
        // =====================================================

        public void GoNextStep()
        {
            if (delayBetweenSteps > 0)
            {
                StartCoroutine(
                    DelayedNextStep()
                );
            }
            else
            {
                GoToStep(
                    currentStep + 1
                );
            }
        }


        // =====================================================
        // DELAYED GUIDED STEP
        // =====================================================

        private IEnumerator DelayedNextStep()
        {
            yield return new WaitForSeconds(
                delayBetweenSteps
            );


            GoToStep(
                currentStep + 1
            );
        }


        // =====================================================
        // GO TO GUIDED STEP
        // =====================================================

        public void GoToStep(
            int index
        )
        {
            if (currentStep >= 0 &&
                currentStep < steps.Count)
            {
                Step previous =
                    steps[currentStep];


                if (previous != null)
                {
                    if (previous.uiObject)
                        previous.uiObject
                            .SetActive(false);


                    if (previous.notesObject)
                        previous.notesObject
                            .SetActive(false);


                    previous.onStepEnd?.Invoke();


                    if (previous.interaction)
                    {
                        previous.interaction
                            .onCompleted
                            .RemoveListener(
                                GoNextStep
                            );


                        previous.interaction
                            .ResetInteraction();
                    }
                }
            }


            if (index < 0 ||
                index >= steps.Count)
            {
                Debug.Log(
                    "Storyboard Finished"
                );

                return;
            }


            currentStep =
                index;


            Step step =
                steps[currentStep];


            if (step.uiObject)
            {
                step.uiObject.SetActive(
                    InteractionManager.Instance
                        .Guided
                );
            }


            if (step.notesObject)
            {
                step.notesObject.SetActive(
                    InteractionManager.Instance
                        .Guided
                );
            }


            if (audioSource &&
                step.audioClip &&
                InteractionManager.Instance.Guided)
            {
                audioSource.Stop();


                audioSource.clip =
                    step.audioClip;


                audioSource.Play();
            }


            step.onStepStart?.Invoke();


            if (step.stepType ==
                StepType.Delay)
            {
                StartCoroutine(
                    DelayStep(step)
                );

                return;
            }


            if (step.interaction)
            {
                step.interaction
                    .onCompleted
                    .RemoveListener(
                        GoNextStep
                    );


                step.interaction
                    .onCompleted
                    .AddListener(
                        GoNextStep
                    );


                step.interaction.Begin();
            }
        }


        // =====================================================
        // DELAY GUIDED STEP
        // =====================================================

        private IEnumerator DelayStep(
            Step step
        )
        {
            yield return new WaitForSeconds(
                step.delayDuration
            );


            step.onStepEnd?.Invoke();


            GoNextStep();
        }
    }
}
