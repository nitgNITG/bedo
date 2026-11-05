using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Unity.Mathematics;
using TutorialFramework;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class WireEntryRepair
{
 static string Folder="Reports/WireEntries";
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Log(string s)=>File.AppendAllText(Folder+"/Geometry.txt",s+"\n");
 static void Tick(){if(!File.Exists("Temp/wire-entry-fix.flag")||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Temp/wire-entry-fix.flag");try{Run();}catch(Exception e){Log("FAIL "+e);}}
 static WireEntryRepair(){EditorApplication.update+=Tick;}
 static SplineContainer Container(WireEntryProbe.Item item){GlobalObjectId.TryParse(item.id,out var id);return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as SplineContainer;}
 static Vector3 World(SplineContainer c,BezierKnot k)=>c.transform.TransformPoint((Vector3)k.Position);
 struct Curve
 {
  public Vector3 p0,p1,p2,p3;
  public Curve(Vector3 a,Vector3 b,Vector3 c,Vector3 d){p0=a;p1=b;p2=c;p3=d;}
  public Vector3 At(float t){float u=1-t;return u*u*u*p0+3*u*u*t*p1+3*u*t*t*p2+t*t*t*p3;}
  public void Split(float t,out Curve left,out Curve right){var a=Vector3.Lerp(p0,p1,t);var b=Vector3.Lerp(p1,p2,t);var c=Vector3.Lerp(p2,p3,t);var d=Vector3.Lerp(a,b,t);var e=Vector3.Lerp(b,c,t);var f=Vector3.Lerp(d,e,t);left=new Curve(p0,a,d,f);right=new Curve(f,e,c,p3);}
  public Curve Part(float a,float b){Split(b,out var first,out var ignored);if(a<=0)return first;first.Split(a/b,out ignored,out var result);return result;}
 }
 static Curve[] Original(WireEntryProbe.Item item){var curves=new List<Curve>();for(int k=0;k<item.points.Length-1;k++)curves.Add(new Curve(item.points[k],item.points[k]+item.tout[k],item.points[k+1]+item.tin[k+1],item.points[k+1]));return curves.ToArray();}
 static Spline Make(SplineContainer c,IList<Curve> curves){var knots=new List<BezierKnot>();for(int k=0;k<=curves.Count;k++){var pos=k==curves.Count?curves[k-1].p3:curves[k].p0;var incoming=k>0?curves[k-1].p2-pos:Vector3.zero;var outgoing=k<curves.Count?curves[k].p1-pos:Vector3.zero;knots.Add(new BezierKnot((float3)c.transform.InverseTransformPoint(pos),(float3)c.transform.InverseTransformVector(incoming),(float3)c.transform.InverseTransformVector(outgoing),quaternion.identity));}return new Spline(knots,false);}
 static void Assign(SplineContainer c,Spline spline){var all=c.Splines.ToArray();all[0]=spline;c.Splines=all;}
 struct Location {public int curve;public float t,length;}
 static List<Location> LengthTable(Curve[] curves){var result=new List<Location>{new Location{curve=0,t=0,length=0}};float length=0;for(int k=0;k<curves.Length;k++){var prev=curves[k].p0;for(int i=1;i<=128;i++){float t=i/128f;var point=curves[k].At(t);length+=Vector3.Distance(prev,point);result.Add(new Location{curve=k,t=t,length=length});prev=point;}}return result;}
 static Location Along(List<Location> table,float length){for(int k=1;k<table.Count;k++)if(table[k].length>=length){var a=table[k-1];var b=table[k];float start=a.curve==b.curve?a.t:0;return new Location{curve=b.curve,t=Mathf.Lerp(start,b.t,Mathf.InverseLerp(a.length,b.length,length)),length=length};}return table.Last();}
 static void Run()
 {
  var scene=SceneManager.GetActiveScene();Require(scene.path=="Assets/_Project/Scenes/PV Lab.unity"&&!scene.isDirty,"Open saved PV Lab outside Play Mode");
  var data=JsonUtility.FromJson<WireEntryProbe.Data>(File.ReadAllText("Temp/WireEntry/audit.json"));
  var items=data.wires.Where(w=>w.a!=null&&w.b!=null&&w.a.mesh.EndsWith("/Plugs.fbx")&&w.b.mesh.EndsWith("/Plugs.fbx")).GroupBy(w=>w.id).Select(g=>g.First()).ToArray();
  File.WriteAllText(Folder+"/Geometry.txt","");Require(items.Length>95,"Unexpected coverage");
  foreach(var i in items){var c=Container(i);Require(c,"Missing spline "+i.name);Assign(c,Make(c,Original(i)));}
  Preview(data.wires,"before");var changed=new List<string>();
  foreach(var i in items){var c=Container(i);var original=Original(i);var p=original.First().p0;var q=original.Last().p3;
   bool reverse=Vector3.Distance(p,i.b.mouth)+Vector3.Distance(q,i.a.mouth)<Vector3.Distance(p,i.a.mouth)+Vector3.Distance(q,i.b.mouth);var a=reverse?i.b:i.a;var b=reverse?i.a:i.b;
   Require(Vector3.Distance(p,a.mouth)<.2f&&Vector3.Distance(q,b.mouth)<.2f,"Existing plug mismatch; refusing "+i.context+" "+i.name);
   var lengths=LengthTable(original);float total=lengths.Last().length;float trim=Mathf.Min(2.5f,total*.2f,Vector3.Distance(p,q)*.2f);
   var left=Along(lengths,trim);var right=Along(lengths,total-trim);var trunk=new List<Curve>();
   for(int k=left.curve;k<=right.curve;k++)trunk.Add(original[k].Part(k==left.curve?left.t:0,k==right.curve?right.t:1));
   var da=a.direction.normalized;var db=b.direction.normalized;var ca=p+da*.12f;var cb=q+db*.12f;var ta=trunk.First().p0;var tb=trunk.Last().p3;
   var startDirection=(trunk.First().p1-ta).normalized;var endDirection=(tb-trunk.Last().p2).normalized;
   var curves=new List<Curve>{new Curve(p,p+da*.04f,ca-da*.04f,ca),new Curve(ca,ca+da*.02f,ta-startDirection*Mathf.Min(.35f,Vector3.Distance(ca,ta)/3f),ta)};
   curves.AddRange(trunk);curves.Add(new Curve(tb,tb+endDirection*Mathf.Min(.35f,Vector3.Distance(tb,cb)/3f),cb+db*.02f,cb));curves.Add(new Curve(cb,cb-db*.04f,q+db*.04f,q));
   var result=Make(c,curves);Assign(c,result);EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);
   for(int k=0;k<trunk.Count;k++)for(int n=0;n<=32;n++){float t=n/32f;var a0=result[k+2];var a1=result[k+3];var actual=new Curve(World(c,a0),c.transform.TransformPoint((Vector3)(a0.Position+a0.TangentOut)),c.transform.TransformPoint((Vector3)(a1.Position+a1.TangentIn)),World(c,a1)).At(t);Require(Vector3.Distance(actual,trunk[k].At(t))<.0002f,"Middle route changed "+i.name);}
   for(int end=0;end<2;end++){int x=end==0?0:result.Count-1,y=end==0?1:result.Count-2;var plug=end==0?a:b;var axis=World(c,result[y])-World(c,result[x]);Require(Mathf.Abs(axis.magnitude-.12f)<.00015f&&Vector3.Dot(axis.normalized,plug.direction)>.9999f,"Collar alignment failed "+i.name);}
   changed.Add(i.id);Log("PASS "+i.context+" | "+i.name+" | removed local hook within "+trim.ToString("F3")+" arc units; exact central Bezier route retained; straight 0.12-unit plug exits");
  }
  File.WriteAllLines(Folder+"/ChangedContainers.txt",changed);Preview(data.wires,"after");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Log("PASS: "+items.Length+" unique banana-plug cables / "+(items.Length*2)+" ends corrected across "+data.wires.Count(w=>w.a!=null&&w.b!=null&&w.a.mesh.EndsWith("/Plugs.fbx")&&w.b.mesh.EndsWith("/Plugs.fbx"))+" shared Guided/Free bindings. Other splines, camera/plug transforms, references and central paths retained. Ethernet/power cords excluded.");
 }
 static void Preview(WireEntryProbe.Item[] items,string phase)
 {
  var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var states=all.ToDictionary(t=>t.gameObject,t=>t.gameObject.activeSelf);
  var mover=all.Select(t=>t.GetComponent<SmoothCameraMovement>()).First(m=>m);var positions=new SerializedObject(mover).FindProperty("cameraPositions");var source=mover.GetComponent<Camera>();if(!source)source=Camera.main;Require(source,"No scene camera");
  var camObj=new GameObject("Wire entry diagnostic camera"){hideFlags=HideFlags.HideAndDontSave};var cam=camObj.AddComponent<Camera>();cam.CopyFrom(source);cam.enabled=false;
  var rt=new RenderTexture(1920,1080,24);rt.Create();cam.targetTexture=rt;
  try{
   foreach(var experiment in new[]{"Exp 1","Exp 2","Exp 3","Exp 4"}){
    foreach(var pair in states)if(pair.Key)pair.Key.SetActive(pair.Value);
    foreach(var board in all.Select(t=>t.GetComponent<StoryboardSteps>()).Where(b=>b))board.gameObject.SetActive(false);
    foreach(var canvas in all.Select(t=>t.GetComponent<Canvas>()).Where(c=>c))canvas.gameObject.SetActive(false);
    foreach(var w in items.Where(w=>w.context==experiment)){var c=Container(w);var board=all.Select(t=>t.GetComponent<StoryboardSteps>()).Single(b=>b&&b.name==experiment);
     var interaction=board.steps.Select(s=>s.interaction as WireInteraction).First(i=>i&&i.wireObject&&i.wireObject.GetComponentsInChildren<SplineContainer>(true).Contains(c));
     foreach(var go in new[]{interaction.wireObject,interaction.plugA,interaction.plugB})for(var t=go.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
     foreach(var ex in interaction.wireObject.GetComponentsInChildren<SplineExtrude>(true))ex.Rebuild();
    }
    foreach(int index in new[]{0,1,8}){var target=positions.GetArrayElementAtIndex(index).objectReferenceValue as Transform;if(!target)continue;cam.transform.SetPositionAndRotation(target.position,target.rotation);cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Folder+"/Views/"+phase+"-"+experiment.Replace(" ","")+"-camera"+index+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);RenderTexture.active=old;}
   }
  }finally{foreach(var pair in states)if(pair.Key)pair.Key.SetActive(pair.Value);cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(camObj);}
 }
}
