using System.Collections.Generic;
using UnityEngine;

public class StepListPopulator : MonoBehaviour
{
    [System.Serializable]
    public class PopupData
    {
        [Tooltip("Show this popup after this step number (1-based).")]
        public int afterStep = 1;

        [TextArea]
        public string text;
    }

    [Header("Steps")]
    [TextArea]
    public List<string> steps = new List<string>();

    [Header("Popups")]
    public List<PopupData> popups = new List<PopupData>();

    [Header("References")]
    [SerializeField] private StepItem stepItemPrefab;
    [SerializeField] private Transform contentParent;

    private void Start()
    {
        PopulateList();
    }

    public void PopulateList()
    {
        if (contentParent == null || stepItemPrefab == null)
        {
            Debug.LogError("StepListPopulator is missing references.");
            return;
        }

        // Clear previous items
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }

        // Create all steps
        for (int i = 0; i < steps.Count; i++)
        {
            StepItem step =
                Instantiate(stepItemPrefab, contentParent);

            step.InitItem(
                (i + 1).ToString(),
                steps[i]
            );

            // Add popups after this step
            foreach (PopupData popup in popups)
            {
                if (popup.afterStep == i + 1)
                {
                    StepItem popupItem =
                        Instantiate(stepItemPrefab, contentParent);

                    popupItem.InitItem(
                        "*",
                        popup.text
                    );
                }
            }
        }

    }

    public void Refresh()
    {
        PopulateList();
    }
}