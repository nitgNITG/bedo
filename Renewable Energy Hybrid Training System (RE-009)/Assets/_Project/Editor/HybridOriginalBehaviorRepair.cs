using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TutorialFramework;
using Bedo.Core.UI;
public static class HybridOriginalBehaviorRepair {
static T[] All<T>()where T:Component=>HybridGuidedCompletion.All<T>();static string P(Transform t)=>HybridGuidedCompletion.Path(t);
static void Dirty(UnityEngine.Object o)=>EditorUtility.SetDirty(o);
static void Ref(UnityEngine.Object o,string f,UnityEngine.Object value){var so=new SerializedObject(o);so.FindProperty(f).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();Dirty(o);}
static void EnabledEvent(UnityEvent e,Renderer r,bool value){var action=(UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>),r,typeof(Renderer).GetProperty("enabled").GetSetMethod());UnityEventTools.AddBoolPersistentListener(e,action,value);}
static void RemoveRendererEvents(UnityEvent e){for(int i=e.GetPersistentEventCount()-1;i>=0;i--)if(e.GetPersistentMethodName(i)=="set_enabled")UnityEventTools.RemovePersistentListener(e,i);}
static string Color(GameObject g)=>new[]{"Black","Red","Yellow","Blue"}.FirstOrDefault(c=>g.name.IndexOf(c,StringComparison.OrdinalIgnoreCase)>=0)??"Comm";
class End {public Interaction pin;public GameObject plug;public WireInteraction wire;public Vector3 localExit,localDirection;}
static End[] Sources(StoryboardSteps board){return All<WireInteraction>().Where(w=>w.GetComponentInParent<StoryboardSteps>()!=board&&(P(w.transform).StartsWith("Experiments/Exp 2/")||P(w.transform).StartsWith("Experiments/Exp 3/")||P(w.transform).StartsWith("Experiments/Exp 4/"))&&w.pointA&&w.pointB&&w.plugA&&w.plugB&&w.wireObject).SelectMany(w=>new[]{(w.pointA,w.plugA),(w.pointB,w.plugB)}.Select(pair=>{
var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);if(!c||c.Spline.Count<2)return null;var a=c.transform.TransformPoint((Vector3)c.Spline[0].Position);var b=c.transform.TransformPoint((Vector3)c.Spline[c.Spline.Count-1].Position);bool start=Vector3.Distance(a,pair.Item2.transform.position)<Vector3.Distance(b,pair.Item2.transform.position);var exit=start?a:b;var neighbor=c.transform.TransformPoint((Vector3)c.Spline[start?1:c.Spline.Count-2].Position);return new End{pin=pair.Item1,plug=pair.Item2,wire=w,localExit=pair.Item2.transform.InverseTransformPoint(exit),localDirection=pair.Item2.transform.InverseTransformDirection((neighbor-exit).normalized)};
})).Where(e=>e!=null).ToArray();}
static void CameraArgument(UnityEvent e,SmoothCameraMovement camera,int index){// Retain non-camera callbacks on the actual row.

for(int i=0;i<e.GetPersistentEventCount();i++)if(e.GetPersistentTarget(i)==camera&&e.GetPersistentMethodName(i)=="MoveToPosition"){UnityEventTools.RegisterIntPersistentListener(e,i,camera.MoveToPosition,index);return;}UnityEventTools.AddIntPersistentListener(e,camera.MoveToPosition,index);}
static int ReadCamera(SerializedProperty e,SmoothCameraMovement camera){var calls=e.FindPropertyRelative("m_PersistentCalls.m_Calls");for(int i=0;i<calls.arraySize;i++){var c=calls.GetArrayElementAtIndex(i);if(c.FindPropertyRelative("m_Target").objectReferenceValue==camera)return c.FindPropertyRelative("m_Arguments.m_IntArgument").intValue;}return 0;}
[MenuItem("RE009/Restore Source Switches Save Hover And Empty Graph")]
public static void Controls(){
if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var sys=All<HybridTrainingController>().Single();var board=sys.storyboard;var bind=board.GetComponent<HybridGuidedBindings>();
var graphSave=bind.graphPage.transform.Find("save screen");var sourceHover=graphSave.GetComponent<HoverObjectToggle>();var sourceHighlight=(GameObject)new SerializedObject(sourceHover).FindProperty("objectToToggle").objectReferenceValue;
foreach(var save in sys.monitor.GetComponentsInChildren<MapCameraScreenshotButton_RT>(true)){
if(!save.GetComponent<BoxCollider>())continue;var hover=save.GetComponent<HoverObjectToggle>();if(!hover)hover=save.gameObject.AddComponent<HoverObjectToggle>();var existing=save.transform.Find("Hover Highlight");if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);var highlight=UnityEngine.Object.Instantiate(sourceHighlight,save.transform,false);highlight.name="Hover Highlight";highlight.transform.localPosition=graphSave.InverseTransformPoint(sourceHighlight.transform.position);highlight.transform.localRotation=Quaternion.Inverse(graphSave.rotation)*sourceHighlight.transform.rotation;highlight.transform.localScale=Vector3.Scale(sourceHighlight.transform.lossyScale,new Vector3(1/graphSave.lossyScale.x,1/graphSave.lossyScale.y,1/graphSave.lossyScale.z));foreach(var col in highlight.GetComponentsInChildren<Collider>(true))col.enabled=false;highlight.SetActive(false);Ref(hover,"objectToToggle",highlight);
}
var glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Inputs (photovolatics)-Material #275600.mat");if(!glow)throw new Exception("The authored glowing green material is missing.");
var reference=All<StoryboardSteps>().Single(b=>P(b.transform).StartsWith("Experiments/Wind References/"));var mainReference=reference.GetComponentsInChildren<SingleClickInteraction>(true).Single(c=>c.name=="on_button004 (1)"&&P(c.transform).Contains("/Steps/"));
var covers=bind.resetCovers.Where(r=>r).ToList();var hidden=bind.replacedVisuals.Where(r=>r).ToList();
foreach(var name in new[]{"Main Board ON","PV ON","Wind ON"}){
var control=board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Single(s=>s.name==name);var frame=control.transform.parent;
foreach(var extra in frame.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("Green ON Indicator")||t.name=="PV OFF Pressed Cover").ToArray()){covers.RemoveAll(r=>r&&r.transform.IsChildOf(extra));hidden.RemoveAll(r=>r&&r.transform.IsChildOf(extra));UnityEngine.Object.DestroyImmediate(extra.gameObject);}
RemoveRendererEvents(control.onCompleted);
Renderer front;
if(name=="Wind ON")front=frame.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="green led_on Pressed Cover");else front=control.GetComponent<Renderer>();
if(name=="Main Board ON")front.sharedMaterials=mainReference.GetComponent<Renderer>().sharedMaterials;
var duplicates=front.GetComponentsInChildren<Renderer>(true).Where(r=>r!=front&&!r.name.Contains("Fake Glow")).ToArray();foreach(var d in duplicates)if(d.name.Contains(" ON")||d.name=="Glowing Green Copy")UnityEngine.Object.DestroyImmediate(d.gameObject);
var go=new GameObject("Glowing Green Copy",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(front.transform,false);go.GetComponent<MeshFilter>().sharedMesh=front.GetComponent<MeshFilter>().sharedMesh;go.GetComponent<MeshRenderer>().sharedMaterial=glow;
// Use the working main-board copy's depth convention: a small recess, never a lateral offset.
var meshBounds=front.GetComponent<MeshFilter>().sharedMesh.bounds;var size=meshBounds.size;int axis=size.x<size.y?0:1;if(size.z<size[axis])axis=2;var local=Vector3.zero;local[axis]=1;var normal=front.transform.TransformDirection(local).normalized;var row=board.steps.Single(s=>s.interaction==control);var cam=All<SmoothCameraMovement>().Single();int rowIndex=board.steps.IndexOf(row);var srow=new SerializedObject(board).FindProperty("steps").GetArrayElementAtIndex(rowIndex);int cameraIndex=ReadCamera(srow.FindPropertyRelative("onStepStart"),cam);var pose=(Transform)new SerializedObject(cam).FindProperty("cameraPositions").GetArrayElementAtIndex(cameraIndex).objectReferenceValue;if(Vector3.Dot(normal,front.bounds.center-pose.position)<0)normal=-normal;go.transform.position=front.transform.position+normal*.025f;
front.enabled=true;if(!covers.Contains(front))covers.Add(front);EnabledEvent(control.onCompleted,front,false);
// Copy the reference OFF mesh into the guided frame; the shared hardware OFF object
// is disabled by mode setup and cannot supply the guided red half reliably.
if(name!="Wind ON"){
Renderer sourceOff=name=="Main Board ON"?mainReference.transform.parent.Find("Off_button004 (1)").GetComponent<Renderer>():All<Renderer>().Single(r=>P(r.transform)=="Equipment/Wind Lab/INPUTS (solar panel)/Off_button001");
var oldRed=frame.Find("Red OFF Half");if(oldRed)UnityEngine.Object.DestroyImmediate(oldRed.gameObject);
var red=new GameObject("Red OFF Half",typeof(MeshFilter),typeof(MeshRenderer));red.transform.SetParent(frame,false);red.transform.SetPositionAndRotation(sourceOff.transform.position,sourceOff.transform.rotation);var parentScale=frame.lossyScale;red.transform.localScale=Vector3.Scale(sourceOff.transform.lossyScale,new Vector3(1/parentScale.x,1/parentScale.y,1/parentScale.z));red.GetComponent<MeshFilter>().sharedMesh=sourceOff.GetComponent<MeshFilter>().sharedMesh;red.GetComponent<Renderer>().sharedMaterials=sourceOff.sharedMaterials;red.SetActive(true);
}
// Physical red OFF halves stay visible. Hide only duplicate green covers owned by this guided copy.
foreach(var physical in All<Renderer>().Where(r=>P(r.transform).StartsWith("Equipment/Wind Lab/")&&(r.name.StartsWith("Off_button")||r.name.ToLower().Contains("red led")))){hidden.Remove(physical);physical.enabled=true;Dirty(physical);}
if(name=="Wind ON"){var physical=All<Transform>().Single(t=>P(t)=="Equipment/Wind Lab/Outputs_WInd/green led_on");foreach(var r in physical.GetComponentsInChildren<Renderer>(true))if(!hidden.Contains(r))hidden.Add(r);}
Dirty(control);Dirty(front);
}
bind.resetCovers=covers.Where(r=>r).Distinct().ToArray();bind.replacedVisuals=hidden.Where(r=>r).Distinct().ToArray();bind.powerOnIndicators=Array.Empty<Renderer>();sys.recordGraphMeasurements=false;
var graphSO=new SerializedObject(sys.graph);graphSO.FindProperty("lineChartPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Graphs/Wind EXP1 Runtime.prefab");graphSO.ApplyModifiedPropertiesWithoutUndo();
var canvas=bind.graphPage.transform.Find("EXP1 Wind Graph UI");bind.graphReadouts=new[]{"RPM Readings","DC Voltage Readings","DC Current Readings","Voltage Graph Tab","AC-Speed Graph Tab"}.Select(n=>canvas.Find(n).gameObject).ToArray();
var old=canvas.Find("Unused Reading Area Background");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
var first=board.steps[0];if(first.onStepStart==null)first.onStepStart=new UnityEvent();bool initialized=false;for(int i=first.onStepStart.GetPersistentEventCount()-1;i>=0;i--)if(first.onStepStart.GetPersistentMethodName(i)=="Initialize"){if(first.onStepStart.GetPersistentTarget(i)==bind)initialized=true;else if(!first.onStepStart.GetPersistentTarget(i))UnityEventTools.RemovePersistentListener(first.onStepStart,i);}if(!initialized)UnityEventTools.AddVoidPersistentListener(first.onStepStart,bind.Initialize);Dirty(board);
Dirty(bind);Dirty(sys);EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);AssetDatabase.SaveAssets();
}
[MenuItem("RE009/Copy Original Plug Alignment And Alternate Wire Clicks")]
public static void PlugsAndOrder(){
if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var sys=All<HybridTrainingController>().Single();var board=sys.storyboard;var sources=Sources(board);var camera=All<SmoothCameraMovement>().Single();var logs=new List<string>();var prepared=new List<(StoryboardSteps.Step row,WireInteraction w,int start,int end)>();
var bso=new SerializedObject(board);for(int i=0;i<board.steps.Count;i++)if(board.steps[i].interaction is WireInteraction w)prepared.Add((board.steps[i],w,ReadCamera(bso.FindProperty("steps").GetArrayElementAtIndex(i).FindPropertyRelative("onStepStart"),camera),ReadCamera(new SerializedObject(w).FindProperty("onFirstPointClicked"),camera)));
foreach(var item in prepared){var w=item.w;var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var endpoints=new List<(Vector3 p,Vector3 d)>();
foreach(var e in new[]{(w.pointA,w.plugA),(w.pointB,w.plugB)}){
var mesh=e.Item2.GetComponentsInChildren<MeshFilter>(true).First(f=>f.sharedMesh).sharedMesh;var source=sources.Where(s=>Color(s.plug)==Color(e.Item2)&&s.plug.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==mesh)).OrderBy(s=>Vector3.Distance(s.pin.transform.position,e.Item1.transform.position)+Quaternion.Angle(s.pin.transform.rotation,e.Item1.transform.rotation)*.02f+Vector3.Distance(s.pin.transform.position,s.plug.transform.position)*.2f).First();
e.Item2.transform.rotation=e.Item1.transform.rotation*Quaternion.Inverse(source.pin.transform.rotation)*source.plug.transform.rotation;
endpoints.Add((e.Item2.transform.TransformPoint(source.localExit),e.Item2.transform.TransformDirection(source.localDirection).normalized));Dirty(e.Item2.transform);
}
var spline=new Spline(c.Spline);var first=c.transform.TransformPoint((Vector3)spline[0].Position);bool startsA=Vector3.Distance(first,endpoints[0].p)<Vector3.Distance(first,endpoints[1].p);var start=endpoints[startsA?0:1];var end=endpoints[startsA?1:0];
foreach(var v in new[]{(0,start.p,start.d),(1,start.p+start.d*.22f,start.d),(spline.Count-2,end.p+end.d*.22f,-end.d),(spline.Count-1,end.p,-end.d)}){var k=spline[v.Item1];k.Position=(float3)c.transform.InverseTransformPoint(v.Item2);k.Rotation=quaternion.identity;var tangent=c.transform.InverseTransformVector(v.Item3*(.22f/3));k.TangentIn=(float3)(-tangent);k.TangentOut=(float3)tangent;spline[v.Item1]=k;}
c.Splines=new[]{spline};c.GetComponent<SplineExtrude>().Rebuild();Dirty(c);Dirty(c.GetComponent<SplineExtrude>().targetMesh);
}
// This scene has never had alternating click order applied. A saved marker prevents a second swap.
var bind=board.GetComponent<HybridGuidedBindings>();if(!bind.alternatingWireClicksApplied){string group="";int index=0;foreach(var item in prepared){string next=item.row.uiObject?item.row.uiObject.name:"";if(next!=group){group=next;index=0;}var w=item.w;if(index++%2==1){(w.pointA,w.pointB)=(w.pointB,w.pointA);(w.plugA,w.plugB)=(w.plugB,w.plugA);(w.markerA,w.markerB)=(w.markerB,w.markerA);CameraArgument(item.row.onStepStart,camera,item.end);CameraArgument(w.onFirstPointClicked,camera,item.start);Dirty(w);}logs.Add(group+": "+w.pointA.name+" -> "+w.pointB.name);}bind.alternatingWireClicksApplied=true;Dirty(bind);}
Dirty(board);EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines("Logs/HybridReview/alternating-order.txt",logs);
}

