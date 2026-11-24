using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;
public static class WindExp4Step11Setup
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
 static readonly System.Collections.Generic.Dictionary<GameObject,bool> preview=new System.Collections.Generic.Dictionary<GameObject,bool>();
 [MenuItem("PV System/Preview EXP4 Step11 Wiring")]
 public static void Preview()
 {
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
  foreach(var w in Steps().GetComponentsInChildren<WireInteraction>(true).Where(w=>w.transform.parent.name=="Step 11"))foreach(var go in new[]{w.wireObject,w.plugA,w.plugB,w.markerA,w.markerB}){if(!preview.ContainsKey(go))preview[go]=go.activeSelf;go.SetActive(true);}
  SceneView.lastActiveSceneView.LookAt(new Vector3(-124,157,-53),Quaternion.identity,20f);SceneView.RepaintAll();
 }
 [MenuItem("PV System/End EXP4 Step11 Wiring Preview")]
 public static void EndPreview(){foreach(var item in preview)if(item.Key)item.Key.SetActive(item.Value);preview.Clear();SceneView.RepaintAll();}
 static MeshRenderer[] Equipment()=>Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
  .Where(r=>r.enabled&&!r.GetComponentInParent<StoryboardSteps>()&&!r.GetComponentInParent<Canvas>()&&!r.GetComponentInParent<UnityEngine.Splines.SplineContainer>()&&r.bounds.size.x<40&&r.bounds.size.y<45&&r.bounds.size.z<20).ToArray();
 static void ClearPointers(Transform steps,System.Collections.Generic.IEnumerable<StoryboardSteps.Step> rows)
 {
  var equipment=Equipment();int changed=0;
  var markers=rows.Select(s=>s.interaction).OfType<WireInteraction>().SelectMany(w=>new[]{w.markerA,w.markerB}).Where(m=>m&&m.transform.IsChildOf(steps)).Distinct().ToArray();
  foreach(var marker in markers)
  {
   var bounds=MarkerBounds(marker);var start=marker.transform.position;
   // The module fronts face world -Z. Move only intersecting arrows and preserve X/Y.
   for(int pass=0;pass<equipment.Length;pass++)
   {
    var obstacles=equipment.Where(r=>r.bounds.Intersects(bounds)).ToArray();if(obstacles.Length==0)break;
    float forward=obstacles.Min(r=>r.bounds.min.z)-bounds.max.z-.15f;
    marker.transform.position+=Vector3.forward*forward;bounds=MarkerBounds(marker);
   }
   if(equipment.Any(r=>r.bounds.Intersects(bounds)))throw new InvalidOperationException("Pointer still intersects equipment: "+marker.name);
   if(Vector3.Distance(start,marker.transform.position)>.001f){Dirty(marker.transform);changed++;Debug.Log("EXP4 ELEVEN POINTER CLEARED "+marker.name+" forward="+(start.z-marker.transform.position.z).ToString("F3"));}
  }
  Debug.Log("EXP4 ELEVEN POINTERS: "+changed+" overlapping arrows moved forward; X/Y and plug assignments preserved.");
 }
 [MenuItem("PV System/Configure EXP4 Step11 DC Connections And Pointer Clearance")]
 public static void Configure()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty||scene.path!="Assets/_Project/Scenes/Hydro Lab.unity")throw new InvalidOperationException("Save Hydro Lab and stop Play Mode first.");
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();
  var root=steps.Cast<Transform>().Where(t=>t.name=="Step 11").Single(t=>t.Cast<Transform>().Any(c=>c.name=="Off grid inverter DC Input -ve"));
  if(root.GetComponentInChildren<WireInteraction>(true))throw new InvalidOperationException("Step11 already configured.");
  var templates=steps.GetComponentsInChildren<WireInteraction>(true).Where(w=>w.pointA&&w.pointB&&w.plugA&&w.plugB&&w.wireObject).ToArray();
  var original=owner.steps.ToArray();int anchor=owner.steps.FindIndex(s=>s.interaction&&s.interaction.name.Contains("L3 to wind"));if(anchor<0)throw new InvalidOperationException("L3 anchor missing.");
  var ui=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Exp 4 UI").Find("Subtitle Step 11").gameObject;
  var created=new System.Collections.Generic.List<StoryboardSteps.Step>();
  foreach(bool negative in new[]{true,false})
  {
   string suffix=negative?"-ve":"+ve",color=negative?"Blue":"Yellow";
   var source=templates.First(w=>w.plugA.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)>=0||w.plugB.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)>=0);
   bool sourceA=source.plugA.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)>=0;var sourcePlug=sourceA?source.plugA:source.plugB;var template=sourceA?source.pointA:source.pointB;
   var rectifier=root.Find("Full bringe rectifier "+suffix);var inverter=root.Find("Off grid inverter DC Input "+suffix);
   string name="Full bringe rectifier "+suffix+" to Off-grid Inverter DC Input "+suffix;
   var group=new GameObject(name).transform;group.SetParent(root,false);rectifier.SetParent(group,true);inverter.SetParent(group,true);
   Components(rectifier,template);Components(inverter,template);var aPlug=EnsurePlug(rectifier,sourcePlug);var bPlug=EnsurePlug(inverter,sourcePlug);
   var w=group.gameObject.AddComponent<WireInteraction>();w.pointA=rectifier.GetComponent<Interaction>();w.pointB=inverter.GetComponent<Interaction>();w.plugA=aPlug;w.plugB=bPlug;
   w.markerA=Marker(rectifier,aPlug,sourceA?source.markerA:source.markerB,steps.Find("Pointers"),"Full Bridge Rectifier "+suffix+" (Step 11)");
   w.markerB=Marker(inverter,bPlug,sourceA?source.markerA:source.markerB,steps.Find("Pointers"),"Off-grid Inverter DC Input "+suffix+" (Step 11)");
   w.onFirstPointClicked=new UnityEvent();CameraRoute(w.onFirstPointClicked,5);
   w.wireObject=WindExp4Step1Setup.Wire(steps.Find("Wires"),name,w.pointA,w.pointB,aPlug.transform,bPlug.transform,false,templates,color,"Assets/_Project/Models/Generated/EXP4 Step11 Wires");
   var row=new StoryboardSteps.Step{stepType=StepType.Interaction,interaction=w,uiObject=ui,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent()};CameraRoute(row.onStepStart,8);created.Add(row);Dirty(w);
  }
  owner.steps.InsertRange(anchor+1,created);
  if(!owner.steps.Where(s=>!created.Contains(s)).SequenceEqual(original))throw new InvalidOperationException("Existing rows changed.");
  ClearPointers(steps,owner.steps.Take(anchor+3));
  Dirty(owner);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();
 }
 [MenuItem("PV System/Verify EXP4 Step11 Connections And Pointer Clearance")]
 public static void Verify()
 {
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();
  var root=steps.Cast<Transform>().Where(t=>t.name=="Step 11").Single(t=>t.GetComponentsInChildren<Transform>(true).Any(c=>c.name=="Off grid inverter DC Input -ve"));
  var connections=root.GetComponentsInChildren<WireInteraction>(true);if(connections.Length!=2)throw new InvalidOperationException("Expected two DC wires.");
  var equipment=Equipment();
  foreach(var w in connections)
  {
   string suffix=w.pointA.name.EndsWith("-ve")?"-ve":"+ve",color=suffix=="-ve"?"Blue":"Yellow";
   if(w.pointB.name!="Off grid inverter DC Input "+suffix||!w.plugA.name.Contains(color)||!w.plugB.name.Contains(color)||!w.wireObject||!w.markerA||!w.markerB)throw new InvalidOperationException("Polarity or assignment mismatch.");
   foreach(var point in new[]{w.pointA,w.pointB})if(!point.GetComponent<BoxCollider>()||!point.GetComponent<OnMouseDownEvent>())throw new InvalidOperationException("Missing banana components.");
   var row=owner.steps.Single(s=>s.interaction==w);if(row.uiObject.name!="Subtitle Step 11"||row.onStepStart.GetPersistentTarget(0)!=Trigger(8)||w.onFirstPointClicked.GetPersistentTarget(0)!=Trigger(5))throw new InvalidOperationException("Incorrect UI/camera.");
   var container=w.wireObject.GetComponentInChildren<UnityEngine.Splines.SplineContainer>(true);var spline=container.Spline;
   if(Vector3.Distance(container.transform.TransformPoint((Vector3)spline[0].Position),w.plugA.transform.position)>.001f||Vector3.Distance(container.transform.TransformPoint((Vector3)spline[spline.Count-1].Position),w.plugB.transform.position)>.001f||container.GetComponent<UnityEngine.Splines.SplineExtrude>().targetMesh.vertexCount==0)throw new InvalidOperationException("Wire geometry mismatch.");
  }
  int anchor=owner.steps.FindIndex(s=>s.interaction&&s.interaction.name.Contains("L3 to wind"));
  if(!owner.steps.Skip(anchor+1).Take(2).Select(s=>s.interaction).SequenceEqual(connections.Cast<BaseInteraction>()))throw new InvalidOperationException("Wrong storyboard order.");
  foreach(var marker in owner.steps.Take(anchor+3).Select(s=>s.interaction).OfType<WireInteraction>().SelectMany(w=>new[]{w.markerA,w.markerB}).Where(m=>m&&m.transform.IsChildOf(steps)).Distinct())if(equipment.Any(r=>r.bounds.Intersects(MarkerBounds(marker))))throw new InvalidOperationException("Pointer overlap: "+marker.name);
  Debug.Log("EXP4 STEP11 VERIFIED: matching +/- polarity, blue/yellow plugs, complete banana components, independent spline meshes meet both plugs, pointers clear equipment, Subtitle11 directly after L3, camera8 to camera5; later rows preserved.");
 }
 [MenuItem("PV System/Audit EXP4 Step11 Connections")]
 public static void Audit()
 {
  var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();
  foreach(var root in steps.Cast<Transform>().Where(t=>t.name=="Step 11"))foreach(var t in root.GetComponentsInChildren<Transform>(true))
   Debug.Log("EXP4 ELEVEN OBJECT "+t.name+" parent="+t.parent.name+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))+" local="+t.localPosition.ToString("F4")+" world="+t.position.ToString("F4"));
  foreach(var t in steps.Find("Pointers").Cast<Transform>()){
   var r=t.GetComponentsInChildren<Renderer>(true);var b=r.Length>0?r[0].bounds:new Bounds(t.position,Vector3.zero);foreach(var x in r)b.Encapsulate(x.bounds);
   Debug.Log("EXP4 ELEVEN POINTER "+t.name+" position="+t.position.ToString("F4")+" bounds="+b.center.ToString("F4")+" size="+b.size.ToString("F4"));
   if(t.name=="Off grid inverter Ac Output L"||t.name=="DC LED Input Red")foreach(var equipment in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(x=>x.enabled&&!x.GetComponentInParent<StoryboardSteps>()&&x.bounds.Intersects(b)))Debug.Log("EXP4 ELEVEN OVERLAP "+t.name+" with="+equipment.name+" center="+equipment.bounds.center.ToString("F4")+" size="+equipment.bounds.size.ToString("F4"));
  }
  for(int i=0;i<owner.steps.Count;i++)Debug.Log("EXP4 ELEVEN ROW "+i+" "+(owner.steps[i].interaction?owner.steps[i].interaction.name:"null")+" UI="+(owner.steps[i].uiObject?owner.steps[i].uiObject.name:"null"));
 }
}

