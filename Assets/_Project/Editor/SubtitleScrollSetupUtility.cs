using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Copies the approved Exp 8 Subtitle Step 1 scroll layout to every subtitle step,
/// then transfers the original text into the copied Subtitle object.
/// </summary>
public static class SubtitleScrollSetupUtility
{
    [MenuItem("Bedo/Subtitle Steps/Repair Exp 4 Names and Order from Storyboard")]
    private static void RepairExp4NamesAndOrderFromStoryboard()
    {
        var repairedCount = RepairSubtitleStepsFromStoryboard("Exp 4");
        Debug.Log($"Reordered and renamed {repairedCount} Exp 4 subtitle steps from the Storyboard Steps UI Object sequence.");
    }

    [MenuItem("Bedo/Subtitle Steps/Repair All Other Experiment Names and Order from Storyboard")]
    private static void RepairAllOtherExperimentNamesAndOrderFromStoryboard()
    {
        var summary = new List<string>();

        for (var experimentNumber = 1; experimentNumber <= 9; experimentNumber++)
        {
            if (experimentNumber == 4)
                continue;

            var experimentName = $"Exp {experimentNumber}";
            var repairedCount = RepairSubtitleStepsFromStoryboard(experimentName);
            summary.Add($"{experimentName}: {repairedCount}");
        }

        Debug.Log(
            "Reordered and renamed subtitle steps from each Storyboard Steps UI Object sequence. "
            + string.Join(", ", summary));
    }

    private static int RepairSubtitleStepsFromStoryboard(string experimentName)
    {
        var storyboard = FindExperimentStoryboard(experimentName);
        var panel = FindSceneObject($"{experimentName} UI").transform;
        var orderedSteps = new List<Transform>();

        // A UI object may be referenced by several storyboard elements.  It only
        // represents one visible subtitle step, so retain the first occurrence.
        foreach (var storyboardStep in storyboard.steps)
        {
            if (storyboardStep.uiObject == null)
                continue;

            var subtitleStep = storyboardStep.uiObject.transform;
            if (subtitleStep.parent != panel ||
                !subtitleStep.name.StartsWith("Subtitle Step ", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!orderedSteps.Contains(subtitleStep))
                orderedSteps.Add(subtitleStep);
        }

        if (orderedSteps.Count == 0)
        {
            Debug.LogError($"{experimentName} has no direct Subtitle Step UI Objects referenced by its storyboard.", storyboard);
            return 0;
        }

        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, $"Repair {experimentName} subtitle step order");

        // Temporary names avoid collisions while objects are renamed in place.
        for (var index = 0; index < orderedSteps.Count; index++)
            orderedSteps[index].name = $"__{experimentName.Replace(" ", string.Empty)}SubtitleRepair_{index}";

        for (var index = 0; index < orderedSteps.Count; index++)
        {
            var subtitleStep = orderedSteps[index];
            var stepNumber = index + 1;

            subtitleStep.name = $"Subtitle Step {stepNumber}";
            subtitleStep.SetSiblingIndex(index);

            var indexNumber = FindDirectChild(subtitleStep, "Index Number");
            var indexText = indexNumber == null
                ? null
                : indexNumber.GetComponentInChildren<TMP_Text>(true);

            if (indexText == null)
            {
                Debug.LogWarning(
                    $"{subtitleStep.name} has no Index Number > Text (TMP) label.",
                    subtitleStep);
                continue;
            }

            indexText.text = stepNumber.ToString();
            EditorUtility.SetDirty(indexText);
        }

        EditorUtility.SetDirty(panel.gameObject);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return orderedSteps.Count;
    }

