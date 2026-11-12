using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    /// <summary>
    /// An exclusive physical Free Lab button. Buttons sharing a group name
    /// behave like tabs: selecting one opens its view and closes the others;
    /// selecting the already-open button has no effect.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabToggleButton : MonoBehaviour
    {
        private static readonly Dictionary<string, List<FreeLabToggleButton>>
            Groups = new Dictionary<string, List<FreeLabToggleButton>>();

        [Header("Button")]
        [SerializeField]
        private Interaction interaction;

        [SerializeField]
        private string groupId = "Free Lab Views";

        [SerializeField]
        private bool startsSelected;

        [Header("When This Button Opens")]
        [SerializeField]
        private List<GameObject> objectsToShow = new List<GameObject>();

        [SerializeField]
        private List<GameObject> objectsToHide = new List<GameObject>();

        [SerializeField]
        private UnityEvent onOpened;

        [SerializeField]
        private UnityEvent onClosed;

        [Header("Runtime State (Play Mode)")]
        [SerializeField]
        private bool isSelected;

        public bool IsSelected => isSelected;

        private void CreateMissingGraphsCounterpart()
        {
            // Existing Free Lab scene setup contains a configured System
            // Button but no component on its direct Graphs Button sibling.
            // Complete that pair at runtime without changing Guided copies.
            if (gameObject.name != "System Button" ||
                transform.parent == null ||
                groupId != "UI VIEW")
            {
                return;
            }

            foreach (Transform sibling in transform.parent)
            {
                if (sibling.name != "Graphs Button")
                    continue;

                FreeLabToggleButton graphsToggle =
                    sibling.GetComponent<FreeLabToggleButton>();

                if (graphsToggle == null)
                {
                    graphsToggle = sibling.gameObject.AddComponent<
                        FreeLabToggleButton
                    >();
                }

                graphsToggle.Configure(
                    groupId,
                    false,
                    objectsToHide,
                    objectsToShow
                );
                return;
            }
        }

        private void Configure(
            string newGroupId,
            bool selectedAtStart,
            List<GameObject> show,
            List<GameObject> hide)
        {
            Unregister();
            groupId = newGroupId;
            startsSelected = selectedAtStart;
            objectsToShow = new List<GameObject>(show);
            objectsToHide = new List<GameObject>(hide);
            Register();
        }

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

            CreateMissingGraphsCounterpart();
        }

        private void OnEnable()
        {
            Register();

            // These are physical Interaction GameObjects, not Unity UI
            // Buttons. Enable their existing mouse-down interaction only
            // while the dedicated Free Lab hierarchy is active.
            if (IsFreeMode() && interaction != null)
                interaction.Enable();
        }

        private void Start()
        {
            if (startsSelected)
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
            if (isSelected || !IsFreeMode())
                return;

            Register();

            if (Groups.TryGetValue(groupId, out List<FreeLabToggleButton> group))
            {
                // Copy the list because closing a view may disable an object.
                FreeLabToggleButton[] buttons = group.ToArray();

                foreach (FreeLabToggleButton button in buttons)
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
            if (!Groups.TryGetValue(groupId, out List<FreeLabToggleButton> group))
            {
                group = new List<FreeLabToggleButton>();
                Groups.Add(groupId, group);
            }

            if (!group.Contains(this))
                group.Add(this);
        }

        private void Unregister()
        {
            if (!Groups.TryGetValue(groupId, out List<FreeLabToggleButton> group))
                return;

            group.Remove(this);

            if (group.Count == 0)
                Groups.Remove(groupId);
        }

        private static void SetActive(
            List<GameObject> objects,
            bool active)
        {
            foreach (GameObject item in objects)
            {
                if (item != null)
                    item.SetActive(active);
            }
        }

        private static bool IsFreeMode()
        {
            return InteractionManager.Instance != null &&
                InteractionManager.Instance.Free;
        }
    }
}
