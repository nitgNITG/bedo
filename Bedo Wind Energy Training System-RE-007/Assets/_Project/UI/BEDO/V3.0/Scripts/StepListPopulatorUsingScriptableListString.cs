using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepListPopulatorUsingScriptableListString : MonoBehaviour
{
    [Tooltip("List of step descriptions.")]
    public ScriptableListString Steps;

    [Tooltip("List of popup descriptions.")]
    public ScriptableListString PopUps;

    [Tooltip("Indices after which to show popups (1-based step numbers).")]
    public List<int> PopupAfterSteps;

    [Tooltip("Prefab of the StepItem to instantiate.")]
    public StepItem StepItemPrefab;

    [Tooltip("Content container (e.g., Vertical Layout) for the scroll list.")]
    public Transform ContentParent;

    private void Start()
    {
        PopulateList();
    }


    public void AddPopupAfterStepIndex(int index)
    {
        PopupAfterSteps.Add(index);
    }
    /// <summary>
    /// Clears existing items and populates the scroll list.
    /// </summary>
    public void PopulateList()
    {
        if (ContentParent == null || StepItemPrefab == null || Steps == null)
        {
            Debug.LogError("StepListPopulator not configured correctly.");
            return;
        }

        // Clear existing children
        for (int i = ContentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(ContentParent.GetChild(i).gameObject);
        }

        int popupIndex = 0;

        for (int i = 0; i < Steps.Count; i++)
        {
            // Instantiate step item
            var stepGO = Instantiate(StepItemPrefab, ContentParent);
            int displayIndex = i + 1;
            string desc = Steps[i];
            stepGO.InitItem(displayIndex.ToString(), desc);

            // Check if a popup should follow this step
            if (PopupAfterSteps != null && PopUps != null && popupIndex < PopUps.Count)
            {
                if (PopupAfterSteps.Contains(displayIndex))
                {
                    var popupDesc = PopUps[popupIndex];
                    var popupGO = Instantiate(StepItemPrefab, ContentParent);
                    // Use "*" to indicate popup
                    popupGO.InitItem("*", popupDesc);
                    popupIndex++;
                }
            }
        }
    }
}
