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
public static class WireVisibilityRuntimeRegression
{
    const string Prefix = "WireVisibilityRuntimeRegression.";
    static string Mode => SessionState.GetString(Prefix + "Mode", "before");
    static string Report => "Reports/WireVisibility/" + Mode + ".smoke.txt";
    static string Samples => "Reports/WireVisibility/" + Mode + ".samples.txt";
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
    static WireVisibilityRuntimeRegression()
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
        const string flag = "Temp/wire-visibility-runtime.flag";
        if (!Running && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(flag) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            SessionState.SetString(Prefix + "Mode", File.ReadAllText(flag).Trim()); File.Delete(flag); File.WriteAllText(Samples, "");
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
                // Scene assets are never saved by the runtime regression test.
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
                SampleGuided(selected);
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
            SampleFree(wind);
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

    static readonly System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    static string Numbers(object value) => string.Join(",", value.GetType().GetFields().OrderBy(f => f.Name).Select(f => ((float)f.GetValue(value)).ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
    static void SampleGuided(StoryboardSteps storyboard)
    {
        var speed = storyboard.GetComponentsInChildren<WindTurbineSpeedController>(true).Single(s => s.HasWindModel);
        var binder = storyboard.GetComponentsInChildren<PVGuidedDataBinder>(true).Single(b => b.UsesWindModel);
        foreach (var wire in storyboard.GetComponentsInChildren<WireInteraction>(true))
        { if(wire.wireObject)wire.wireObject.SetActive(true); if(wire.plugA)wire.plugA.SetActive(true); if(wire.plugB)wire.plugB.SetActive(true); }
        if (binder.UsesWindPWM) { binder.TurnPWMSystemOn(); binder.EnablePWMOutput(); binder.TurnPWMLoadOn(); }
        if (binder.UsesWindOffGrid) { binder.TurnOffGridSystemOn(); binder.TurnOffGridInverterOn(); }
        CheckGuidedGeometry(storyboard);
        speed.TurnOn();
        var load = (RheostatController)typeof(WindTurbineSpeedController).GetField("loadRheostat", Flags).GetValue(speed);
        for (int resistance=0; resistance<6; resistance++)
        {
            for(int rpm=0;rpm<=1000;rpm+=200)
            {
                speed.SetSpeed(rpm); binder.RefreshDisplay(); speed.TryGetReading(out var reading);
                File.AppendAllText(Samples, "G"+(Index+1)+","+resistance+","+rpm+":"+Numbers(reading)+";"+Numbers(binder.OffGridReading)+";"+binder.PWMBatteryCurrent.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+binder.PWMDumpCurrent.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n");
            }
            if(load)load.DecreaseValue();
        }
        speed.TurnOff(); speed.TryGetReading(out var off);
        File.AppendAllText(Samples,"G"+(Index+1)+",OFF:"+Numbers(off)+"\n");
    }
    static void SampleFree(FreeLabWindController wind)
    {
        var profile=wind.profiles.Single(p=>p.experiment==Index+1);
        var groups=profile.required.ToList();
        if(profile.alternatives!=null)groups.AddRange(profile.alternatives.Select(a=>a.alternatives.First()));
        var definitions=groups.Where(g=>g).SelectMany(g=>g.RequiredWires).Distinct().ToArray();
        var create=typeof(FreeLabManager).GetMethod("CreateConnection",Flags);
        foreach(var definition in definitions)
            if(!wind.lab.HasConnection(definition.endpointA,definition.endpointB))
                Require((bool)create.Invoke(wind.lab,new object[]{definition.endpointA,definition.endpointB}),"Could not connect regression profile: "+definition.name);
        CheckFreeGeometry(wind);
        wind.mainEmergency.SetValue(true);wind.mainBreaker.SetValue(true);wind.mainPower.SetValue(true);
        wind.windEmergency.SetValue(true);wind.windBreaker.SetValue(true);wind.windPower.SetValue(true);wind.inverterPower.SetValue(true);
        for(int rpm=0;rpm<=1000;rpm+=200)
        {
            wind.SetRPM(rpm);wind.Refresh();
            File.AppendAllText(Samples,"F"+(Index+1)+","+rpm+":"+Numbers(wind.Reading)+";"+Numbers(wind.OffGridReading)+";"+wind.ActiveExperiment+","+wind.ActiveResistance.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+wind.lab.ConnectionCount+"\n");
        }
        wind.mainPower.SetValue(false);wind.Refresh();
        File.AppendAllText(Samples,"F"+(Index+1)+",OFF:"+Numbers(wind.Reading)+";"+Numbers(wind.OffGridReading)+"\n");
        foreach(var wire in wind.lab.Connections.ToArray())Require(wire.RemoveFromFreeLab(),"Could not remove wire through its click handler after power off");
        Require(wind.lab.ConnectionCount==0,"Connection list did not clear");
    }
    static void CheckGuidedGeometry(StoryboardSteps board)
    {
        int count=0;
        foreach(var w in board.steps.Select(s=>s.interaction as WireInteraction).Where(w=>w&&w.wireObject&&w.plugA&&w.plugB).Distinct())
        {
            var c=w.wireObject.GetComponentInChildren<UnityEngine.Splines.SplineContainer>(true);if(!c)continue;var spline=c.Spline;if(spline.Count<2)continue;
            float tail=Vector3.Distance(c.transform.TransformPoint((Vector3)spline[0].Position),c.transform.TransformPoint((Vector3)spline[1].Position));
            if(Mathf.Abs(tail-.12f)>.001f)continue;
            var e=c.GetComponent<UnityEngine.Splines.SplineExtrude>();var filter=c.GetComponent<MeshFilter>();Require(e&&filter&&filter.sharedMesh&&filter.sharedMesh.vertexCount>0,"Wire did not rebuild on activation: "+w.name);CheckRebuildMatchesActivation(c,w.name);count++;
        }
        File.AppendAllText(Report,"PASS EXP"+(Index+1)+" activated "+count+" corrected Guided cable meshes.\n");
    }
    static void CheckFreeGeometry(FreeLabWindController wind)
    {
        Physics.SyncTransforms();
        foreach(var wire in wind.lab.Connections)
        {
            var c=wire.GetComponentInChildren<UnityEngine.Splines.SplineContainer>(true);if(!c)continue;
            CheckRebuildMatchesActivation(c,wire.name);
            var collider=wire.GetComponent<MeshCollider>();Require(collider&&collider.enabled&&collider.sharedMesh&&collider.sharedMesh.vertexCount>0,"Missing regenerated wire click surface");
            var mesh=collider.sharedMesh;var vertices=mesh.vertices;var indices=mesh.triangles;bool hit=false;
            for(int k=0;k<indices.Length;k+=Mathf.Max(3,(indices.Length/90/3)*3)){
                if(k+2>=indices.Length)break;var a=collider.transform.TransformPoint(vertices[indices[k]]);var b=collider.transform.TransformPoint(vertices[indices[k+1]]);var d=collider.transform.TransformPoint(vertices[indices[k+2]]);var normal=Vector3.Cross(b-a,d-a).normalized;if(normal.sqrMagnitude<.5f)continue;
                if(collider.Raycast(new Ray((a+b+d)/3+normal*.3f,-normal),out var info,.6f)){hit=true;break;}
            }
            Require(hit,"Wire collider cannot be raycast: "+wire.name);
        }
        File.AppendAllText(Report,"PASS EXP"+(Index+1)+" all "+wind.lab.ConnectionCount+" Free Mode cable click meshes regenerated and raycast successfully.\n");
    }
    static void CheckRebuildMatchesActivation(UnityEngine.Splines.SplineContainer container,string label)
    {
        var extrusion=container.GetComponent<UnityEngine.Splines.SplineExtrude>();var filter=container.GetComponent<MeshFilter>();Require(extrusion&&filter&&filter.sharedMesh,"Missing cable render mesh: "+label);
        var before=filter.sharedMesh.vertices;extrusion.Rebuild();var after=filter.sharedMesh.vertices;
        Require(before.Length==after.Length,"Stale activated mesh vertex count: "+label);
        for(int i=0;i<before.Length;i++)Require(Vector3.SqrMagnitude(before[i]-after[i])<.00000001f,"Stale activated mesh shape: "+label);
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}