[MenuItem("RE009/Verify Original Plug Alignment And Alternating Cameras")]
public static void VerifyAlignment(){var sys=All<HybridTrainingController>().Single();var sources=Sources(sys.storyboard);int count=0;var result=new List<string>();foreach(var w in sys.storyboard.steps.Select(s=>s.interaction).OfType<WireInteraction>()){
var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var first=c.transform.TransformPoint((Vector3)c.Spline[0].Position);var last=c.transform.TransformPoint((Vector3)c.Spline[c.Spline.Count-1].Position);
foreach(var e in new[]{(w.pointA,w.plugA),(w.pointB,w.plugB)}){var mesh=e.Item2.GetComponentsInChildren<MeshFilter>(true).First(f=>f.sharedMesh).sharedMesh;var source=sources.Where(s=>Color(s.plug)==Color(e.Item2)&&s.plug.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==mesh)).OrderBy(s=>Vector3.Distance(s.pin.transform.position,e.Item1.transform.position)+Quaternion.Angle(s.pin.transform.rotation,e.Item1.transform.rotation)*.02f+Vector3.Distance(s.pin.transform.position,s.plug.transform.position)*.2f).First();var rotation=e.Item1.transform.rotation*Quaternion.Inverse(source.pin.transform.rotation)*source.plug.transform.rotation;var exit=e.Item2.transform.TransformPoint(source.localExit);if(Quaternion.Angle(rotation,e.Item2.transform.rotation)>.02f)throw new Exception("Plug rotated away from original: "+e.Item1.name);if(Mathf.Min(Vector3.Distance(exit,first),Vector3.Distance(exit,last))>.005f)throw new Exception("Cable does not meet authored plug exit: "+e.Item1.name);count++;}
}result.Add("PASS: "+count+" plug rotations match EXP2-4 templates and cable endpoints meet the original authored exit positions within 0.005 world units.");File.WriteAllLines("Logs/HybridReview/plug-verification.txt",result);}
[MenuItem("RE009/Test System Save Hover And Layered Switches")]
public static void TestVisuals(){if(!Application.isPlaying)throw new Exception("Run guided sequence first.");var sys=All<HybridTrainingController>().Single(s=>s.isActiveAndEnabled);var bind=sys.storyboard.GetComponent<HybridGuidedBindings>();var log=new List<string>();
foreach(var name in new[]{"Main Board ON","PV ON","Wind ON"}){var click=sys.storyboard.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Single(s=>s.name==name);var front=name=="Wind ON"?click.transform.parent.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="green led_on Pressed Cover"):click.GetComponent<Renderer>();var glow=front.transform.Find("Glowing Green Copy");if(!glow||!glow.gameObject.activeInHierarchy||!glow.GetComponent<Renderer>().enabled||front.enabled)throw new Exception("Power layers incorrect after click: "+name);if(AssetDatabase.GetAssetPath(glow.GetComponent<Renderer>().sharedMaterial)!="Assets/_Project/Materials/Inputs (photovolatics)-Material #275600.mat")throw new Exception("Wrong authored glow material "+name);log.Add("PASS clicked switch has hidden front and glowing recessed copy: "+name);}
foreach(var name in new[]{"Main Board ON","PV ON"}){var control=sys.storyboard.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Single(c=>c.name==name);var red=control.transform.parent.Find("Red OFF Half");if(!red||!red.gameObject.activeInHierarchy||!red.GetComponent<Renderer>().enabled)throw new Exception("Red OFF half hidden: "+name);}
log.Add("PASS original red OFF halves remain visible.");
bind.tabs.ShowSystem();foreach(var save in sys.monitor.GetComponentsInChildren<MapCameraScreenshotButton_RT>(true)){if(!save.GetComponent<BoxCollider>())continue;var hover=save.GetComponent<HoverObjectToggle>();if(!hover)throw new Exception("System Save hover component missing");var target=(GameObject)new SerializedObject(hover).FindProperty("objectToToggle").objectReferenceValue;hover.SendMessage("OnMouseEnter");if(!target.activeInHierarchy||!target.transform.IsChildOf(save.transform))throw new Exception("System Save hover is not its own visible highlight");hover.SendMessage("OnMouseExit");if(target.activeSelf)throw new Exception("System Save hover did not turn off");log.Add("PASS System Save owns and toggles its copied original hover object.");}
bind.tabs.ShowGraph(bind.graphButton);foreach(var r in bind.graphReadouts)if(r.activeInHierarchy!=sys.recordGraphMeasurements)throw new Exception("Graph readout visibility does not match recording mode");var chart=bind.graphPage.GetComponentsInChildren<XCharts.Runtime.LineChart>(true).Single();if(!sys.recordGraphMeasurements&&chart.series.Any(s=>s.dataCount>0))throw new Exception("Unrequested measurements plotted");log.Add(sys.recordGraphMeasurements?"PASS original chart and synchronized live readouts are enabled.":"PASS original chart is empty without extra readouts.");All<SmoothCameraMovement>().Single().MoveToPosition(CameraPositionReorderUtility.HybridMonitorIndex());foreach(var row in sys.storyboard.steps)if(row.uiObject)row.uiObject.SetActive(false);File.WriteAllLines("Logs/HybridReview/original-behavior-tests.txt",log);
}
}
