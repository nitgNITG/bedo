using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Runtime.InteropServices;

namespace TutorialFramework
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance;


        [Header("Mode")]
        public ExperimentMode currentMode =
            ExperimentMode.Guided;


        [Header("Scene Restart")]
        [SerializeField]
        private string scenarioSceneName = "Choose Experiment";


        [Header("Scene Split")]
        [Tooltip("The lightweight scene that contains only the experiment-selection screen.")]
        [SerializeField]
        private string mainMenuSceneName = "Main Menu";


        [Header("Fade")]
        [SerializeField]
        private Image fadeImage;

        [SerializeField]
        private float fadeDuration = 0.25f;

        [SerializeField]
        private float fadeInDuration = 0.1f;

        [Tooltip("Keeps the persistent black transition canvas above all scene UI while a new scene is loading.")]
        [SerializeField]
        private int fadeCanvasSortingOrder = 32767;


        private bool restarting = false;


        // A mode/experiment action can arrive while the current scene is
        // fading back in.  Keep that request rather than dropping it.
        private bool restartQueued = false;


        // Becomes true immediately after LoadScene.  A change before that
        // point is already included by the in-progress reload; a change
        // after it needs one additional reload.
        private bool sceneReloaded = false;


        public bool FirstTimeRun = false;


        [Header("Experiment")]
        public string ExperimentName = "Choose Experiment";


        [Header("Window Title")]
        [Tooltip("The fixed title for the Wind Energy lab.")]
        [SerializeField]
        private string fixedWindowTitle =
            "Wind Energy";

        [Tooltip("Leave off to keep the fixed machine title. Turn on later to restore experiment-specific window titles.")]
        [SerializeField]
        private bool useExperimentNameInWindowTitle = false;


        // =====================================================
        // ACTIVE STORYBOARD
        // =====================================================

        /*
         * IMPORTANT:
         * We store the name instead of a StoryboardSteps reference.
         *
         * StoryboardSteps belongs to the scene and can be destroyed
         * when the scene reloads.
         */

        [Header("Active Storyboard")]
        [SerializeField]
        private string activeStoryboardName = "";


        public string ActiveStoryboardName =>
            activeStoryboardName;


#if UNITY_STANDALONE_WIN

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();


        [DllImport("user32.dll")]
        private static extern bool SetWindowText(
            System.IntPtr hwnd,
            string text
        );

