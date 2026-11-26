using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TutorialFramework;
using Object=UnityEngine.Object;

public static class HybridFreeModeSetup
{
 static void Ref(Object o,string field,Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Bool(Object o,string field,bool value){var s=new SerializedObject(o);s.FindProperty(field).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Clear(Object o,params string[] fields){var s=new SerializedObject(o);foreach(var f in fields)s.FindProperty(f).ClearArray();s.ApplyModifiedPropertiesWithoutUndo();}
 static Transform Folder(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 [MenuItem("RE009/Repair Hybrid Free Graph References")]
 public static void RepairGraph(){
  if(Application.isPlaying)throw new Exception("Stop Play first.");
  var source=HybridGuidedCompletion.All<HybridTrainingController>().Single(h=>h.storyboard&&h.storyboard.StoryboardID==HybridExperimentSetup.Id);
  var f=HybridGuidedCompletion.All<FreeLabWindController>().Single();var binding=source.storyboard.GetComponent<HybridGuidedBindings>();
  if(!f.graph){var go=Folder(f.transform,"Hybrid Free Graph").gameObject;f.graph=go.AddComponent<PVGraphController>();EditorUtility.CopySerialized(source.graph,f.graph);}
  f.hybrid.graph=f.graph;Ref(f.graph,"hybridController",f.hybrid);Ref(f.graph,"freeWindController",null);Ref(f.graph,"owningStoryboard",null);
  if(!f.graphButton){var go=Object.Instantiate(binding.graphButton.gameObject,f.controlsRoot,true);go.name="Graph Button";f.graphButton=go.GetComponent<Interaction>();}
  f.gameObject.SetActive(true);EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);
 }
 [MenuItem("RE009/Audit Hybrid Free Scene")]
 public static void AuditScene(){var f=HybridGuidedCompletion.All<FreeLabWindController>().Single();var log=new List<string>();
 foreach(var socket in f.sockets){var so=new SerializedObject(socket.endpoint);log.Add("ENDPOINT "+socket.endpoint.name+" indicator="+(so.FindProperty("possibleConnectionIndicator").objectReferenceValue?"yes":"MISSING")+" slots="+socket.plugs.Length);}
 foreach(var c in f.controls){log.Add("CONTROL "+c.control.name+" parent="+HybridGuidedCompletion.Path(c.control.transform.parent));foreach(var r in c.control.transform.parent.GetComponentsInChildren<Renderer>(true))log.Add("VISUAL "+r.name+" active="+r.gameObject.activeSelf+" mat="+string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name)));}
 File.WriteAllLines("Logs/HybridReview/free-scene-audit.txt",log);}
 [MenuItem("RE009/Bind Hybrid Free Warning And Remove Deleted Free Callbacks")]
 public static void BindWarning(){
  if(Application.isPlaying)throw new Exception("Stop Play first.");var f=HybridGuidedCompletion.All<FreeLabWindController>().Single();
  var panel=(GameObject)new SerializedObject(f.lab).FindProperty("freeModeWarningPanel").objectReferenceValue;
  foreach(var button in panel.GetComponentsInChildren<Button>(true)){
   bool close=false;for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)if(button.onClick.GetPersistentMethodName(i)=="CloseFreeModeWarning"){
    close=true;UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick,i);
   }
   if(close)UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,f.lab.CloseFreeModeWarning);
  }
  foreach(var root in new[]{f.graphPage,f.hybrid.monitor})foreach(var click in root.GetComponentsInChildren<Template.OnMouseDownE>(true))
   for(int i=click.mouseDown.GetPersistentEventCount()-1;i>=0;i--){string method=click.mouseDown.GetPersistentMethodName(i);if(!click.mouseDown.GetPersistentTarget(i)&&(method=="SaveGraphPNG"||method=="SaveDataCSV"||method=="ClearRuntimeGraph"))UnityEditor.Events.UnityEventTools.RemovePersistentListener(click.mouseDown,i);}
  EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);
 }
 [MenuItem("RE009/Require Guided Lamp Wiring In Free Mode")]
 public static void RequireGuidedLamps(){
  if(Application.isPlaying)throw new Exception("Stop Play first.");EditorSceneManager.SaveOpenScenes();
  File.Copy("Assets/_Project/Scenes/Hybrid Lab.unity","Logs/HybridReview/before-required-lamps.unity",true);
  var f=HybridGuidedCompletion.All<FreeLabWindController>().Single();var h=f.hybrid;
  var so=new SerializedObject(f.mainPower);var row=so.FindProperty("enableConfigurations").GetArrayElementAtIndex(0);
  row.FindPropertyRelative("anyConnectionGroups").ClearArray();var groups=row.FindPropertyRelative("requiredConnectionGroups");
  var required=new[]{h.mainWiring.free,h.batteryWiring.free,h.inverterWiring.free}.Concat(h.lamps.Select(l=>l.connection.free)).ToArray();
  groups.arraySize=required.Length;for(int i=0;i<required.Length;i++)groups.GetArrayElementAtIndex(i).objectReferenceValue=required[i];
  so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);
 }
 [MenuItem("RE009/Repair Free Main OFF Piece And Graph Selection")]
 public static void RepairOffAndSelection(){
  if(Application.isPlaying)throw new Exception("Stop Play first.");EditorSceneManager.SaveOpenScenes();
  if(!File.Exists("Logs/HybridReview/before-free-controls-repair.unity"))File.Copy("Assets/_Project/Scenes/Hybrid Lab.unity","Logs/HybridReview/before-free-controls-repair.unity");
  var f=HybridGuidedCompletion.All<FreeLabWindController>().Single();
  var guided=HybridGuidedCompletion.All<HybridTrainingController>().Single(h=>h.storyboard&&h.storyboard.StoryboardID==HybridExperimentSetup.Id);
  var main=guided.storyboard.steps.Select(r=>r.interaction).OfType<SingleClickInteraction>().Single(c=>c.name=="Main Board ON");
  var source=main.interaction.transform.parent.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Red OFF Half");
  var go=f.mainOffButton;
  if(!go)go=new GameObject("Off_button004 (1)",typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider),typeof(Template.OnMouseDownE));
  go.transform.SetParent(source.transform.parent,false);go.transform.localPosition=source.transform.localPosition;go.transform.localRotation=source.transform.localRotation;go.transform.localScale=source.transform.localScale;
  go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;go.GetComponent<MeshRenderer>().sharedMaterials=source.sharedMaterials;
  var collider=go.GetComponent<BoxCollider>();var bounds=go.GetComponent<MeshFilter>().sharedMesh.bounds;collider.center=bounds.center;collider.size=bounds.size;
  go.transform.SetParent(f.mainPower.transform.parent,true);f.mainOffButton=go;
  f.graphSelectionTab=f.graphButton.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Selection Tab").gameObject;
  f.systemSelectionTab=f.systemButton.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Selection Tab").gameObject;
  f.graphSelectionTab.SetActive(false);f.systemSelectionTab.SetActive(true);
  EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);
 }
 [MenuItem("RE009/Preview Free Main Board Controls")]
 public static void PreviewBoard(){if(!Application.isPlaying)throw new Exception("Run Free Mode test first.");HybridGuidedCompletion.All<SmoothCameraMovement>().Single().MoveToPosition(1);}
 [MenuItem("RE009/Preview Free Graph Selection")]
 public static void PreviewGraph(){if(!Application.isPlaying)throw new Exception("Run Free Mode test first.");var f=HybridGuidedCompletion.All<FreeLabWindController>().Single();f.mainEmergency.SetValue(true);f.mainBreaker.SetValue(true);f.lab.HandleInteraction(f.mainPower.GetComponent<Interaction>(),out _);f.Refresh();f.graphButton.onClicked.Invoke();HybridGuidedCompletion.All<SmoothCameraMovement>().Single().MoveToPosition(CameraPositionReorderUtility.HybridMonitorIndex());}
 [MenuItem("RE009/Remove Inherited Free Inverter Button")]
 public static void RemoveInheritedInverterButton(){
  if(Application.isPlaying)throw new Exception("Stop Play first.");EditorSceneManager.SaveOpenScenes();File.Copy("Assets/_Project/Scenes/Hybrid Lab.unity","Logs/HybridReview/before-remove-inverter-button.unity",true);
  var f=HybridGuidedCompletion.All<FreeLabWindController>().Single();var obsolete=f.inverterPower;if(!obsolete)throw new Exception("Imported inverter button already removed");
  foreach(var control in f.GetComponentsInChildren<FreeLabControl>(true).Where(c=>c!=obsolete)){
   var so=new SerializedObject(control);var list=so.FindProperty("powerControlsToCutAndLock");for(int i=list.arraySize-1;i>=0;i--)if(list.GetArrayElementAtIndex(i).objectReferenceValue==obsolete){list.GetArrayElementAtIndex(i).objectReferenceValue=null;list.DeleteArrayElementAtIndex(i);}so.ApplyModifiedPropertiesWithoutUndo();
  }
  f.controls=f.controls.Where(c=>c.control!=obsolete).ToArray();f.inverterPower=null;
  var frame=obsolete.transform.parent;if(frame.parent!=f.controlsRoot)throw new Exception("Unexpected inverter control parent; refusing broader removal");Object.DestroyImmediate(frame.gameObject);
  EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);
 }
 [MenuItem("RE009/Build Hybrid Free Mode From Current Guided Experiment")]
 public static void Build()
 {
  if(Application.isPlaying)throw new Exception("Stop Play first.");
  EditorSceneManager.SaveOpenScenes();Directory.CreateDirectory("Logs/HybridReview");File.Copy("Assets/_Project/Scenes/Hybrid Lab.unity","Logs/HybridReview/before-hybrid-free.unity",true);
  var source=HybridGuidedCompletion.All<HybridTrainingController>().Single(s=>s.storyboard&&s.storyboard.StoryboardID==HybridExperimentSetup.Id);
  var old=HybridGuidedCompletion.All<FreeLabWindController>().Single();
  var oldRoot=old.GetComponentInParent<FreeLabSceneRoot>(true).gameObject;
  var root=Object.Instantiate(source.storyboard.gameObject,source.storyboard.transform.parent);root.name="Hybrid Free Mode Staging";root.SetActive(false);
  var board=root.GetComponent<StoryboardSteps>();var binding=root.GetComponent<HybridGuidedBindings>();var hybrid=root.GetComponentInChildren<HybridTrainingController>(true);
  var lab=root.AddComponent<FreeLabManager>();EditorUtility.CopySerialized(old.lab,lab);Clear(lab,"endpoints","controls","evaluators","sliders","threeStateToggles","calculationStageRouters","devices","deviceManagers","wireDefinitions");Ref(lab,"connectionState",null);
  var marker=root.AddComponent<FreeLabSceneRoot>();Ref(marker,"labContent",root);
  var runtime=root.AddComponent<FreeLabWindController>();runtime.lab=lab;runtime.hybrid=hybrid;runtime.endpointsRoot=Folder(root.transform,"Endpoints");runtime.wiresRoot=Folder(root.transform,"Wires");runtime.controlsRoot=Folder(root.transform,"Controls");Ref(lab,"wireContainer",runtime.wiresRoot);
  runtime.guidedRoots=HybridGuidedCompletion.All<StoryboardSteps>().Where(b=>!b.transform.IsChildOf(root.transform)).Select(b=>b.gameObject).ToArray();
  runtime.rotor=old.rotor;runtime.windSound=old.windSound;runtime.rotorVisualMultiplier=old.rotorVisualMultiplier;
  Ref(runtime,"turbineDevicesGroup",new SerializedObject(old).FindProperty("turbineDevicesGroup").objectReferenceValue);
  runtime.monitors=binding.monitors;runtime.graphPage=binding.graphPage;runtime.blackScreen=hybrid.blackScreen;runtime.graph=hybrid.graph;runtime.exporter=old.exporter;
  var graphSO=new SerializedObject(hybrid.graph);runtime.exporter=graphSO.FindProperty("exporter").objectReferenceValue as PVExperimentExporter;
  runtime.speedSlider=old.speedSlider;runtime.speedSliderText=old.speedSliderText;runtime.speedCollider=old.speedCollider;
  runtime.graphButton=binding.graphButton;runtime.systemButton=binding.systemButton;
  runtime.pages=new[]{new FreeLabWindController.Page{root=hybrid.monitor}};runtime.profiles=Array.Empty<FreeLabWindController.Profile>();runtime.rheostats=Array.Empty<FreeLabWindController.Rheostat>();runtime.lamps=Array.Empty<FreeLabWindController.Lamp>();
  hybrid.freeController=runtime;hybrid.storyboard=null;hybrid.windSpeed=null;hybrid.recordGraphMeasurements=true;
  var wireMap=new Dictionary<WireInteraction,FreeLabWireDefinition>();var endpoints=new Dictionary<string,FreeLabEndpoint>();var slots=new Dictionary<FreeLabEndpoint,List<GameObject>>();var originals=new List<GameObject>();
  string Key(Interaction point){var n=point.name;int i=n.IndexOf(" (Lamp ",StringComparison.Ordinal);return i>=0?n.Substring(0,i):n;}
  FreeLabEndpoint Endpoint(Interaction point,GameObject plug){
   string key=Key(point);if(!endpoints.TryGetValue(key,out var endpoint)){
    endpoint=point.GetComponent<FreeLabEndpoint>()??point.gameObject.AddComponent<FreeLabEndpoint>();Ref(endpoint,"interaction",point);Ref(endpoint,"wireAnchor",point.transform);Clear(endpoint,"additionalConnectionPlugs");
    endpoint.name=key;point.transform.SetParent(runtime.endpointsRoot,true);point.onClicked=new UnityEvent();point.Allow=true;point.gameObject.SetActive(true);foreach(var c in point.GetComponents<Collider>())c.enabled=true;
    foreach(var child in point.GetComponentsInChildren<Transform>(true))if(child.name.ToLower().Contains("possible connection")){endpoint.SetPossibleConnectionIndicator(child.gameObject);child.gameObject.SetActive(false);break;}
    endpoints.Add(key,endpoint);slots.Add(endpoint,new List<GameObject>());
   }
   else if(point.gameObject!=endpoint.gameObject)originals.Add(point.gameObject);
   plug.transform.SetParent(endpoint.transform,true);plug.SetActive(false);if(!slots[endpoint].Contains(plug))slots[endpoint].Add(plug);return endpoint;
  }
  var wires=board.steps.Select(r=>r.interaction).OfType<WireInteraction>().Distinct().ToArray();
  foreach(var w in wires){var d=w.wireObject.GetComponent<FreeLabWireDefinition>()??w.wireObject.AddComponent<FreeLabWireDefinition>();d.endpointA=Endpoint(w.pointA,w.plugA);d.endpointB=Endpoint(w.pointB,w.plugB);d.wireParentTemplate=w.wireObject;d.plugA=w.plugA;d.plugB=w.plugB;d.allowWhileOperating=false;w.wireObject.transform.SetParent(runtime.wiresRoot,true);w.wireObject.SetActive(false);wireMap.Add(w,d);}
  foreach(var duplicate in originals.Distinct())if(duplicate)Object.DestroyImmediate(duplicate);
  runtime.sockets=slots.Select(p=>new FreeLabWindController.Socket{endpoint=p.Key,plugs=p.Value.ToArray()}).ToArray();runtime.definitions=wireMap.Values.ToArray();
  FreeLabConnectionGroup Group(string name,IEnumerable<WireInteraction> list){var g=Folder(root.transform,name).gameObject.AddComponent<FreeLabConnectionGroup>();g.Configure(name,list.Where(w=>w&&wireMap.ContainsKey(w)).Select(w=>wireMap[w]));return g;}
  void Circuit(string name,HybridTrainingController.Circuit c){c.free=Group(name,c.guided??Array.Empty<WireInteraction>());c.guided=Array.Empty<WireInteraction>();}
  Circuit("Main Board Supply",hybrid.mainWiring);Circuit("Hybrid Battery",hybrid.batteryWiring);Circuit("Inverter Battery",hybrid.inverterWiring);Circuit("Wind Generator",hybrid.windWiring);Circuit("Solar Array",hybrid.solarWiring);
  for(int i=0;i<hybrid.lamps.Length;i++)Circuit("AC Lamp "+(i+1),hybrid.lamps[i].connection);
  var controls=new List<FreeLabWindController.Control>();
  FreeLabControl Control(SingleClickInteraction s){var point=s.interaction;var c=point.GetComponent<FreeLabControl>()??point.gameObject.AddComponent<FreeLabControl>();Ref(c,"interaction",point);Clear(c,"enableConfigurations","powerControlsToCutAndLock");Bool(c,"marksSystemOperating",false);Bool(c,"requiresAtLeastOneConnection",false);point.onClicked=new UnityEvent();point.Allow=true;
   Renderer cover=null;for(int i=0;i<s.onCompleted.GetPersistentEventCount();i++)if(s.onCompleted.GetPersistentMethodName(i)=="set_enabled")cover=s.onCompleted.GetPersistentTarget(i) as Renderer;
   controls.Add(new FreeLabWindController.Control{control=c,authoredInteraction=s,animator=s.animator,onAnimation=s.animationName,offAnimation=s.animationName,pressedVisual=cover});return c;}
  var singles=board.steps.Select(r=>r.interaction).OfType<SingleClickInteraction>().Where(s=>s.interaction).Distinct().ToArray();
  var cm=singles.ToDictionary(s=>s.name,Control);
  runtime.mainEmergency=cm["Main Board Emergency"];runtime.mainBreaker=cm["Main Board Circuit Breaker"];runtime.mainPower=cm["Main Board ON"];
  runtime.windEmergency=cm["Wind Emergency"];runtime.windBreaker=cm["Wind Circuit Breaker"];runtime.windPower=cm["Wind ON"];runtime.analyzerMode=hybrid.windRun=cm["Wind Run Selector"];
  hybrid.solarEmergency=cm["PV Emergency"];hybrid.solarPower=cm["PV ON"];hybrid.solarRun=cm["PV Run Selector"];
  var enable=Folder(runtime.controlsRoot,"PV Enable State").gameObject;enable.AddComponent<Interaction>();hybrid.solarEnable=enable.AddComponent<FreeLabControl>();Ref(hybrid.solarEnable,"interaction",enable.GetComponent<Interaction>());enable.SetActive(true);
  runtime.controls=controls.ToArray();runtime.inverterPower=null;
  void Lock(FreeLabControl safety,params FreeLabControl[] power){var so=new SerializedObject(safety);var p=so.FindProperty("powerControlsToCutAndLock");p.arraySize=power.Length;for(int i=0;i<power.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=power[i];so.ApplyModifiedPropertiesWithoutUndo();}
  Lock(runtime.mainEmergency,runtime.mainPower,runtime.windPower,hybrid.solarPower);Lock(runtime.mainBreaker,runtime.mainPower,runtime.windPower,hybrid.solarPower);Lock(runtime.windEmergency,runtime.windPower);Lock(runtime.windBreaker,runtime.windPower);Lock(hybrid.solarEmergency,hybrid.solarPower);
  Bool(runtime.mainPower,"marksSystemOperating",true);
  void Requirement(FreeLabControl c,FreeLabConnectionGroup[] groups,FreeLabControl[] required,bool lamps=false){var so=new SerializedObject(c);var p=so.FindProperty("enableConfigurations");p.arraySize=1;var row=p.GetArrayElementAtIndex(0);var a=row.FindPropertyRelative("requiredConnectionGroups");a.arraySize=groups.Length;for(int i=0;i<groups.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=groups[i];var r=row.FindPropertyRelative("requiredControls");r.arraySize=required.Length;for(int i=0;i<required.Length;i++){r.GetArrayElementAtIndex(i).FindPropertyRelative("control").objectReferenceValue=required[i];r.GetArrayElementAtIndex(i).FindPropertyRelative("requiredValue").boolValue=true;}if(lamps){a=row.FindPropertyRelative("anyConnectionGroups");a.arraySize=hybrid.lamps.Length;for(int i=0;i<hybrid.lamps.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=hybrid.lamps[i].connection.free;}so.ApplyModifiedPropertiesWithoutUndo();}
  Requirement(runtime.mainPower,new[]{hybrid.mainWiring.free,hybrid.batteryWiring.free,hybrid.inverterWiring.free}.Concat(hybrid.lamps.Select(l=>l.connection.free)).ToArray(),new[]{runtime.mainEmergency,runtime.mainBreaker});
  Requirement(runtime.windPower,new[]{hybrid.windWiring.free},new[]{runtime.mainPower,runtime.windEmergency,runtime.windBreaker});Requirement(hybrid.solarPower,new[]{hybrid.solarWiring.free},new[]{runtime.mainPower,hybrid.solarEmergency});
  hybrid.freeModeHiddenHardware=binding.replacedVisuals;
  foreach(var c in controls){
   var originalParent=c.control.transform.parent;
   var frame=Folder(originalParent,c.control.name+" frame");frame.SetParent(runtime.controlsRoot,true);
   c.control.transform.SetParent(frame,true);
   if(c.pressedVisual&&!c.pressedVisual.transform.IsChildOf(c.control.transform))c.pressedVisual.transform.SetParent(frame,true);
   foreach(var red in originalParent.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Off_button",StringComparison.OrdinalIgnoreCase)).ToArray()){
    if(red==originalParent||red.IsChildOf(c.control.transform))continue;
    red.SetParent(frame,true);red.gameObject.SetActive(true);
   }
  }
  foreach(var b in root.GetComponentsInChildren<BaseInteraction>(true))Object.DestroyImmediate(b);Object.DestroyImmediate(binding);Object.DestroyImmediate(board);
  // Only keep the authored endpoints, controls, active hybrid runtime and their wires.
  var keep=new HashSet<Transform>{runtime.endpointsRoot,runtime.wiresRoot,runtime.controlsRoot,hybrid.transform};foreach(var c in controls)keep.Add(c.control.transform.parent);
  foreach(Transform child in root.transform.Cast<Transform>().ToArray())if(!keep.Contains(child)&&!child.GetComponent<FreeLabConnectionGroup>())Object.DestroyImmediate(child.gameObject);
  Object.DestroyImmediate(oldRoot);root.name="Free Lab";root.SetActive(true);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
  RepairGraph();BindWarning();RepairOffAndSelection();File.WriteAllText("Logs/HybridReview/free-build.txt",$"Built one hybrid Free Lab: {runtime.definitions.Length} authored wires; {runtime.sockets.Length} unique endpoints; {runtime.controls.Length} physical controls; four required guided lamp circuits. Removed old Free Lab hierarchy.");
 }
}

[InitializeOnLoad]
public static class HybridFreeModeVerification
{
 static double next;
 static HybridFreeModeVerification(){if(SessionState.GetBool("RE009.FreeTest",false))EditorApplication.update+=Tick;}
 static void Check(bool valid,string text){if(!valid)throw new Exception(text);}
 static T[] All<T>() where T:Component=>HybridGuidedCompletion.All<T>();
 [MenuItem("RE009/Test Hybrid Free Mode")]
 public static void Begin(){if(Application.isPlaying)throw new Exception("Stop Play first.");SessionState.SetBool("RE009.FreeTest",true);SessionState.SetInt("RE009.FreeTestStage",0);EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;}
 [MenuItem("RE009/Inspect Free Test State")]
 public static void Inspect(){File.WriteAllText("Logs/HybridReview/free-test-state.txt",$"play={Application.isPlaying}/{EditorApplication.isPlaying} stage={SessionState.GetInt("RE009.FreeTestStage",0)} requested={SessionState.GetBool("RE009.FreeTest",false)} manager={InteractionManager.Instance} controls={All<FreeLabWindController>().Length}");Tick();}
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
 try{
 int stage=SessionState.GetInt("RE009.FreeTestStage",0);var manager=InteractionManager.Instance;
 if(!manager){var start=All<Button>().FirstOrDefault(b=>b.name=="Start Underline Button"&&HybridGuidedCompletion.Path(b.transform).Contains(HybridGuidedCompletion.Display+"/"));if(start){for(var t=start.transform;t;t=t.parent)t.gameObject.SetActive(true);start.onClick.Invoke();next+=5;}return;}
 if(stage==0){var start=All<Button>().FirstOrDefault(b=>b.name=="Start Underline Button"&&HybridGuidedCompletion.Path(b.transform).Contains(HybridGuidedCompletion.Display+"/"));if(!start)return;for(var t=start.transform;t;t=t.parent)t.gameObject.SetActive(true);start.onClick.Invoke();SessionState.SetInt("RE009.FreeTestStage",1);next+=5;return;}
 if(stage==1){SessionState.SetInt("RE009.FreeTestStage",2);manager.SetFree();next+=6;return;}
 if(!manager.Free)return;var f=All<FreeLabWindController>().SingleOrDefault(c=>c.isActiveAndEnabled);if(!f||!f.IsReady)return;
 SessionState.SetBool("RE009.FreeTest",false);EditorApplication.update-=Tick;Run(f);
 }catch(Exception e){SessionState.SetBool("RE009.FreeTest",false);EditorApplication.update-=Tick;File.WriteAllText("Logs/HybridReview/free-tests.txt","FAIL: "+e);Debug.LogException(e);}}
 static void Run(FreeLabWindController f){var h=f.hybrid;var lab=f.lab;
 Check(All<FreeLabSceneRoot>().Length==1&&All<FreeLabManager>().Length==1,"More than one Free Mode exists");Check(!f.inverterPower&&f.controls.All(c=>c.control.name!="Power connector button"),"Obsolete inverter button remains");Check(f.definitions.Length==36,"Old wires remain or hybrid wires missing");Check(f.definitions.SelectMany(d=>d.wireParentTemplate.GetComponentsInChildren<UnityEngine.Splines.SplineExtrude>(true)).All(e=>e.targetMesh&&!AssetDatabase.Contains(e.targetMesh)),"Free wires still share persistent guided meshes");Check(f.sockets.All(s=>s.endpoint&&s.endpoint.Interaction&&s.plugs.All(p=>p)),"Endpoint/plug references missing");Check(f.graph&&h.graph,"Graph reference missing");
 void Connect(FreeLabWireDefinition d){lab.HandleInteraction(d.endpointA.Interaction,out _);lab.HandleInteraction(d.endpointB.Interaction,out _);Check(lab.HasConnection(d.endpointA,d.endpointB),"Could not connect "+d.name);}
 var lampWires=h.lamps.SelectMany(l=>l.connection.free.RequiredWires).ToHashSet();
 foreach(var d in f.definitions.Where(d=>!lampWires.Contains(d)))Connect(d);
 Check(!f.mainPower.CanEnable(lab),"Main enabled without safety or lamp");f.mainEmergency.SetValue(true);f.mainBreaker.SetValue(true);
 foreach(var lamp in h.lamps.Take(3))foreach(var d in lamp.connection.free.RequiredWires)Connect(d);
 Check(!f.mainPower.CanEnable(lab),"Partial lamp wiring incorrectly permits power-on");
 foreach(var d in h.lamps.Last().connection.free.RequiredWires)Connect(d);
 Check(f.mainPower.CanEnable(lab),"Complete guided lamp wiring rejected");Check(!h.SolarEnabled,"Enable should start off");
 lab.HandleInteraction(f.mainPower.GetComponent<Interaction>(),out _);f.Refresh();
 Check(f.mainPower.Value&&h.lamps.All(l=>l.light.activeInHierarchy),"Power-on did not light all guided lamps with Enable off");
 var wire=lab.Connections.First(w=>lampWires.Any(d=>(w.EndpointA==d.endpointA&&w.EndpointB==d.endpointB)||(w.EndpointA==d.endpointB&&w.EndpointB==d.endpointA)));
 int n=lab.ConnectionCount;lab.RemoveConnection(wire);Check(lab.ConnectionCount==n,"Live wire incorrectly removed");
 var panel=(GameObject)new SerializedObject(lab).FindProperty("freeModeWarningPanel").objectReferenceValue;panel.GetComponentsInChildren<Button>(true).First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)=="CloseFreeModeWarning")).onClick.Invoke();Check(!panel.activeSelf,"Warning OK failed");
 Check(f.mainOffButton&&f.mainOffButton.activeInHierarchy&&f.mainOffButton.GetComponent<Renderer>().enabled,"Red OFF piece missing");f.mainOffButton.GetComponent<Template.OnMouseDownE>().mouseDown.Invoke();Check(!f.mainPower.Value&&h.lamps.All(l=>!l.light.activeSelf),"Red OFF button failed to cut lamp power");
 var removedA=wire.EndpointA;var removedB=wire.EndpointB;lab.RemoveConnection(wire);Check(!lab.HasConnection(removedA,removedB)&&!f.mainPower.CanEnable(lab),"Missing lamp wire does not prevent power-on");
 foreach(var d in lampWires)if(!lab.HasConnection(d.endpointA,d.endpointB))Connect(d);
 f.mainEmergency.SetValue(true);f.mainBreaker.SetValue(true);f.mainPower.SetValue(true);f.windEmergency.SetValue(true);f.windBreaker.SetValue(true);f.windPower.SetValue(true);h.windRun.SetValue(true);h.solarEmergency.SetValue(true);h.solarPower.SetValue(true);h.solarRun.SetValue(true);h.solarEnable.SetValue(true);f.SetRPM(1000);f.Refresh();h.SendMessage("LateUpdate");
 Check(h.lamps.All(l=>l.light.activeInHierarchy),"All connected lamps not on");Check(h.Reading.pvPower>0&&h.Reading.wind.acPower>0,"Hybrid sources not producing");Check(h.powerAnalyzerCanvas.activeInHierarchy,"Analyzer hidden");Check(h.powerAnalyzerText.text==PVGuidedDataBinder.FormatPowerAnalyzer(h.Reading.wind.acPower,Mathf.Sqrt(3f)*Mathf.Abs(h.Reading.wind.acVoltage*h.Reading.wind.acCurrent)),"Analyzer wrong source");
 Check(f.sockets.Where(s=>s.endpoint.name.StartsWith("Off grid inverter Ac Output")).All(s=>s.plugs.Length==4&&s.plugs.All(p=>p.activeSelf)),"Inverter four-plug stack failed");
 f.graphButton.onClicked.Invoke();Check(f.graphPage.activeInHierarchy&&!f.monitors.activeSelf&&f.graphSelectionTab.activeInHierarchy&&!f.systemSelectionTab.activeSelf,"Graph selection failed");f.systemButton.onClicked.Invoke();Check(f.monitors.activeInHierarchy&&!f.graphPage.activeSelf&&f.systemSelectionTab.activeInHierarchy&&!f.graphSelectionTab.activeSelf,"System selection failed");
 h.solarEnable.SetValue(false);f.Refresh();Check(f.mainPower.Value&&h.Reading.pvPower==0&&h.Reading.wind.acPower>0,"PV Enable incorrectly cuts system/wind power");f.mainEmergency.SetValue(false);f.Refresh();Check(!f.mainPower.Value&&!f.windPower.Value&&h.lamps.All(l=>!l.light.activeSelf),"Emergency did not cut power and lamps");
 File.WriteAllText("Logs/HybridReview/free-tests.txt","PASS: single hybrid Free Mode; 36 current wires; all four guided lamp circuits required; no inherited inverter button; main ON lights all four lamps automatically; red OFF cuts power; graph/system Selection Tabs switch correctly; Enable-independent system power; wind/PV math and analyzer; four stacked inverter plugs per polarity; live removal blocked and off-state removal works; graph/system tabs; emergency safety cutoff.");Debug.Log("HYBRID FREE TEST PASS");
 }
}
