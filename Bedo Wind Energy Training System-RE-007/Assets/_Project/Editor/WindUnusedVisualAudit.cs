using System;
using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Object=UnityEngine.Object;
public static class WindUnusedVisualAudit
{
 [Serializable]public class Removal{public string name,path,kind,experiment;public long parent,transform;public long[] ids;}
 [Serializable]public class Report{public List<Removal> removals=new List<Removal>();public int candidates,retained;}
 static long Id(Object o)=>(long)GlobalObjectId.GetGlobalObjectIdSlow(o).targetObjectId;
 static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
 [MenuItem("PV System/Audit Unused Wind EXP2 EXP3 EXP4 Wires And Pointers")]
 public static void Audit()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty||scene.path!="Assets/_Project/Scenes/Wind Lab.unity")throw new InvalidOperationException("Save Wind Lab first.");
  var owners=Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(o=>o.gameObject.scene==scene&&new[]{"Exp 2","Exp 3","Exp 4"}.Contains(o.name)&&o.StoryboardID.IndexOf("Wind",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  foreach(var o in owners)Debug.Log("WIND CLEAN OWNER "+o.name+" ID="+o.StoryboardID+" path="+Path(o.transform));
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();
  var references=new Dictionary<Object,List<Component>>();
  foreach(var component in all.Where(c=>!(c is Transform))){
   var serialized=new SerializedObject(component);var prop=serialized.GetIterator();while(prop.Next(true)){
    if(prop.propertyType!=SerializedPropertyType.ObjectReference||!prop.objectReferenceValue)continue;
    var target=prop.objectReferenceValue;if(!references.TryGetValue(target,out var list))references[target]=list=new List<Component>();list.Add(component);
   }
  }
  var report=new Report();
  foreach(var owner in owners)foreach(var folder in owner.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Wires"||t.name=="Pointers"))foreach(Transform t in folder){
   bool wire=folder.name=="Wires";if(wire&&!t.GetComponentInChildren<SplineContainer>(true))continue;
   if(!wire&&t.GetComponentsInChildren<Renderer>(true).Length==0)continue;
   // Only visual children: never bananas, plugs, colliders or interaction objects.
   if(t.GetComponentInChildren<BaseInteraction>(true)||t.GetComponentInChildren<Interaction>(true)||t.GetComponentInChildren<Collider>(true))continue;
   report.candidates++;var objects=t.GetComponentsInChildren<Component>(true).Cast<Object>().Concat(t.GetComponentsInChildren<Transform>(true).Select(x=>(Object)x.gameObject)).Distinct().ToArray();
   var users=objects.Where(references.ContainsKey).SelectMany(x=>references[x]).Where(c=>c&&!c.transform.IsChildOf(t)).Distinct().ToArray();
   if(users.Length>0||PrefabUtility.IsPartOfAnyPrefab(t.gameObject)){report.retained++;Debug.Log("WIND CLEAN KEEP "+Path(t)+" users="+string.Join(";",users.Take(6).Select(c=>Path(c.transform)+":"+c.GetType().Name)));continue;}
   var entry=new Removal{name=t.name,path=Path(t),kind=folder.name,experiment=owner.name,parent=Id(folder),transform=Id(t),ids=objects.Select(Id).Distinct().ToArray()};report.removals.Add(entry);Debug.Log("WIND CLEAN UNUSED "+entry.experiment+" "+entry.kind+" "+entry.name);
  }
  System.IO.File.WriteAllText(".wind-unused-visuals.json",JsonUtility.ToJson(report,true));Debug.Log("WIND CLEAN REPORT candidates="+report.candidates+" retained="+report.retained+" unused="+report.removals.Count);
 }
}
