using System;
using System.Linq;
using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static partial class HybridGuidedCompletion
{
 [MenuItem("RE009/Complete Dump Load Mains Links")]
 public static void DumpMains(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);if(owner.steps.Any(s=>s.interaction&&s.interaction.name.Contains("Dump Load Mains")))return;
 var steps=owner.transform.Find("Steps");var module=At("Equipment/Wind Lab/Wind-Solar-Hybrid sys");var added=new List<StoryboardSteps.Step>();
 foreach(bool black in new[]{false,true}){
  string color=black?"Black":"Red";var prototype=owner.steps.Select(s=>s.interaction).OfType<WireInteraction>().First(w=>w.name=="Main Board "+color+" 1 to Hybrid Mains "+color);
  var group=Object.Instantiate(prototype.gameObject,steps.Find("Step 1"),true);group.name="Hybrid Mains "+color+" Output to Dump Load Mains "+color;var wire=group.GetComponent<WireInteraction>();
  var start=module.Find(black?"Cylinder440 (14)":"Cylinder439 (14)");
  var approximate=new Vector3(-122.17f,black?153.96f:154.88f,-52.37f);var terminal=All<Renderer>().Where(r=>Path(r.transform).StartsWith("Equipment/Wind Lab/Bananas (Modules)/")).OrderBy(r=>Vector3.Distance(r.transform.position,approximate)).First();if(Vector3.Distance(terminal.transform.position,approximate)>.15f)throw new Exception("Cannot identify dump mains terminal "+color);
  wire.pointA.name="Hybrid Mains "+color+" Output";wire.pointB.name="Dump Load Mains "+color;wire.pointA.transform.SetPositionAndRotation(start.position,start.rotation);wire.pointB.transform.SetPositionAndRotation(terminal.transform.position,terminal.transform.rotation);wire.pointA.Allow=wire.pointB.Allow=false;wire.pointA.onClicked=new UnityEvent();wire.pointB.onClicked=new UnityEvent();wire.onCompleted=new UnityEvent();wire.onUndone=new UnityEvent();wire.onFirstPointClicked=new UnityEvent();Route(wire.onFirstPointClicked,8);
  wire.markerA.transform.position=start.position;wire.markerB.transform.position=terminal.transform.position;wire.markerA.SetActive(false);wire.markerB.SetActive(false);
  wire.wireObject=WindExp4Step1Setup.Wire(steps.Find("Wires"),wire.name,wire.pointA,wire.pointB,wire.plugA.transform,wire.plugB.transform,black,new[]{prototype},color,"Assets/_Project/Models/Generated/Hybrid Wires");wire.wireObject.SetActive(false);wire.plugA.SetActive(false);wire.plugB.SetActive(false);
  var row=new StoryboardSteps.Step{interaction=wire,uiObject=owner.steps[0].uiObject,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent()};Route(row.onStepStart,17);added.Add(row);
 }
 int next=owner.steps.FindIndex(s=>s.uiObject&&s.uiObject.name=="Subtitle Step 2");owner.steps.InsertRange(next,added);
 var system=owner.GetComponentInChildren<HybridTrainingController>(true);system.mainWiring.guided=owner.steps.Where(s=>s.uiObject&&s.uiObject.name=="Subtitle Step 1").Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
 var bindings=owner.GetComponent<HybridGuidedBindings>();var so=new SerializedObject(system.windSpeed);so.FindProperty("speedStepIndex").intValue=owner.steps.FindIndex(s=>s.interaction==bindings.speedWait);so.ApplyModifiedPropertiesWithoutUndo();var enable=system.monitor.GetComponentInChildren<HybridEnableSwitch>(true);enable.instructionIndex=owner.steps.FindIndex(s=>s.interaction==enable.instruction);
 Dirty(owner);Dirty(system);Dirty(system.windSpeed);Dirty(enable);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
 }
}
