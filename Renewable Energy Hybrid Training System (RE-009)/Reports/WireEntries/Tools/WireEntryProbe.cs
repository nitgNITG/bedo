using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using TutorialFramework;

[InitializeOnLoad]
public static class WireEntryProbe
{
 [Serializable]public class Plug {public string name,mesh;public Vector3 origin,mouth,direction,size;}
 [Serializable]public class Item {public string context,name,id,meshPath;public Plug a,b;public Vector3[] points,tin,tout;public bool runtimeRoute;}
 [Serializable]public class Data {public Item[] wires;}
 static WireEntryProbe(){EditorApplication.update+=Tick;}
 static void Tick(){if(!File.Exists("Temp/wire-entry-probe.flag")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/wire-entry-probe.flag");try{Run();}catch(Exception e){File.WriteAllText("Temp/WireEntry/error.txt",e.ToString());}}
 static Plug Read(GameObject go){if(!go)return null;var f=go.GetComponentInChildren<MeshFilter>(true);if(!f||!f.sharedMesh)return null;var b=f.sharedMesh.bounds;return new Plug{name=go.name,mesh=AssetDatabase.GetAssetPath(f.sharedMesh),origin=go.transform.position,mouth=f.transform.TransformPoint(new Vector3(b.center.x,0,b.min.z+.025f)),direction=f.transform.TransformDirection(Vector3.back).normalized,size=f.transform.TransformVector(b.size)};}
 static void Run(){var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/PV Lab.unity"||scene.isDirty)throw new Exception("Open saved PV Lab");var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var items=new List<Item>();File.WriteAllText("Temp/WireEntry/skips.txt",string.Join("\n",all.Select(t=>t.GetComponent<StoryboardSteps>()).Where(b=>b).Select(b=>b.name+" steps="+b.steps.Count+" wires="+b.steps.Count(s=>s.interaction is WireInteraction))));
 foreach(var board in all.Select(t=>t.GetComponent<StoryboardSteps>()).Where(b=>b&&new[]{"Exp 1","Exp 2","Exp 3","Exp 4"}.Contains(b.name)))foreach(var w in board.steps.Select(s=>s.interaction as WireInteraction).Where(w=>w&&w.wireObject&&w.plugA&&w.plugB).Distinct())Add(items,board.name,w.wireObject,w.plugA,w.plugB);
 foreach(var d in all.Select(t=>t.GetComponent<FreeLabWireDefinition>()).Where(d=>d&&d.wireParentTemplate))Add(items,"Free",d.wireParentTemplate,d.plugA?d.plugA:d.endpointA.SelectionPlug,d.plugB?d.plugB:d.endpointB.SelectionPlug);
 File.WriteAllText("Temp/WireEntry/audit.json",JsonUtility.ToJson(new Data{wires=items.ToArray()},true));Debug.Log("Wire entry audit: "+items.Count);
 }
 static void Add(List<Item> list,string context,GameObject go,GameObject a,GameObject b){var c=go.GetComponentInChildren<SplineContainer>(true);if(!c||c.Spline.Count<2)return;var s=c.Spline;if(s.Count<2)return;var extrude=c.GetComponent<SplineExtrude>();var route=go.GetComponent<FreeLabWire>();
 list.Add(new Item{context=context,name=go.name,id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),meshPath=extrude?AssetDatabase.GetAssetPath(extrude.targetMesh):"",a=Read(a),b=Read(b),points=s.Select(k=>c.transform.TransformPoint((Vector3)k.Position)).ToArray(),tin=s.Select(k=>c.transform.TransformVector((Vector3)(Unity.Mathematics.math.rotate(k.Rotation,k.TangentIn)))).ToArray(),tout=s.Select(k=>c.transform.TransformVector((Vector3)(Unity.Mathematics.math.rotate(k.Rotation,k.TangentOut)))).ToArray(),runtimeRoute=route&&new SerializedObject(route).FindProperty("fitToAllocatedPlugs").boolValue});}
}
