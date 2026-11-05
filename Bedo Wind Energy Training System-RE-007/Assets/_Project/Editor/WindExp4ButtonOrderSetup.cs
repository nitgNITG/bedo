using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WindExp4ButtonOrderSetup
{
    [MenuItem("PV System/Order EXP4 Emergency CB On As Steps 5 6 7")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || scene.path != "Assets/_Project/Scenes/PV Lab.unity")
            throw new InvalidOperationException("Save PV Lab in Edit Mode first.");
        var owner = Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(o => o.StoryboardID == "OFF Grid System Using Wind Turbine");
        var steps = owner.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Steps");
        string[] names = { "Emergeny_button002 (1)", "single_432 (1)", "on_button004 (1)" };
        var rows = names.Select(name => owner.steps.Single(s => s.interaction != null && s.interaction.name == name)).ToArray();
        var folders = rows.Select(s => s.interaction.transform.parent).ToArray();
        if (folders.Any(t => t.parent != steps) || folders.Distinct().Count() != 3)
            throw new InvalidOperationException("Unexpected main-board button hierarchy.");
        var connectionRows = owner.steps.Where(s => s.interaction is WireInteraction w &&
            (w.name == "DC LED -ve to Off-Grid Inverter Battery -ve" || w.name == "Off-Grid Inverter Battery +ve to DC LED +ve")).ToArray();
        if (connectionRows.Length != 2) throw new InvalidOperationException("Step4 connections missing.");
        var step4 = connectionRows[0].interaction.transform.parent;
        if (connectionRows.Any(s => s.interaction.transform.parent != step4))
            throw new InvalidOperationException("Step4 hierarchy mismatch.");
        foreach (var row in rows) owner.steps.Remove(row);
        int insert = owner.steps.FindLastIndex(s => connectionRows.Contains(s)) + 1;
        owner.steps.InsertRange(insert, rows);
        for (int i = 0; i < folders.Length; i++)
        {
            folders[i].name = "Step " + (5 + i);
            folders[i].SetSiblingIndex(step4.GetSiblingIndex() + 1 + i);
            EditorUtility.SetDirty(folders[i].gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(folders[i].gameObject);
        }
        EditorUtility.SetDirty(owner);
        PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
        if (!owner.steps.Skip(insert).Take(3).SequenceEqual(rows)) throw new InvalidOperationException("Button order mismatch.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("EXP4 BUTTON ORDER SAVED: Emergency, CB, On moved after Step4 at elements " + insert + ", " + (insert + 1) + ", " + (insert + 2) + "; folders renamed Step5/6/7. Existing row objects, UI, interactions and events preserved.");
    }
}
