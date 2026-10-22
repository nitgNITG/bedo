using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Shows an assigned scene object only while an experiment is running in
    /// either Guided or Free Mode. The main menu has FirstTimeRun set to
    /// false, so the object is hidden there even though the remembered mode
    /// is still Guided or Free.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModeActiveVisibility : MonoBehaviour
    {
        [Tooltip("The object to show during an active Guided or Free experiment.")]
        [SerializeField] private GameObject target;

        [Tooltip("Used to rebind the target after the scenario scene reloads. Leave this as the camera indicator name for the current lab.")]
        [SerializeField] private string targetNameAfterSceneReload =
            "CameraIndiactor And Button";

        [Tooltip("Keep enabled for this project's main-menu behavior.")]
        [SerializeField] private bool requireExperimentRunning = true;

        private bool hasAppliedValue;
        private bool lastValue;

        private void Awake()
        {
            RememberTargetName();
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            ResolveReloadedTarget();

            InteractionManager manager = InteractionManager.Instance;
            bool shouldShow = manager != null &&
                (!requireExperimentRunning || manager.FirstTimeRun) &&
                (manager.Guided || manager.Free);

            if (target == null || (hasAppliedValue &&
                lastValue == shouldShow))
            {
                return;
            }

            hasAppliedValue = true;
            lastValue = shouldShow;
            target.SetActive(shouldShow);
        }

        private void RememberTargetName()
        {
            if (target != null && string.IsNullOrWhiteSpace(
                targetNameAfterSceneReload))
            {
                targetNameAfterSceneReload = target.name;
            }
        }

        private void ResolveReloadedTarget()
        {
            // InteractionManager persists across scene reloads, while the
            // assigned UI object belongs to the scenario scene and is
            // destroyed/recreated. Rebind it before applying visibility.
            if (target != null || string.IsNullOrWhiteSpace(
                targetNameAfterSceneReload))
            {
                return;
            }

            foreach (Transform candidate in
                Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    candidate.name != targetNameAfterSceneReload)
                {
                    continue;
                }

                target = candidate.gameObject;
                // This is a newly created scene object, so the previous
                // applied-state cache must not suppress its first update.
                hasAppliedValue = false;
                return;
            }
        }
    }
}
