using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static partial class HybridGuidedCompletion
{
 static void ConfigurePopups(){
 var oldQuiz=All<MultipleChoiceQuiz>().First(q=>Path(q.transform).Contains("/Middle Panel/Exp 1/")&&q.questions.Count>0);
 var popupRoot=new GameObject("Hybrid Guided Popups",typeof(RectTransform));popupRoot.transform.SetParent(oldQuiz.transform.parent.parent,false);popups=popupRoot.transform;
 var oldNotification=All<Transform>().First(t=>t.name=="exp 4 notifications"||t.name=="exp4 notifications");
 var noteGroup=new GameObject("Hybrid Notifications",typeof(RectTransform));noteGroup.transform.SetParent(oldNotification.parent,false);
 var from=oldNotification as RectTransform;var to=noteGroup.transform as RectTransform;to.anchorMin=from.anchorMin;to.anchorMax=from.anchorMax;to.pivot=from.pivot;to.sizeDelta=from.sizeDelta;to.anchoredPosition=from.anchoredPosition;to.localScale=from.localScale;
 if(oldNotification.GetComponent<VerticalLayoutGroup>()){var group=noteGroup.AddComponent<VerticalLayoutGroup>();EditorUtility.CopySerialized(oldNotification.GetComponent<VerticalLayoutGroup>(),group);}
 bindings.notesGroup=noteGroup;
 var notificationTemplate=oldNotification.Cast<Transform>().First(t=>t.name.StartsWith("Notification"));
 foreach(var note in definition.notes){
  var go=Object.Instantiate(notificationTemplate.gameObject,noteGroup.transform,false);go.name="Notification After Step "+note.afterInstruction;
  var texts=go.GetComponentsInChildren<TMP_Text>(true);var body=texts.OrderByDescending(t=>t.text.Length).First();body.text=note.english;
  foreach(var t in texts.Where(t=>t!=body&&t.name==body.name))t.text=note.english;
  foreach(var button in go.GetComponentsInChildren<Button>(true)){button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddBoolPersistentListener(button.onClick,go.SetActive,false);}
  go.SetActive(false);
  // Notifications belong to their instruction, without a separate guided step.
  rows[note.afterInstruction].Last().notesObject=go;
 }
 foreach(var data in definition.quizzes){
  var go=Object.Instantiate(oldQuiz.gameObject,popups,false);go.name="Quiz After Step "+data.afterInstruction;
  var quiz=go.GetComponent<MultipleChoiceQuiz>();quiz.questions=quiz.questions.Take(1).ToList();var q=quiz.questions[0];
  var question=q.questionObject.GetComponentsInChildren<TMP_Text>(true).First(t=>Path(t.transform).EndsWith("Body/ScrollView/Viewport/Content"));question.text=data.english;
  for(int i=0;i<q.answers.Count;i++){
   var a=q.answers[i];var choice=a.button.transform.parent;
   if(i>=3){choice.gameObject.SetActive(false);continue;}
   a.correct=i==data.correctAnswerIndex;
   var label=choice.GetComponentsInChildren<TMP_Text>(true).First(t=>Path(t.transform).Contains("ButtonOnText"));label.text=data.englishAnswers[i];
   var visuals=choice.GetComponentsInChildren<Transform>(true);
   if(!a.correctHighlight)a.correctHighlight=visuals.First(t=>t.name.ToLowerInvariant().Contains("correct")&&!t.name.ToLowerInvariant().Contains("incorrect")).gameObject;
   if(!a.incorrectHighlight)a.incorrectHighlight=visuals.First(t=>t.name.ToLowerInvariant().Contains("incorrect")).gameObject;
   a.correctHighlight.SetActive(false);a.incorrectHighlight.SetActive(false);
  }
  q.answers=q.answers.Take(3).ToList();q.correctFeedbackObject=null;q.incorrectFeedbackObject=null;q.incorrectFeedbackObject2=null;q.onCorrectAnswer=new UnityEvent();q.onIncorrectAnswer=new UnityEvent();
  var wait=Wait(data.afterInstruction,"Quiz Completion "+data.afterInstruction);UnityEventTools.AddVoidPersistentListener(q.onCorrectAnswer,wait.ForceComplete);
  var row=new StoryboardSteps.Step{stepType=StepType.Interaction,interaction=wait,notesObject=go,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent()};rows[data.afterInstruction].Add(row);go.SetActive(false);Dirty(quiz);
 }
 var list=All<StepListPopulator>().Single(t=>Path(t.transform).Contains("/Steps/Exp 1/"));list.steps=definition.instructions.Select(i=>i.english).ToList();list.popups=definition.notes.Select(n=>new StepListPopulator.PopupData{afterStep=n.afterInstruction,text=n.english}).ToList();Dirty(list);
 var panel=list.transform;while(panel.parent&&panel.parent.name!="Steps")panel=panel.parent;
 foreach(Transform sibling in panel.parent)if(sibling.name.StartsWith("Exp "))sibling.gameObject.SetActive(sibling==panel);
 // Keep the original close/layout controls, but use the finished text for panel 25.
 var finish=ui.Find("Subtitle Step 25");foreach(var text in finish.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Subtitle"))text.text=definition.instructions[24].english;
 }
 static void ConfigureGraph(){
 var source=old.GetComponentsInChildren<PVGraphController>(true).First(g=>new SerializedObject(g).FindProperty("owningStoryboard").objectReferenceValue==old);
 var go=new GameObject("Hybrid Graph Controller");go.transform.SetParent(owner.transform,false);var graph=go.AddComponent<PVGraphController>();EditorUtility.CopySerialized(source,graph);
 Ref(graph,"lineChart",null);Ref(graph,"owningStoryboard",owner);Ref(graph,"hybridController",system);Ref(graph,"guidedDataBinder",null);Ref(graph,"freeWindController",null);Ref(graph,"freeModeDataBinder",null);Ref(graph,"dataReader",null);
 var exporter=bindings.graphPage.GetComponent<PVExperimentExporter>();Ref(graph,"exporter",exporter);var exportSO=new SerializedObject(exporter);exportSO.FindProperty("windExport").boolValue=true;exportSO.FindProperty("freeWindExport").boolValue=false;exportSO.ApplyModifiedPropertiesWithoutUndo();
 var graphSO=new SerializedObject(graph);graphSO.FindProperty("xAxisLabel").stringValue="Speed (RPM)";graphSO.FindProperty("yAxisLabel").stringValue="DC voltage (V)";graphSO.ApplyModifiedPropertiesWithoutUndo();
 // In this template the graph display is shared outside the storyboards. Reject
 // hidden legacy display references rather than leaving a graph under an inactive experiment.
 foreach(string field in new[]{"lineChartSpawnParent","plottedXValuesText","plottedYValuesText","plottedCurrentValuesText"}){
  var reference=new SerializedObject(graph).FindProperty(field).objectReferenceValue as Component;
  if(reference&&reference.transform.IsChildOf(old.transform))throw new Exception("Graph reference still inside old experiment: "+field+" "+Path(reference.transform));
 }
 system.graph=graph;
 var sourceButton=Source("Graph Button");var button=Object.Instantiate(sourceButton.gameObject,steps,true);button.name="Graph Button";var single=button.GetComponent<SingleClickInteraction>();single.IsNextStep=false;single.onCompleted=new UnityEvent();single.onUndone=new UnityEvent();if(single.marker){single.marker=null;}bindings.graphButton=single.interaction;
 var tabs=bindings.tabs;var so=new SerializedObject(tabs);var entries=so.FindProperty("experiments");entries.arraySize=1;var entry=entries.GetArrayElementAtIndex(0);entry.FindPropertyRelative("storyboard").objectReferenceValue=owner;entry.FindPropertyRelative("graphButton").objectReferenceValue=single.interaction;entry.FindPropertyRelative("firstGraphInteraction").objectReferenceValue=null;
 var selection=button.transform.Find("Selection Tab");entry.FindPropertyRelative("selectionTab").objectReferenceValue=selection?selection.gameObject:null;so.ApplyModifiedPropertiesWithoutUndo();
 var click=button.GetComponent<OnMouseDownEvent>();Ref(click,"windMonitorTabs",tabs);var clickSO=new SerializedObject(click);clickSO.FindProperty("systemTab").boolValue=false;clickSO.ApplyModifiedPropertiesWithoutUndo();Dirty(tabs);Dirty(graph);Dirty(exporter);
 }
 static Transform Card(Transform button){while(button.parent&&!button.parent.GetComponent<HorizontalLayoutGroup>())button=button.parent;return button;}
 static void CardText(Transform card){
 card.name=Display;card.gameObject.SetActive(true);
 foreach(var text in card.GetComponentsInChildren<TMP_Text>(true)){
  string path=Path(text.transform);
  if(path.EndsWith("/Content/Texts/Title"))text.text="Experiment 01";
  else if(path.EndsWith("/Content/Texts/Description"))text.text="OFF Grid Inverter";
  else if(path.Contains("Scroll View Objectives/")&&text.name=="Description")text.text=string.Join("\n",definition.objectives.Select(o=>o.english));
  else if(text.text.Contains("Welcome!")||text.text.Contains("In this experiment,"))text.text="Welcome! In this experiment, you will study the function of an off-grid inverter when connected to various power sources such as solar panels, wind turbines, and batteries. You will observe how it converts DC power into AC to supply household loads in a standalone hybrid energy system.";
  Dirty(text);
 }
 Dirty(card.gameObject);
 }
 [MenuItem("RE009/Register Guided Experiment In Both Menus")]
 public static void Menus(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 definition=JsonUtility.FromJson<Definition>(File.ReadAllText("Assets/_Project/Data/Hybrid/RE009Storyboard.json"));
 if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/_Project/Scenes/Hybrid Lab.unity")EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hybrid Lab.unity");
 owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
 if(owner.steps.Count<45)throw new Exception("Complete the sequence first.");
 var manager=All<InteractionManager>().Single();var selector=All<ExperimentSelector>().Single();ArrayRefs(selector,"experiments",new[]{owner});
 var starts=All<Button>().Where(b=>Card(b.transform).name.StartsWith("Experiment ")&&b.transform.name=="Start Underline Button").ToArray();
 var first=starts.First(b=>Card(b.transform).name.StartsWith("Experiment 1:"));foreach(var b in starts)Card(b.transform).gameObject.SetActive(b==first);CardText(Card(first.transform));
 first.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddStringPersistentListener(first.onClick,manager.SetExperimentName,Display);UnityEventTools.AddObjectPersistentListener(first.onClick,selector.SelectExperiment,owner);Dirty(first);
 var managerSO=new SerializedObject(manager);managerSO.FindProperty("fixedWindowTitle").stringValue="Renewable Energy Hybrid Training System (RE-009)";managerSO.ApplyModifiedPropertiesWithoutUndo();Dirty(manager);
 foreach(var title in All<TMP_Text>().Where(t=>t.text=="Wind Energy Training System"||t.text=="Wind Energy Training System (RE-007)")){title.text="Renewable Energy Hybrid Training System (RE-009)";Dirty(title);}
 EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
 EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");
 var launchers=All<MainMenuExperimentLauncher>();var launcher=launchers.First(l=>new SerializedObject(l).FindProperty("experimentName").stringValue.StartsWith("Experiment 1:"));
 foreach(var l in launchers)Card(l.transform).gameObject.SetActive(l==launcher);CardText(Card(launcher.transform));launcher.Configure(HybridExperimentSetup.Id,Display);Dirty(launcher);
 foreach(var m in All<InteractionManager>()){var ms=new SerializedObject(m);ms.FindProperty("fixedWindowTitle").stringValue="Renewable Energy Hybrid Training System (RE-009)";ms.ApplyModifiedPropertiesWithoutUndo();Dirty(m);}
 foreach(var title in All<TMP_Text>().Where(t=>t.text=="Wind Energy Training System"||t.text=="Wind Energy Training System (RE-007)")){title.text="Renewable Energy Hybrid Training System (RE-009)";Dirty(title);}
 EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hybrid Lab.unity");
 }
}
