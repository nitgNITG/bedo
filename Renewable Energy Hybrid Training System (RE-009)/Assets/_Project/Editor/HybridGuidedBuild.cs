using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static partial class HybridGuidedCompletion
{
 [Serializable]public class TextPair{public string english,arabic;}
 [Serializable]public class Instruction:TextPair{public int number;}
 [Serializable]public class Note:TextPair{public int afterInstruction;}
 [Serializable]public class Quiz:Note{public string[] englishAnswers,arabicAnswers;public int correctAnswerIndex;}
 [Serializable]public class Definition{public Instruction[] instructions;public Note[] notes;public Quiz[] quizzes;public TextPair[] objectives;}
 static StoryboardSteps owner,old;
 static HybridTrainingController system;
 static HybridGuidedBindings bindings;
 static Transform steps,ui,popups;
 static Dictionary<int,List<StoryboardSteps.Step>> rows;
 static Dictionary<string,int> cameras;
 static Definition definition;
 static List<Renderer> covers,replaced;
 static Transform At(string path)=>All<Transform>().Single(t=>Path(t)==path);
 static Transform Folder(int n){var t=steps.Find("Step "+n);if(!t){t=new GameObject("Step "+n).transform;t.SetParent(steps,false);}return t;}
 static void Route(UnityEvent ev,int index){var camera=All<SmoothCameraMovement>().Single();UnityEventTools.AddIntPersistentListener(ev,camera.MoveToPosition,index);}
 static int Cam(string name,Vector3 pos,Vector3 target){
 var movement=All<SmoothCameraMovement>().Single();var so=new SerializedObject(movement);var list=so.FindProperty("cameraPositions");
 for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue is Transform t&&t.name==name)return i;
 var root=(list.GetArrayElementAtIndex(0).objectReferenceValue as Transform).parent;
 var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=pos;go.transform.rotation=Quaternion.LookRotation(target-pos);
 int index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=go.transform;so.ApplyModifiedPropertiesWithoutUndo();Dirty(movement);return index;
 }
 static int CameraFor(string n){
 n=n.ToLowerInvariant();if(n.Contains("inverter device"))return cameras["device"];if(n.StartsWith("pv 1")||n.Contains("solar panel comm"))return cameras["solar1"];if(n.StartsWith("pv 2"))return cameras["solar2"];if(n.StartsWith("pv 3"))return cameras["solar3"];
 if(n.Contains("hybrid"))return cameras["hybrid"];if(n.Contains("dump"))return 8;if(n.Contains("wind mill"))return 11;if(n.Contains("main board"))return 1;if(n.Contains("off-grid")||n.Contains("off grid"))return 5;if(n.Contains("ac halogen")||n.Contains("ac led"))return 3;if(n.Contains("battery"))return 4;return 0;
 }
 static StoryboardSteps.Step Row(int n,BaseInteraction interaction,int camera=0,float delay=-1){
 var row=new StoryboardSteps.Step{interaction=interaction,uiObject=ui.Find("Subtitle Step "+n).gameObject,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent(),stepType=delay<0?StepType.Interaction:StepType.Delay,delayDuration=Mathf.Max(0,delay)};
 Route(row.onStepStart,camera);rows[n].Add(row);return row;
 }
 static void RouteWire(WireInteraction w,StoryboardSteps.Step row){row.onStepStart=new UnityEvent();Route(row.onStepStart,CameraFor(w.pointA.name));w.onFirstPointClicked=new UnityEvent();Route(w.onFirstPointClicked,CameraFor(w.pointB.name));}
 static SingleClickInteraction Wait(int n,string name){var go=new GameObject(name);go.transform.SetParent(Folder(n),false);var s=go.AddComponent<SingleClickInteraction>();s.onCompleted=new UnityEvent();s.onUndone=new UnityEvent();s.IsNextStep=true;return s;}
 static SingleClickInteraction Source(string name)=>old.GetComponentsInChildren<SingleClickInteraction>(true).First(s=>s.name==name&&s.interaction&&Path(s.transform).Contains("/Steps/"));
 static SingleClickInteraction Control(int n,SingleClickInteraction source,string name,Transform replacement=null,int camera=1){
 var target=replacement?replacement.position:source.transform.position;var rotation=replacement?replacement.rotation:source.transform.rotation;
 var delta=rotation*Quaternion.Inverse(source.transform.rotation);
 var frame=new GameObject(name+" Animation Frame").transform;frame.SetParent(Folder(n),false);frame.position=target-delta*(source.transform.position-source.transform.parent.position);frame.rotation=delta*source.transform.parent.rotation;frame.localScale=source.transform.parent.lossyScale;
 var clone=Object.Instantiate(source.gameObject,frame,false);clone.name=name;var click=clone.GetComponent<SingleClickInteraction>();click.onCompleted=new UnityEvent();click.onUndone=new UnityEvent();click.IsNextStep=true;click.interaction.Allow=false;click.interaction.onClicked=new UnityEvent();
 if(source.marker){click.marker=Object.Instantiate(source.marker,Folder(n),true);click.marker.name=name+" Pointer";click.marker.transform.position=target+Vector3.up*1.4f+Vector3.back*.5f;click.marker.SetActive(false);}
 // Keep each copied Animator's local coordinate frame, including the wind CB's scaled parent.
 if(replacement&&replacement.GetComponent<Renderer>())replaced.Add(replacement.GetComponent<Renderer>());
 foreach(var r in At("Equipment/Wind Lab").GetComponentsInChildren<Renderer>(true).Where(r=>Vector3.Distance(r.transform.position,target)<.03f&&r.bounds.size.magnitude<8))if(!replaced.Contains(r))replaced.Add(r);
 Renderer cover=null;
 if(n==15)cover=At("Equipment/Wind Lab/INPUTS (solar panel)/Off_button001").GetComponent<Renderer>();
 else if(n==13||n==21)for(int i=0;i<source.onCompleted.GetPersistentEventCount();i++)if(source.onCompleted.GetPersistentTarget(i) is Renderer r){cover=r;break;}
 if(cover){covers.Add(cover);var setter=(UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>),cover,typeof(Renderer).GetProperty("enabled").GetSetMethod());UnityEventTools.AddBoolPersistentListener(click.onCompleted,setter,false);}
 UnityEventTools.AddIntPersistentListener(click.onCompleted,system.CompleteInstruction,n);Row(n,click,camera);return click;
 }
 static WireInteraction Cable(int n,string name,string a,Vector3 ap,Quaternion ar,int ac,string b,Vector3 bp,Quaternion br,int bc){
 var template=old.steps.Select(s=>s.interaction).OfType<WireInteraction>().First(w=>w.name.ToLowerInvariant().Contains("ethernet"));
 var go=Object.Instantiate(template.gameObject,Folder(n),true);go.name=name;var wire=go.GetComponent<WireInteraction>();
 wire.pointA.name=a;wire.pointB.name=b;wire.pointA.transform.SetPositionAndRotation(ap,ar);wire.pointB.transform.SetPositionAndRotation(bp,br);wire.pointA.onClicked=new UnityEvent();wire.pointB.onClicked=new UnityEvent();wire.pointA.Allow=wire.pointB.Allow=false;
 wire.markerA=Object.Instantiate(template.markerA,go.transform,true);wire.markerB=Object.Instantiate(template.markerB,go.transform,true);
 wire.markerA.name=a+" Pointer";wire.markerB.name=b+" Pointer";wire.markerA.transform.position=ap+Vector3.up*1.2f+Vector3.back*.3f;wire.markerB.transform.position=bp+Vector3.up*1.2f+Vector3.back*.3f;wire.markerA.SetActive(false);wire.markerB.SetActive(false);
 wire.onCompleted=new UnityEvent();wire.onUndone=new UnityEvent();wire.onFirstPointClicked=new UnityEvent();Route(wire.onFirstPointClicked,bc);
 wire.wireObject=WindExp4Step1Setup.Wire(steps.Find("Wires"),name,wire.pointA,wire.pointB,wire.plugA.transform,wire.plugB.transform,true,new[]{template},"Comm","Assets/_Project/Models/Generated/Hybrid Wires");
 var container=wire.wireObject.GetComponentInChildren<SplineContainer>(true);var start=wire.plugA.transform.position;var end=wire.plugB.transform.position;float z=Mathf.Min(start.z,end.z)-5;
 var points=new[]{start,start-wire.plugA.transform.forward*2.5f,new Vector3(start.x,start.y-2,z),new Vector3(end.x,end.y-2,z),end-wire.plugB.transform.forward*2.5f,end};
 container.Splines=new[]{new Spline(points.Select(p=>(float3)container.transform.InverseTransformPoint(p)),TangentMode.AutoSmooth,false)};container.Spline.SetTangentMode(0,TangentMode.Linear);container.Spline.SetTangentMode(container.Spline.Count-1,TangentMode.Linear);container.GetComponent<SplineExtrude>().Rebuild();
 foreach(var c in wire.wireObject.GetComponentsInChildren<FreeLabWireDefinition>(true))Object.DestroyImmediate(c);foreach(var c in wire.wireObject.GetComponentsInChildren<FreeLabWire>(true))Object.DestroyImmediate(c);foreach(var c in wire.wireObject.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
 wire.wireObject.SetActive(false);wire.plugA.SetActive(false);wire.plugB.SetActive(false);Row(n,wire,ac);return wire;
 }
 static void CopyObjectFields(Object source,Object target,params string[] fields){var from=new SerializedObject(source);var to=new SerializedObject(target);foreach(string f in fields){var p=from.FindProperty(f);if(p!=null)to.CopyFromSerializedProperty(p);}to.ApplyModifiedPropertiesWithoutUndo();}
 static void ArrayRefs(Object target,string field,IEnumerable<Object> values){var list=values.ToArray();var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=list.Length;for(int i=0;i<list.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=list[i];so.ApplyModifiedPropertiesWithoutUndo();Dirty(target);}
 [MenuItem("RE009/Complete Guided Sequence")]
 public static void Build(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);old=All<StoryboardSteps>().Single(b=>b.StoryboardID=="OFF Grid System Using Wind Turbine");
 if(owner.steps.Count!=32)throw new Exception("Expected the unchanged 32 staged circuit wires before final setup.");
 steps=owner.transform.Find("Steps");ui=All<Transform>().Single(t=>t.name=="Hybrid Experiment UI");system=owner.GetComponentInChildren<HybridTrainingController>(true);bindings=owner.gameObject.AddComponent<HybridGuidedBindings>();bindings.board=owner;bindings.system=system;bindings.subtitleGroup=ui.gameObject;
 covers=new List<Renderer>();replaced=new List<Renderer>();definition=JsonUtility.FromJson<Definition>(File.ReadAllText("Assets/_Project/Data/Hybrid/RE009Storyboard.json"));
 cameras=new Dictionary<string,int>{["solar1"]=Cam("Hybrid Solar Board 1",new Vector3(-75,143,-82),new Vector3(-75,128,-59)),["solar2"]=Cam("Hybrid Solar Board 2",new Vector3(-30.8f,143,-82),new Vector3(-30.8f,128,-59)),["solar3"]=Cam("Hybrid Solar Board 3",new Vector3(13.7f,143,-82),new Vector3(13.7f,128,-59)),["hybrid"]=Cam("Hybrid Controller",new Vector3(-137,157,-78),new Vector3(-137,149,-52.3f)),["device"]=Cam("Hybrid Inverter COM Port",new Vector3(-98.4f,174,-70),new Vector3(-98.4f,174,-56.05f))};
 rows=Enumerable.Range(1,25).ToDictionary(n=>n,n=>new List<StoryboardSteps.Step>());
 foreach(var r in owner.steps){int n=int.Parse(r.uiObject.name.Replace("Subtitle Step ",""));rows[n].Add(r);RouteWire((WireInteraction)r.interaction,r);}
 var solarPort=At("Equipment/Wind Lab/UNUSED (Solar)/Plane002");
 Cable(2,"PV Solar Panel Comm.Out to Main Board Comm.In","Solar Panel Comm.Out",solarPort.position,solarPort.rotation,cameras["solar1"],"Main Board Comm.In",new Vector3(-154.97f,159.76f,-52.65f),Quaternion.Euler(0,180,0),1);
 var ethernet=old.steps.Select(s=>s.interaction).OfType<WireInteraction>().First(w=>w.name.ToLowerInvariant().Contains("ethernet"));
 Cable(3,"Main Board Comm.Out to Wind Mill Comm.In","Main Board Comm.Out",ethernet.pointA.transform.position,ethernet.pointA.transform.rotation,1,"wind mill module Comm.In",ethernet.pointB.transform.position,ethernet.pointB.transform.rotation,11);
 // The inherited off-grid face has no Ethernet socket. Add the missing physical socket required by this storyboard.
 var socket=Object.Instantiate(solarPort.gameObject,At("Equipment/Wind Lab/UNUSED (MDOULE SYSTEM)/off grid"),true);socket.name="Hybrid Off-grid Inverter Comm.In Socket";socket.transform.SetPositionAndRotation(new Vector3(-116.2f,162.9f,-52.65f),Quaternion.Euler(0,180,0));
 var label=new GameObject("Comm.In Label",typeof(TextMeshPro));label.transform.SetParent(socket.transform,false);label.transform.position=socket.transform.position+Vector3.up*.65f;label.transform.rotation=Quaternion.identity;var labelText=label.GetComponent<TextMeshPro>();labelText.text="Comm.In";labelText.fontSize=1.4f;labelText.alignment=TextAlignmentOptions.Center;labelText.rectTransform.sizeDelta=new Vector2(2,.6f);
 Cable(6,"Off-grid Inverter Module Comm.In to Inverter Device COM","Off-grid Inverter Module Comm.In",socket.transform.position,socket.transform.rotation,5,"Inverter Device COM",new Vector3(-98.46f,174.60f,-56.08f),Quaternion.Euler(0,180,0),cameras["device"]);
 Control(11,Source("Emergeny_button002 (1)"),"Main Board Emergency",null,1);
 Control(12,Source("single_432 (1)"),"Main Board Circuit Breaker",null,1);
 Control(13,Source("on_button004 (1)"),"Main Board ON",null,1);
 Control(14,Source("Emergeny_button wind"),"PV Emergency",At("Equipment/Wind Lab/INPUTS (solar panel)/Emergeny_button001"),cameras["solar1"]);
 Control(15,Source("Power_Switch wind"),"PV ON",At("Equipment/Wind Lab/INPUTS (solar panel)/on_button001"),cameras["solar1"]);
 var mode=All<SingleClickInteraction>().First(s=>Path(s.transform).StartsWith("Experiments/Exp 1/")&&s.name=="Mode selection wind001"&&s.interaction);
 Control(16,mode,"PV Run Selector",At("Equipment/Wind Lab/INPUTS (solar panel)/Pump_Switch"),cameras["solar1"]);
 Control(19,Source("Emergeny_button wind"),"Wind Emergency",null,11);Control(20,Source("Breaker wind"),"Wind Circuit Breaker",null,11);Control(21,Source("Power_Switch wind"),"Wind ON",null,11);Control(22,mode,"Wind Run Selector",null,11);
 var speedSource=old.GetComponentsInChildren<WindTurbineSpeedController>(true).Single();var speedObject=new GameObject("Hybrid Wind Speed Controller");speedObject.transform.SetParent(owner.transform,false);var speed=speedObject.AddComponent<WindTurbineSpeedController>();EditorUtility.CopySerialized(speedSource,speed);
 var oldAudio=new SerializedObject(speed).FindProperty("windMillAudio").objectReferenceValue as AudioSource;
 if(oldAudio){var audio=speedObject.AddComponent<AudioSource>();EditorUtility.CopySerialized(oldAudio,audio);audio.playOnAwake=false;Ref(speed,"windMillAudio",audio);}
 Ref(speed,"runtimeState",null);Ref(speed,"dataBinder",null);Ref(speed,"graphController",null);Ref(speed,"loadRheostat",null);Ref(speed,"speedStoryboard",owner);speed.OnMaximumReached=new UnityEvent();speed.OnSpeedChanged=new UnityEvent<float>();system.windSpeed=speed;
 var speedSO=new SerializedObject(speed);speedSO.FindProperty("initialSpeed").floatValue=0;speedSO.FindProperty("defaultResistance").floatValue=4;speedSO.ApplyModifiedPropertiesWithoutUndo();
 var checker=All<SliderRangeChecker>().Single(c=>new SerializedObject(c).FindProperty("windSpeedControllers").arraySize>0);var checkerSO=new SerializedObject(checker);var slider=checkerSO.FindProperty("slider").objectReferenceValue as Slider;var speedCollider=checkerSO.FindProperty("windSliderCollider").objectReferenceValue as Collider;
 UnityEventTools.AddPersistentListener(speed.OnSpeedChanged,slider.SetValueWithoutNotify);
 bindings.speedCollider=speedCollider;bindings.speedPopup=checkerSO.FindProperty("windSpeedPopup").objectReferenceValue as CanvasGroup;ArrayRefs(checker,"windSpeedControllers",new[]{speed});Ref(speed,"speedSlider",slider);Ref(speed,"speedSliderCollider",speedCollider);
 var speedWait=Wait(23,"Reach Maximum Wind Speed");bindings.speedWait=speedWait;var speedRow=Row(23,speedWait,12);UnityEventTools.AddVoidPersistentListener(speedRow.onStepStart,bindings.ShowSpeed);UnityEventTools.AddVoidPersistentListener(speed.OnMaximumReached,bindings.CompleteSpeed);
 Row(17,null,12,4);Row(24,null,12,5);var finished=Wait(25,"Experiment Finished");Row(25,finished,12);
 var enable=system.monitor.GetComponentInChildren<HybridEnableSwitch>(true);var enableWait=Wait(18,"Enable PV Output");enable.instruction=enableWait;Row(18,enableWait,12);
 var windUI=At("Equipment/Wind Lab/UI/Wind UI");bindings.monitors=windUI.Find("Monitors").gameObject;bindings.graphPage=windUI.Find("Wind graph").gameObject;bindings.tabs=windUI.GetComponent<WindMonitorTabs>();bindings.systemButton=windUI.Find("System Button").GetComponent<Interaction>();bindings.oldScreens=windUI.Find("Monitors").Cast<Transform>().Where(t=>t.gameObject!=system.monitor).Select(t=>t.gameObject).ToArray();
 ConfigurePopups();ConfigureGraph();
 owner.steps=Enumerable.Range(1,25).SelectMany(n=>rows[n]).ToList();
 enable.instructionIndex=owner.steps.FindIndex(s=>s.interaction==enableWait);speedSO=new SerializedObject(speed);speedSO.FindProperty("speedStepIndex").intValue=owner.steps.FindIndex(s=>s.interaction==speedWait);speedSO.ApplyModifiedPropertiesWithoutUndo();
 UnityEventTools.AddVoidPersistentListener(owner.steps[0].onStepStart,bindings.Initialize);
 foreach(int n in new[]{17,24})UnityEventTools.AddVoidPersistentListener(rows[n][0].onStepStart,bindings.ShowSystem);
 bindings.resetCovers=covers.Distinct().ToArray();bindings.replacedVisuals=replaced.Distinct().ToArray();
 CopyObjectFields(old,owner,"experimentTitleText","introPanel","warningObject","warningText","warningDuration","RunOnStart");owner.audioSource=owner.gameObject.AddComponent<AudioSource>();owner.audioSource.playOnAwake=false;owner.delayBetweenSteps=.15f;
 Ref(owner,"selectedExperimentObject",ui.gameObject);ArrayRefs(owner,"startFromIntroObjects",new Object[]{ui.gameObject,bindings.notesGroup});ArrayRefs(owner,"guidedOnlyObjects",new Object[]{steps.gameObject});
 foreach(var b in All<StoryboardSteps>().Where(b=>b!=owner&&Path(b.transform).StartsWith("Experiments/Exp ")))b.gameObject.SetActive(false);
 owner.gameObject.SetActive(false);Dirty(owner);Dirty(system);Dirty(bindings);Dirty(enable);Dirty(speed);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
 File.WriteAllText("Temp/RE009Planning/guided-build.txt","Built "+owner.steps.Count+" guided rows / 25 subtitle numbers, 35 wires, complete controls, 3 notes, 2 quizzes and cameras.");
 }
}
