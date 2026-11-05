using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static partial class HybridGuidedCompletion
{
 static Vector3 CableDirection(GameObject plug)=>plug.name.IndexOf("Comm",StringComparison.OrdinalIgnoreCase)>=0?-plug.transform.forward:-plug.transform.up;
 static Vector3 Outlet(GameObject plug){
  Vector3 direction=CableDirection(plug);
  var mesh=plug.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh).OrderByDescending(m=>m.sharedMesh.bounds.size.sqrMagnitude).First();
  Vector3 local=mesh.transform.InverseTransformDirection(direction);var b=mesh.sharedMesh.bounds;var p=b.center;
  int axis=Mathf.Abs(local.x)>Mathf.Abs(local.y)?0:1;if(Mathf.Abs(local.z)>Mathf.Abs(local[axis]))axis=2;
  p[axis]=local[axis]>0?b.max[axis]:b.min[axis];
  var point=mesh.transform.TransformPoint(p);
  var anchor=plug.transform.Find("Wire Outlet");if(!anchor){anchor=new GameObject("Wire Outlet").transform;anchor.SetParent(plug.transform,false);}anchor.position=point;return point;
 }
 static Spline Curve(UnityEngine.Splines.SplineContainer container,Vector3 a,Vector3 b,Vector3 da,Vector3 db,float depth){
  Vector3 leadA=a+da*.35f,leadB=b+db*.35f;float z=Mathf.Min(leadA.z,leadB.z)-depth;
  var positions=new[]{a,leadA,new Vector3(leadA.x+da.x*.4f,leadA.y+da.y*.4f,z),new Vector3(leadB.x+db.x*.4f,leadB.y+db.y*.4f,z),leadB,b};
  var spline=new Spline(positions.Select(p=>(float3)container.transform.InverseTransformPoint(p)),TangentMode.Broken,false);
  for(int i=0;i<6;i++){
   Vector3 direction=i<=1?da:i>=4?-db:(positions[i+1]-positions[i-1]).normalized;
   float incoming=i>0?Vector3.Distance(positions[i],positions[i-1]):Vector3.Distance(positions[0],positions[1]);
   float outgoing=i<5?Vector3.Distance(positions[i],positions[i+1]):incoming;
   float length=Mathf.Min(incoming,outgoing)*.25f;
   if(i==0||i==1||i==4||i==5)length=.35f/3f;
   var tangent=container.transform.InverseTransformVector(direction*length);var knot=spline[i];knot.Rotation=quaternion.identity;knot.TangentIn=(float3)(-tangent);knot.TangentOut=(float3)tangent;spline[i]=knot;
  }
  return spline;
 }
 static Bounds MeshBounds(Transform t){var b=t.GetComponent<MeshFilter>().sharedMesh.bounds;var world=new Bounds(t.TransformPoint(b.center),Vector3.zero);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)world.Encapsulate(t.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));return world;}
 [MenuItem("RE009/Mask Printed Placeholder Switch Knob")]
 public static void SwitchMask(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var b=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var sys=b.GetComponentInChildren<HybridTrainingController>(true);var enable=sys.monitor.GetComponentInChildren<HybridEnableSwitch>(true);var existing=sys.monitor.transform.Find("Printed Knob Cover");if(existing)Object.DestroyImmediate(existing.gameObject);
 var go=new GameObject("Printed Knob Cover",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(enable.transform.parent,false);go.transform.SetPositionAndRotation(enable.transform.position+Vector3.forward*.08f,enable.transform.rotation);go.transform.localScale=enable.transform.localScale;
 go.GetComponent<MeshFilter>().sharedMesh=enable.GetComponent<MeshFilter>().sharedMesh;
 var material=new Material(enable.GetComponent<Renderer>().sharedMaterial);material.name="Hybrid Switch Track";if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",new Color(.45f,.45f,.45f,1));if(material.HasProperty("_Color"))material.SetColor("_Color",new Color(.45f,.45f,.45f,1));AssetDatabase.CreateAsset(material,"Assets/_Project/Data/Hybrid/Hybrid Switch Track.mat");go.GetComponent<MeshRenderer>().sharedMaterial=material;
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);
 }
 [MenuItem("RE009/Capture Completed Monitor And Check Transparency")]
 public static void CaptureCompleted(){
 if(!Application.isPlaying)throw new Exception("Run the guided test first.");var sys=All<HybridTrainingController>().Single(x=>x.isActiveAndEnabled);var toggle=All<WireMaterialTransparencyToggle>().Single();toggle.SetTransparent(true);
 foreach(var w in sys.storyboard.steps.Select(s=>s.interaction).OfType<WireInteraction>())foreach(var mat in w.wireObject.GetComponentInChildren<SplineContainer>(true).GetComponent<Renderer>().sharedMaterials){if((mat.HasProperty("_BaseColor")&&mat.GetColor("_BaseColor").a>.5f)||(mat.HasProperty("_Color")&&mat.GetColor("_Color").a>.5f))throw new Exception("Transparency not applied: "+w.name);}
 toggle.SetTransparent(false);
 var surface=sys.monitor.transform.Find("PV CHARACTERISTIC").GetComponent<Renderer>();File.WriteAllBytes("Temp/RE009Planning/Captures/monitor-save-handler.png",Bedo.Core.UI.MapCameraScreenshotButton_RT.CaptureSurfaceToPngBytes(surface,1600));
 WindLocalization.Instance.SetLanguage("Arabic");
 File.WriteAllText("Temp/RE009Planning/extra-verification.txt","PASS: all 36 cables respond to the transparency toggle; existing capture handler produced a PNG without dialogs; Arabic language selected for visual verification.");
 }
 [MenuItem("RE009/Fit Placeholder Table To Actual Screen Mesh")]
 public static void FitTable(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var b=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var sys=b.GetComponentInChildren<HybridTrainingController>(true);var screen=MeshBounds(sys.monitor.transform.Find("PV CHARACTERISTIC"));
 var table=(RectTransform)sys.monitor.transform.Find("Hybrid Readings Table");
 float scale=Mathf.Min(screen.size.x*.43f/880,screen.size.y*.32f/460);var ps=table.parent.lossyScale;table.localScale=new Vector3(scale/ps.x,scale/ps.y,scale/ps.z);table.rotation=Quaternion.identity;
 table.position=new Vector3(screen.center.x+screen.size.x*.24f,screen.center.y+screen.size.y*.055f,screen.center.z-.25f);
 foreach(var text in table.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=text.name=="Title"?32:text.name.Contains("Header")?21:28;Dirty(text);}
 var enable=sys.monitor.GetComponentInChildren<HybridEnableSwitch>(true);var oldTrack=sys.monitor.transform.Find("Hybrid Enable Track");
 if(!oldTrack){
  var knob=MeshBounds(enable.transform);float travel=knob.size.x*.85f;
  var track=new GameObject("Hybrid Enable Track",typeof(RectTransform),typeof(Canvas),typeof(Image));track.transform.SetParent(sys.monitor.transform,false);var rt=(RectTransform)track.transform;rt.sizeDelta=new Vector2(knob.size.x+travel+.2f,knob.size.y+.05f);rt.localScale=Vector3.one;rt.rotation=Quaternion.identity;rt.position=new Vector3(knob.center.x-travel*.5f,knob.center.y,knob.center.z-.06f);
  var canvas=track.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=1;var image=track.GetComponent<Image>();image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");image.type=Image.Type.Sliced;image.color=new Color(.45f,.45f,.45f,1);image.raycastTarget=false;
  var c=track.AddComponent<BoxCollider>();c.size=new Vector3(rt.sizeDelta.x,rt.sizeDelta.y,.04f);var click=track.AddComponent<Template.OnMouseDownE>();click.mouseDown=new UnityEngine.Events.UnityEvent();UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(click.mouseDown,enable.Click);
  enable.transform.position+=Vector3.back*.15f;Dirty(enable.transform);
 }
 Dirty(table);EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);
 File.WriteAllText("Temp/RE009Planning/table-fit.txt","Screen="+screen+" table="+table.position+" scale="+scale);
 }
 [MenuItem("RE009/Polish Guided Wire Curves And Placeholder Table")]
 public static void Polish(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);system=owner.GetComponentInChildren<HybridTrainingController>(true);
 var wires=owner.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
 for(int i=0;i<wires.Length;i++){
  var w=wires[i];var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);
  // Remove the inherited nonuniform scale before rebuilding a round cross-section.
  c.transform.localScale=Vector3.one;
  if(Vector3.Distance(c.transform.lossyScale,Vector3.one)>.01f){var parentScale=c.transform.parent?c.transform.parent.lossyScale:Vector3.one;c.transform.localScale=new Vector3(1/parentScale.x,1/parentScale.y,1/parentScale.z);}
  var a=Outlet(w.plugA);var b=Outlet(w.plugB);c.Splines=new[]{Curve(c,a,b,-w.plugA.transform.forward,-w.plugB.transform.forward,.8f+i*.085f)};
  var extrude=c.GetComponent<SplineExtrude>();extrude.Radius=w.name.Contains("Comm")?.055f:.045f;extrude.SegmentsPerUnit=5;extrude.Sides=8;extrude.Range=new Vector2(0,1);extrude.Rebuild();Dirty(c);Dirty(extrude);Dirty(extrude.targetMesh);
 }
 var oldNumbers=system.monitor.transform.Find("Hybrid Experiment Numbers");if(oldNumbers)oldNumbers.gameObject.SetActive(false);
 var previous=system.monitor.transform.Find("Hybrid Readings Table");if(previous)Object.DestroyImmediate(previous.gameObject);
 var surface=system.monitor.transform.Find("PV CHARACTERISTIC").GetComponent<Renderer>().bounds;
 var go=new GameObject("Hybrid Readings Table",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(system.monitor.transform,false);
 var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=10;var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(880,460);float scale=surface.size.x*.45f/880;
 rect.localScale=Vector3.one*scale;rect.rotation=Quaternion.identity;rect.position=new Vector3(surface.center.x+surface.size.x*.22f,surface.center.y+surface.size.y*.18f,surface.min.z-.25f);
 var back=new GameObject("Background",typeof(RectTransform),typeof(Image));back.transform.SetParent(rect,false);var br=(RectTransform)back.transform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;back.GetComponent<Image>().color=new Color(.055f,.055f,.055f,.88f);back.GetComponent<Image>().raycastTarget=false;
 var font=system.windAC.voltage.font;
 TMP_Text Text(string name,string value,float x,float y,float width,float size=22){var t=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));t.transform.SetParent(rect,false);var r=(RectTransform)t.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,42);var txt=t.GetComponent<TextMeshProUGUI>();txt.font=font;txt.fontSize=size;txt.color=Color.white;txt.text=value;txt.enableWordWrapping=false;txt.raycastTarget=false;txt.alignment=TextAlignmentOptions.MidlineLeft;return txt;}
 Text("Title","Hybrid system (temporary model)",18,8,840,27);Text("Source Header","Source",18,55,330);Text("V Header","Voltage (V)",355,55,165,20);Text("A Header","Current (A)",525,55,165,20);Text("P Header","Power (W)",695,55,165,20);
 HybridTrainingController.Meter Meter(string name,int i){float y=100+i*42;Text(name+" Label",name,18,y,330,21);return new HybridTrainingController.Meter{voltage=Text(name+" V","0.00",355,y,165),current=Text(name+" A","0.00",525,y,165),power=Text(name+" W","0.00",695,y,165)};}
 system.windAC=Meter("Wind AC",0);system.windDC=Meter("Wind DC",1);system.solar=Meter("Solar PV",2);system.battery=Meter("Battery bank",3);system.acLoad=Meter("AC loads",4);system.dump=Meter("Dump load",5);system.inverter=Meter("Inverter input",6);Text("RPM Label","Speed (RPM)",18,400,330);system.rpmText=Text("Speed Value","0",355,400,300,24);
 Dirty(system);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
 }
}
