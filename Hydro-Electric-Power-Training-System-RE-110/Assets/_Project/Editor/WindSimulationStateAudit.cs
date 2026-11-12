using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TutorialFramework;
using TutorialFramework.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class WindSimulationStateAudit
{
    [Serializable] sealed class Entry { public string key,hash; }
    [Serializable] sealed class Baseline { public int transforms,missing; public List<Entry> entries; }

    [MenuItem("Tools/Wind Simulation/Audit State Ownership")]
    public static void Audit()
    {
        var scene=SceneManager.GetActiveScene();
        Require(scene.path=="Assets/_Project/Scenes/PV Lab.unity"&&!scene.isDirty&&!EditorApplication.isPlayingOrWillChangePlaymode,"Open saved PV Lab outside Play Mode.");
        Canvas.ForceUpdateCanvases();
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var session=all.Select(t=>t.GetComponent<WindSimulationSession>()).Single(s=>s);
        Require(session.contexts.Length==5&&session.contexts.Distinct().Count()==5,"Expected five independent contexts.");
        Require(session.switches.Length==8&&session.switches.Distinct().Count()==8,"Switches must not share mutable values.");
        foreach(var c in all.Select(t=>t.GetComponent<WindTurbineSpeedController>()).Where(c=>c&&c.HasWindModel))
        {
            Require(c.SharedState&&session.contexts.Contains(c.SharedState),"Missing guided context.");
            Require(new SerializedObject(c).FindProperty("simulationSettings").objectReferenceValue,"Missing speed settings.");
        }
        var free=all.Select(t=>t.GetComponent<FreeLabWindController>()).Single(c=>c);
        Require(free.SharedState==session.contexts.Last(),"Free Mode must own its separate context.");
        Require(new SerializedObject(free.lab).FindProperty("connectionState").objectReferenceValue==session.connections,"Connection list has multiple owners.");
        foreach(var r in all.Select(t=>t.GetComponent<RheostatController>()).Where(c=>c))
        using(var so=new SerializedObject(r))foreach(string field in new[]{"resistanceState","maximumResistance","minimumResistance","resistanceIncrement","rotationIncrement"})Bound(so,field);
        foreach(var c in free.controls)using(var so=new SerializedObject(c.control))Bound(so,"switchState");
        foreach(var b in all.Select(t=>t.GetComponent<PVGuidedDataBinder>()).Where(c=>c&&c.UsesWindModel))
        using(var so=new SerializedObject(b))foreach(string field in new[]{"pwmLoadCurrentReference","pwmBatteryVoltageReference","pwmMaximumChargeCurrentReference","pwmScreenChangePerSecondReference"})Bound(so,field);
        foreach(var asset in session.contexts.Cast<Object>().Concat(session.numbers).Concat(session.switches))
        using(var so=new SerializedObject(asset))
        {Require(!so.FindProperty("_saved").boolValue,"Simulation state must not persist via PlayerPrefs.");Require(so.FindProperty("_resetOn").enumValueIndex==2,"Session reset must be explicit.");}
        Require(session.connections.Count==0,"Destroyed connection references leaked out of Play Mode.");

        Baseline baseline;
        using(var zip=new ZipArchive(File.OpenRead("Reports/EquipmentPrefabs/VerificationBaseline.zip"),ZipArchiveMode.Read))
        using(var reader=new StreamReader(zip.GetEntry("Baseline.json").Open()))baseline=JsonUtility.FromJson<Baseline>(reader.ReadToEnd());
        var objects=all.Where(t=>!t.IsChildOf(session.transform)).SelectMany(t=>new Object[]{t.gameObject}.Concat(t.GetComponents<Component>().Where(c=>c))).ToArray();
        var keys=objects.ToDictionary(o=>o.GetInstanceID(),Key);
        var expected=baseline.entries.ToDictionary(e=>e.key,e=>e.hash);var actual=new Dictionary<string,string>();
        using(var sha=SHA256.Create())foreach(var obj in objects)
        {
            var json=EditorJsonUtility.ToJson(obj);
            json=WithoutNewFields(json,Excluded(obj));
            if(obj is Transform t&&t.name=="Systems"&&!t.parent)
                json=json.Replace(",{\"instanceID\":"+session.transform.GetInstanceID()+"}","");
            foreach(string f in new[]{"m_CorrespondingSourceObject","m_PrefabInstance","m_PrefabAsset"})json=Regex.Replace(json,"\\\""+f+"\\\":\\{[^}]*\\}","\""+f+"\":{}");
            json=Regex.Replace(json,"\\\"instanceID\\\":(-?\\d+)",m=>
            {
                int id=int.Parse(m.Groups[1].Value);if(id==0)return "\"object\":\"null\"";
                if(keys.TryGetValue(id,out string key))return "\"object\":\""+key.Replace("\\","\\\\").Replace("\"","\\\"")+"\"";
                var value=EditorUtility.InstanceIDToObject(id);
                if(value&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long local))return "\"asset\":\""+guid+":"+local+"\"";
                return "\"unknown\":"+id;
            });
            actual.Add(keys[obj.GetInstanceID()],BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json)))) ;
        }
        var diffs=expected.Keys.Union(actual.Keys).Where(k=>!expected.ContainsKey(k)||!actual.ContainsKey(k)||expected[k]!=actual[k]).ToArray();
        File.WriteAllLines("Reports/SimulationState/SerializationDifferences.txt",diffs);
        Require(diffs.Length==0,"Existing serialized content differs: "+diffs.Length+" objects. See SerializationDifferences.txt.");
        int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        Require(missing==baseline.missing,"Missing-component count changed.");
        File.WriteAllText("Reports/SimulationState/StructuralAudit.txt","PASS: five independent SOAP contexts, eight independent switch assets, all production references bound.\nPASS: session-owned connection list; no destroyed wire references in Edit Mode.\nPASS: state is not saved to PlayerPrefs and uses explicit session resets.\nPASS: all "+actual.Count+" existing serialized object/component hashes match; only new SOAP properties and the new session child are excluded.\nPASS: existing transforms, steps, events, wiring, visual data and references preserved; "+missing+" pre-existing missing components unchanged.\n");
    }
    static void Bound(SerializedObject so,string field){var r=so.FindProperty(field);Require(!r.FindPropertyRelative("UseLocal").boolValue&&r.FindPropertyRelative("Variable").objectReferenceValue,"Unbound SOAP reference: "+so.targetObject.name+"/"+field);}
    static string[] Excluded(Object obj)
    {
        if(obj is RheostatController)return new[]{"resistanceState","maximumResistance","minimumResistance","resistanceIncrement","rotationIncrement"};
        if(obj is WindTurbineSpeedController||obj is FreeLabWindController)return new[]{"runtimeState","simulationSettings"};
        if(obj is FreeLabControl)return new[]{"switchState"};
        if(obj is FreeLabManager)return new[]{"connectionState"};
        if(obj is PVGuidedDataBinder)return new[]{"pwmLoadCurrentReference","pwmBatteryVoltageReference","pwmMaximumChargeCurrentReference","pwmScreenChangePerSecondReference"};
        return Array.Empty<string>();
    }
    // Remove named top-level JSON properties without reformatting original data.
    static string WithoutNewFields(string json,string[] excluded)
    {
        if(excluded.Length==0)return json;
        const string wrapper="{\"MonoBehaviour\":";
        if(json.StartsWith(wrapper,StringComparison.Ordinal))return wrapper+WithoutNewFields(json.Substring(wrapper.Length,json.Length-wrapper.Length-1),excluded)+"}";
        var fields=new List<string>();int p=1;
        while(p<json.Length-1)
        {
            int start=p;Require(json[p]=='"',"Unexpected JSON key.");int end=++p;while(json[end]!='"'||json[end-1]=='\\')end++;
            string name=json.Substring(p,end-p);p=end+2;int depth=0;bool quoted=false,escape=false;
            while(p<json.Length-1)
            {
                char c=json[p];if(quoted){if(escape)escape=false;else if(c=='\\')escape=true;else if(c=='"')quoted=false;}
                else if(c=='"')quoted=true;else if(c=='{'||c=='[')depth++;else if(c=='}'||c==']')depth--;else if(c==','&&depth==0)break;p++;
            }
            if(!excluded.Contains(name))fields.Add(json.Substring(start,p-start));if(p<json.Length-1)p++;
        }
        return "{"+string.Join(",",fields)+"}";
    }
    static string Key(Object obj)
    {
        if(obj is GameObject go)return EquipmentPrefabWorkflow.PathOf(go.transform)+"|GameObject";
        var c=(Component)obj;return EquipmentPrefabWorkflow.PathOf(c.transform)+"|"+c.GetType().FullName+"["+Array.IndexOf(c.GetComponents<Component>(),c)+"]";
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
