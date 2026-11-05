using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static partial class HybridGuidedCompletion
{
 struct WireSample { public int owner; public Vector3 position; }
 static Vector3[] Sample(SplineContainer c,Spline s){float length=SplineUtility.CalculateLength(s,float4x4.identity);int n=Mathf.Clamp(Mathf.CeilToInt(length/.075f),32,3000);return Enumerable.Range(0,n+1).Select(i=>c.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(s,(float)i/n))).ToArray();}
 static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x/.18f),Mathf.FloorToInt(p.y/.18f),Mathf.FloorToInt(p.z/.18f));
 [MenuItem("RE009/Resolve Wire Clearances And Finish Visual Bindings")]
 public static void Clearances(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);system=owner.GetComponentInChildren<HybridTrainingController>(true);
 var sourceGlow=new SerializedObject(system.windSpeed).FindProperty("systemOnGlow").objectReferenceValue as GameObject;
 if(sourceGlow&&sourceGlow.transform.GetComponentInParent<StoryboardSteps>()!=owner){var glow=Object.Instantiate(sourceGlow,owner.transform.Find("Steps/Step 21"),true);glow.name="Wind System ON Glow";glow.SetActive(false);Ref(system.windSpeed,"systemOnGlow",glow);}
 var wires=owner.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();var cells=new Dictionary<Vector3Int,List<WireSample>>();var anchors=new List<Vector3[]>();var report=new List<string>();
 for(int i=0;i<wires.Length;i++){
  var w=wires[i];var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);
  foreach(var end in new[]{(w.pointA,w.plugA,w.pointB),(w.pointB,w.plugB,w.pointA)})if(end.Item2.name.IndexOf("Comm",StringComparison.OrdinalIgnoreCase)<0){
   var axis=end.Item1.transform.forward;var desired=Vector3.ProjectOnPlane(end.Item3.transform.position-end.Item1.transform.position,axis).normalized;
   bool positive=end.Item2.name.IndexOf("Red",StringComparison.OrdinalIgnoreCase)>=0||end.Item2.name.IndexOf("Yellow",StringComparison.OrdinalIgnoreCase)>=0;
   desired=(desired+Vector3.ProjectOnPlane(Vector3.up,axis)*(positive?1.5f:-1.5f)).normalized;
   if(end.Item1.name.Contains("Wind V")||end.Item1.name.Contains("module L2"))desired=Vector3.ProjectOnPlane(Vector3.right,axis).normalized;
   if(desired.sqrMagnitude>.1f){float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(-end.Item2.transform.up,axis),desired,axis);end.Item2.transform.RotateAround(end.Item1.transform.position,axis,angle);}
  }
  var a=Outlet(w.plugA);var b=Outlet(w.plugB);anchors.Add(new[]{a,b});
  Spline chosen=null;Vector3[] samples=null;int tries=0;string lastHit="";
  for(int attempt=0;attempt<128;attempt++){
   float depth=.65f+(i%7)*.12f+(attempt/8)*.12f;bool positiveRoute=w.plugA.name.IndexOf("Red",StringComparison.OrdinalIgnoreCase)>=0||w.plugA.name.IndexOf("Yellow",StringComparison.OrdinalIgnoreCase)>=0;float offset=(positiveRoute?.7f:-.7f)+(attempt==0?0:((attempt%8)-3.5f)*.24f);
   var da=CableDirection(w.plugA);var db=CableDirection(w.plugB);var leadA=a+da*.65f;var leadB=b+db*.65f;float z=Mathf.Min(leadA.z,leadB.z)-depth;
   var spline=Curve(c,a,b,da,db,depth);
   foreach(int knotIndex in new[]{2,3}){var k=spline[knotIndex];var p=(Vector3)k.Position;p.x+=offset;k.Position=(float3)p;spline[knotIndex]=k;}
   var points=Sample(c,spline);bool collides=false;
   foreach(var p in points){if(Vector3.Distance(p,a)<.65f||Vector3.Distance(p,b)<.65f)continue;var cell=Cell(p);
    for(int x=-1;x<=1&&!collides;x++)for(int y=-1;y<=1&&!collides;y++)for(int zz=-1;zz<=1&&!collides;zz++)if(cells.TryGetValue(cell+new Vector3Int(x,y,zz),out var bucket))foreach(var prev in bucket){if(Vector3.Distance(prev.position,anchors[prev.owner][0])<.65f||Vector3.Distance(prev.position,anchors[prev.owner][1])<.65f)continue;if((p-prev.position).sqrMagnitude<.13f*.13f){lastHit=" near "+wires[prev.owner].name+" at "+p+" vs "+prev.position+" anchors "+a+" / "+b;collides=true;break;}}
    if(collides)break;
   }
   if(!collides){chosen=spline;samples=points;tries=attempt+1;break;}
  }
  if(chosen==null)throw new Exception("Unable to clear wire without a custom route: "+w.name+lastHit);
  c.Splines=new[]{chosen};var extrusion=c.GetComponent<SplineExtrude>();extrusion.Rebuild();Dirty(c);Dirty(extrusion.targetMesh);
  foreach(var p in samples){var key=Cell(p);if(!cells.TryGetValue(key,out var bucket))cells[key]=bucket=new List<WireSample>();bucket.Add(new WireSample{owner=i,position=p});}
  report.Add("PASS "+w.name+" (route candidates="+tries+")");
 }
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);File.WriteAllLines("Temp/RE009Planning/wire-clearance.txt",report);
 }
}
