using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using TutorialFramework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.UI;
using Template;
using Bedo.Core.UI;
public static class HybridReviewFixes {
static T[] All<T>()where T:Component=>HybridGuidedCompletion.All<T>();
static string Path(Transform t)=>HybridGuidedCompletion.Path(t);
[MenuItem("RE009/Review Current Controls And Notifications")]
public static void Audit(){
var b=All<HybridTrainingController>().Single();var bindings=b.storyboard.GetComponent<HybridGuidedBindings>();var log=new StringBuilder();
var objects=All<Transform>().Where(t=>t.name=="System Button"||t.name=="Graph Button"||t.name=="Selection Tab"||t.name.ToLower().Contains("save screen")||t.name=="Clear"||t.name=="export data"||t.name=="Wind Mill Speed").ToArray();
foreach(var t in objects){log.AppendLine("\n"+Path(t)+" pos="+t.position+" local="+t.localPosition+" rot="+t.localEulerAngles+" scale="+t.localScale+" active="+t.gameObject.activeSelf);foreach(var c in t.GetComponents<Component>()){if(!c)continue;log.AppendLine(c.GetType().Name+" "+EditorJsonUtility.ToJson(c));}foreach(Transform child in t)log.AppendLine(" child "+child.name+" pos="+child.localPosition+" scale="+child.localScale);}
for(int i=0;i<b.storyboard.steps.Count;i++){var s=b.storyboard.steps[i];log.AppendLine("STEP "+i+" "+s.stepType+" "+(s.interaction?s.interaction.name:"-")+" UI="+(s.uiObject?s.uiObject.name:"-")+" notes="+(s.notesObject?s.notesObject.name:"-"));}
foreach(var t in All<TMP_Text>().Where(t=>t.text.IndexOf("Wind speed",StringComparison.OrdinalIgnoreCase)>=0))log.AppendLine("TEXT "+Path(t.transform)+" = "+t.text);
foreach(var c in bindings.graphPage.GetComponentsInChildren<Component>(true).Concat(b.monitor.GetComponentsInChildren<Component>(true))){if(c is Transform)continue;var so=new SerializedObject(c);var prop=so.GetIterator();while(prop.Next(true))if(prop.propertyType==SerializedPropertyType.ObjectReference)log.AppendLine("REF "+Path(c.transform)+" "+c.GetType().Name+" "+prop.propertyPath+" => "+(prop.objectReferenceValue is Component cc?Path(cc.transform):prop.objectReferenceValue?prop.objectReferenceValue.name:"NULL"));if(c is TMP_Text text)log.AppendLine("LABEL "+Path(c.transform)+" "+text.text+" font="+text.fontSize+" color="+text.color);}
File.WriteAllText("Logs/HybridReview/controls.txt",log.ToString());
}

static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);}
static void CopyTransform(Transform source,Transform target){target.localPosition=source.localPosition;target.localRotation=source.localRotation;target.localScale=source.localScale;}
[MenuItem("RE009/Restore Original Monitor Controls And Inline Notifications")]
public static void FixControls(){
if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var system=All<HybridTrainingController>().Single();var b=system.storyboard;var bind=b.GetComponent<HybridGuidedBindings>();
var st=bind.systemButton.transform;var gt=bind.graphButton.transform;gt.position=new Vector3(-16.03998f,st.position.y,st.position.z);gt.rotation=st.rotation;gt.localScale=Vector3.Scale(st.lossyScale,new Vector3(1/gt.parent.lossyScale.x,1/gt.parent.lossyScale.y,1/gt.parent.lossyScale.z));
var sysCol=st.GetComponent<BoxCollider>();var graphCol=gt.GetComponent<BoxCollider>();if(!graphCol)graphCol=gt.gameObject.AddComponent<BoxCollider>();EditorUtility.CopySerialized(sysCol,graphCol);graphCol.enabled=true;
var ss=st.Find("Selection Tab");var gs=gt.Find("Selection Tab");CopyTransform(ss,gs);gs.gameObject.SetActive(false);ss.gameObject.SetActive(true);
var image=gt.GetComponent<Image>();if(image)image.raycastTarget=false;var button=gt.GetComponent<Button>();if(button)button.enabled=false;
var exporter=bind.graphPage.GetComponent<PVExperimentExporter>();
foreach(var spec in new[]{("save screen","SaveGraphPNG"),("export data","SaveDataCSV"),("Clear","ClearRuntimeGraph")}){
 var t=bind.graphPage.transform.Find(spec.Item1);var click=t.GetComponent<OnMouseDownE>();click.mouseDown=new UnityEvent();
 if(spec.Item2=="SaveGraphPNG")UnityEventTools.AddVoidPersistentListener(click.mouseDown,exporter.SaveGraphPNG);
 if(spec.Item2=="SaveDataCSV")UnityEventTools.AddVoidPersistentListener(click.mouseDown,exporter.SaveDataCSV);
 // Clear is bound once at runtime by the active graph controller.
 var col=t.GetComponent<BoxCollider>();var size=col.size;size.z=.1f;col.size=size;col.enabled=true;
 foreach(var ui in t.GetComponentsInChildren<Button>(true))ui.enabled=false;
 var hover=t.GetComponent<HoverObjectToggle>();var h=new SerializedObject(hover).FindProperty("objectToToggle").objectReferenceValue as GameObject;if(!h)throw new Exception("Missing original hover object for "+t.name);h.SetActive(false);foreach(var c in h.GetComponentsInChildren<Collider>(true))c.enabled=false;
 EditorUtility.SetDirty(click);EditorUtility.SetDirty(col);EditorUtility.SetDirty(hover);
}
var gso=new SerializedObject(system.graph);var clears=gso.FindProperty("clearButtons");clears.arraySize=1;clears.GetArrayElementAtIndex(0).objectReferenceValue=bind.graphPage.transform.Find("Clear").GetComponent<OnMouseDownE>();gso.ApplyModifiedPropertiesWithoutUndo();
foreach(var name in new[]{"save screen (1)","save screen (2)"}){var t=bind.graphPage.transform.Find(name);if(t)t.gameObject.SetActive(false);}
foreach(var save in system.monitor.GetComponentsInChildren<MapCameraScreenshotButton_RT>(true)){
 var col=save.GetComponent<BoxCollider>();if(!col)continue;var c=save.GetComponent<OnMouseDownE>();if(!c)c=save.gameObject.AddComponent<OnMouseDownE>();c.mouseDown=new UnityEvent();UnityEventTools.AddVoidPersistentListener(c.mouseDown,save.SaveScreenshot);var z=col.size;z.z=.1f;col.size=z;foreach(var u in save.GetComponentsInChildren<Button>(true))u.enabled=false;EditorUtility.SetDirty(c);
}
int removed=0;for(int i=b.steps.Count-1;i>0;i--){var row=b.steps[i];var previous=b.steps[i-1];if(row.stepType!=StepType.Delay||!row.notesObject||!row.notesObject.name.StartsWith("Notification After Step ")||!row.uiObject||!previous.uiObject||row.uiObject.name!=previous.uiObject.name)continue;previous.notesObject=row.notesObject;b.steps.RemoveAt(i);removed++;}
// Observation instructions remain real storyboard steps; only notification-only duplicates go.
foreach(int n in new[]{16,23}){
 var row=b.steps.FirstOrDefault(s=>s.uiObject&&s.uiObject.name=="Subtitle Step "+n);
 if(row==null){var panel=bind.subtitleGroup.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Subtitle Step "+n).gameObject;row=new StoryboardSteps.Step{stepType=StepType.Delay,delayDuration=n==16?4:5,uiObject=panel,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent()};var movement=All<SmoothCameraMovement>().Single();UnityEventTools.AddIntPersistentListener(row.onStepStart,movement.MoveToPosition,12);UnityEventTools.AddVoidPersistentListener(row.onStepStart,bind.ShowSystem);int before=b.steps.FindIndex(s=>s.uiObject&&s.uiObject.name=="Subtitle Step "+(n+1));b.steps.Insert(before,row);}
}
foreach(var note in bind.notesGroup.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Notification After Step "))){int n=int.Parse(note.name.Replace("Notification After Step ",""));foreach(var r in b.steps.Where(r=>r.notesObject==note.gameObject))r.notesObject=null;var host=b.steps.Last(r=>r.uiObject&&r.uiObject.name=="Subtitle Step "+n);host.notesObject=note.gameObject;}
foreach(var row in b.steps.Where(r=>r.interaction&&r.interaction.name.StartsWith("Quiz Completion "))){string n=row.interaction.name.Replace("Quiz Completion ","");row.notesObject=All<MultipleChoiceQuiz>().Single(q=>q.name=="Quiz After Step "+n).gameObject;}
var enable=system.monitor.GetComponentInChildren<HybridEnableSwitch>(true);enable.instructionIndex=b.steps.FindIndex(s=>s.interaction==enable.instruction);EditorUtility.SetDirty(enable);var speedSO=new SerializedObject(system.windSpeed);speedSO.FindProperty("speedStepIndex").intValue=b.steps.FindIndex(s=>s.interaction==bind.speedWait);speedSO.ApplyModifiedPropertiesWithoutUndo();
foreach(var text in All<TMP_Text>().Where(t=>Path(t.transform).Contains("/Wind Speed Panel/")&&t.name=="Title")){text.text="Motor speed";EditorUtility.SetDirty(text);}
EditorUtility.SetDirty(b);EditorUtility.SetDirty(system.graph);EditorUtility.SetDirty(gt);EditorUtility.SetDirty(gs);EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllText("Logs/HybridReview/control-fix.txt","Restored graph/system transforms, selection strip, collider controls, single save/export callbacks, original hover objects; moved "+removed+" notifications onto instruction rows.");
}

[MenuItem("RE009/Inspect Runtime Graph Controls")]
public static void RuntimeControls(){var sys=All<HybridTrainingController>().Single();var bind=sys.storyboard.GetComponent<HybridGuidedBindings>();var log=new StringBuilder();foreach(var component in new Component[]{bind.tabs,bind.graphButton,bind.systemButton,bind.graphButton.GetComponent<OnMouseDownEvent>()}){var so=new SerializedObject(component);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference)log.AppendLine(component.GetType().Name+" "+p.propertyPath+" "+(p.objectReferenceValue is GameObject g?Path(g.transform):p.objectReferenceValue is Component c?Path(c.transform):p.objectReferenceValue?p.objectReferenceValue.name:"NULL"));}log.AppendLine("active="+sys.storyboard.gameObject.activeInHierarchy+" graph="+bind.graphPage.activeSelf+" monitors="+bind.monitors.activeSelf+" allow="+bind.graphButton.Allow+","+bind.systemButton.Allow);File.WriteAllText("Logs/HybridReview/runtime-controls.txt",log.ToString());}
[MenuItem("RE009/Show Hybrid Graph At Monitor Camera")]
public static void ShowGraph(){var s=All<HybridTrainingController>().Single(x=>x.isActiveAndEnabled);var bind=s.storyboard.GetComponent<HybridGuidedBindings>();bind.tabs.ShowSystem();bind.tabs.ShowGraph(bind.graphButton);All<SmoothCameraMovement>().Single().MoveToPosition(CameraPositionReorderUtility.HybridMonitorIndex());foreach(var row in s.storyboard.steps)if(row.uiObject)row.uiObject.SetActive(false);}

[MenuItem("RE009/Test Monitor Collider Hover And Capture")]
public static void TestButtons(){
if(!Application.isPlaying)throw new Exception("Test in Play Mode.");var sys=All<HybridTrainingController>().Single(s=>s.isActiveAndEnabled);var bind=sys.storyboard.GetComponent<HybridGuidedBindings>();var cam=All<SmoothCameraMovement>().Single().GetComponent<Camera>();var log=new StringBuilder();
foreach(var name in new[]{"save screen","export data","Clear"}){var t=bind.graphPage.transform.Find(name);var col=t.GetComponent<BoxCollider>();if(!col||!col.enabled||col.size.z<=0)throw new Exception("Missing usable collider "+name);var click=t.GetComponent<OnMouseDownE>();if(name!="Clear"&&(click.mouseDown.GetPersistentEventCount()!=1||!click.mouseDown.GetPersistentTarget(0)))throw new Exception("Save/export callback missing or duplicated "+name);var hover=t.GetComponent<HoverObjectToggle>();var target=(GameObject)new SerializedObject(hover).FindProperty("objectToToggle").objectReferenceValue;hover.SendMessage("OnMouseEnter");if(!target.activeInHierarchy)throw new Exception("Hover did not activate "+name);hover.SendMessage("OnMouseExit");if(target.activeSelf)throw new Exception("Hover did not clear "+name);
var v=col.bounds.center-cam.transform.position;if(!Physics.Raycast(cam.transform.position,v.normalized,out var hit,v.magnitude+1)||hit.collider!=col)throw new Exception("Button collider is obstructed "+name+" by "+(hit.collider?Path(hit.transform):"nothing"));log.AppendLine("PASS collider and individual hover: "+name);}
var graphSurface=(Renderer)new SerializedObject(bind.graphPage.GetComponent<PVExperimentExporter>()).FindProperty("windCaptureSurface").objectReferenceValue;var png=MapCameraScreenshotButton_RT.CaptureSurfaceToPngBytes(graphSurface,1200);if(png.Length<10000)throw new Exception("Graph capture empty");File.WriteAllBytes("Logs/HybridReview/verified-graph-save.png",png);var save=sys.monitor.GetComponentInChildren<MapCameraScreenshotButton_RT>(true);var surface=(Renderer)new SerializedObject(save).FindProperty("captureSurface").objectReferenceValue;png=MapCameraScreenshotButton_RT.CaptureSurfaceToPngBytes(surface,1200);if(png.Length<10000)throw new Exception("Monitor capture empty");File.WriteAllBytes("Logs/HybridReview/verified-monitor-save.png",png);log.AppendLine("PASS graph and system monitor PNG capture; save/export exactly one valid callback.");File.WriteAllText("Logs/HybridReview/button-tests.txt",log.ToString());
}
}
