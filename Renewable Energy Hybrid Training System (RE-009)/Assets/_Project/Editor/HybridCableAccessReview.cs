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
public static partial class HybridGuidedCompletion {
struct RequiredPinView {public Vector3 eye,target;public int row;public string name;}
[MenuItem("RE009/Check And Clear Hybrid Cable Access")]
public static void ClearHybridAccess(){
ReviewHybridAccess(true);
}
[MenuItem("RE009/Audit Hybrid Cable Access Without Changes")]
public static void AuditHybridAccess(){ReviewHybridAccess(false);}
static void ReviewHybridAccess(bool apply,HashSet<WireInteraction> editable=null){
if(Application.isPlaying)throw new Exception("Stop Play Mode first.");owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
var movement=All<SmoothCameraMovement>().Single();var poses=new SerializedObject(movement).FindProperty("cameraPositions");var so=new SerializedObject(owner);var rows=so.FindProperty("steps");
int ReadCamera(SerializedProperty e){var calls=e.FindPropertyRelative("m_PersistentCalls.m_Calls");for(int i=0;i<calls.arraySize;i++){var x=calls.GetArrayElementAtIndex(i);if(x.FindPropertyRelative("m_Target").objectReferenceValue==movement)return x.FindPropertyRelative("m_Arguments.m_IntArgument").intValue;}return 0;}
Vector3 Eye(int index)=>((Transform)poses.GetArrayElementAtIndex(index).objectReferenceValue).position;
var views=new List<RequiredPinView>();var wires=new List<(int row,WireInteraction wire)>();
for(int i=0;i<owner.steps.Count;i++)if(owner.steps[i].interaction is WireInteraction w){wires.Add((i,w));var first=ReadCamera(rows.GetArrayElementAtIndex(i).FindPropertyRelative("onStepStart"));var calls=new SerializedObject(w).FindProperty("onFirstPointClicked");var second=ReadCamera(calls);views.Add(new RequiredPinView{eye=Eye(first),target=w.pointA.transform.position,row=i,name=w.pointA.name});views.Add(new RequiredPinView{eye=Eye(second),target=w.pointB.transform.position,row=i,name=w.pointB.name});}
var temporary=new GameObject("Temporary Cable Geometry Audit");temporary.hideFlags=HideFlags.HideAndDontSave;
var hardware=All<MeshRenderer>().Where(r=>Path(r.transform).StartsWith("Equipment/")&&r.enabled&&r.gameObject.activeInHierarchy&&!Path(r.transform).Contains("/UI/")&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh).ToArray();
try{
foreach(var r in hardware){var g=new GameObject("Audit "+r.name);g.hideFlags=HideFlags.HideAndDontSave;g.layer=31;g.transform.SetParent(temporary.transform,false);g.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);g.transform.localScale=r.transform.lossyScale;g.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;}
Physics.SyncTransforms();var accepted=new Dictionary<Vector3Int,List<Vector3>>();var report=new List<string>();int changes=0;
// Scoped repairs must avoid later cables too, while leaving those routes unchanged.
if(editable!=null)foreach(var item in wires.Where(i=>!editable.Contains(i.wire))){var container=item.wire.wireObject.GetComponentInChildren<SplineContainer>(true);foreach(var point in Sample(container,container.Spline)){var cell=Cell(point);if(!accepted.TryGetValue(cell,out var bucket))accepted[cell]=bucket=new List<Vector3>();bucket.Add(point);}}
foreach(var item in wires){var w=item.wire;if(editable!=null&&!editable.Contains(w)){report.Add("UNCHANGED "+w.name);continue;}var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var a=c.transform.TransformPoint((Vector3)c.Spline[0].Position);var b=c.transform.TransformPoint((Vector3)c.Spline[c.Spline.Count-1].Position);var da=(c.transform.TransformPoint((Vector3)c.Spline[1].Position)-a).normalized;var db=(c.transform.TransformPoint((Vector3)c.Spline[c.Spline.Count-2].Position)-b).normalized;
float Score(Vector3[] points,out string reason){float score=0;reason="";foreach(var p in points){if(Vector3.Distance(p,a)<.85f||Vector3.Distance(p,b)<.85f)continue;
if(Physics.CheckSphere(p,.09f,1<<31,QueryTriggerInteraction.Ignore)){score+=100;reason="hardware intersection";}
foreach(var v in views){if(v.row<item.row||Vector3.Distance(v.target,w.pointA.transform.position)<.2f||Vector3.Distance(v.target,w.pointB.transform.position)<.2f)continue;var ray=v.target-v.eye;float t=Vector3.Dot(p-v.eye,ray)/ray.sqrMagnitude;if(t<=0||t>=1.015f)continue;float distance=Vector3.Distance(p,v.eye+ray*t);if(distance<.6f*t){score+=10;reason="banana approach: "+v.name;}}
var cell=Cell(p);bool crossing=false;for(int x=-1;x<=1&&!crossing;x++)for(int y=-1;y<=1&&!crossing;y++)for(int z=-1;z<=1&&!crossing;z++)if(accepted.TryGetValue(cell+new Vector3Int(x,y,z),out var bucket))foreach(var q in bucket)if(Vector3.Distance(p,q)<.14f){score+=2;reason="cable crossing";crossing=true;break;}
}return score;}
var original=new Spline(c.Spline);var originalPoints=Sample(c,original);float originalScore=Score(originalPoints,out var originalReason);Spline best=original;var bestPoints=originalPoints;float bestScore=originalScore;
if(apply&&originalScore>0&&(editable==null||editable.Contains(w))){var chord=b-a;var side=Vector3.Cross(chord,Vector3.back).normalized;float len=chord.magnitude;
for(int attempt=0;attempt<160&&bestScore>0;attempt++){
float depth=.35f+(attempt/20)*.38f;float lateral=(attempt%10==0?0:((attempt%10+1)/2)*(attempt%2==0?1:-1)*.24f);float sag=attempt%20<10?0:Mathf.Min(1,len*.03f);
var p=new[]{a,a+da*.3f,Vector3.Lerp(a,b,.3f)+Vector3.back*depth+side*lateral+Vector3.down*sag,Vector3.Lerp(a,b,.7f)+Vector3.back*depth+side*lateral+Vector3.down*sag,b+db*.3f,b};
var candidate=RoutedCurve(c,p,da,db);var points=Sample(c,candidate);var score=Score(points,out _);if(score<bestScore){best=candidate;bestScore=score;bestPoints=points;}
}
}
if(apply&&bestScore>0&&(editable==null||editable.Contains(w))){var side=Vector3.Cross(b-a,Vector3.back).normalized;
for(int attempt=0;attempt<768&&bestScore>0;attempt++){
float fraction=attempt%3==0?.06f:attempt%3==1?.12f:.2f;float depthA=.25f+(attempt/3%8)*.3f;float depthB=.25f+(attempt/24%8)*.3f;float lateral=(attempt/192%4-1.5f)*.5f;bool opposite=attempt%2==0;
var p=new[]{a,a+da*.22f,Vector3.Lerp(a,b,fraction)+Vector3.back*depthA+side*lateral,Vector3.Lerp(a,b,1-fraction)+Vector3.back*depthB+side*lateral*(opposite?-1:1),b+db*.22f,b};
var candidate=RoutedCurve(c,p,da,db);var points=Sample(c,candidate);var score=Score(points,out _);if(score<bestScore){best=candidate;bestScore=score;bestPoints=points;}
}
}
if(bestScore<originalScore){c.Splines=new[]{best};var ex=c.GetComponent<SplineExtrude>();ex.Rebuild();Dirty(c);Dirty(ex.targetMesh);changes++;}
foreach(var p in bestPoints){var cell=Cell(p);if(!accepted.TryGetValue(cell,out var bucket))accepted[cell]=bucket=new List<Vector3>();bucket.Add(p);}report.Add((bestScore==0?"PASS ":"REVIEW ")+w.name+" initial="+originalScore+" final="+bestScore+" "+(originalScore>0?originalReason:"unchanged"));
}
File.WriteAllLines("Logs/HybridReview/"+(apply?"cable-access.txt":"cable-access-audit.txt"),new[]{"Changed "+changes+" of "+wires.Count+" cables. Required-camera clearance plus hardware and prior-cable tests."}.Concat(report));
}finally{UnityEngine.Object.DestroyImmediate(temporary);}
if(apply){AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);}
}
}
