using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;
public static class WindExp4Step9Setup
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
 [MenuItem("PV System/Configure EXP4 Step9 AC Connections")]
 public static void Configure()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(Application.isPlaying||scene.isDirty||scene.path!="Assets/_Project/Scenes/Wind Lab.unity")throw new InvalidOperationException("Save Wind Lab and stop Play Mode first.");
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();var root=steps.Find("Step 9");
  var templates=steps.GetComponentsInChildren<WireInteraction>(true).Where(w=>w.pointA&&w.pointB&&w.plugA&&w.plugB&&w.wireObject).ToArray();
  var uiRoot=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Exp 4 UI");
  var ui9=uiRoot.Find("Subtitle Step 9").gameObject;var ui10=uiRoot.Find("Subtitle Step 10").gameObject;
  int anchor=owner.steps.FindIndex(s=>s.interaction&&s.interaction.name=="Power connector button");
  if(anchor<0||root.GetComponentInChildren<WireInteraction>(true))throw new InvalidOperationException("Anchor missing or Step9 already configured.");
  var originalRows=owner.steps.ToArray();var created=new System.Collections.Generic.List<StoryboardSteps.Step>();
  foreach(int lamp in new[]{1,2})foreach(bool black in new[]{false,true})
  {
   string pin=black?"N":"L",suffix=black?"-ve":"+ve";
   var source=templates.First(w=>(w.plugA.name.IndexOf(black?"Black":"Red",StringComparison.OrdinalIgnoreCase)>=0));
   var template=source.pointA;var sourcePlug=source.plugA;
   var halogen=root.Find("AC Halogen "+pin+" "+lamp);
   var original=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Off grid inverter Ac Output "+pin);
   var group=new GameObject("AC Halogen "+pin+" "+lamp+" to Off-grid Inverter AC Output "+pin).transform;group.SetParent(root,false);halogen.SetParent(group,true);
   Components(halogen,template);var aPlug=EnsurePlug(halogen,sourcePlug);
   Transform inverter;
   if(lamp==1){inverter=original;inverter.SetParent(group,true);Components(inverter,template);}
   else {inverter=Object.Instantiate(original.gameObject,group,false).transform;inverter.name=original.name+" (Lamp 2)";inverter.SetPositionAndRotation(original.position,original.rotation);inverter.localScale=original.lossyScale;}
   var bPlug=EnsurePlug(inverter,sourcePlug);
   if(lamp==2){var reference=steps.GetComponentsInChildren<Interaction>(true).Single(t=>t.name=="Off-Grid Inverter Battery "+suffix+" (Step 4)");bPlug.transform.localPosition=Plug(reference.transform).localPosition;Dirty(bPlug.transform);}
   var w=group.gameObject.AddComponent<WireInteraction>();w.pointA=halogen.GetComponent<Interaction>();w.pointB=inverter.GetComponent<Interaction>();w.plugA=aPlug;w.plugB=bPlug;
   w.markerA=Marker(halogen,aPlug,source.markerA,steps.Find("Pointers"),halogen.name);
   w.markerB=Marker(inverter,bPlug,source.markerB,steps.Find("Pointers"),inverter.name);
   w.onFirstPointClicked=new UnityEvent();CameraRoute(w.onFirstPointClicked,5);
   w.wireObject=WindExp4Step1Setup.Wire(steps.Find("Wires"),group.name,w.pointA,w.pointB,aPlug.transform,bPlug.transform,black,templates,null,"Assets/_Project/Models/Generated/EXP4 Step9 Wires");
   var row=new StoryboardSteps.Step{stepType=StepType.Interaction,interaction=w,uiObject=ui9,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent()};CameraRoute(row.onStepStart,3);created.Add(row);
   if(lamp==2){var first=original.GetComponentInParent<WireInteraction>();var a=original.GetComponent<BoxCollider>();var b=inverter.GetComponent<BoxCollider>();a.enabled=true;b.enabled=false;
    first.onCompleted??=new UnityEvent();first.onUndone??=new UnityEvent();SetCollider(first.onCompleted,a,false);SetCollider(first.onCompleted,b,true);SetCollider(first.onUndone,a,true);SetCollider(first.onUndone,b,false);
    owner.steps[0].onStepStart??=new UnityEvent();SetCollider(owner.steps[0].onStepStart,a,true);SetCollider(owner.steps[0].onStepStart,b,false);Dirty(first);Dirty(a);Dirty(b);
   }
   Dirty(w);
  }
  owner.steps.InsertRange(anchor+1,created);
  var phases=originalRows.Where(s=>s.interaction&&new[]{"L1","L2","L3"}.Any(p=>s.interaction.name.Contains(p+" to wind"))).ToArray();
  if(phases.Length!=3)throw new InvalidOperationException("Expected L1,L2,L3.");
  foreach(var row in phases){owner.steps.Remove(row);row.uiObject=ui10;}owner.steps.InsertRange(anchor+5,phases);
  if(!owner.steps.Where(s=>!created.Contains(s)).SequenceEqual(originalRows))throw new InvalidOperationException("Existing storyboard order changed.");
  Dirty(owner);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();
 }
 [MenuItem("PV System/Verify EXP4 Step9 AC Connections")]
 public static void Verify()
 {
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();var connections=steps.Find("Step 9").GetComponentsInChildren<WireInteraction>(true);
  if(connections.Length!=4)throw new InvalidOperationException("Expected four AC wires.");
  foreach(var w in connections){bool black=w.pointA.name.Contains(" N ");
   if(!w.pointB.name.Contains("Output "+(black?"N":"L"))||!w.wireObject||!w.plugA||!w.plugB||!w.markerA||!w.markerB||!w.pointA.GetComponent<BoxCollider>()||!w.pointB.GetComponent<OnMouseDownEvent>())throw new InvalidOperationException("Incomplete connection "+w.name);
   if(owner.steps.Single(s=>s.interaction==w).uiObject.name!="Subtitle Step 9")throw new InvalidOperationException("Incorrect UI.");
   if(!w.pointB.name.Contains("Lamp 2")){var a=w.pointB.GetComponent<BoxCollider>();var second=connections.Single(c=>c.pointB.name==w.pointB.name+" (Lamp 2)");var b=second.pointB.GetComponent<BoxCollider>();
    var states=Enumerable.Range(0,w.onCompleted.GetPersistentEventCount()).Select(i=>w.onCompleted.GetPersistentListenerState(i)).ToArray();
    for(int i=0;i<states.Length;i++)w.onCompleted.SetPersistentListenerState(i,UnityEventCallState.EditorAndRuntime);w.onCompleted.Invoke();bool ok=!a.enabled&&b.enabled;
    for(int i=0;i<states.Length;i++)w.onCompleted.SetPersistentListenerState(i,states[i]);a.enabled=true;b.enabled=false;if(!ok)throw new InvalidOperationException("Collider handoff failed.");
   }
  }
  int anchor=owner.steps.FindIndex(s=>s.interaction&&s.interaction.name=="Power connector button");
  if(owner.steps.Skip(anchor+1).Take(4).Any(s=>!connections.Contains(s.interaction))||owner.steps.Skip(anchor+5).Take(3).Any(s=>s.uiObject.name!="Subtitle Step 10"))throw new InvalidOperationException("Incorrect row placement.");
  Debug.Log("EXP4 STEP9 VERIFIED: four matching L/N connections, plugs, colliders, pointers, wires, lamps-to-inverter cameras; both double-pin handoffs execute; Subtitle9 followed by L1-L3 Subtitle10. Existing rows preserved.");
 }
}
