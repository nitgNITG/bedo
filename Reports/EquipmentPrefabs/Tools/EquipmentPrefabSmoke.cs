using System;
using System.IO;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EquipmentPrefabSmoke
{
    const string Prefix = "WindPrefabSmoke.";
    const string Report = "Reports/EquipmentPrefabs/RuntimeSmoke.txt";
    static readonly string[] Ids = {
        "Speed Curve Characteristics",
        "Regulating DC Output from Wind Turbine using PWM Charger",
        "Regulating DC Output from Wind Turbine using MPPT Charger",
        "OFF Grid System Using Wind Turbine"
    };
    static bool Running { get => SessionState.GetBool(Prefix + "Running", false); set => SessionState.SetBool(Prefix + "Running", value); }
    static int Stage { get => SessionState.GetInt(Prefix + "Stage", 0); set => SessionState.SetInt(Prefix + "Stage", value); }
    static int Index { get => SessionState.GetInt(Prefix + "Index", 0); set => SessionState.SetInt(Prefix + "Index", value); }
    static double Due { get => SessionState.GetFloat(Prefix + "Due", 0); set => SessionState.SetFloat(Prefix + "Due", (float)value); }
    static EquipmentPrefabSmoke()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += LogError;
    }

    static void LogError(string message, string stack, LogType type)
    {
        if (Running && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            SessionState.SetInt(Prefix + "Errors", SessionState.GetInt(Prefix + "Errors", 0) + 1);
            File.AppendAllText(Report, "RUNTIME " + type + ": " + message + "\n");
        }
    }

    static void Tick()
    {
        const string flag = "Temp/equipment-smoke.flag";
        if (!Running && File.Exists(flag) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            File.Delete(flag);
            if (SceneManager.GetActiveScene().isDirty) { File.WriteAllText(Report, "BLOCKED: unsaved scene changes.\n"); return; }
            File.WriteAllText(Report, "Unity " + Application.unityVersion + "; prefab regression smoke test.\n");
            SessionState.SetInt(Prefix + "Errors", 0); Index = 0; Stage = 0; Running = true;
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");
            Due = EditorApplication.timeSinceStartup + 6;
            EditorApplication.EnterPlaymode(); return;
        }
        if (!Running || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < Due) return;
        try
        {
            var scene = SceneManager.GetActiveScene();
            if (Stage == 3)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");
                Stage = 4; Due = EditorApplication.timeSinceStartup + 4; return;
            }
            if (Stage == 4)
            {
                EquipmentPrefabConversion.VerifyBaseline(scene);
                int errors = SessionState.GetInt(Prefix + "Errors", 0);
                File.AppendAllText(Report, (errors == 0 ? "PASS" : "FAIL") + ": " + errors + " runtime errors; edit scene restored and baseline verified.\n");
                Running = false; return;
            }
            if (!EditorApplication.isPlaying) throw new Exception("Play Mode ended unexpectedly.");
            var manager = InteractionManager.Instance;
            Require(manager, "InteractionManager is unavailable");
            if (Stage == 0)
            {
                Require(scene.name == "Main Menu", "Expected Main Menu");
                var launch = Resources.FindObjectsOfTypeAll<MainMenuExperimentLauncher>().Single(l => l.gameObject.scene == scene &&
                    new SerializedObject(l).FindProperty("storyboardId").stringValue == Ids[Index]);
                manager.currentMode = ExperimentMode.Guided;
                launch.GetComponent<Button>().onClick.Invoke();
                Stage = 1; Due = EditorApplication.timeSinceStartup + 7; return;
            }
            Require(scene.name == "PV Lab", "Expected PV Lab");
            Require(manager.ActiveStoryboardName == Ids[Index], "Selected storyboard identity changed");
            var storyboards = Resources.FindObjectsOfTypeAll<StoryboardSteps>().Where(s => s.gameObject.scene == scene).ToArray();
            File.AppendAllText(Report, "Observed runtime storyboards: " + string.Join(", ", storyboards.Select(s => s.StoryboardID + " @ " + s.name)) + "\n");
            var selected = storyboards.Single(s => s.StoryboardID == Ids[Index]);
            var wind = Resources.FindObjectsOfTypeAll<FreeLabWindController>().Single(w => w.gameObject.scene == scene);
            Require(wind.sourceModel && wind.controllerModel && wind.offGridModel, "Missing scientific model binding");
            Require(wind.definitions.Length > 0 && wind.definitions.All(d => d && d.endpointA && d.endpointB && d.wireParentTemplate), "Missing wire definition binding");
            Require(wind.lamps.Length == 8 && wind.lamps.All(l => l.light), "Missing lamp binding");
            if (Stage == 1)
            {
                Require(manager.Guided && selected.gameObject.activeInHierarchy, "Guided experiment is not active");
                File.AppendAllText(Report, "PASS EXP" + (Index + 1) + " Main Menu -> Guided: " + selected.steps.Count + " storyboard entries; model, lamp and " + wind.definitions.Length + " wire references resolve.\n");
                manager.SetFree(); Stage = 2; Due = EditorApplication.timeSinceStartup + 7; return;
            }
            Require(manager.Free && wind.IsReady, "Free Mode did not initialize");
            Require(wind.rheostats.Length == 3, "Expected three rheostats");
            foreach (var r in wind.rheostats)
            {
                Require(r.controller && r.adjustmentCanvas, "Missing rheostat/controller canvas binding");
                Require(Mathf.Approximately(r.controller.CurrentResistance, r.maximumOhms), "Incorrect initial resistance");
                var buttons = r.adjustmentCanvas.GetComponentsInChildren<Button>(true);
                buttons.Single(b => b.name == "Minus").onClick.Invoke();
                Require(Mathf.Approximately(r.controller.CurrentResistance, r.maximumOhms * .8f), "Minus is linked to the wrong rheostat");
                buttons.Single(b => b.name == "Plus").onClick.Invoke();
                Require(Mathf.Approximately(r.controller.CurrentResistance, r.maximumOhms), "Plus is linked to the wrong rheostat");
            }
            File.AppendAllText(Report, "PASS EXP" + (Index + 1) + " Guided -> Free: ready; 100/150/800-ohm ranges and each canvas's +/- controls verified.\n");
            Index++;
            if (Index == Ids.Length) { Stage = 3; EditorApplication.ExitPlaymode(); Due = EditorApplication.timeSinceStartup + 2; }
            else { manager.ReturnToExperimentMenu(); Stage = 0; Due = EditorApplication.timeSinceStartup + 6; }
        }
        catch (Exception ex)
        {
            SessionState.SetInt(Prefix + "Errors", SessionState.GetInt(Prefix + "Errors", 0) + 1);
            File.AppendAllText(Report, "FAIL " + ex + "\n");
            if (EditorApplication.isPlaying) { Stage = 3; EditorApplication.ExitPlaymode(); Due = EditorApplication.timeSinceStartup + 2; }
            else Running = false;
        }
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}


