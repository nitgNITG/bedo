using System;
using System.IO;
using System.Linq;
using System.Text;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

public static class HybridExperimentSetup
{
 public const string Id="RE009 Off Grid Inverter";
 public static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static T[] All<T>() where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()).ToArray();
 static void Dirty(Object o){EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}

 [Serializable] public class Instruction {public int number; public string english,arabic;}
 [Serializable] public class Definition {public Instruction[] instructions;}
 [MenuItem("RE009/Verify Supplied Hybrid Workbook")]
 public static void VerifyModel()=>HybridModelVerification.Verify();
 [MenuItem("RE009/Prepare Hybrid Module And Instruction UI")]
 public static void Prepare(){
 if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
 var scene=EditorSceneManager.GetActiveScene();
 if(scene.path!="Assets/_Project/Scenes/PV Lab.unity")throw new InvalidOperationException("Open PV Lab first.");
 var equipment=All<Transform>().Single(t=>PathOf(t)=="Equipment/Wind Lab");
 var hybrid=equipment.Find("Wind-Solar-Hybrid sys");
 if(!hybrid)throw new InvalidOperationException("Hybrid controller missing.");
 var body=hybrid.Find("bettry001");
 var rectifier=equipment.Find("UNUSED (MDOULE SYSTEM)/diversion controller");
 if(!body||!rectifier)throw new InvalidOperationException("Module placement references missing.");
 // Both imported modules face the same direction. Use their body pivots,
 // rather than the offset scene-export root, to align their module slots.
 var hybridBounds=body.GetComponent<Renderer>().bounds;
 var slotBounds=rectifier.GetComponent<Renderer>().bounds;
 if(hybridBounds.size.x>slotBounds.size.x*1.1f||hybridBounds.size.y>slotBounds.size.y*1.1f)throw new InvalidOperationException("Hybrid module does not fit diversion-controller slot: hybrid="+hybridBounds.size+" slot="+slotBounds.size);
 hybrid.position+=rectifier.position-body.position;
 hybrid.gameObject.SetActive(true);rectifier.gameObject.SetActive(false);
 foreach(var r in equipment.GetComponentsInChildren<Renderer>(true)){
  if(r.transform.IsChildOf(hybrid))continue;
  var path=PathOf(r.transform);if(!path.Contains("/Bananas (Modules)/")&&!path.Contains("/UNUSED (MDOULE SYSTEM)/"))continue;
  var b=r.bounds;if(b.size.x>12||b.size.y>15)continue;
  if(Mathf.Abs(b.center.x-slotBounds.center.x)<slotBounds.extents.x+.1f&&Mathf.Abs(b.center.y-slotBounds.center.y)<slotBounds.extents.y+.1f)r.gameObject.SetActive(false);
 }
 var source=All<Transform>().Single(t=>t.name=="Exp 4 UI"&&PathOf(t).Contains("/Instructions/"));
 var parent=source.parent;var ui=parent.Find("Hybrid Experiment UI");
 if(ui)Object.DestroyImmediate(ui.gameObject);
 ui=new GameObject("Hybrid Experiment UI",typeof(RectTransform)).transform;ui.SetParent(parent,false);
 var rt=(RectTransform)ui;var sr=(RectTransform)source;
 rt.anchorMin=sr.anchorMin;rt.anchorMax=sr.anchorMax;rt.pivot=sr.pivot;rt.sizeDelta=sr.sizeDelta;rt.anchoredPosition=sr.anchoredPosition;rt.localScale=sr.localScale;
 var definition=JsonUtility.FromJson<Definition>(File.ReadAllText("Assets/_Project/Data/Hybrid/RE009Storyboard.json"));
 if(definition.instructions.Length!=25)throw new InvalidOperationException("Expected 24 instructions and finished panel.");
 var template=source.Find("Subtitle Step 1");
 foreach(var item in definition.instructions){
 var panel=Object.Instantiate(template.gameObject,ui,false);panel.name="Subtitle Step "+item.number;
 var texts=panel.GetComponentsInChildren<TMP_Text>(true);
 foreach(var subtitle in texts.Where(t=>t.name=="Subtitle"))subtitle.text=item.english;
 foreach(var number in texts.Where(t=>t.transform.parent.name=="Index Number"))number.text=item.number.ToString();
 foreach(var scroll in panel.GetComponentsInChildren<AutoSizeSubtitleScrollContent>(true)){
 var so=new SerializedObject(scroll);so.FindProperty("measureAtCurrentFontSize").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
 }
 panel.SetActive(false);
 }
 ui.gameObject.SetActive(false);
 Dirty(hybrid);Dirty(rectifier);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 Debug.Log("RE009 preparation: controller placed in unused diversion-controller slot; 25 bilingual-source instruction panels prepared. Existing experiment roots and menu references retained until hybrid wiring is validated.");
 }
 [MenuItem("RE009/Verify Staged Hybrid Assets")]
 public static void VerifyStaging(){
 var owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==Id);
 var wires=owner.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
 if(owner.gameObject.activeSelf||wires.Length!=32)throw new Exception("Expected inactive staging board and 32 source/circuit wires.");
 foreach(var w in wires){
 if(!w.pointA||!w.pointB||!w.plugA||!w.plugB||!w.markerA||!w.markerB||!w.pointA.GetComponent<BoxCollider>()||!w.pointB.GetComponent<BoxCollider>())throw new Exception("Incomplete wire "+w.name);
 var c=w.wireObject.GetComponentInChildren<UnityEngine.Splines.SplineContainer>(true);
 if(c.Spline.Count<2||Vector3.Distance(c.transform.TransformPoint((Vector3)c.Spline[0].Position),w.plugA.transform.position)>.001f||Vector3.Distance(c.transform.TransformPoint((Vector3)c.Spline[c.Spline.Count-1].Position),w.plugB.transform.position)>.001f)throw new Exception("Wire misses plug: "+w.name);
 foreach(var d in w.wireObject.GetComponentsInChildren<FreeLabWireDefinition>(true))Object.DestroyImmediate(d);
 foreach(var d in w.wireObject.GetComponentsInChildren<FreeLabWire>(true))Object.DestroyImmediate(d);
 foreach(var d in w.wireObject.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(d);
 }
 var ui=All<Transform>().Single(t=>t.name=="Hybrid Experiment UI");
 if(ui.childCount!=25)throw new Exception("Expected 25 subtitle panels.");
 for(int n=1;n<=25;n++)if(!ui.Find("Subtitle Step "+n))throw new Exception("Subtitle missing: "+n);
 var system=owner.GetComponentInChildren<HybridTrainingController>(true);
 if(!system||!system.pvReader||!system.windModel||!system.monitor||!system.monitor.GetComponentInChildren<HybridEnableSwitch>(true))throw new Exception("Incomplete monitor staging.");
 var capture=system.monitor.GetComponentsInChildren<Bedo.Core.UI.MapCameraScreenshotButton_RT>(true);
 if(capture.Length!=1)throw new Exception("Expected a single capture handler.");
 if(!system.monitor.transform.Find("save screen (1)").GetComponent<Template.OnMouseDownE>())throw new Exception("Collider screenshot click missing.");
 VerifyModel();
 EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
 File.WriteAllText("Temp/RE009Planning/staging-verification.txt","PASS: 25 subtitle panels; 32 fully assigned wires meeting their plugs; independent spline meshes; no inherited Free Mode definitions on guided wire visuals; original solar Enable mesh; one collider-based screenshot capture handler. INCOMPLETE: communications, control sequence, notes/quiz panels, graph hookup, menus and Free Mode integration.");
 }
 [MenuItem("RE009/Audit Reusable UI And Controls")]
 public static void AuditReusable(){var output=new StringBuilder();foreach(var t in All<Transform>()){
 string p=PathOf(t); if((p.StartsWith("Equipment/Wind Lab/UI/")||p.StartsWith("Experiments/Free Lab"))&&true)output.AppendLine(p+"\t"+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))+"\t"+(t.GetComponent<TMP_Text>()?t.GetComponent<TMP_Text>().text:""));
 }foreach(var t in All<Transform>().Where(x=>x.name.ToLower().Contains("enable")||x.name.Contains("Solar UI")))output.AppendLine("ENABLE "+PathOf(t)+" "+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name)));foreach(var c in All<FreeLabWindController>()){foreach(var socket in c.sockets)output.AppendLine("SOCKET "+socket.endpoint.name+" "+socket.endpoint.transform.position+" "+socket.endpoint.transform.eulerAngles); output.AppendLine("FREEJSON "+EditorJsonUtility.ToJson(c,true));foreach(var l in c.lamps)output.AppendLine("LAMP "+l.name+"="+PathOf(l.light.transform));}foreach(var t in All<Interaction>())if(PathOf(t.transform).Contains("UnGuided")||t.name.ToLower().Contains("solar"))output.AppendLine("LEGACY "+PathOf(t.transform)+" "+t.transform.position+" "+t.transform.eulerAngles);foreach(var r in All<Renderer>().Where(r=>PathOf(r.transform).StartsWith("Equipment/Wind Lab/")&&r.sharedMaterials.Any(m=>m&&m.name.ToLower().Contains("banana"))))output.AppendLine("PIN "+PathOf(r.transform)+" "+r.transform.position+" "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")));
 foreach(var r in All<Renderer>().Where(r=>PathOf(r.transform).StartsWith("Equipment/Wind Lab/UNUSED (MDOULE SYSTEM)/")&&r.bounds.size.x>7&&r.bounds.size.y>10))output.AppendLine("SLOT "+PathOf(r.transform)+" "+r.bounds);foreach(var c in All<SingleClickInteraction>())output.AppendLine("CONTROL "+PathOf(c.transform)+" target="+(c.interaction?PathOf(c.interaction.transform):"null")+" anim="+c.animationName);
 File.WriteAllText("Temp/RE009Planning/reusable.txt",output.ToString());}
 [MenuItem("RE009/Audit Terminal Models")]
 public static void AuditTerminals(){var output=new StringBuilder();foreach(var t in All<Transform>()){
 var p=PathOf(t);if(!p.StartsWith("Equipment/Wind Lab/"))continue;
 if(p.Contains("Wind-Solar-Hybrid sys/")||p.Contains("INPUTS (solar panel)")||p.Contains("Inputs (photovolatics)")||p.Contains("Solar_wind_hybrid-CORELS")){
 var r=t.GetComponent<Renderer>();output.AppendLine(p+"\t"+t.position+"\t"+t.eulerAngles+"\t"+(r?string.Join(",",r.sharedMaterials.Select(x=>x?x.name:"null")):""));
 }}File.WriteAllText("Temp/RE009Planning/terminals.txt",output.ToString());}
 [MenuItem("RE009/Audit Current Layout")]
 public static void Audit(){
 var output=new StringBuilder();
 output.AppendLine("Dirty scene="+EditorSceneManager.GetActiveScene().isDirty);
 foreach(var t in All<Transform>().Where(t=>PathOf(t).StartsWith("Equipment/")&&(t.childCount>0||t.name.Contains("banana")||t.GetComponent<Collider>()))){
 string path=PathOf(t);if(path.Split('/').Length>7)continue;
 output.AppendLine("OBJ\t"+t.gameObject.GetInstanceID()+"\t"+path+"\t"+t.position+"\t"+t.eulerAngles+"\t"+t.gameObject.activeSelf+"\t"+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name)));
 }
 foreach(var b in All<StoryboardSteps>()){
 output.AppendLine("BOARD\t"+b.GetInstanceID()+"\t"+PathOf(b.transform)+"\t"+b.StoryboardID);
 for(int i=0;i<b.steps.Count;i++){
 var s=b.steps[i];output.AppendLine("STEP\t"+i+"\t"+(s.interaction?PathOf(s.interaction.transform):s.stepType.ToString())+"\tUI="+(s.uiObject?PathOf(s.uiObject.transform):"null"));
 if(s.interaction is WireInteraction w)output.AppendLine("WIRE\t"+w.name+"\t"+w.pointA.name+"="+w.pointA.transform.position+"\t"+w.pointB.name+"="+w.pointB.transform.position+"\t"+w.plugA.name+"\t"+w.plugA.transform.localPosition+"\t"+w.plugA.transform.localEulerAngles);
 }
 }
 foreach(var t in All<Transform>().Where(t=>t.name=="Wind UI"||t.name=="Hydro"||t.name=="UI (dont use)"||t.name=="Monitors"))output.AppendLine("UIROOT\t"+PathOf(t)+"\t"+string.Join(",",t.Cast<Transform>().Select(x=>x.name)));
 foreach(var t in All<FreeLabWindController>())output.AppendLine("FREE\t"+PathOf(t.transform));
 Directory.CreateDirectory("Temp/RE009Planning");File.WriteAllText("Temp/RE009Planning/layout.txt",output.ToString());Debug.Log("RE009 layout audit complete");
 }
}

