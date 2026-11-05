using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class V12FixRunner
{
    static V12FixRunner() { EditorApplication.update += Tick; }
    static void Tick()
    {
        const string flag = "Temp/v12-fix.flag";
        if (!File.Exists(flag) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string action = File.ReadAllText(flag).Trim(); File.Delete(flag);
        try { Run(action); } catch (Exception e) { File.WriteAllText("Temp/V12/error.txt", e.ToString()); }
    }
    public static string Path(Transform t) => t ? (t.parent ? Path(t.parent) + "/" : "") + t.name : "null";
    static void Run(string action)
    {
        var scene = SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var lines = new System.Text.StringBuilder("Scene " + scene.path + " dirty=" + scene.isDirty + " play=" + Application.isPlaying + "\n");
        string[] types = { "WireMaterialTransparencyToggle", "WindMonitorTabs", "ExperimentMenuTabHighlight", "MultipleChoiceQuiz", "WindTurbineSpeedController", "RheostatController", "MapCameraScreenshotButton_RT", "PVGraphController", "FreeLabControl", "FreeLabConnectionGroup", "FreeLabEndpoint" };
        foreach (var t in all)
        {
            foreach (var c in t.GetComponents<MonoBehaviour>().Where(c => c && types.Contains(c.GetType().Name)))
            { lines.AppendLine("--- " + GlobalObjectId.GetGlobalObjectIdSlow(c) + " " + Path(t) + " " + c.GetType().Name); lines.AppendLine(EditorJsonUtility.ToJson(c, true)); }
            if (t.name.IndexOf("Selection Tab", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Correct", StringComparison.OrdinalIgnoreCase) >= 0)
                lines.AppendLine("OBJECT " + GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject) + " " + Path(t) + " active=" + t.gameObject.activeSelf);
        }
        File.WriteAllText("Temp/V12/audit.txt", lines.ToString());
    }
}
