using System;
using System.IO;
using System.Linq;
using System.Text;
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
 public const string Display="Experiment 1: OFF Grid Inverter";
 public static T[] All<T>() where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()).ToArray();
 public static string Path(Transform t)=>HybridExperimentSetup.PathOf(t);
 public static void Ref(Object target,string key,Object value){var so=new SerializedObject(target);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();Dirty(target);}
 public static void Dirty(Object target){EditorUtility.SetDirty(target);PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
 public static void CopyHybridSelectionFields(StoryboardSteps from,StoryboardSteps to){CopyObjectFields(from,to,"storyboardID","selectedExperimentObject","startFromIntroObjects","guidedOnlyObjects");}
 [MenuItem("RE009/Reload Saved Staging After Incomplete Assembly")]
 public static void ReloadStaging(){if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var b=All<StoryboardSteps>().Single(x=>x.StoryboardID==HybridExperimentSetup.Id);if(b.steps.Count!=32)throw new Exception("Only valid before final assembly has been saved.");EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hybrid Lab.unity");}
 [MenuItem("RE009/Inspect Guided Reuse")]
 public static void Inspect(){var o=new StringBuilder();
 foreach(var c in All<SmoothCameraMovement>()){o.AppendLine("CAMERA "+Path(c.transform));var a=new SerializedObject(c).FindProperty("cameraPositions");for(int i=0;i<a.arraySize;i++){var t=a.GetArrayElementAtIndex(i).objectReferenceValue as Transform;o.AppendLine("POSE "+i+" "+(t?Path(t)+" "+t.position+" "+t.eulerAngles:"null"));}}
 foreach(var r in All<Renderer>()){var p=Path(r.transform);if(p.StartsWith("Equipment/Wind Lab/")&&(p.Contains("UNUSED (Solar)")||p.Contains("Solar_wind_hybrid-CORELS")||p.Contains("/INVERTER/")||p.Contains("/male/")))o.AppendLine("MODEL "+p+" pivot="+r.transform.position+" euler="+r.transform.eulerAngles+" bounds="+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name)));}
 foreach(var s in All<SingleClickInteraction>().Where(s=>Path(s.transform).StartsWith("Experiments/Exp 4/exp 4/Steps/")||Path(s.transform).Contains("Step Objects/Step 4")))o.AppendLine("SINGLE "+Path(s.transform)+" point="+(s.interaction?Path(s.interaction.transform):"null")+" animator="+(s.animator?Path(s.animator.transform):"null")+" parent="+s.transform.parent.position+" scale="+s.transform.parent.lossyScale);
 foreach(var g in All<PVGraphController>()){o.AppendLine("GRAPH "+Path(g.transform));var so=new SerializedObject(g);foreach(string f in new[]{"owningStoryboard","lineChartSpawnParent","plottedXValuesText","plottedYValuesText","plottedCurrentValuesText","exporter"}){var r=so.FindProperty(f).objectReferenceValue as Component;o.AppendLine("GRAPHREF "+f+" "+(r?Path(r.transform):"null"));}}
 foreach(var c in All<SliderRangeChecker>())o.AppendLine("CHECKER "+Path(c.transform)+" wind="+new SerializedObject(c).FindProperty("windSpeedControllers").arraySize);
 foreach(var t in All<Transform>().Where(t=>t.name.ToLowerInvariant().Contains("notification")&&!t.name.StartsWith("Notification")))o.AppendLine("NOTE "+Path(t));
 foreach(var q in All<MultipleChoiceQuiz>()){o.AppendLine("QUIZ "+Path(q.transform)+" "+EditorJsonUtility.ToJson(q));if(q.questions.Count>0)foreach(var t in q.questions[0].questionObject.GetComponentsInChildren<TMP_Text>(true))o.AppendLine("QTEXT "+Path(t.transform)+" "+t.text);}
 foreach(var t in All<TMP_Text>().Where(t=>Path(t.transform).Contains("List Content/Experiment 1:")||Path(t.transform).Contains("/Notifications/exp 4")))o.AppendLine("UITEXT "+Path(t.transform)+" "+t.text);
 foreach(var s in All<StepListPopulator>())o.AppendLine("LIST "+Path(s.transform));
 Directory.CreateDirectory("Temp/RE009Planning");File.WriteAllText("Temp/RE009Planning/guided-reuse.txt",o.ToString());
 }
}