#endif


        // =====================================================
        // AWAKE
        // =====================================================

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log(
                    "Destroying duplicate Manager: "
                    + gameObject.name
                );

                Destroy(gameObject);
                return;
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;


            Instance = this;


            SceneManager.sceneLoaded += OnSceneLoaded;



            DontDestroyOnLoad(
                transform.root.gameObject
            );

            ConfigureFadeCanvas();


            Debug.Log(
                "Persistent Manager created"
            );


            if (string.IsNullOrWhiteSpace(ExperimentName))
            {
                ExperimentName =
                    "Choose Experiment";
            }


            UpdateWindowTitle();
        }


        // =====================================================
        // START
        // =====================================================

        private void Start()
        {
            if (fadeImage != null)
            {
                SetFadeAlpha(0f);
            }


            UpdateWindowTitle();


            // A previous menu-scene build disabled the shared Experiments
            // root while disabling Free Lab.  Restore that UI root here, then
            // disable only runtime components that must not initialize in the
            // lightweight menu scene.
            PrepareMainMenuScene();
            WindStudentTracking.StartSession();
        }


        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }


        private void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode
        )
        {
            PrepareMainMenuScene();
        }


        private void PrepareMainMenuScene()
        {
            if (!IsInMainMenuScene)
                return;


            // The Main Menu must open on its saved experiment-selection
            // screen.  Its tab controls normally close their panels in
            // Start(), so disable only those controls in this scene.
            foreach (PanelToggleButton panelToggle in
                     FindObjectsByType<PanelToggleButton>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None
                     ))
            {
                if (panelToggle.gameObject.scene ==
                    SceneManager.GetActiveScene())
                {
                    panelToggle.enabled = false;
                }
            }


            GameObject experimentsRoot = null;

            foreach (GameObject root in
                     SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "Experiments")
                {
                    experimentsRoot = root;
                    break;
                }
            }


            if (experimentsRoot == null)
                return;


            // This keeps the existing selector and its button UI visible.
            experimentsRoot.SetActive(true);


            // The physical lab is in Wind Lab.  Main Menu retains only the
            // selector UI, so prevent Free Lab and storyboards from doing
            // their normal scene-wide initialization here.
            foreach (FreeLabManager freeLabManager in
                     experimentsRoot.GetComponentsInChildren<
                         FreeLabManager
                     >(true))
            {
                freeLabManager.enabled = false;
            }


            foreach (StoryboardSteps storyboard in
                     experimentsRoot.GetComponentsInChildren<
                         StoryboardSteps
                     >(true))
            {
                storyboard.enabled = false;
            }
        }


        // =====================================================
        // EXPERIMENT SELECTION
        // =====================================================

        /*
         * Called by a button belonging to a StoryboardSteps.
         *
         * This does NOT start, stop, reload, or otherwise change
         * the storyboard.
         *
         * It simply tells the persistent manager:
         *
         * "This is the experiment that has priority."
         */

        public void SetActiveStoryboard(
            StoryboardSteps storyboard
        )
        {
            if (storyboard == null)
            {
                Debug.LogWarning(
                    "SetActiveStoryboard received null."
                );

                return;
            }


            SetActiveStoryboard(
                storyboard.StoryboardID
            );
        }


        /*
         * Name-based version.
         */

        public void SetActiveStoryboard(
            string storyboardName
        )
        {
            if (string.IsNullOrWhiteSpace(
                storyboardName))
            {
                Debug.LogWarning(
                    "Cannot activate storyboard with an empty name."
                );

                return;
            }


            activeStoryboardName =
                storyboardName;

            RefreshSelectedExperimentObjects();


            Debug.Log(
                "Active storyboard changed to: "
                + activeStoryboardName
            );
        }


        // =====================================================
        // CHECK ACTIVE STORYBOARD
        // =====================================================

        public bool IsExperimentActive(
            StoryboardSteps storyboard
        )
        {
            if (storyboard == null)
                return false;


            return IsExperimentActive(
                storyboard.StoryboardID
            );
        }


        public bool IsExperimentActive(
            string storyboardName
        )
        {
            if (string.IsNullOrWhiteSpace(
                activeStoryboardName))
            {
                return false;
            }


            return activeStoryboardName ==
                   storyboardName;
        }


        // =====================================================
        // CLEAR ACTIVE STORYBOARD
        // =====================================================

        /*
         * Optional call for your experiment-selection/menu
         * if you ever need to explicitly clear the selection.
         *
         * This does NOT change scenes or UI.
         */

        public void ClearActiveStoryboard()
        {
            activeStoryboardName = "";
            RefreshSelectedExperimentObjects();


            Debug.Log(
                "Active storyboard cleared."
            );
        }


        public bool HasActiveStoryboard()
        {
            return !string.IsNullOrWhiteSpace(
                activeStoryboardName
            );
        }

        private void RefreshSelectedExperimentObjects()
        {
            foreach (StoryboardSteps storyboard in
                FindObjectsOfType<StoryboardSteps>(true))
            {
                if (storyboard != null)
                    storyboard.SetSelectedExperimentObjectActive(
                        IsExperimentActive(storyboard));
            }
        }


        // =====================================================
        // EXPERIMENT NAME
        // =====================================================

        public void SetExperimentName(string name)
        {
            ExperimentName = name;


            if (string.IsNullOrWhiteSpace(ExperimentName))
            {
                ExperimentName =
                    "Choose Experiment";
            }


            UpdateWindowTitle();
        }


        // =====================================================
        // WINDOW TITLE
        // =====================================================

        private void UpdateWindowTitle()
        {
            // The experiment-name title behavior is intentionally retained
            // behind this toggle for a future return to per-experiment titles.
            string windowTitle = useExperimentNameInWindowTitle
                ? ExperimentName
                : fixedWindowTitle;

            if (string.IsNullOrWhiteSpace(windowTitle))
                windowTitle = ExperimentName;

#if UNITY_STANDALONE_WIN

            System.IntPtr window =
                GetActiveWindow();


            if (window != System.IntPtr.Zero)
            {
                SetWindowText(window, windowTitle);
            }

#endif

            Debug.Log(
                "Window title changed to: "
                + windowTitle
            );
        }


        // =====================================================
        // FADE
        // =====================================================

        private void SetFadeAlpha(float alpha)
        {
            if (fadeImage == null)
                return;


            Color c =
                fadeImage.color;


            c.a = alpha;


            fadeImage.color = c;
        }


        /// <summary>
        /// The transition canvas belongs to the persistent manager, while all
        /// regular UI belongs to the scene being replaced. Give the fade
        /// canvas the highest order so outgoing UI cannot appear above black
        /// during asynchronous scene loading.
        /// </summary>
        private void ConfigureFadeCanvas()
        {
            if (fadeImage == null)
                return;

            Canvas fadeCanvas = fadeImage.GetComponentInParent<Canvas>(true);
            if (fadeCanvas == null)
                return;

            fadeCanvas.overrideSorting = true;
            fadeCanvas.sortingOrder = fadeCanvasSortingOrder;
        }


        // =====================================================
        // MODE
        // =====================================================

        public bool Guided =>
            currentMode == ExperimentMode.Guided;


        public bool Free =>
            currentMode == ExperimentMode.Free;


        /// <summary>
        /// True while the lightweight experiment-selection scene is open.
        /// </summary>
        public bool IsInMainMenuScene =>
            SceneManager.GetActiveScene().name == mainMenuSceneName;


        /// <summary>
        /// Opens the full lab scene after an experiment is chosen from the
        /// lightweight experiment-selection scene.  The selected storyboard
        /// name remains on this persistent manager, so the lab starts that
        /// experiment directly without an extra restart.
        /// </summary>
        public void LoadSelectedExperimentScene()
        {
            if (!IsInMainMenuScene)
            {
                RestartScenario();
                return;
            }


            StartCoroutine(
                LoadDifferentSceneRoutine(scenarioSceneName)
            );
        }


        /// <summary>
        /// Leaves the lab and returns to the lightweight experiment menu.
        /// </summary>
        public void ReturnToExperimentMenu()
        {
            if (IsInMainMenuScene)
                return;
            WindStudentTracking.AbortActiveExperiment();



            FirstTimeRun = false;
            ClearActiveStoryboard();
            ExperimentName = "Choose Experiment";


            StartCoroutine(
                LoadDifferentSceneRoutine(mainMenuSceneName)
            );
        }


        public void SetGuided()
        {
            if (Guided)
                return;


            currentMode =
                ExperimentMode.Guided;


            RestartScenario();
        }


        public void SetFree()
        {
            if (Free)
                return;


            currentMode =
                ExperimentMode.Free;


            RestartScenario();
        }


        // =====================================================
        // RESTART SCENARIO
        // =====================================================

        public void RestartScenario()
        {
            WindStudentTracking.AbortActiveExperiment();

            if (restarting)
            {
                if (sceneReloaded)
                {
                    restartQueued = true;


                    Debug.Log(
                        "Restart queued while a restart is in progress."
                    );
                }

                return;
            }


            restarting = true;
            sceneReloaded = false;


            StartCoroutine(
                RestartRoutine()
            );
        }


        private IEnumerator RestartRoutine()
        {
            yield return Fade(1f, fadeInDuration);


            Debug.Log(
    "RESTARTING SCENE: "
    + scenarioSceneName
);

            // Keep the existing full-reset behavior, but let Unity spread the
            // scene's asset loading across frames instead of freezing on a
            // blocking LoadScene call behind the black transition.
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                scenarioSceneName,
                LoadSceneMode.Single
            );

            if (loadOperation != null)
                yield return loadOperation;


            sceneReloaded = true;


            yield return null;

            Debug.Log(
    "SCENE RELOADED: "
    + SceneManager.GetActiveScene().name
);


            UpdateWindowTitle();


            yield return Fade(0f, fadeDuration);


            if (restartQueued)
            {
                restartQueued = false;
                sceneReloaded = false;


                StartCoroutine(
                    RestartRoutine()
                );

                yield break;
            }


            restarting = false;
        }


        private IEnumerator LoadDifferentSceneRoutine(
            string sceneName
        )
        {
            if (restarting || string.IsNullOrWhiteSpace(sceneName))
                yield break;


            restarting = true;


            yield return Fade(1f, fadeInDuration);


            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Single
                );


            if (loadOperation != null)
                yield return loadOperation;


            yield return null;


            UpdateWindowTitle();


            yield return Fade(0f, fadeDuration);


            restarting = false;
        }


        private IEnumerator Fade(
            float target,
            float duration
        )
        {
            if (fadeImage == null)
                yield break;


            float start =
                fadeImage.color.a;


            float timer = 0f;


            while (timer < duration)
            {
                timer += Time.deltaTime;


                float t =
                    timer / duration;


                Color c =
                    fadeImage.color;


                c.a =
                    Mathf.Lerp(
                        start,
                        target,
                        t
                    );


                fadeImage.color = c;


                yield return null;
            }


            Color final =
                fadeImage.color;


            final.a = target;


            fadeImage.color = final;
        }

        // =====================================================
        // PROCESS INTERACTION
        // =====================================================

        public void ProcessInteraction(
            Interaction clicked
        )
        {
            BaseInteraction interaction =
                clicked.GetComponentInParent<BaseInteraction>();


            if (interaction == null)
                return;

            if (Guided && interaction is WireInteraction wire && !wire.CanClickPoint(clicked))
                return;


            // =================================================
            // FIND THE STORYBOARD THAT OWNS THIS INTERACTION
            // =================================================

            StoryboardSteps storyboard =
                interaction.GetComponentInParent<
                    StoryboardSteps
                >();


            // =================================================
            // DEBUG
            // =================================================

            Debug.Log(
                "===== INTERACTION DEBUG =====\n" +
                "Clicked Object: " + clicked.gameObject.name + "\n" +
                "BaseInteraction: " + interaction.gameObject.name + "\n" +
                "Owning Storyboard: " +
                    (storyboard != null
                        ? storyboard.StoryboardID
                        : "NULL") + "\n" +
                "Active Storyboard: " +
                    ActiveStoryboardName + "\n" +
                "Is Owning Storyboard Active: " +
                    (storyboard != null &&
                     IsExperimentActive(storyboard)) + "\n" +
                "============================="
            );


            // =================================================
            // FREE MODE
            // =================================================

            if (Free)
            {
                bool invokeExistingClickEvent;

                FreeLabManager.GetOrCreate().HandleInteraction(
                    clicked,
                    out invokeExistingClickEvent
                );

                return;
            }


            // =================================================
            // GUIDED MODE
            // =================================================

            if (interaction is WireInteraction guidedWire)
            {
                guidedWire.PointClicked(clicked);
            }
            else if (
                interaction
                is SingleClickInteraction guidedSingle)
            {
                guidedSingle.Execute();
            }
        }
    }
}
