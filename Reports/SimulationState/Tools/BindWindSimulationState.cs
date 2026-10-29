using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TutorialFramework;
using TutorialFramework.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BindWindSimulationState
{
    const string Folder = "Assets/_Project/Data/Wind/Simulation";
    static readonly List<WindFloatVariable> numbers = new List<WindFloatVariable>();
    static readonly List<WindBoolVariable> switches = new List<WindBoolVariable>();
    static BindWindSimulationState() { EditorApplication.update += Poll; }
    static void Poll()
    {
        const string flag = "Temp/bind-wind-state.flag";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(flag)) return;
        File.Delete(flag);
        try { Bind(); File.WriteAllText("Reports/SimulationState/Binding.txt", "PASS SOAP state bindings saved; Main Menu and experiment steps untouched.\n"); }
        catch (Exception ex) { File.WriteAllText("Reports/SimulationState/Binding.txt", ex.ToString()); Debug.LogException(ex); }
    }

    [MenuItem("Tools/Wind Simulation/Bind State Assets")]
    public static void Bind()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty || scene.path != "Assets/_Project/Scenes/PV Lab.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open the saved PV Lab scene outside Play Mode before binding.");
        if (Resources.FindObjectsOfTypeAll<WindSimulationSession>().Any(s => s.gameObject.scene == scene))
            throw new InvalidOperationException("State is already bound; refusing to recreate or overwrite assets.");
        if (Directory.Exists(Folder)) throw new InvalidOperationException("Simulation asset folder already exists; review before retrying.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        numbers.Clear(); switches.Clear();
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var speeds = all.Select(t => t.GetComponent<WindTurbineSpeedController>()).Where(c => c && c.HasWindModel).ToArray();
        if (speeds.Length != 4) throw new InvalidOperationException("Expected four wind speed owners.");
        foreach (var speed in speeds)
        {
            using (var so = new SerializedObject(speed))
            foreach (var pair in new[] { ("minSpeed",0f), ("maxSpeed",1000f), ("speedStep",100f), ("initialSpeed",0f), ("visualSpeedMultiplier",.05f) })
                if (!Mathf.Approximately(so.FindProperty(pair.Item1).floatValue, pair.Item2))
                    throw new InvalidOperationException("Unexpected speed configuration; cannot safely share settings: " + speed.name + "/" + pair.Item1);
        }
        var settings = Create<WindSimulationSettings>("Speed Settings");
        using (var so = new SerializedObject(settings))
        {
            Reference(so,"maximumRPM",Number("Speed/Maximum RPM",1000)); Reference(so,"sliderIncrement",Number("Speed/Slider Increment",200));
            Reference(so,"minimumRPM",Number("Speed/Minimum RPM",0)); Reference(so,"initialRPM",Number("Speed/Initial RPM",0));
            Reference(so,"legacyClickIncrement",Number("Speed/Legacy Click Increment",100)); Reference(so,"visualRotorMultiplier",Number("Speed/Visual Multiplier",.05f));
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        var contexts = new List<WindRuntimeState>();
        foreach (var speed in speeds.OrderBy(c => c.GetComponentInParent<StoryboardSteps>(true).name))
        {
            var storyboard = speed.GetComponentInParent<StoryboardSteps>(true);
            string label = storyboard.name;
            var state = Create<WindRuntimeState>(label + "/Runtime State"); contexts.Add(state);
            using (var so = new SerializedObject(state)) { so.FindProperty("_value.activeExperiment").intValue = int.Parse(label.Substring(label.Length-1)); so.ApplyModifiedPropertiesWithoutUndo(); }
            using (var so = new SerializedObject(speed)) { so.FindProperty("runtimeState").objectReferenceValue=state; so.FindProperty("simulationSettings").objectReferenceValue=settings; Commit(so,speed); }
        }
        var free = all.Select(t=>t.GetComponent<FreeLabWindController>()).Single(c=>c);
        var freeState = Create<WindRuntimeState>("Free Mode/Runtime State"); contexts.Add(freeState);
        using(var so=new SerializedObject(freeState)) { so.FindProperty("_value.activeExperiment").intValue=1; so.ApplyModifiedPropertiesWithoutUndo(); }
        using(var so=new SerializedObject(free)) { so.FindProperty("runtimeState").objectReferenceValue=freeState;so.FindProperty("simulationSettings").objectReferenceValue=settings;Commit(so,free); }
        var connections=Create<WindConnectionState>("Free Mode/Physical Connections");
        using(var so=new SerializedObject(free.lab)) {so.FindProperty("connectionState").objectReferenceValue=connections;Commit(so,free.lab);}
        foreach(var control in free.controls.Select(c=>c.control).Distinct())
        {
            var variable=Create<WindBoolVariable>("Switches/"+Safe(control.ControlId)); switches.Add(variable);
            using(var so=new SerializedObject(control))
            {
                using(var asset=new SerializedObject(variable)){asset.FindProperty("_value").boolValue=so.FindProperty("value").boolValue;asset.ApplyModifiedPropertiesWithoutUndo();}
                Reference(so,"switchState",variable);Commit(so,control);
            }
        }
        int index=0;
        foreach(var rheostat in all.Select(t=>t.GetComponent<RheostatController>()).Where(c=>c))
        {
            string prefix="Rheostats/"+(++index)+" "+Safe(rheostat.name);
            using(var so=new SerializedObject(rheostat))
            {
                Reference(so,"resistanceState",Number(prefix+"/Resistance",0));
                foreach(var pair in new[]{("maximumResistance","maxResistance"),("minimumResistance","minResistance"),("resistanceIncrement","resistanceStep"),("rotationIncrement","rotationPerClick")})
                    Reference(so,pair.Item1,Number(prefix+"/"+pair.Item2,so.FindProperty(pair.Item2).floatValue));
                Commit(so,rheostat);
            }
        }
        foreach(var binder in all.Select(t=>t.GetComponent<PVGuidedDataBinder>()).Where(c=>c&&c.UsesWindModel))
        {
            string prefix=binder.GetComponentInParent<StoryboardSteps>(true).name+"/Charger";
            using(var so=new SerializedObject(binder))
            {
                foreach(string property in new[]{"pwmLoadCurrent","pwmBatteryVoltage","pwmMaximumChargeCurrent","pwmScreenChangePerSecond"})
                    Reference(so,property+"Reference",Number(prefix+"/"+property,so.FindProperty(property).floatValue));
                Commit(so,binder);
            }
        }
        var owner=new GameObject("Wind Simulation Session");
        owner.transform.SetParent(scene.GetRootGameObjects().Single(g=>g.name=="Systems").transform,false);
        var session=owner.AddComponent<WindSimulationSession>();session.contexts=contexts.ToArray();session.numbers=numbers.ToArray();session.switches=switches.ToArray();session.connections=connections;
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save state assignments.");
        File.WriteAllText("Reports/SimulationState/Assets.txt",contexts.Count+" contexts, "+numbers.Count+" numeric variables, "+switches.Count+" switches, 1 connection list.\n"+string.Join("\n",AssetDatabase.FindAssets("",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath)));
    }

    static string Safe(string value)=>string.Concat(value.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
    static T Create<T>(string name)where T:ScriptableObject
    {
        var path=Folder+"/"+name+".asset";Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
        var asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
    }
    static WindFloatVariable Number(string name,float value)
    {
        var asset=Create<WindFloatVariable>(name);using(var so=new SerializedObject(asset)){so.FindProperty("_value").floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}numbers.Add(asset);return asset;
    }
    static void Reference(SerializedObject so,string path,UnityEngine.Object asset)
    {var p=so.FindProperty(path);p.FindPropertyRelative("UseLocal").boolValue=false;p.FindPropertyRelative("Variable").objectReferenceValue=asset;}
    static void Commit(SerializedObject so,UnityEngine.Object owner)
    {so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(owner);if(PrefabUtility.IsPartOfPrefabInstance(owner))PrefabUtility.RecordPrefabInstancePropertyModifications(owner);}
}
