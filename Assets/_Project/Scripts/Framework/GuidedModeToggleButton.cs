using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    /// <summary>
    /// An exclusive Guided Mode tab/button for existing Interaction objects.
    /// Buttons with the same group ID act like tabs: selecting one opens its
    /// content and closes the other selected button in that group.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuidedModeToggleButton : MonoBehaviour
    {
        private static readonly Dictionary<string, List<GuidedModeToggleButton>>
            Groups = new Dictionary<string, List<GuidedModeToggleButton>>();

        [Header("Button")]
        [SerializeField] private Interaction interaction;
        [SerializeField] private string groupId = "Guided Views";
        [SerializeField] private bool startsSelected;

        [Header("When This Button Opens")]
        [SerializeField] private List<GameObject> objectsToShow =
            new List<GameObject>();
        [SerializeField] private List<GameObject> objectsToHide =
            new List<GameObject>();
        [SerializeField] private UnityEvent onOpened;
        [SerializeField] private UnityEvent onClosed;

        [Header("Runtime State (Play Mode)")]
        [SerializeField] private bool isSelected;

        public bool IsSelected => isSelected;

        private void Reset()
        {
            interaction = GetComponent<Interaction>();
        }

        private void Awake()
        {
            if (interaction == null)
                interaction = GetComponent<Interaction>();

            if (interaction != null)
                interaction.onClicked.AddListener(Select);
        }

        private void OnEnable()
        {
            Register();
        }

        private void Start()
        {
            if (startsSelected && IsGuidedMode())
                Select();
        }

        private void OnDestroy()
        {
            if (interaction != null)
                interaction.onClicked.RemoveListener(Select);

            Unregister();
        }

        public void Select()
        {
            if (isSelected || !IsGuidedMode())
                return;

            Register();
            if (Groups.TryGetValue(groupId,
                out List<GuidedModeToggleButton> group))
            {
                GuidedModeToggleButton[] buttons = group.ToArray();
                foreach (GuidedModeToggleButton button in buttons)
                {
                    if (button != null && button != this)
                        button.Close();
                }
            }

            isSelected = true;
            SetActive(objectsToShow, true);
            SetActive(objectsToHide, false);
            onOpened?.Invoke();
        }

        public void Close()
        {
            if (!isSelected)
                return;

            isSelected = false;
            SetActive(objectsToShow, false);
            onClosed?.Invoke();
        }

        private void Register()
        {
            if (!Groups.TryGetValue(groupId,
                out List<GuidedModeToggleButton> group))
            {
                group = new List<GuidedModeToggleButton>();
                Groups.Add(groupId, group);
            }

            if (!group.Contains(this))
                group.Add(this);
        }

        private void Unregister()
        {
            if (!Groups.TryGetValue(groupId,
                out List<GuidedModeToggleButton> group))
            {
                return;
            }

            group.Remove(this);
            if (group.Count == 0)
                Groups.Remove(groupId);
        }

        private static void SetActive(List<GameObject> objects, bool active)
        {
            foreach (GameObject item in objects)
            {
                if (item != null)
                    item.SetActive(active);
            }
        }

        private static bool IsGuidedMode()
        {
            return InteractionManager.Instance != null &&
                InteractionManager.Instance.Guided;
        }
    }
}
