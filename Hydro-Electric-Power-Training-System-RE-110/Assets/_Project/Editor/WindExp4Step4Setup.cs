using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;
public static class WindExp4Step4Setup
{
 static Transform Steps()=>Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(o=>o.StoryboardID=="OFF Grid System Using Wind Turbine").GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Steps");
 static Transform Plug(Transform banana)=>banana.Cast<Transform>().FirstOrDefault(t=>t.name.IndexOf("plug",StringComparison.OrdinalIgnoreCase)>=0);
 static void Dirty(Object o){EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
 static void Components(Transform banana,Interaction template)
 {
  if(!banana.GetComponent<BoxCollider>()){var c=banana.gameObject.AddComponent<BoxCollider>();EditorUtility.CopySerialized(template.GetComponent<BoxCollider>(),c);Dirty(c);}
  if(!banana.GetComponent<OnMouseDownEvent>())banana.gameObject.AddComponent<OnMouseDownEvent>();
  if(!banana.GetComponent<Interaction>()){var c=banana.gameObject.AddComponent<Interaction>();EditorUtility.CopySerialized(template,c);c.Allow=false;Dirty(c);}
 }
 static GameObject EnsurePlug(Transform banana,GameObject source)
 {
  var p=Plug(banana);
  if(p==null){p=Object.Instantiate(source,banana,false).transform;p.name=source.name;}
  p.localPosition=source.transform.localPosition;p.localRotation=source.transform.localRotation;p.localScale=source.transform.localScale;
  var sourceRenderers=source.GetComponentsInChildren<Renderer>(true);var renderers=p.GetComponentsInChildren<Renderer>(true);
  if(sourceRenderers.Length==renderers.Length)for(int i=0;i<renderers.Length;i++){renderers[i].sharedMaterials=sourceRenderers[i].sharedMaterials;Dirty(renderers[i]);}
  p.gameObject.SetActive(false);Dirty(p);return p.gameObject;
 }
 static GameObject Marker(Transform banana,GameObject plug,GameObject template,Transform folder,string name)
 {
  var previous=folder.Find(name);var go=previous?previous.gameObject:Object.Instantiate(template,folder,false);go.name=name;
  go.transform.position=plug.transform.position+Vector3.up*1.5f;go.SetActive(false);Dirty(go.transform);return go;
 }
 static void SetCollider(UnityEvent ev,BoxCollider collider,bool enabled)
 {
  for(int i=ev.GetPersistentEventCount()-1;i>=0;i--)if(ev.GetPersistentTarget(i)==collider&&ev.GetPersistentMethodName(i)=="set_enabled")UnityEventTools.RemovePersistentListener(ev,i);
  var setter=(UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>),collider,typeof(Collider).GetProperty("enabled").GetSetMethod());
  UnityEventTools.AddBoolPersistentListener(ev,setter,enabled);
 }
 static DelayedEventTrigger Trigger(int index)=>Object.FindObjectsByType<DelayedEventTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.transform.parent!=null&&t.transform.parent.name=="Camera move"&&t.name=="Move camera to pos "+index);
 static void CameraRoute(UnityEvent ev,int index)
 {
  for(int i=ev.GetPersistentEventCount()-1;i>=0;i--)
   if(ev.GetPersistentTarget(i) is SmoothCameraMovement||ev.GetPersistentTarget(i) is DelayedEventTrigger d&&d.name.IndexOf("camera",StringComparison.OrdinalIgnoreCase)>=0)UnityEventTools.RemovePersistentListener(ev,i);
  UnityEventTools.AddVoidPersistentListener(ev,Trigger(index).TriggerDelay);
 }
 [MenuItem("PV System/Verify EXP4 Step4 Collider Handoff And LED Fix")]
 public static void Verify()
 {
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();
  foreach(bool negative in new[]{true,false})
  {
   string suffix=negative?"-ve":"+ve";
   var original=steps.GetComponentsInChildren<Interaction>(true).Single(p=>p.name=="Off-Grid Inverter Battery "+suffix);
   var copy=steps.GetComponentsInChildren<Interaction>(true).Single(p=>p.name=="Off-Grid Inverter Battery "+suffix+" (Step 4)");
   var first=original.GetComponentInParent<WireInteraction>();var next=copy.GetComponentInParent<WireInteraction>();
   var led=next.pointA==copy?next.pointB:next.pointA;
   if(led.name!="DC LED "+suffix||next.plugA==null||next.plugB==null||next.markerA==null||next.markerB==null||next.wireObject==null)throw new InvalidOperationException("Polarity or assignment mismatch.");
   if(Vector3.Distance(original.transform.position,copy.transform.position)>.001f||Vector3.Distance(Plug(original.transform).localPosition,Plug(copy.transform).localPosition)>.001f)throw new InvalidOperationException("Duplicated banana or plug moved.");
   var a=original.GetComponent<BoxCollider>();var b=copy.GetComponent<BoxCollider>();
   if(!a.enabled||b.enabled)throw new InvalidOperationException("Initial collider ownership incorrect.");
   if(first.onCompleted.GetPersistentEventCount()!=2||Enumerable.Range(0,2).Any(i=>first.onCompleted.GetPersistentMethodName(i)!="set_enabled"))throw new InvalidOperationException("Unexpected completion listeners; do not execute them in editor.");
   var listenerStates=Enumerable.Range(0,2).Select(i=>first.onCompleted.GetPersistentListenerState(i)).ToArray();
   for(int i=0;i<2;i++)first.onCompleted.SetPersistentListenerState(i,UnityEventCallState.EditorAndRuntime);
   first.onCompleted.Invoke();
   for(int i=0;i<2;i++)first.onCompleted.SetPersistentListenerState(i,listenerStates[i]);
   bool handedOff=!a.enabled&&b.enabled;
   a.enabled=true;b.enabled=false;
   if(!handedOff)throw new InvalidOperationException("Collider handoff did not execute.");
  }
  foreach(var row in owner.steps.Skip(8).Take(2))
  {
   var w=(WireInteraction)row.interaction;
   if(!new[]{w.pointA.name,w.pointB.name}.Any(n=>n.StartsWith("DC LED"))||w.wireObject.name.Contains("Halogen"))throw new InvalidOperationException("Step1 LED fix missing.");
  }
  Debug.Log("EXP4 STEP4 VERIFIED: matching polarity, all assignments, unchanged copied plug placement; both completion events successfully switch colliders; Step1 final connections use DC LED and replacement wires.");
 }
 [MenuItem("PV System/Configure EXP4 Step4 And Step1 LED Fix")]
 public static void Configure()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty||scene.path!="Assets/_Project/Scenes/Hydro Lab.unity")throw new InvalidOperationException("Save Hydro Lab and stop Play Mode first.");
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();var pointers=steps.Find("Pointers");var wires=steps.Find("Wires");
  var templates=steps.GetComponentsInChildren<WireInteraction>(true).Where(w=>w.pointA&&w.pointB&&w.plugA&&w.plugB&&w.wireObject).ToArray();
  var step1=steps.Find("Step 1");
  foreach(bool black in new[]{false,true})
  {
   Transform group;
   // Select by LED polarity when both replacement groups exist.
   group=step1.Cast<Transform>().Single(t=>t.GetComponent<WireInteraction>()!=null&&t.Cast<Transform>().Any(c=>c.name.Equals(black?"DC LED black":"DC LED Red",StringComparison.OrdinalIgnoreCase)));
   var w=group.GetComponent<WireInteraction>();var led=group.Cast<Transform>().Single(t=>t.name.Equals(black?"DC LED black":"DC LED Red",StringComparison.OrdinalIgnoreCase));
   var replaced=black?w.pointA:w.pointB;var template=replaced;Components(led,template);
   var plug=EnsurePlug(led,black?w.plugA:w.plugB);
   var marker=Marker(led,plug,black?w.markerA:w.markerB,pointers,black?"DC LED Input Black":"DC LED Input Red");
   // Keep the old copied endpoint from intercepting the replacement's clicks.
   replaced.GetComponent<BoxCollider>().enabled=false;Dirty(replaced.GetComponent<BoxCollider>());
   if(black){w.pointA=led.GetComponent<Interaction>();w.plugA=plug;w.markerA=marker;}else{w.pointB=led.GetComponent<Interaction>();w.plugB=plug;w.markerB=marker;}
   string name=black?"battery Black to DC LED Black":"battery red to DC LED Red";
   w.wireObject=WindExp4Step1Setup.Wire(wires,name,w.pointA,w.pointB,w.plugA.transform,w.plugB.transform,black,templates,null,"Assets/_Project/Models/Generated/EXP4 Step1 Wires");
   group.name=black?"DC LED Input Black to Battery Output Black":"Battery Output Red to DC LED Input Red";Dirty(group.gameObject);Dirty(w);
   var row=owner.steps.First(s=>s.interaction==w);row.onStepStart??=new UnityEvent();w.onFirstPointClicked??=new UnityEvent();CameraRoute(row.onStepStart,black?3:4);CameraRoute(w.onFirstPointClicked,black?4:3);
  }
  var root=steps.Cast<Transform>().Where(t=>t.name=="Step 4").Single(t=>t.GetComponentsInChildren<Transform>(true).Any(c=>c.name=="DC LED -ve"));
  var newInteractions=new System.Collections.Generic.List<WireInteraction>();
  foreach(bool negative in new[]{true,false})
  {
   string suffix=negative?"-ve":"+ve",color=negative?"Blue":"Yellow";
   var original=steps.GetComponentsInChildren<Interaction>(true).Single(p=>p.name=="Off-Grid Inverter Battery "+suffix);
   var firstWire=original.GetComponentInParent<WireInteraction>();var originalPlug=(firstWire.pointA==original?firstWire.plugA:firstWire.plugB);
   var templatePointer=firstWire.pointA==original?firstWire.markerA:firstWire.markerB;
   var led=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="DC LED "+suffix);
   string groupName=negative?"DC LED -ve to Off-Grid Inverter Battery -ve":"Off-Grid Inverter Battery +ve to DC LED +ve";
   var group=root.Find(groupName);if(group==null){group=new GameObject(groupName).transform;group.SetParent(root,false);led.SetParent(group,true);}
   Components(led,original);var ledPlug=EnsurePlug(led,originalPlug);
   var copy=group.Find("Off-Grid Inverter Battery "+suffix+" (Step 4)");
   if(copy==null){copy=Object.Instantiate(original.gameObject,group,false).transform;copy.name="Off-Grid Inverter Battery "+suffix+" (Step 4)";copy.SetPositionAndRotation(original.transform.position,original.transform.rotation);copy.localScale=original.transform.lossyScale;}
   var copyPlug=EnsurePlug(copy,originalPlug);
   var originalCollider=original.GetComponent<BoxCollider>();var copyCollider=copy.GetComponent<BoxCollider>();copyCollider.enabled=false;originalCollider.enabled=true;Dirty(originalCollider);Dirty(copyCollider);
   var w=group.GetComponent<WireInteraction>()??group.gameObject.AddComponent<WireInteraction>();
   w.pointA=(negative?led:copy).GetComponent<Interaction>();w.pointB=(negative?copy:led).GetComponent<Interaction>();
   w.plugA=negative?ledPlug:copyPlug;w.plugB=negative?copyPlug:ledPlug;
   w.markerA=Marker(w.pointA.transform,w.plugA,templatePointer,pointers,negative?"DC LED -ve":"Off-Grid Inverter Battery +ve (Step 4)");
   w.markerB=Marker(w.pointB.transform,w.plugB,templatePointer,pointers,negative?"Off-Grid Inverter Battery -ve (Step 4)":"DC LED +ve");
   w.wireObject=WindExp4Step1Setup.Wire(wires,groupName,w.pointA,w.pointB,w.plugA.transform,w.plugB.transform,false,templates,color,"Assets/_Project/Models/Generated/EXP4 Step4 Wires");
   firstWire.onCompleted??=new UnityEvent();firstWire.onUndone??=new UnityEvent();
   SetCollider(firstWire.onCompleted,originalCollider,false);SetCollider(firstWire.onCompleted,copyCollider,true);
   SetCollider(firstWire.onUndone,originalCollider,true);SetCollider(firstWire.onUndone,copyCollider,false);
   owner.steps[0].onStepStart??=new UnityEvent();SetCollider(owner.steps[0].onStepStart,originalCollider,true);SetCollider(owner.steps[0].onStepStart,copyCollider,false);
   Dirty(firstWire);Dirty(w);newInteractions.Add(w);
  }
  var ui=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Exp 4 UI").GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Subtitle Step 4").gameObject;
  int afterL3=owner.steps.FindIndex(s=>s.interaction&&s.interaction.name.Contains("L3 to wind"))+1;if(afterL3==0)throw new InvalidOperationException("L3 missing.");
  if(!owner.steps.Skip(afterL3).Take(2).Select(s=>s.interaction).SequenceEqual(newInteractions.Cast<BaseInteraction>()))owner.steps.InsertRange(afterL3,newInteractions.Select(w=>new StoryboardSteps.Step{stepType=StepType.Interaction,interaction=w,uiObject=ui,onStepStart=new UnityEvent()}));
  foreach(var w in newInteractions){var row=owner.steps.Skip(afterL3).Take(2).Single(s=>s.interaction==w);w.onFirstPointClicked??=new UnityEvent();CameraRoute(row.onStepStart,w.pointA.name.StartsWith("DC LED")?3:5);CameraRoute(w.onFirstPointClicked,w.pointB.name.StartsWith("DC LED")?3:5);Dirty(w);}
  Dirty(owner);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Debug.Log("EXP4 STEP4 SAVED: two matching-polarity LED/inverter connections inserted after L3, Subtitle Step4 assigned; duplicate inverter colliders hand off after first connections and reset at experiment start; last two Step1 connections now use DC LED endpoints and new wires.");
 }
}
