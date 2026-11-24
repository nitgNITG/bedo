using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Read-only inventory for the equipment prefab acceptance pass.</summary>
public static class EquipmentPrefabWorkflow
{
    public const string ReportFolder = "Reports/EquipmentPrefabs";

    [MenuItem("Tools/Wind Equipment/Audit Delivery Scenes")]
    public static void AuditBoth()
    {
        var previous = SceneManager.GetActiveScene();
        if (previous.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Save your scene and leave Play Mode before auditing delivery scenes.");
        var previousPath = previous.path;
        foreach (var path in new[] { "Assets/_Project/Scenes/Main Menu.unity", "Assets/_Project/Scenes/Hydro Lab.unity" })
        {
            EditorSceneManager.OpenScene(path); Audit();
        }
        if (SceneManager.GetActiveScene().path != previousPath) EditorSceneManager.OpenScene(previousPath);
    }

    [MenuItem("Tools/Wind Equipment/Audit Current Scene")]
    public static void Audit()
    {
        Directory.CreateDirectory(ReportFolder);
        var scene = SceneManager.GetActiveScene();
        var report = new StringBuilder();
        report.AppendLine("Scene: " + scene.path + " dirty=" + scene.isDirty);
        report.AppendLine("Unity: " + Application.unityVersion);
        foreach (var root in scene.GetRootGameObjects())
        {
            report.AppendLine("ROOT " + root.name);
            if (root.name == "Equipment") Tree(root.transform, report, 0, 6);
            if (root.name == "Experiments") Tree(root.transform, report, 0, 2);
        }
        int missing = All(scene).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        report.AppendLine("Transforms: " + All(scene).Count() + "; missing components: " + missing);
        var roots = All(scene).Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray();
        report.AppendLine("Connected prefab instance roots: " + roots.Length);
        File.WriteAllText(ReportFolder + "/" + scene.name + ".prefabs.tsv", "Hierarchy\tAsset type\tNearest source\tOriginal source\n" +
            string.Join("\n", roots.Select(t => PathOf(t) + "\t" + PrefabUtility.GetPrefabAssetType(t.gameObject) + "\t" +
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) + "\t" +
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject)))));
        var free = All(scene).Select(t => t.GetComponent<TutorialFramework.FreeLabWindController>()).FirstOrDefault(c => c);
        if (free)
        {
            report.AppendLine("FREE LAMP MAPPING");
            foreach (var lamp in free.lamps) report.AppendLine(lamp.name + " ac=" + lamp.ac + " led=" + lamp.led + " light=" + (lamp.light ? PathOf(lamp.light.transform) : "NULL"));
        }
        File.WriteAllText(ReportFolder + "/" + scene.name + ".inventory.txt", report.ToString());
        Debug.Log("Equipment prefab inventory complete: " + scene.name);
    }

    public static System.Collections.Generic.IEnumerable<Transform> All(Scene scene)
        => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true));
    public static string PathOf(Transform t) => (t.parent ? PathOf(t.parent) + "/" : "") + t.name + "[" + t.GetSiblingIndex() + "]";

    static void Tree(Transform t, StringBuilder output, int depth, int maxDepth)
    {
        var components = t.GetComponents<Component>().Where(c => c && !(c is Transform))
            .Select(c => c.GetType().Name).ToArray();
        output.AppendLine(new string(' ', depth * 2) + t.name + " [" + t.childCount + "] " +
            PrefabUtility.GetPrefabAssetType(t.gameObject) + " root=" + PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) +
            " active=" + t.gameObject.activeSelf + " source=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) +
            " components=" + string.Join(",", components));
        if (depth >= maxDepth) return;
        foreach (Transform child in t) Tree(child, output, depth + 1, maxDepth);
    }
}
