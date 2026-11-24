using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WindExp4SubtitlesSetup
{
    [MenuItem("PV System/Fix EXP4 Steps 5 To 8 Subtitles And Pointer")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || scene.path != "Assets/_Project/Scenes/Hydro Lab.unity")
            throw new InvalidOperationException("Save Hydro Lab in Edit Mode first.");
        var owner = Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(o => o.StoryboardID == "OFF Grid System Using Wind Turbine");
        var steps = owner.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Steps");
        var ui = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Exp 4 UI");
        string[] names = { "Emergeny_button002 (1)", "single_432 (1)", "on_button004 (1)" };
        for (int i = 0; i < names.Length; i++)
        {
            var row = owner.steps.Single(s => s.interaction != null && s.interaction.name == names[i] && s.interaction.transform.parent.name == "Step " + (5 + i));
            row.uiObject = ui.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Subtitle Step " + (5 + i)).gameObject;
        }
        var folder = steps.Cast<Transform>().Single(t => t.name == "Step 8" && t.GetComponentsInChildren<SingleClickInteraction>(true).Any());
        var click = folder.GetComponentsInChildren<SingleClickInteraction>(true).Single();
        if (click.interaction == null) throw new InvalidOperationException("Step8 click target is not assigned.");
        var pointers = steps.Find("Pointers");
        var template = owner.steps.Select(s => s.interaction).OfType<SingleClickInteraction>().First(s => s != click && s.marker != null).marker;
        string markerName = "Off-Grid Inverter Power Switch";
        var existing = pointers.Find(markerName);
        var marker = existing != null ? existing.gameObject : Object.Instantiate(template, pointers, false);
        marker.name = markerName;
        marker.transform.position = click.interaction.transform.position + Vector3.up * 1.5f;
        marker.SetActive(false);
        click.marker = marker;
        var row8 = owner.steps.SingleOrDefault(s => s.interaction == click);
        if (row8 != null) row8.uiObject = ui.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Subtitle Step 8").gameObject;
        EditorUtility.SetDirty(owner);
        EditorUtility.SetDirty(click);
        EditorUtility.SetDirty(marker.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
        PrefabUtility.RecordPrefabInstancePropertyModifications(click);
        if (click.marker != marker || Vector3.Distance(marker.transform.position, click.interaction.transform.position + Vector3.up * 1.5f) > .001f)
            throw new InvalidOperationException("Step8 pointer placement mismatch.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("EXP4 SUBTITLES SAVED: Emergency/CB/On use EXP4 subtitles5/6/7; Step8 pointer assigned above " + click.interaction.name + "; existing Step8 row uses subtitle8.");
    }
}
