using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static partial class HybridGuidedCompletion
{
 [MenuItem("RE009/Bind Existing Wind Analyzer To Hybrid Readings")]
 public static void BindHybridAnalyzer(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var sys=board.GetComponentInChildren<HybridTrainingController>(true);
 var source=All<PVGuidedDataBinder>().First(b=>{var so=new SerializedObject(b);return so.FindProperty("powerAnalyzerCanvas").objectReferenceValue&&so.FindProperty("powerAnalyzerText").objectReferenceValue;});
 var fields=new SerializedObject(source);var canvas=(GameObject)fields.FindProperty("powerAnalyzerCanvas").objectReferenceValue;var text=(TMPro.TMP_Text)fields.FindProperty("powerAnalyzerText").objectReferenceValue;
 if(!text.transform.IsChildOf(canvas.transform))throw new Exception("Analyzer text is outside its canvas.");
 if(canvas.GetComponentInParent<StoryboardSteps>()&&canvas.GetComponentInParent<StoryboardSteps>()!=board){var copy=Object.Instantiate(canvas,board.transform,true);copy.name="Hybrid Power Analyzer";text=copy.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.name==text.name);canvas=copy;}
 sys.powerAnalyzerCanvas=canvas;sys.powerAnalyzerText=text;canvas.SetActive(false);Dirty(sys);EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
 }
 [MenuItem("RE009/Correct Guided Switch Visual Bindings")]
 public static void CorrectControlBindings(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var sys=board.GetComponentInChildren<HybridTrainingController>(true);var bind=board.GetComponent<HybridGuidedBindings>();
 var wind=All<StoryboardSteps>().Single(b=>b.StoryboardID=="OFF Grid System Using Wind Turbine");
 var hardware=At("Equipment/Wind Lab");var reset=new List<Renderer>();var indicators=new List<Renderer>();var hidden=new List<Renderer>();var report=new List<string>();
 SingleClickInteraction SourceControl(string name,bool solar=false)=>wind.GetComponentsInChildren<SingleClickInteraction>(true).Single(s=>s.name==name&&Path(s.transform).Contains(solar?"/Step Objects/Step 4/Clickables/":"/Steps/"));
 var specs=new[]{(11,SourceControl("Emergeny_button002 (1)"),(Transform)null),(12,SourceControl("single_432 (1)"),(Transform)null),(13,SourceControl("on_button004 (1)"),(Transform)null),
 (14,SourceControl("Emergeny_button 2",true),At("Equipment/Wind Lab/INPUTS (solar panel)/Emergeny_button001")),
 (15,SourceControl("on_button001 (1)",true),At("Equipment/Wind Lab/INPUTS (solar panel)/on_button001")),
 (16,SourceControl("Pump_Switch",true),At("Equipment/Wind Lab/INPUTS (solar panel)/Pump_Switch")),
 (19,SourceControl("Emergeny_button wind"),(Transform)null),(20,SourceControl("Breaker wind"),(Transform)null),(21,SourceControl("Power_Switch wind"),(Transform)null),
 (22,All<SingleClickInteraction>().First(s=>Path(s.transform).StartsWith("Experiments/Exp 1/")&&s.name=="Mode selection wind001"&&s.interaction),(Transform)null)};
 foreach(var spec in specs){
 int originalNumber=spec.Item1;int n=originalNumber>6?originalNumber-1:originalNumber;var source=spec.Item2;var target=spec.Item3;var row=board.steps.Single(s=>s.uiObject&&s.uiObject.name=="Subtitle Step "+n&&s.interaction is SingleClickInteraction);var prior=(SingleClickInteraction)row.interaction;
 var targetPosition=target?target.position:source.transform.position;var delta=target?target.rotation*Quaternion.Inverse(source.transform.rotation):Quaternion.identity;
 var frame=new GameObject(prior.name+" Correct Animation Frame").transform;frame.SetParent(prior.transform.parent.parent,false);frame.position=targetPosition-delta*(source.transform.position-source.transform.parent.position);frame.rotation=delta*source.transform.parent.rotation;frame.localScale=source.transform.parent.lossyScale;
 var go=Object.Instantiate(source.gameObject,frame,false);go.name=prior.name;var click=go.GetComponent<SingleClickInteraction>();click.onCompleted=new UnityEvent();click.onUndone=new UnityEvent();click.interaction.onClicked=new UnityEvent();click.interaction.Allow=false;
 if(originalNumber==14||originalNumber==15||originalNumber==16){
 var audioSource=originalNumber==14?SourceControl("Emergeny_button wind"):originalNumber==15?SourceControl("Power_Switch wind"):specs.Last().Item2;
 CopyObjectFields(audioSource.interaction,click.interaction,"interactionSound");
 }
 if(prior.marker){click.marker=prior.marker;click.marker.transform.SetParent(frame,true);click.marker.transform.position=targetPosition+Vector3.up*1.4f+Vector3.back*.5f;}
 // Rebind visual-only callbacks to this experiment's copies, never to an inactive wind step.
 for(int i=0;i<source.onCompleted.GetPersistentEventCount();i++)if(source.onCompleted.GetPersistentTarget(i) is Renderer sourceRenderer&&source.onCompleted.GetPersistentMethodName(i)=="set_enabled"){
 Renderer cover;
 if(sourceRenderer.transform==source.transform)cover=go.GetComponent<Renderer>();
 else if(sourceRenderer.transform.IsChildOf(source.transform)){var relative=sourceRenderer.transform.name;cover=go.GetComponentsInChildren<Renderer>(true).Single(r=>r.name==relative);}
 else{var copy=Object.Instantiate(sourceRenderer.gameObject,frame,false);copy.name=sourceRenderer.name+" Pressed Cover";copy.transform.position=frame.TransformPoint(source.transform.parent.InverseTransformPoint(sourceRenderer.transform.position));copy.transform.rotation=delta*sourceRenderer.transform.rotation;cover=copy.GetComponent<Renderer>();}
 reset.Add(cover);var setter=(UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>),cover,typeof(Renderer).GetProperty("enabled").GetSetMethod());UnityEventTools.AddBoolPersistentListener(click.onCompleted,setter,false);
 }
 if(originalNumber==15){
 var off=At("Equipment/Wind Lab/INPUTS (solar panel)/Off_button001").GetComponent<Renderer>();
 var cover=Object.Instantiate(off.gameObject,frame,true).GetComponent<Renderer>();cover.name="PV OFF Pressed Cover";reset.Add(cover);hidden.Add(off);
 var setter=(UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>),cover,typeof(Renderer).GetProperty("enabled").GetSetMethod());UnityEventTools.AddBoolPersistentListener(click.onCompleted,setter,false);
 }
 if(originalNumber==13||originalNumber==15){
 var model=At(originalNumber==13?"Equipment/Wind Lab/Inputs (photovolatics)/on_button004":"Equipment/Wind Lab/INPUTS (solar panel)/on_button001");
 var indicator=Object.Instantiate(model.gameObject,frame,true);indicator.name=click.name+" Green ON Indicator";
 foreach(var collider in indicator.GetComponentsInChildren<Collider>(true))collider.enabled=false;
 var lit=indicator.GetComponent<Renderer>();lit.enabled=false;indicators.Add(lit);hidden.Add(model.GetComponent<Renderer>());
 var setter=(UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>),lit,typeof(Renderer).GetProperty("enabled").GetSetMethod());UnityEventTools.AddBoolPersistentListener(click.onCompleted,setter,true);
 }
 // Hide the stationary hardware mesh that the copied animated mesh actually replaces.
 foreach(var visible in go.GetComponentsInChildren<MeshRenderer>(true)){
 var mesh=visible.GetComponent<MeshFilter>();if(!mesh)continue;
 var duplicate=hardware.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh==mesh.sharedMesh).OrderBy(r=>Vector3.Distance(r.bounds.center,visible.bounds.center)).FirstOrDefault();
 if(duplicate&&Vector3.Distance(duplicate.bounds.center,visible.bounds.center)<3f)hidden.Add(duplicate);
 }
 UnityEventTools.AddIntPersistentListener(click.onCompleted,sys.CompleteInstruction,n);row.interaction=click;Dirty(click);report.Add(n+": "+source.animationName+" / visual callbacks="+(click.onCompleted.GetPersistentEventCount()-1));
 Object.DestroyImmediate(prior.transform.parent.gameObject);
 }
 bind.resetCovers=reset.Distinct().ToArray();bind.powerOnIndicators=indicators.ToArray();bind.replacedVisuals=hidden.Distinct().ToArray();Dirty(bind);Dirty(board);
 EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);File.WriteAllLines("Temp/RE009Planning/corrected-controls.txt",report);
 }
}
