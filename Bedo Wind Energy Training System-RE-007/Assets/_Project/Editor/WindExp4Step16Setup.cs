using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;
public static class WindExp4Step16Setup
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
 static Bounds MarkerBounds(GameObject marker)
 {
  var renderers=marker.GetComponentsInChildren<Renderer>(true);
  if(renderers.Length==0)throw new InvalidOperationException("Marker has no renderer: "+marker.name);
  var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);return bounds;
 }
 static MeshRenderer[] Equipment()=>Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
  .Where(r=>r.enabled&&!r.GetComponentInParent<StoryboardSteps>()&&!r.GetComponentInParent<Canvas>()&&!r.GetComponentInParent<UnityEngine.Splines.SplineContainer>()&&r.bounds.size.x<40&&r.bounds.size.y<45&&r.bounds.size.z<20).ToArray();
 static void ClearMarker(GameObject marker)
 {
  var equipment=Equipment();var bounds=MarkerBounds(marker);
  for(int pass=0;pass<equipment.Length;pass++){
   var overlaps=equipment.Where(r=>r.bounds.Intersects(bounds)).ToArray();if(overlaps.Length==0)break;
   marker.transform.position+=Vector3.forward*(overlaps.Min(r=>r.bounds.min.z)-bounds.max.z-.15f);bounds=MarkerBounds(marker);
  }
  if(equipment.Any(r=>r.bounds.Intersects(bounds)))throw new InvalidOperationException("Pointer overlap: "+marker.name);Dirty(marker.transform);
 }
 [MenuItem("PV System/Configure EXP4 Step16 At Elements34 And35")]
 public static void Configure()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty||scene.path!="Assets/_Project/Scenes/Wind Lab.unity")throw new InvalidOperationException("Save Wind Lab and stop Play Mode first.");
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();var root=steps.Find("Step 16");
  if(!root||root.GetComponentInChildren<WireInteraction>(true)||owner.steps.Count<36||owner.steps[34].interaction)throw new InvalidOperationException("Step16 already configured, missing, or element34 is occupied.");
  var original=owner.steps.ToArray();var templates=steps.GetComponentsInChildren<WireInteraction>(true).Where(w=>w.pointA&&w.pointB&&w.plugA&&w.plugB&&w.wireObject).ToArray();
  var ui=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Exp 4 UI").Find("Subtitle Step 17").gameObject;
  var created=new System.Collections.Generic.List<WireInteraction>();
  foreach(bool black in new[]{true,false})
  {
   string color=black?"Black":"Red",pin=black?"N":"L";
   var source=templates.First(w=>w.plugA.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)>=0||w.plugB.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)>=0);
   bool sourceA=source.plugA.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)>=0;var template=sourceA?source.pointA:source.pointB;var sourcePlug=sourceA?source.plugA:source.plugB;
   var board=root.Find(black?"Main board black 2":"Main Board red 2");var inverter=root.Find("Off grid inverter AC Input "+pin);
   if(!board||!inverter)throw new InvalidOperationException("Missing "+pin+" terminal.");
   string name="Main Board "+color+" 2 to Off-grid Inverter AC Input "+pin;
   var group=new GameObject(name).transform;group.SetParent(root,false);board.SetParent(group,true);inverter.SetParent(group,true);
   Components(board,template);Components(inverter,template);var aPlug=EnsurePlug(board,sourcePlug);var bPlug=EnsurePlug(inverter,sourcePlug);
   var w=group.gameObject.AddComponent<WireInteraction>();w.pointA=board.GetComponent<Interaction>();w.pointB=inverter.GetComponent<Interaction>();w.plugA=aPlug;w.plugB=bPlug;
   var pointerTemplate=sourceA?source.markerA:source.markerB;
   w.markerA=Marker(board,aPlug,pointerTemplate,steps.Find("Pointers"),"Main Board "+color+" 2 (Step 16)");
   w.markerB=Marker(inverter,bPlug,pointerTemplate,steps.Find("Pointers"),"Off-grid Inverter AC Input "+pin+" (Step 16)");ClearMarker(w.markerA);ClearMarker(w.markerB);
   w.onFirstPointClicked=new UnityEvent();CameraRoute(w.onFirstPointClicked,5);
   w.wireObject=WindExp4Step1Setup.Wire(steps.Find("Wires"),name,w.pointA,w.pointB,aPlug.transform,bPlug.transform,black,templates,null,"Assets/_Project/Models/Generated/EXP4 Step16 Wires");Dirty(w);created.Add(w);
  }
  // Reuse the user's existing empty element34, retaining its note, audio and callbacks.
  var first=owner.steps[34];first.stepType=StepType.Interaction;first.interaction=created[0];first.uiObject=ui;first.onStepStart??=new UnityEvent();CameraRoute(first.onStepStart,1);
  var second=new StoryboardSteps.Step{stepType=StepType.Interaction,interaction=created[1],uiObject=ui,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent()};CameraRoute(second.onStepStart,1);owner.steps.Insert(35,second);
  if(owner.steps.Count!=original.Length+1||!owner.steps.Take(34).SequenceEqual(original.Take(34))||!owner.steps.Skip(36).SequenceEqual(original.Skip(35)))throw new InvalidOperationException("Storyboard preservation failed.");
  Dirty(owner);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();
 }
 [MenuItem("PV System/Verify EXP4 Step16 At Elements34 And35")]
 public static void Verify()
 {
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();var root=steps.Find("Step 16");var connections=root.GetComponentsInChildren<WireInteraction>(true);
  if(connections.Length!=2||!owner.steps.Skip(34).Take(2).Select(s=>s.interaction).SequenceEqual(connections.Cast<BaseInteraction>()))throw new InvalidOperationException("Wrong connections/order.");
  foreach(var w in connections)
  {
   bool black=w.pointA.name.IndexOf("black",StringComparison.OrdinalIgnoreCase)>=0;string pin=black?"N":"L",color=black?"Black":"Red";
   if(w.pointB.name!="Off grid inverter AC Input "+pin||w.plugA.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)<0||w.plugB.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)<0||!w.wireObject||!w.markerA||!w.markerB)throw new InvalidOperationException("Incorrect polarity or assignments.");
   foreach(var point in new[]{w.pointA,w.pointB})if(!point.GetComponent<BoxCollider>()||!point.GetComponent<BoxCollider>().enabled||!point.GetComponent<OnMouseDownEvent>())throw new InvalidOperationException("Missing banana components.");
   var row=owner.steps.Single(s=>s.interaction==w);if(row.uiObject.name!="Subtitle Step 17"||row.onStepStart.GetPersistentTarget(row.onStepStart.GetPersistentEventCount()-1)!=Trigger(1)||w.onFirstPointClicked.GetPersistentTarget(0)!=Trigger(5))throw new InvalidOperationException("Incorrect UI/cameras.");
   var container=w.wireObject.GetComponentInChildren<UnityEngine.Splines.SplineContainer>(true);var spline=container.Spline;
   if(Vector3.Distance(container.transform.TransformPoint((Vector3)spline[0].Position),w.plugA.transform.position)>.001f||Vector3.Distance(container.transform.TransformPoint((Vector3)spline[spline.Count-1].Position),w.plugB.transform.position)>.001f||container.GetComponent<UnityEngine.Splines.SplineExtrude>().targetMesh.vertexCount==0)throw new InvalidOperationException("Wire geometry mismatch.");
   foreach(var marker in new[]{w.markerA,w.markerB})if(Equipment().Any(r=>r.bounds.Intersects(MarkerBounds(marker))))throw new InvalidOperationException("Pointer overlap.");
  }
  Debug.Log("EXP4 STEP16 VERIFIED: Main Board Black to inverter AC N at element34, Red to AC L at35; both Subtitle17; original35 becomes36; components, plugs, pointers, independent wire meshes and camera1-to5 assignments valid. Earlier and later rows preserved.");
 }
 [MenuItem("PV System/Audit EXP4 Step16 Connections")]
 public static void Audit()
 {
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();
  foreach(var root in steps.Cast<Transform>().Where(t=>t.name=="Step 16"))foreach(var t in root.GetComponentsInChildren<Transform>(true))Debug.Log("EXP4 SIXTEEN OBJECT "+t.name+" parent="+t.parent.name+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))+" local="+t.localPosition.ToString("F4")+" world="+t.position.ToString("F4"));
  for(int i=0;i<owner.steps.Count;i++)Debug.Log("EXP4 SIXTEEN ROW "+i+" "+(owner.steps[i].interaction?owner.steps[i].interaction.name:"null")+" UI="+(owner.steps[i].uiObject?owner.steps[i].uiObject.name:"null"));
 }
}