    [MenuItem("Bedo/Subtitle Steps/Replace 800-ohm With 100-ohm In Subtitle Text")]
    private static void Replace800OhmInSubtitleText()
    {
        const string oldValue = "800-ohm";
        const string newValue = "100-ohm";
        var changedObjects = new List<string>();
        var changedOccurrences = 0;

        foreach (var subtitle in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
        {
            if (!subtitle.name.Equals("Subtitle", StringComparison.OrdinalIgnoreCase))
                continue;

            var subtitleStep = FindSubtitleStepAncestor(subtitle.transform);
            if (subtitleStep == null ||
                subtitle.text.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            var originalText = subtitle.text;
            var occurrenceCount = CountOccurrences(originalText, oldValue);

            Undo.RecordObject(subtitle, "Replace 800-ohm in subtitle text");
            subtitle.text = originalText.Replace(oldValue, newValue);
            EditorUtility.SetDirty(subtitle);

            changedOccurrences += occurrenceCount;
            changedObjects.Add(
                $"{GetHierarchyPath(subtitleStep)} ({occurrenceCount}):\n"
                + $"BEFORE: {originalText.Replace("\n", " ")}");
        }

        if (changedObjects.Count > 0)
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(
            $"Replaced {changedOccurrences} instance(s) of {oldValue} in "
            + $"{changedObjects.Count} subtitle text object(s) with {newValue}.\n\n"
            + string.Join("\n\n", changedObjects));
    }

    private static Transform FindSubtitleStepAncestor(Transform transform)
    {
        for (var current = transform; current != null; current = current.parent)
        {
            if (current.name.StartsWith("Subtitle Step ", StringComparison.OrdinalIgnoreCase))
                return current;
        }

        return null;
    }

    private static int CountOccurrences(string value, string searchValue)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(searchValue, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += searchValue.Length;
        }

        return count;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        for (var current = transform.parent; current != null; current = current.parent)
            path = current.name + " > " + path;

        return path;
    }

    [MenuItem("Bedo/Subtitle Scrolls/Disable Subtitle Step Bars")]
    private static void DisableSubtitleStepBars()
    {
        var disabledCount = 0;

        foreach (var step in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (!step.name.StartsWith("Subtitle Step ", StringComparison.OrdinalIgnoreCase))
                continue;

            for (var index = 0; index < step.childCount; index++)
            {
                var child = step.GetChild(index);
                if (!child.name.Equals("Bar", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (child.gameObject.activeSelf)
                {
                    Undo.RecordObject(child.gameObject, "Disable subtitle step bar");
                    child.gameObject.SetActive(false);
                    EditorUtility.SetDirty(child.gameObject);
                    disabledCount++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Disabled {disabledCount} direct Subtitle Step Bar objects.");
    }

    [MenuItem("Bedo/Subtitle Scrolls/Apply Exp 8 Setup to Every Subtitle Step")]
    private static void ApplyExp8SetupToEveryStep()
    {
        var templatePanel = FindSceneObject("Exp 8 UI");
        var templateStep = FindDirectChild(templatePanel.transform, "Subtitle Step 1");
        var templateScroll = templateStep.GetComponentInChildren<ScrollRect>(true);

        if (templateScroll == null || templateScroll.content == null)
        {
            Debug.LogError("Exp 8 Subtitle Step 1 needs a configured Scroll Rect before it can be copied.");
            return;
        }

        // 20-pixel bar minus 15-pixel overlap leaves the requested 5-pixel reservation.
        templateScroll.verticalScrollbarSpacing = -15f;
        EditorUtility.SetDirty(templateScroll);

        for (var experimentNumber = 1; experimentNumber <= 8; experimentNumber++)
        {
            var panel = FindExperimentPanel(experimentNumber);
            foreach (var step in panel.GetComponentsInChildren<Transform>(true))
            {
                if (!step.name.StartsWith("Subtitle Step ", StringComparison.OrdinalIgnoreCase) || step == templateStep)
                    continue;

                // A completed step already has its approved copied layout. This keeps the
                // menu command safe to run again without adding another Scroll View.
                if (FindActiveDirectScroll(step) != null)
                    continue;

                var oldSubtitle = FindOwnSubtitle(step);
                if (oldSubtitle == null)
                {
                    Debug.LogWarning($"{panel.name} > {step.name}: no Subtitle text object was found.", step);
                    continue;
                }

                var oldText = oldSubtitle.GetComponent<TMP_Text>().text;
                Undo.RegisterFullObjectHierarchyUndo(step.gameObject, $"Set up {panel.name} {step.name} scroll");

                foreach (var oldScroll in step.GetComponentsInChildren<ScrollRect>(true))
                {
                    if (oldScroll.transform.parent == step)
                        oldScroll.gameObject.SetActive(false);
                }

                oldSubtitle.gameObject.SetActive(false);

                var newScroll = UnityEngine.Object.Instantiate(templateScroll.gameObject).GetComponent<ScrollRect>();
                Undo.RegisterCreatedObjectUndo(newScroll.gameObject, $"Create {panel.name} {step.name} scroll");
                newScroll.transform.SetParent(step, false);
                newScroll.name = "Scroll View";
                newScroll.verticalScrollbarSpacing = -15f;

                var copiedSubtitle = FindDirectChild(newScroll.content, "Subtitle").GetComponent<TMP_Text>();
                copiedSubtitle.text = oldText;
                copiedSubtitle.gameObject.SetActive(true);

                EditorUtility.SetDirty(newScroll);
                EditorUtility.SetDirty(copiedSubtitle);
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Applied the Exp 8 Subtitle Step 1 scroll setup to every subtitle step. Each copy contains that step's original text.");
    }

    private static GameObject FindExperimentPanel(int experimentNumber)
    {
        foreach (var transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name.Trim().Equals($"Exp {experimentNumber} UI", StringComparison.OrdinalIgnoreCase))
                return transform.gameObject;
        }

        throw new InvalidOperationException($"Exp {experimentNumber} UI could not be found.");
    }

    private static TutorialFramework.StoryboardSteps FindExperimentStoryboard(string objectName)
    {
        foreach (var storyboard in UnityEngine.Object.FindObjectsOfType<TutorialFramework.StoryboardSteps>(true))
        {
            if (storyboard.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                return storyboard;
        }

        throw new InvalidOperationException($"Storyboard {objectName} could not be found.");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var transform in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name.Trim().Equals(objectName, StringComparison.OrdinalIgnoreCase))
                return transform.gameObject;
        }

        throw new InvalidOperationException($"{objectName} could not be found.");
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (var index = 0; index < parent.childCount; index++)
        {
            var child = parent.GetChild(index);
            if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                return child;
        }

        throw new InvalidOperationException($"{childName} could not be found under {parent.name}.");
    }

    private static Transform FindOwnSubtitle(Transform step)
    {
        foreach (var text in step.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name.Equals("Subtitle", StringComparison.OrdinalIgnoreCase))
                return text.transform;
        }

        return null;
    }

    private static ScrollRect FindActiveDirectScroll(Transform step)
    {
        for (var index = 0; index < step.childCount; index++)
        {
            var child = step.GetChild(index);
            var scroll = child.GetComponent<ScrollRect>();
            if (scroll != null && child.gameObject.activeSelf)
                return scroll;
        }

        return null;
    }

}
