using System;
using System.Linq;
using System.Collections.Generic;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;
public static class WindExp4Step3Setup
{
 [MenuItem("PV System/Audit EXP4 Step4 And LED Replacements")]
 public static void AuditStep4()
 {
  var steps=Steps();
  foreach(var root in steps.Cast<Transform>().Where(t=>t.name=="Step 4"||t.name=="Step 1"))
   foreach(var t in root.GetComponentsInChildren<Transform>(true))
   {
    if(root.name=="Step 1"&&t.parent!=root&&!t.name.Contains("LED")&&!t.name.Contains("Led")&&!t.name.Contains("led"))continue;
    Debug.Log("EXP4 STEP4 AUDIT "+root.name+"/"+t.name+" parent="+(t.parent?t.parent.name:"null")+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name))+" pos="+t.position.ToString("F4"));
    var w=t.GetComponent<WireInteraction>();if(w)Debug.Log("EXP4 STEP4 EXISTING "+w.name+" A="+(w.pointA?w.pointA.name:"null")+" B="+(w.pointB?w.pointB.name:"null")+" wire="+(w.wireObject?w.wireObject.name:"null"));
   }
 }
 static Transform Steps()=>Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(o=>o.StoryboardID=="OFF Grid System Using Wind Turbine").GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Steps");
 static Transform Plug(Transform banana)=>banana.Cast<Transform>().Single(t=>t.name.IndexOf("plug",StringComparison.OrdinalIgnoreCase)>=0);
 static void Dirty(Object o){EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
 static int CameraFor(Interaction point)
 {
  string n=point.name.ToLowerInvariant();
  if(n.Contains("off-grid")||n.Contains("off grid"))return 5;
  if(n.Contains("battery"))return 4;
  if(n.Contains("full")||n.Contains("rectifier"))return 8;
  if(n.Contains("halogen")||n.Contains("lamp"))return 3;
  if(n.Contains("main"))return 1;
  if(n.Contains("wind"))return 11;
  throw new InvalidOperationException("Unmapped banana camera: "+point.name);
 }
 static void Route(UnityEvent ev,DelayedEventTrigger trigger)
 {
  for(int i=ev.GetPersistentEventCount()-1;i>=0;i--)
  {
   var target=ev.GetPersistentTarget(i);string method=ev.GetPersistentMethodName(i);
   if(target is SmoothCameraMovement&&method=="MoveToPosition"||target is DelayedEventTrigger&&target.name.IndexOf("camera",StringComparison.OrdinalIgnoreCase)>=0)UnityEventTools.RemovePersistentListener(ev,i);
  }
  UnityEventTools.AddVoidPersistentListener(ev,trigger.TriggerDelay);
 }
 [MenuItem("PV System/Configure EXP4 Step3 And Cameras Through Step3")]
 public static void Configure()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(Application.isPlaying||scene.isDirty||scene.path!="Assets/_Project/Scenes/Hydro Lab.unity")throw new InvalidOperationException("Save Hydro Lab and stop Play Mode first.");
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();
  var roots=steps.Cast<Transform>().Where(t=>t.name=="Step 3").ToArray();
  var root=roots.Single(t=>t.GetComponentsInChildren<Transform>(true).Any(c=>c.name=="Off-Grid Inverter Battery -ve"));
  var groups=root.Cast<Transform>().ToArray();if(groups.Length!=2)throw new InvalidOperationException("Expected two new Step3 connections.");
  var templates=steps.GetComponentsInChildren<WireInteraction>(true).Where(w=>w.pointA&&w.pointB&&w.plugA&&w.wireObject).ToArray();
  var pointers=steps.Find("Pointers");var wires=steps.Find("Wires");
  foreach(var group in groups)
  {
   bool negative=group.name.Contains("-ve");string color=negative?"Blue":"Yellow";
   var bananas=group.Cast<Transform>().Where(t=>t.GetComponent<MeshFilter>()!=null).ToArray();if(bananas.Length!=2)throw new InvalidOperationException("Expected two bananas: "+group.name);
   var template=templates.First(w=>w.plugA.name.Contains(color));
   var battery=bananas.Single(t=>!t.name.Contains("Inverter"));var inverter=bananas.Single(t=>t.name.Contains("Inverter"));
   var originalBattery=battery.GetComponent<Interaction>();
   var sourcePlug=Plug(battery);var plugPos=sourcePlug.localPosition;var plugRot=sourcePlug.localRotation;var plugScale=sourcePlug.localScale;
   foreach(var banana in bananas)
   {
    if(!banana.GetComponent<BoxCollider>()){var c=banana.gameObject.AddComponent<BoxCollider>();EditorUtility.CopySerialized(originalBattery.GetComponent<BoxCollider>(),c);Dirty(c);}
    if(!banana.GetComponent<OnMouseDownEvent>())banana.gameObject.AddComponent<OnMouseDownEvent>();
    if(!banana.GetComponent<Interaction>()){var i=banana.gameObject.AddComponent<Interaction>();EditorUtility.CopySerialized(originalBattery,i);i.Allow=false;Dirty(i);}
    var plug=banana.Cast<Transform>().FirstOrDefault(t=>t.name.IndexOf("plug",StringComparison.OrdinalIgnoreCase)>=0);
    if(plug==null){plug=Object.Instantiate(sourcePlug.gameObject,banana,false).transform;plug.name=color+" plug";}
    plug.localPosition=plugPos;plug.localRotation=plugRot;plug.localScale=plugScale;plug.gameObject.SetActive(false);Dirty(plug);
   }
   var w=group.GetComponent<WireInteraction>()??group.gameObject.AddComponent<WireInteraction>();
   w.pointA=(negative?inverter:battery).GetComponent<Interaction>();w.pointB=(negative?battery:inverter).GetComponent<Interaction>();
   w.plugA=Plug(w.pointA.transform).gameObject;w.plugB=Plug(w.pointB.transform).gameObject;
   foreach(bool first in new[]{true,false})
   {
    var point=first?w.pointA:w.pointB;var current=first?w.markerA:w.markerB;
    if(point.transform==battery&&current!=null&&!current.name.Contains("MPPT"))continue;
    string name="Off-Grid Inverter Battery "+(negative?"-ve":"+ve");
    var old=pointers.Find(name);var marker=old?old.gameObject:Object.Instantiate(template.markerA,pointers,false);marker.name=name;
    float height=Mathf.Abs(template.markerA.transform.position.y-template.plugA.transform.position.y);marker.transform.position=Plug(point.transform).position+Vector3.up*Mathf.Max(1f,height);marker.SetActive(false);
    if(first)w.markerA=marker;else w.markerB=marker;Dirty(marker.transform);
   }
   string wireName=negative?"Off-Grid Inverter Battery -ve to Battery -ve":"Battery +ve to Off-Grid Inverter Battery +ve";
   w.wireObject=WindExp4Step1Setup.Wire(wires,wireName,w.pointA,w.pointB,Plug(w.pointA.transform),Plug(w.pointB.transform),false,templates,color,"Assets/_Project/Models/Generated/EXP4 Step3 Wires");Dirty(w);
  }
  int ethernet=owner.steps.FindIndex(s=>s.interaction&&s.interaction.name.Contains("ethernet"));if(ethernet<0)throw new InvalidOperationException("Ethernet missing.");
  var ui=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Exp 4 UI").GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Subtitle Step 3").gameObject;
  var newWires=groups.Select(g=>g.GetComponent<WireInteraction>()).ToArray();
  if(!owner.steps.Skip(ethernet+1).Take(2).Select(s=>s.interaction).SequenceEqual(newWires.Cast<BaseInteraction>()))
   owner.steps.InsertRange(ethernet+1,newWires.Select(w=>new StoryboardSteps.Step{stepType=StepType.Interaction,interaction=w,uiObject=ui}));
  var camera=Object.FindObjectsByType<SmoothCameraMovement>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
  var triggers=Object.FindObjectsByType<DelayedEventTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.transform.parent!=null&&t.transform.parent.name=="Camera move").ToArray();
  var triggerMap=new Dictionary<int,DelayedEventTrigger>();
  foreach(int index in new[]{1,3,4,5,8,11})
  {
   var trigger=triggers.Single(t=>t.name=="Move camera to pos "+index);
   var calls=new SerializedObject(trigger).FindProperty("onTriggeredAfterDelay.m_PersistentCalls.m_Calls");
   if(calls.arraySize!=1||calls.GetArrayElementAtIndex(0).FindPropertyRelative("m_Arguments.m_IntArgument").intValue!=index)throw new InvalidOperationException("Camera trigger index mismatch.");
   // Repair missing camera target without changing the authored delay or index.
   var settings=new SerializedObject(trigger);settings.FindProperty("onTriggeredAfterDelay.m_PersistentCalls.m_Calls").GetArrayElementAtIndex(0).FindPropertyRelative("m_Target").objectReferenceValue=camera;settings.ApplyModifiedPropertiesWithoutUndo();Dirty(trigger);triggerMap[index]=trigger;
  }
  int routed=0;
  foreach(var row in owner.steps.Take(ethernet+1+2+3))
  {
   var w=row.interaction as WireInteraction;if(w==null)throw new InvalidOperationException("Expected a wire in early rows.");
   row.onStepStart??=new UnityEvent();w.onFirstPointClicked??=new UnityEvent();Route(row.onStepStart,triggerMap[CameraFor(w.pointA)]);Route(w.onFirstPointClicked,triggerMap[CameraFor(w.pointB)]);Dirty(w);routed++;
   Debug.Log("EXP4 CAMERA ROUTE: "+w.name+" "+CameraFor(w.pointA)+" -> "+CameraFor(w.pointB));
  }
  Dirty(owner);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Debug.Log("EXP4 STEP3 SAVED: two rows inserted after Ethernet, before existing L1/L2/L3; subtitle Step3 assigned; "+routed+" early wire rows routed through existing camera triggers.");
 }
}
