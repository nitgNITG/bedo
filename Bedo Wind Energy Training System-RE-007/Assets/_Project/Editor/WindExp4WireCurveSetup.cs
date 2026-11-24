using System;
using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Object=UnityEngine.Object;

public static class WindExp4WireCurveSetup
{
 const string Prefix="Assets/_Project/Models/Generated/EXP4 Step";
 static StoryboardSteps Owner()=>Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(o=>o.StoryboardID=="OFF Grid System Using Wind Turbine");
 static WireInteraction[] Wires()=>Owner().GetComponentsInChildren<WireInteraction>(true).Where(w=>w.wireObject&&w.plugA&&w.plugB)
  .Where(w=>{var e=w.wireObject.GetComponentInChildren<SplineExtrude>(true);return e&&e.targetMesh&&AssetDatabase.GetAssetPath(e.targetMesh).StartsWith(Prefix,StringComparison.Ordinal);}).ToArray();
 static Vector3[] Points(WireInteraction w){var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);return c.Spline.Select(k=>c.transform.TransformPoint((Vector3)k.Position)).ToArray();}
 static readonly Dictionary<GameObject,bool> preview=new Dictionary<GameObject,bool>();
 [MenuItem("PV System/Audit Created EXP4 Wire Curves")]
 public static void Audit()
 {
  foreach(var w in Wires()){
   var p=Points(w);float length=0,turn=0;for(int i=1;i<p.Length;i++)length+=Vector3.Distance(p[i-1],p[i]);for(int i=1;i<p.Length-1;i++)turn=Mathf.Max(turn,Vector3.Angle(p[i]-p[i-1],p[i+1]-p[i]));
   Debug.Log("EXP4 CURVE "+w.name+" knots="+p.Length+" ratio="+(length/Vector3.Distance(p[0],p[p.Length-1])).ToString("F3")+" turn="+turn.ToString("F1")+" points="+string.Join(";",p.Select(v=>v.ToString("F3"))));
  }
 }
 static void Show(string folder)
 {
  if(Application.isPlaying||preview.Count>0)throw new InvalidOperationException("End previous preview first.");
  var wires=Wires().Where(w=>AssetDatabase.GetAssetPath(w.wireObject.GetComponentInChildren<SplineExtrude>(true).targetMesh).Contains(folder)).ToArray();
  var bounds=new Bounds(wires[0].plugA.transform.position,Vector3.zero);
  foreach(var w in wires){bounds.Encapsulate(w.plugA.transform.position);bounds.Encapsulate(w.plugB.transform.position);foreach(var go in new[]{w.wireObject,w.plugA,w.plugB}){for(var t=go.transform;t!=null;t=t.parent){if(!preview.ContainsKey(t.gameObject))preview[t.gameObject]=t.gameObject.activeSelf;t.gameObject.SetActive(true);}}}
  SceneView.lastActiveSceneView.LookAt(bounds.center,Quaternion.identity,Mathf.Max(10,bounds.size.magnitude*.65f));SceneView.RepaintAll();
 }
 [MenuItem("PV System/Preview Created EXP4 Step1 Curves")]public static void Show1()=>Show("EXP4 Step1 Wires");
 [MenuItem("PV System/Preview Created EXP4 Step9 Curves")]public static void Show9()=>Show("EXP4 Step9 Wires");
 [MenuItem("PV System/Preview Created EXP4 Step4 Curves")]public static void Show4()=>Show("EXP4 Step4 Wires");
 [MenuItem("PV System/Preview Created EXP4 Step16 Curves")]public static void Show16()=>Show("EXP4 Step16 Wires");
 [MenuItem("PV System/Preview Created EXP4 Step11 Curves")]public static void Show11()=>Show("EXP4 Step11 Wires");
 [MenuItem("PV System/End Created EXP4 Curve Preview")]
 public static void Hide(){foreach(var x in preview)if(x.Key)x.Key.SetActive(x.Value);preview.Clear();SceneView.RepaintAll();}
 [MenuItem("PV System/Smooth Created EXP4 Wire Curves")]
 public static void Smooth()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty||preview.Count>0||scene.path!="Assets/_Project/Scenes/Wind Lab.unity")throw new InvalidOperationException("Save Wind Lab and end previews first.");
  var owned=Wires();var containers=owned.Select(w=>w.wireObject.GetComponentInChildren<SplineContainer>(true)).ToHashSet();
  var originals=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SplineContainer>(true)).Where(c=>!containers.Contains(c)).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
  foreach(var w in owned)
  {
   var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var e=c.GetComponent<SplineExtrude>();
   string path=AssetDatabase.GetAssetPath(e.targetMesh);if(!path.StartsWith(Prefix,StringComparison.Ordinal))throw new InvalidOperationException("Unowned mesh.");
   // Two cubic segments join with equal tangents. Ends leave the panel forwards;
   // the middle adds modest gravity slack without inherited loops or reversals.
   var a=w.plugA.transform.position;var b=w.plugB.transform.position;var delta=b-a;float distance=delta.magnitude;
   float horizontal=Mathf.Abs(delta.x)/Mathf.Max(distance,.001f);
   float sag=Mathf.Clamp(distance*.12f,.6f,3f)*Mathf.Clamp(horizontal,.15f,1f);
   float depth=Mathf.Clamp(distance*.075f,1.1f,2.8f);
   var midpoint=(a+b)*.5f+Vector3.down*sag+Vector3.back*depth;
   var tangent=delta*.23f;var exit=Vector3.back*Mathf.Clamp(distance*.06f,.65f,1.8f);
   Func<Vector3,float3> localPoint=p=>(float3)c.transform.InverseTransformPoint(p);
   Func<Vector3,float3> localVector=v=>(float3)c.transform.InverseTransformVector(v);
   var knots=new[]{new BezierKnot(localPoint(a),localVector(-exit),localVector(exit),quaternion.identity),new BezierKnot(localPoint(midpoint),localVector(-tangent),localVector(tangent),quaternion.identity),new BezierKnot(localPoint(b),localVector(exit),localVector(-exit),quaternion.identity)};
   Undo.RecordObject(c,"Smooth created EXP4 wire");c.Splines=new[]{new Spline(knots,false)};
   bool active=w.wireObject.activeSelf;w.wireObject.SetActive(true);e.Rebuild();w.wireObject.SetActive(active);
   EditorUtility.SetDirty(c);EditorUtility.SetDirty(e.targetMesh);PrefabUtility.RecordPrefabInstancePropertyModifications(c);
   Debug.Log("EXP4 CURVE SMOOTHED "+w.wireObject.name+" mesh="+path);
  }
  foreach(var original in originals)if(EditorJsonUtility.ToJson(original.Key)!=original.Value)throw new InvalidOperationException("An existing wire changed: "+original.Key.name);
  Verify();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Debug.Log("EXP4 CURVES SAVED: "+owned.Length+" created wires smoothed; all other spline containers unchanged.");
 }
 [MenuItem("PV System/Verify Created EXP4 Wire Curves")]
 public static void Verify()
 {
  foreach(var w in Wires())
  {
   var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var s=c.Spline;var e=c.GetComponent<SplineExtrude>();
   if(Vector3.Distance(c.transform.TransformPoint((Vector3)s[0].Position),w.plugA.transform.position)>.002f||Vector3.Distance(c.transform.TransformPoint((Vector3)s[s.Count-1].Position),w.plugB.transform.position)>.002f||e.targetMesh.vertexCount==0)throw new InvalidOperationException("Missing geometry or disconnected end: "+w.name);
   if(s.Count==3&&Vector3.Angle((Vector3)s[1].TangentOut,-(Vector3)s[1].TangentIn)>.01f)throw new InvalidOperationException("Discontinuous middle tangent.");
  }
  Debug.Log("EXP4 CURVES VERIFIED: endpoints meet plugs, middle tangents continuous, independent meshes rebuilt.");
 }
}
