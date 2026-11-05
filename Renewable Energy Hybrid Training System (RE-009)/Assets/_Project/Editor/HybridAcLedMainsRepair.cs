using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using TutorialFramework;
using Object=UnityEngine.Object;
public static partial class HybridGuidedCompletion
{
 [MenuItem("RE009/Audit AC LED Mains Terminals")]
 public static void AuditAcLedMains(){var log=new List<string>();foreach(var p in HybridGuidedCompletion.All<Interaction>().Where(p=>p.name.Contains("AC LED")||p.name.Contains("DC LED Mains")))log.Add(HybridGuidedCompletion.Path(p.transform)+" pos="+p.transform.position+" rot="+p.transform.eulerAngles);foreach(var r in HybridGuidedCompletion.All<Renderer>().Where(r=>HybridGuidedCompletion.Path(r.transform).StartsWith("Equipment/Wind Lab/Bananas (Modules)/")&&r.transform.position.y>184&&r.transform.position.y<187))log.Add("HARDWARE "+r.name+" "+r.transform.position+" "+string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name)));File.WriteAllLines("Logs/HybridReview/ac-led-mains-terminals.txt",log);}

 [MenuItem("RE009/Correct Step One To AC LED In Both Modes")]
 public static void CorrectAcLedMains(){
  if(Application.isPlaying)throw new Exception("Stop Play first.");EditorSceneManager.SaveOpenScenes();
  File.Copy("Assets/_Project/Scenes/PV Lab.unity","Logs/HybridReview/before-ac-led-mains.unity",true);
  var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var free=All<FreeLabWindController>().Single();
  var raw=All<Transform>().Single(t=>Path(t)=="Experiments/AC LED Lamp Module power Connectors");
  var targets=new Dictionary<string,Transform>{
   ["DC LED Mains Red Input"]=raw.Find("AC LED Red input"),["DC LED Mains Black Input"]=raw.Find("AC LED Lamp Module Input Black"),
   ["DC LED Mains Red Output"]=raw.Find(" AC LED Red output"),["DC LED Mains Black Output"]=raw.Find(" AC LED output black")};
  if(targets.Values.Any(t=>!t))throw new Exception("AC LED physical terminal missing");
  var changed=board.steps.Where(r=>r.uiObject&&r.uiObject.name=="Subtitle Step 1").Select(r=>r.interaction).OfType<WireInteraction>().Where(w=>w.name.Contains("DC LED")).ToArray();
  if(changed.Length!=4)throw new Exception("Expected exactly four DC LED mains wires");
  var sourceNames=changed.ToDictionary(w=>w,w=>w.name);
  foreach(var w in changed){
   foreach(var point in new[]{w.pointA,w.pointB})if(targets.TryGetValue(point.name,out var target)){
    point.transform.SetPositionAndRotation(target.position,target.rotation);point.name=point.name.Replace("DC LED","AC LED");
    var ep=point.GetComponent<FreeLabEndpoint>();if(ep)ep.SetEndpointId(point.name);
   }
   w.name=w.name.Replace("DC LED","AC LED");w.wireObject.name=w.name;
   foreach(var end in new[]{(w.pointA,w.plugA,w.pointB),(w.pointB,w.plugB,w.pointA)}){
    var normal=end.Item1.transform.forward;var direction=Vector3.ProjectOnPlane(end.Item3.transform.position-end.Item1.transform.position,normal).normalized;
    var current=Vector3.ProjectOnPlane(CableDirection(end.Item2),normal).normalized;
    if(direction.sqrMagnitude>.1f&&current.sqrMagnitude>.1f)end.Item2.transform.RotateAround(end.Item1.transform.position,normal,Vector3.SignedAngle(current,direction,normal));
   }
   var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var a=PhysicalPlugMouth(w.plugA);var b=PhysicalPlugMouth(w.plugB);var da=PhysicalPlugDirection(w.plugA);var db=PhysicalPlugDirection(w.plugB);
   var points=new[]{a,a+da*.22f,Vector3.Lerp(a,b,.3f)+Vector3.back*.8f,Vector3.Lerp(a,b,.7f)+Vector3.back*.8f,b+db*.22f,b};
   c.Splines=new[]{RoutedCurve(c,points,da,db)};var extrusion=c.GetComponent<SplineExtrude>();extrusion.Rebuild();Dirty(c);Dirty(extrusion.targetMesh);
   string asset=AssetDatabase.GetAssetPath(extrusion.targetMesh);if(asset.Contains("Hybrid EXP1 Template Wires")&&asset.Contains("DC LED"))AssetDatabase.RenameAsset(asset,w.name);
  }
  // Update the existing Free Mode sockets and templates, without adding endpoints or wires.
  foreach(var socket in free.sockets)if(targets.TryGetValue(socket.endpoint.name,out var target)){
   socket.endpoint.transform.SetPositionAndRotation(target.position,target.rotation);socket.endpoint.name=socket.endpoint.name.Replace("DC LED","AC LED");socket.endpoint.SetEndpointId(socket.endpoint.name);
  }
  foreach(var w in changed){var d=free.definitions.Single(x=>x.name==sourceNames[w]);d.name=w.name;d.wireParentTemplate.name=w.name;
   void CopyPlug(Interaction guidedPoint,GameObject guidedPlug,FreeLabEndpoint endpoint,GameObject plug){plug.transform.SetParent(endpoint.transform,false);plug.transform.localPosition=guidedPlug.transform.localPosition;plug.transform.localRotation=guidedPlug.transform.localRotation;plug.transform.localScale=guidedPlug.transform.localScale;}
   CopyPlug(w.pointA,w.plugA,d.endpointA,d.plugA);CopyPlug(w.pointB,w.plugB,d.endpointB,d.plugB);
   var a=w.wireObject.GetComponentInChildren<SplineContainer>(true);var b=d.wireParentTemplate.GetComponentInChildren<SplineContainer>(true);var route=new Spline(a.Spline);
   for(int n=0;n<route.Count;n++){var knot=route[n];var rotation=new Quaternion(knot.Rotation.value.x,knot.Rotation.value.y,knot.Rotation.value.z,knot.Rotation.value.w);knot.Position=(float3)b.transform.InverseTransformPoint(a.transform.TransformPoint((Vector3)knot.Position));knot.TangentIn=(float3)b.transform.InverseTransformVector(a.transform.TransformVector(rotation*(Vector3)knot.TangentIn));knot.TangentOut=(float3)b.transform.InverseTransformVector(a.transform.TransformVector(rotation*(Vector3)knot.TangentOut));knot.Rotation=quaternion.identity;route[n]=knot;}
   b.Splines=new[]{route};Dirty(b);
  }
  // These four user-supplied objects remain as references, not competing click targets.
  foreach(var t in targets.Values){var interaction=t.GetComponent<Interaction>();if(interaction)interaction.Allow=false;foreach(var collider in t.GetComponents<Collider>())collider.enabled=false;}
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
  File.WriteAllLines("Logs/HybridReview/ac-led-mains-correction.txt",board.steps.Take(12).Select(r=>r.interaction.name));
 }

 [MenuItem("RE009/Clear Only Corrected AC LED Mains Cable Access")]
 public static void ClearAcLedMainsAccess(){
  var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
  var changed=board.steps.Where(r=>r.uiObject&&r.uiObject.name=="Subtitle Step 1").Select(r=>r.interaction).OfType<WireInteraction>().Where(w=>w.name.Contains("AC LED Mains")).ToHashSet();
  if(changed.Count!=4)throw new Exception("Expected only four changed AC LED mains wires");
  ReviewHybridAccess(true,changed);
  var free=All<FreeLabWindController>().Single();
  foreach(var w in changed){var d=free.definitions.Single(x=>x.name==w.name);var a=w.wireObject.GetComponentInChildren<SplineContainer>(true);var b=d.wireParentTemplate.GetComponentInChildren<SplineContainer>(true);var route=new Spline(a.Spline);
   for(int n=0;n<route.Count;n++){var k=route[n];var q=new Quaternion(k.Rotation.value.x,k.Rotation.value.y,k.Rotation.value.z,k.Rotation.value.w);k.Position=(float3)b.transform.InverseTransformPoint(a.transform.TransformPoint((Vector3)k.Position));k.TangentIn=(float3)b.transform.InverseTransformVector(a.transform.TransformVector(q*(Vector3)k.TangentIn));k.TangentOut=(float3)b.transform.InverseTransformVector(a.transform.TransformVector(q*(Vector3)k.TangentOut));k.Rotation=quaternion.identity;route[n]=k;}
   b.Splines=new[]{route};Dirty(b);
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
 }

 static Vector3 PhysicalPlugMouth(GameObject plug){var f=plug.GetComponentInChildren<MeshFilter>(true);var b=f.sharedMesh.bounds;return f.transform.TransformPoint(new Vector3(b.center.x,0,b.min.z+.025f));}
 static Vector3 PhysicalPlugDirection(GameObject plug)=>plug.GetComponentInChildren<MeshFilter>(true).transform.TransformDirection(Vector3.back).normalized;
 [MenuItem("RE009/Audit Corrected Mains Plug Joints")]
 public static void AuditMainsJoints(){var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var log=new List<string>();foreach(var w in board.steps.Take(12).Select(r=>r.interaction).OfType<WireInteraction>()){
 var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var a=c.transform.TransformPoint((Vector3)c.Spline[0].Position);var b=c.transform.TransformPoint((Vector3)c.Spline[c.Spline.Count-1].Position);
 foreach(var plug in new[]{w.plugA,w.plugB}){var mouth=PhysicalPlugMouth(plug);log.Add(w.name+" / "+plug.name+" gap="+Mathf.Min(Vector3.Distance(a,mouth),Vector3.Distance(b,mouth))+" actual="+mouth+" direction="+PhysicalPlugDirection(plug)+" old direction="+CableDirection(plug));}}
 File.WriteAllLines("Logs/HybridReview/mains-joint-audit.txt",log);}

 [MenuItem("RE009/Repair Corrected Mains Plug Joints")]
 public static void RepairMainsJoints(){
  if(Application.isPlaying)throw new Exception("Stop Play first.");EditorSceneManager.SaveOpenScenes();File.Copy("Assets/_Project/Scenes/PV Lab.unity","Logs/HybridReview/before-mains-joints.unity",true);
  var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var free=All<FreeLabWindController>().Single();
  var changed=board.steps.Take(12).Select(r=>r.interaction).OfType<WireInteraction>().Where(w=>w.name.Contains("AC LED Mains")).ToArray();
  foreach(var w in changed){var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var spline=new Spline(c.Spline);
   void End(GameObject plug,bool first){var mouth=PhysicalPlugMouth(plug);var direction=PhysicalPlugDirection(plug);int end=first?0:spline.Count-1,lead=first?1:spline.Count-2,body=first?2:spline.Count-3;
    var a=spline[end];a.Position=(float3)c.transform.InverseTransformPoint(mouth);a.Rotation=quaternion.identity;var tangent=c.transform.InverseTransformVector(direction*(first?1:-1)*.14f/3f);a.TangentIn=(float3)(-tangent);a.TangentOut=(float3)tangent;spline.SetTangentMode(end,TangentMode.Broken);spline[end]=a;
    var b=spline[lead];var leadPoint=mouth+direction*.14f;b.Position=(float3)c.transform.InverseTransformPoint(leadPoint);b.Rotation=quaternion.identity;
    var towardBody=(c.transform.TransformPoint((Vector3)spline[body].Position)-leadPoint).normalized*.12f;
    b.TangentIn=(float3)c.transform.InverseTransformVector(first?-direction*.14f/3f:towardBody);b.TangentOut=(float3)c.transform.InverseTransformVector(first?towardBody:-direction*.14f/3f);spline.SetTangentMode(lead,TangentMode.Broken);spline[lead]=b;
   }
   End(w.plugA,true);End(w.plugB,false);c.Splines=new[]{spline};var ex=c.GetComponent<SplineExtrude>();ex.Rebuild();Dirty(c);Dirty(ex.targetMesh);
   var d=free.definitions.Single(x=>x.name==w.name);var target=d.wireParentTemplate.GetComponentInChildren<SplineContainer>(true);var copy=new Spline(c.Spline);
   for(int n=0;n<copy.Count;n++){var k=copy[n];var q=new Quaternion(k.Rotation.value.x,k.Rotation.value.y,k.Rotation.value.z,k.Rotation.value.w);k.Position=(float3)target.transform.InverseTransformPoint(c.transform.TransformPoint((Vector3)k.Position));k.TangentIn=(float3)target.transform.InverseTransformVector(c.transform.TransformVector(q*(Vector3)k.TangentIn));k.TangentOut=(float3)target.transform.InverseTransformVector(c.transform.TransformVector(q*(Vector3)k.TangentOut));k.Rotation=quaternion.identity;copy[n]=k;}
   target.Splines=new[]{copy};Dirty(target);
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);VerifyMainsJoints();
 }
 [MenuItem("RE009/Verify Corrected Mains Plug Joints")]
 public static void VerifyMainsJoints(){
  var log=new List<string>();var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var free=All<FreeLabWindController>().Single();
  foreach(var w in board.steps.Take(12).Select(r=>r.interaction).OfType<WireInteraction>().Where(w=>w.name.Contains("AC LED Mains"))){var d=free.definitions.Single(x=>x.name==w.name);
   foreach(var mode in new[]{(w.wireObject,w.plugA,w.plugB,"Guided"),(d.wireParentTemplate,d.plugA,d.plugB,"Free")}){var c=mode.Item1.GetComponentInChildren<SplineContainer>(true);var p=c.transform.TransformPoint((Vector3)c.Spline[0].Position);var q=c.transform.TransformPoint((Vector3)c.Spline[c.Spline.Count-1].Position);
    float a=Vector3.Distance(p,PhysicalPlugMouth(mode.Item2)),b=Vector3.Distance(q,PhysicalPlugMouth(mode.Item3));if(a>.003f||b>.003f)throw new Exception(mode.Item4+" outlet gap "+w.name+" "+a+"/"+b);
    var ta=c.transform.TransformVector((Vector3)SplineUtility.EvaluateTangent(c.Spline,0)).normalized;var tb=-c.transform.TransformVector((Vector3)SplineUtility.EvaluateTangent(c.Spline,1)).normalized;
    if(Vector3.Dot(ta,PhysicalPlugDirection(mode.Item2))<.999f||Vector3.Dot(tb,PhysicalPlugDirection(mode.Item3))<.999f)throw new Exception("Cable exits plug at wrong angle: "+w.name);
    log.Add("PASS "+mode.Item4+" "+w.name+" outlet gaps="+a+"/"+b);
   }
  }
  File.WriteAllLines("Logs/HybridReview/mains-joints-verified.txt",log);
 }
}
