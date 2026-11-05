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

public static partial class HybridGuidedCompletion
{
 static Spline RoutedCurve(SplineContainer c,IList<Vector3> p,Vector3 da,Vector3 db){
 var spline=new Spline(p.Select(v=>(float3)c.transform.InverseTransformPoint(v)),TangentMode.Broken,false);
 for(int i=0;i<p.Count;i++){
 var direction=i<2?da:i>=p.Count-2?-db:(p[i+1]-p[i-1]).normalized;
 float before=i==0?.3f:Vector3.Distance(p[i],p[i-1]),after=i==p.Count-1?.3f:Vector3.Distance(p[i],p[i+1]);
 float length=Mathf.Min(before,after)*.28f;if(i<2||i>=p.Count-2)length=Mathf.Min(length,.12f);
 var tangent=c.transform.InverseTransformVector(direction*length);var k=spline[i];k.Rotation=quaternion.identity;k.TangentIn=(float3)(-tangent);k.TangentOut=(float3)tangent;spline[i]=k;
 }return spline;
 }
 [MenuItem("RE009/Route Corrected Wires Through Module Gutters")]
 public static void RouteCorrectedWires(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
 var originals=All<WireInteraction>().Where(w=>Path(w.transform).StartsWith("Experiments/Exp 4/exp 4/Steps/")&&w.pointA&&w.plugA).ToArray();
 var links=board.steps.Where(s=>s.interaction is WireInteraction).ToArray();var counts=new Dictionary<string,int>();var report=new List<string>();
 int mains=0;
 foreach(var row in links){
 var w=(WireInteraction)row.interaction;int step=int.Parse(row.uiObject.name.Replace("Subtitle Step ",""));bool comm=w.plugA.name.IndexOf("Comm",StringComparison.OrdinalIgnoreCase)>=0;bool black=w.plugA.name.Contains("Black");float lane=black?-.32f:.32f;
 foreach(var end in new[]{(w.pointA,w.plugA),(w.pointB,w.plugB)})if(!comm){
 var source=originals.SelectMany(v=>new[]{(v.pointA,v.plugA),(v.pointB,v.plugB)}).Where(v=>v.Item2&&v.Item2.name.IndexOf(end.Item2.name.Split(' ')[0],StringComparison.OrdinalIgnoreCase)>=0).OrderBy(v=>Vector3.Distance(v.Item1.transform.position,v.Item2.transform.position)).FirstOrDefault();
 if(source.Item2){end.Item2.transform.localPosition=source.Item2.transform.localPosition;end.Item2.transform.localRotation=source.Item2.transform.localRotation;end.Item2.transform.localScale=source.Item2.transform.localScale;}
 string key=end.Item1.transform.position.ToString("F2");counts.TryGetValue(key,out int slot);counts[key]=slot+1;
 // A second cable uses a stacked banana, with the existing wind plug's own insertion direction.
 if(slot>0)end.Item2.transform.position+=end.Item1.transform.forward*.85f*slot;
 }
 var ap=w.pointA.transform.position;var bp=w.pointB.transform.position;float depth=comm?2.2f:1.6f;float z=Mathf.Min(ap.z,bp.z)-depth;var middle=new List<Vector3>();
 if(step==1){int link=mains++/2;z=-54.25f-(black?.35f:0);
 if(link==0){float x=-144.65f+lane;middle.Add(new Vector3(x,ap.y,z));middle.Add(new Vector3(x,bp.y,z));}
 if(link==1){float x=-127.9f+lane;middle.Add(new Vector3(x,ap.y,z));middle.Add(new Vector3(x,183.2f+lane,z));middle.Add(new Vector3(bp.x,183.2f+lane,z));}
 if(link==2){float x=-124.25f+lane;middle.Add(new Vector3(ap.x,183.2f+lane,z));middle.Add(new Vector3(x,183.2f+lane,z));middle.Add(new Vector3(x,bp.y,z));}
 if(link==3){float y=172.2f+lane;middle.Add(new Vector3(ap.x,y,z));middle.Add(new Vector3(bp.x,y,z));}
 if(link==4){float x=-139.1f+lane;middle.Add(new Vector3(x,172.9f,z));middle.Add(new Vector3(x,bp.y,z));}
 if(link==5){float x=-127.05f+lane;middle.Add(new Vector3(x,ap.y,z));middle.Add(new Vector3(x,bp.y,z));}
 }
 else if(step==2){middle.Add(new Vector3(ap.x,123.0f,-65));middle.Add(new Vector3(-159.5f,123.0f,-65));middle.Add(new Vector3(-159.5f,bp.y,-55.5f));}
 else if(step==3){middle.Add(new Vector3(-144.5f,ap.y,-55.5f));middle.Add(new Vector3(-144.5f,151f,-55.5f));middle.Add(new Vector3(bp.x,124f,-69));}
 else if(step==4){float y=142.1f+lane;middle.Add(new Vector3(ap.x,y,z));middle.Add(new Vector3(bp.x,y,z));}
 else if(step==5){float x=-144.35f+lane;middle.Add(new Vector3(x,ap.y,z));middle.Add(new Vector3(x,160.4f+lane,z));middle.Add(new Vector3(bp.x,160.4f+lane,z));}
 else if(step==6){float y=160.5f+lane;middle.Add(new Vector3(ap.x,y,z));middle.Add(new Vector3(bp.x,y,z));}
 else if(step==8){
 if(ap.y<174){if(black){middle.Add(new Vector3(ap.x+2.2f,ap.y-.6f,z));middle.Add(new Vector3(ap.x+2.2f,173.7f+lane,z));}else middle.Add(new Vector3(ap.x,173.7f+lane,z));middle.Add(new Vector3(bp.x,173.7f+lane,z));}
 else if(Mathf.Abs(ap.x-bp.x)<3){float x=(ap.x+bp.x)/2+(black?.85f:-.85f);middle.Add(new Vector3(x,ap.y,z));middle.Add(new Vector3(x,bp.y,z));}
 else{float y=173.6f+lane;middle.Add(new Vector3(ap.x,y,z));middle.Add(new Vector3(bp.x,y,z));}
 }
 else{
 // Long inter-stand cables have shallow slack, rather than a wide loop at either connector.
 float sag=comm?3f:2f;middle.Add(Vector3.Lerp(ap,bp,.3f)+Vector3.down*sag+Vector3.back*depth);middle.Add(Vector3.Lerp(ap,bp,.7f)+Vector3.down*sag+Vector3.back*depth);
 }
 if(!comm)foreach(var end in new[]{(w.pointA,w.plugA,middle.First()),(w.pointB,w.plugB,middle.Last())}){
 var normal=end.Item1.transform.forward;var desired=Vector3.ProjectOnPlane(end.Item3-end.Item1.transform.position,normal).normalized;
 if(desired.sqrMagnitude>.1f)end.Item2.transform.RotateAround(end.Item1.transform.position,normal,Vector3.SignedAngle(Vector3.ProjectOnPlane(-end.Item2.transform.up,normal),desired,normal));
 }
 var a=Outlet(w.plugA);var b=Outlet(w.plugB);var da=CableDirection(w.plugA);var db=CableDirection(w.plugB);var p=new List<Vector3>{a,a+da*.4f};p.AddRange(middle);p.Add(b+db*.4f);p.Add(b);
 var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);c.Splines=new[]{RoutedCurve(c,p,da,db)};var extrude=c.GetComponent<SplineExtrude>();extrude.Rebuild();
 if(comm){var blackWire=originals.First(v=>v.plugA.name.Contains("Black")).wireObject.GetComponentInChildren<SplineContainer>(true).GetComponent<Renderer>();c.GetComponent<Renderer>().sharedMaterials=blackWire.sharedMaterials;Dirty(c.GetComponent<Renderer>());}
 Dirty(c);Dirty(extrude.targetMesh);Dirty(w.plugA);Dirty(w.plugB);report.Add(step+": "+w.name);
 }
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);File.WriteAllLines("Temp/RE009Planning/authored-route-review.txt",report);
 }
 [MenuItem("RE009/Separate Corrected Cable Crossings")]
 public static void SeparateAuthoredCrossings(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var wires=board.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
 var buckets=new Dictionary<Vector3Int,List<WireSample>>();var ends=new List<Vector3[]>();var report=new List<string>();bool failed=false;
 for(int i=0;i<wires.Length;i++){
 var w=wires[i];var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var original=c.Spline;var a=Outlet(w.plugA);var b=Outlet(w.plugB);ends.Add(new[]{a,b});Spline chosen=null;Vector3[] samples=null;string conflict="";
 for(int attempt=0;attempt<16;attempt++){
 var candidate=new Spline(original);for(int k=2;k<candidate.Count-2;k++){var knot=candidate[k];knot.Position+=(float3)c.transform.InverseTransformVector(Vector3.back*attempt*.18f);candidate[k]=knot;}
 var points=Sample(c,candidate);bool hit=false;
 foreach(var p in points){if(Vector3.Distance(p,a)<1f||Vector3.Distance(p,b)<1f)continue;var cell=Cell(p);
 for(int x=-1;x<=1&&!hit;x++)for(int y=-1;y<=1&&!hit;y++)for(int z=-1;z<=1&&!hit;z++)if(buckets.TryGetValue(cell+new Vector3Int(x,y,z),out var list))foreach(var q in list){if(Vector3.Distance(q.position,ends[q.owner][0])<1f||Vector3.Distance(q.position,ends[q.owner][1])<1f)continue;if(Vector3.Distance(p,q.position)<.13f){conflict=wires[q.owner].name+" at "+p+" vs "+q.position;hit=true;break;}}
 if(hit)break;}
 if(!hit){chosen=candidate;samples=points;report.Add("PASS "+w.name+" crossing offset="+(attempt*.18f));break;}
 }
 if(chosen==null){failed=true;chosen=original;samples=Sample(c,original);report.Add("REVIEW "+w.name+": requires local adjustment: "+conflict+".");}
 c.Splines=new[]{chosen};var extrusion=c.GetComponent<SplineExtrude>();extrusion.Rebuild();Dirty(c);Dirty(extrusion.targetMesh);
 foreach(var p in samples){var key=Cell(p);if(!buckets.TryGetValue(key,out var bucket))buckets[key]=bucket=new List<WireSample>();bucket.Add(new WireSample{owner=i,position=p});}
 }
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);File.WriteAllLines("Temp/RE009Planning/corrected-crossings.txt",report);if(failed)Debug.LogWarning("Some hybrid cable crossings require local review; see corrected-crossings.txt.");
 }
}
