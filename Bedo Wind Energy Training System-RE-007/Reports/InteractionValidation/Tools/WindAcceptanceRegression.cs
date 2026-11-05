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
public static class WindAcceptanceRegression
{
    const string Prefix = "WindAcceptanceRegression.";
    static string Mode => SessionState.GetString(Prefix + "Mode", "before");
    static string Report => "Reports/InteractionValidation/" + Mode + ".smoke.txt";
    static string Samples => "Reports/InteractionValidation/" + Mode + ".samples.txt";
    static readonly string[] Ids = {
        "Speed Curve Characteristics",
        "Regulating DC Output from Wind Turbine using PWM Charger",
        "Regulating DC Output from Wind Turbine using MPPT Charger",
        "OFF Grid System Using Wind Turbine"
    };
    static bool Running { get => SessionState.GetBool(Prefix + "Running", false); set => SessionState.SetBool(Prefix + "Running", value); }
    static int Stage { get => SessionState.GetInt(Prefix + "Stage", 0); set => SessionState.SetInt(Prefix + "Stage", value); }
    static int Cycle { get => SessionState.GetInt(Prefix + "Cycle", 0); set => SessionState.SetInt(Prefix + "Cycle", value); }
    static int Index { get => SessionState.GetInt(Prefix + "Index", 0); set => SessionState.SetInt(Prefix + "Index", value); }
    static double Due { get => SessionState.GetFloat(Prefix + "Due", 0); set => SessionState.SetFloat(Prefix + "Due", (float)value); }
    static WindAcceptanceRegression()
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
        const string flag = "Temp/acceptance-regression.flag";
        if (!Running && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(flag) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            SessionState.SetString(Prefix + "Mode", File.ReadAllText(flag).Trim()); File.Delete(flag); File.WriteAllText(Samples, "");
            if (SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.WriteAllText(Report, "Unity " + Application.unityVersion + "; prefab regression smoke test.\n");
            SessionState.SetInt(Prefix + "Errors", 0); Index = 0; Cycle = 0; Stage = 0; Running = true;
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
            if (Stage == 10)
            {
                Require(manager && scene.name == "PV Lab", "Reset did not return to PV Lab");
                Require(!(bool)typeof(InteractionManager).GetField("restarting",Flags).GetValue(manager),"Restart still in progress");
                var rw = Resources.FindObjectsOfTypeAll<FreeLabWindController>().Single(w => w.gameObject.scene == scene);
                var session = UnityEngine.Object.FindFirstObjectByType<TutorialFramework.Simulation.WindSimulationSession>();
                Require(session, "Missing explicit reset owner");
                Require(session.connections.Count == 0, "Stale connection survived restart");
                foreach(var context in session.contexts) Require(context.Value.requestedRPM == 0 && !context.Value.turbineRunning, "Stale context input survived restart");
                foreach(var toggle in session.switches) Require(!toggle.Value,"Stale switch survived restart");
                if(manager.Free) foreach(var r in rw.rheostats) Require(Mathf.Approximately(r.controller.CurrentResistance,r.maximumOhms),"Free rheostat did not reset");
                File.AppendAllText(Report,"PASS restart cycle "+(Cycle+1)+"/20, "+manager.currentMode+": zero inputs/wires, switches off, rheostat defaults.\n");
                // Dirty state before every reload, including SOAP assets not active in this mode.
                foreach(var context in session.contexts) {var data=context.Value;data.requestedRPM=800;data.turbineRunning=true;context.Value=data;}
                foreach(var toggle in session.switches) toggle.Value=true;
                Cycle++;
                if(Cycle==20) { Stage=3;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;return; }
                if(Cycle%2==0)manager.SetFree();else manager.SetGuided();
                Due=EditorApplication.timeSinceStartup+7;return;
            }
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
            TestFreeSafety(wind);
            SampleFree(wind);
            Index++;
            if (Index == Ids.Length) { Stage = 10; manager.RestartScenario(); Due = EditorApplication.timeSinceStartup + 7; }
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
        wind.mainEmergency.SetValue(true);wind.mainBreaker.SetValue(true);wind.mainPower.SetValue(true);
        wind.windEmergency.SetValue(true);wind.windBreaker.SetValue(true);wind.windPower.SetValue(true);wind.inverterPower.SetValue(true);
        for(int rpm=0;rpm<=1000;rpm+=200)
        {
            wind.SetRPM(rpm);wind.Refresh();
            File.AppendAllText(Samples,"F"+(Index+1)+","+rpm+":"+Numbers(wind.Reading)+";"+Numbers(wind.OffGridReading)+";"+wind.ActiveExperiment+","+wind.ActiveResistance.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+wind.lab.ConnectionCount+"\n");
        }
        wind.mainPower.SetValue(false);wind.Refresh();
        File.AppendAllText(Samples,"F"+(Index+1)+",OFF:"+Numbers(wind.Reading)+";"+Numbers(wind.OffGridReading)+"\n");
        foreach(var wire in wind.lab.Connections.ToArray())Require(wind.lab.RemoveConnection(wire),"Could not remove wire after power off");
        Require(wind.lab.ConnectionCount==0,"Connection list did not clear");
    }
    static void TestFreeSafety(FreeLabWindController wind)
    {
        Require(!wind.mainPower.CanEnable(wind.lab),"Main power accepted disconnected lab");
        Require(!wind.windPower.CanEnable(wind.lab),"Wind power accepted disconnected lab");
        var create=typeof(FreeLabManager).GetMethod("CreateConnection",Flags);
        var invalid=wind.definitions.Select(d=>d.endpointA).Distinct().SelectMany(a=>wind.definitions.Select(d=>d.endpointB).Distinct(),(a,b)=>new {a,b}).First(p=>p.a!=p.b && !wind.definitions.Any(d=>(d.endpointA==p.a&&d.endpointB==p.b)||(d.endpointB==p.a&&d.endpointA==p.b)));
        Require(!(bool)create.Invoke(wind.lab,new object[]{invalid.a,invalid.b}),"Unauthored connection was accepted");
        var definition=wind.definitions.First();
        Require((bool)create.Invoke(wind.lab,new object[]{definition.endpointA,definition.endpointB}),"Authored cable failed");
        var wire=wind.lab.Connections.Single();
        Require(wire.gameObject==definition.wireParentTemplate,"Cable was duplicated");
        Require(wire.GetComponentsInChildren<Collider>(true).All(c=>c.gameObject==wire.gameObject || c.GetComponent<FreeLabWireClickForwarder>()),"Missing removal click forwarding");
        wind.mainEmergency.SetValue(true); wind.mainBreaker.SetValue(true); wind.mainPower.SetValue(true);
        Require(!wind.lab.RemoveConnection(wire),"Powered cable could be removed");
        wind.mainEmergency.SetValue(false);
        Require(!wind.mainPower.Value,"Emergency did not cut main power");
        Require(wind.lab.RemoveConnection(wire),"Unpowered wire could not be removed");
        Require(wind.lab.ConnectionCount==0,"Wire removal left a stale connection");
        wind.lab.CloseFreeModeWarning();
        File.AppendAllText(Report,"PASS EXP"+(Index+1)+" disconnected activation, invalid cable rejection, exact authored cable reuse, removal forwarding, operating restriction, emergency cut and power-off removal.\n");
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}





