using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Events;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class HybridTemplateRepair
{
 static T[] All<T>() where T:Component=>HybridGuidedCompletion.All<T>();
 static string Path(Transform t)=>HybridGuidedCompletion.Path(t);
 static void Dirty(Object o)=>HybridGuidedCompletion.Dirty(o);
 static readonly Dictionary<Object,Object> replacements=new Dictionary<Object,Object>();
 [MenuItem("RE009/Preserve Partial Repair And Restore Verified Baseline")]
 public static void RestoreBaseline(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 const string path="Assets/_Project/Scenes/PV Lab.unity",backup="Logs/HybridRepairBackup/PV Lab.saved-before-repair.unity";
 if(!All<Transform>().Any(t=>t.name=="Exp 1 (Repair In Progress)"))throw new Exception("No incomplete repair to restore.");
 if(!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(backup)))throw new Exception("Saved scene changed since the backup; stop to preserve user work.");
 string snapshot=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Scenes/HybridRepairPartial.unity");
 if(!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),snapshot,true))throw new Exception("Could not preserve partial repair.");
 EditorSceneManager.OpenScene(path);
 string destination="Logs/HybridRepairBackup/partial-repair-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";File.Move(snapshot,destination);if(File.Exists(snapshot+".meta"))File.Move(snapshot+".meta",destination+".meta");AssetDatabase.Refresh();
 }
 static void Map(Object a,Object b){if(a&&b&&a!=b)replacements[a]=b;}
 static void RemapReferences(){
 foreach(var c in All<Component>().Where(c=>c)){
 var so=new SerializedObject(c);var p=so.GetIterator();bool changed=false;
 while(p.Next(true)){
 if(p.propertyPath=="m_GameObject"||p.propertyPath=="m_Script"||p.propertyPath=="m_Father"||p.propertyPath.StartsWith("m_Children"))continue;
 if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue&&replacements.TryGetValue(p.objectReferenceValue,out var mapped)){p.objectReferenceValue=mapped;changed=true;}
 }
 if(changed){so.ApplyModifiedPropertiesWithoutUndo();Dirty(c);}
 }
 }
 static string Relative(Transform child,Transform root){var names=new List<string>();for(var t=child;t&&t!=root;t=t.parent)names.Add(t.name);names.Reverse();return string.Join("/",names);}
 static string ColorOf(GameObject plug){foreach(string color in new[]{"Black","Red","Yellow","Blue"})if(plug.name.IndexOf(color,StringComparison.OrdinalIgnoreCase)>=0)return color;return "Comm";}
 struct EndpointSource{public Interaction point;public GameObject plug,marker;}
 static EndpointSource[] endpoints;
 static WireInteraction[] templates;
 static EndpointSource MatchPoint(Interaction desired,string color){
 return endpoints.Where(e=>ColorOf(e.plug)==color&&e.point.GetComponent<MeshFilter>()&&e.point.GetComponent<Renderer>())
 .OrderBy(e=>Vector3.Distance(e.point.transform.position,desired.transform.position)+Quaternion.Angle(e.point.transform.rotation,desired.transform.rotation)*.03f+Mathf.Abs(e.point.transform.lossyScale.x-1f)*100).First();
 }
 static Interaction CopyEndpoint(EndpointSource source,Interaction desired,Transform parent,out GameObject plug,out GameObject marker){
 var go=Object.Instantiate(source.point.gameObject,parent,true);go.name=desired.name;
 go.transform.SetPositionAndRotation(desired.transform.position,desired.transform.rotation);
 var point=go.GetComponent<Interaction>();point.Allow=false;point.onClicked=desired.onClicked;
 plug=source.plug.transform.IsChildOf(source.point.transform)?go.transform.Find(Relative(source.plug.transform,source.point.transform)).gameObject:Object.Instantiate(source.plug,go.transform,false);
 if(source.marker.transform.IsChildOf(source.point.transform))marker=go.transform.Find(Relative(source.marker.transform,source.point.transform)).gameObject;
 else{
 marker=Object.Instantiate(source.marker,go.transform,false);marker.transform.localPosition=source.point.transform.InverseTransformPoint(source.marker.transform.position);marker.transform.localRotation=Quaternion.Inverse(source.point.transform.rotation)*source.marker.transform.rotation;
 marker.transform.localScale=Vector3.Scale(source.marker.transform.lossyScale,new Vector3(1/go.transform.lossyScale.x,1/go.transform.lossyScale.y,1/go.transform.lossyScale.z));
 }
 plug.SetActive(false);marker.SetActive(false);
 foreach(var collider in plug.GetComponentsInChildren<Collider>(true))collider.enabled=false;
 var free=point.GetComponent<FreeLabEndpoint>();if(free){free.ClearAdditionalConnectionPlugs();free.SetEndpointId(point.name);free.SetSelectionPlug(plug);free.SetPossibleConnectionIndicator(marker);}
 Map(desired,point);Map(desired.gameObject,go);Map(desired.transform,go.transform);Dirty(point);return point;
 }
 static WireInteraction MatchWire(WireInteraction desired){
 string color=ColorOf(desired.plugA);float length=Vector3.Distance(desired.pointA.transform.position,desired.pointB.transform.position);
 return templates.Where(w=>ColorOf(w.plugA)==color||color!="Comm"&&ColorOf(w.plugB)==color).OrderBy(w=>{
 float direct=Vector3.Distance(w.pointA.transform.position,desired.pointA.transform.position)+Vector3.Distance(w.pointB.transform.position,desired.pointB.transform.position);
 float reverse=Vector3.Distance(w.pointB.transform.position,desired.pointA.transform.position)+Vector3.Distance(w.pointA.transform.position,desired.pointB.transform.position);
 return Mathf.Min(direct,reverse)+Mathf.Abs(Vector3.Distance(w.pointA.transform.position,w.pointB.transform.position)-length);
 }).First();
 }
 static void RetargetCurve(WireInteraction source,WireInteraction wire){
 var from=source.wireObject.GetComponentInChildren<SplineContainer>(true);var to=wire.wireObject.GetComponentInChildren<SplineContainer>(true);var original=from.Spline;
 var points=original.Select(k=>from.transform.TransformPoint((Vector3)k.Position)).ToArray();
 bool sourceStartsA=Vector3.Distance(points[0],source.plugA.transform.position)<Vector3.Distance(points[0],source.plugB.transform.position);
 var startPlug=sourceStartsA?source.plugA:source.plugB;var endPlug=sourceStartsA?source.plugB:source.plugA;
 bool reversed=Vector3.Distance(startPlug.transform.position,wire.plugA.transform.position)+Vector3.Distance(endPlug.transform.position,wire.plugB.transform.position)>Vector3.Distance(startPlug.transform.position,wire.plugB.transform.position)+Vector3.Distance(endPlug.transform.position,wire.plugA.transform.position);
 var newStart=reversed?wire.plugB:wire.plugA;var newEnd=reversed?wire.plugA:wire.plugB;
 Quaternion r0=newStart.transform.rotation*Quaternion.Inverse(startPlug.transform.rotation),r1=newEnd.transform.rotation*Quaternion.Inverse(endPlug.transform.rotation);
 Func<Vector3,Vector3> atStart=v=>newStart.transform.TransformVector(startPlug.transform.InverseTransformVector(v));
 Func<Vector3,Vector3> atEnd=v=>newEnd.transform.TransformVector(endPlug.transform.InverseTransformVector(v));
 Vector3 a=newStart.transform.TransformPoint(startPlug.transform.InverseTransformPoint(points[0])),b=newEnd.transform.TransformPoint(endPlug.transform.InverseTransformPoint(points[points.Length-1]));
 Vector3 oldChord=points[points.Length-1]-points[0],newChord=b-a;
 var bodyRotation=Quaternion.AngleAxis(Vector3.SignedAngle(new Vector3(oldChord.x,oldChord.y,0),new Vector3(newChord.x,newChord.y,0),Vector3.forward),Vector3.forward);
 float ratio=Mathf.Clamp(Vector3.Distance(a,b)/Mathf.Max(.01f,Vector3.Distance(points[0],points[points.Length-1])),.12f,1f);
 var lengths=new float[points.Length];for(int i=1;i<points.Length;i++)lengths[i]=lengths[i-1]+Vector3.Distance(points[i],points[i-1]);
 var result=new Spline(original);
 for(int i=0;i<points.Length;i++){
 float t=lengths[i]/Mathf.Max(.001f,lengths[points.Length-1]);var rotation=bodyRotation;
 Vector3 mapped=Vector3.Lerp(a,b,t)+bodyRotation*(points[i]-Vector3.Lerp(points[0],points[points.Length-1],t))*ratio;
 if(i<2)mapped=a+atStart(points[i]-points[0]);else if(i>=points.Length-2)mapped=b+atEnd(points[i]-points[points.Length-1]);
 else if(i==2)mapped=a+atStart(points[i]-points[0])*ratio;else if(i==points.Length-3)mapped=b+atEnd(points[i]-points[points.Length-1])*ratio;
 var knot=original[i];var tangentRotation=new Quaternion(knot.Rotation.value.x,knot.Rotation.value.y,knot.Rotation.value.z,knot.Rotation.value.w);
 Vector3 incoming=from.transform.TransformVector(tangentRotation*(Vector3)knot.TangentIn),outgoing=from.transform.TransformVector(tangentRotation*(Vector3)knot.TangentOut);
 knot.Position=(float3)to.transform.InverseTransformPoint(mapped);knot.Rotation=quaternion.identity;
 float tangentScale=i<2||i>=points.Length-2?1:ratio;
 incoming=i<3?atStart(incoming):i>=points.Length-3?atEnd(incoming):rotation*incoming;
 outgoing=i<3?atStart(outgoing):i>=points.Length-3?atEnd(outgoing):rotation*outgoing;
 knot.TangentIn=(float3)to.transform.InverseTransformVector(incoming*tangentScale);knot.TangentOut=(float3)to.transform.InverseTransformVector(outgoing*tangentScale);
 result.SetTangentMode(i,TangentMode.Broken);result[i]=knot;
 }
 to.Splines=new[]{result};var e=to.GetComponent<SplineExtrude>();e.Rebuild();Dirty(to);Dirty(e.targetMesh);
 }
 [MenuItem("RE009/Refit Copied EXP1 Cable Curves")]
 public static void Refit(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var source=All<StoryboardSteps>().Single(b=>b.StoryboardID=="Speed Curve Characteristics");
 var root=board.transform.Find("exp 1/Steps");var sourceRoot=source.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Steps");
 foreach(var folder in new[]{root}.Concat(root.Cast<Transform>()).ToArray()){
 var children=folder.Cast<Transform>().ToArray();foreach(var child in children)child.SetParent(folder.parent,true);
 folder.SetPositionAndRotation(sourceRoot.position,sourceRoot.rotation);folder.localScale=Vector3.one;
 foreach(var child in children)child.SetParent(folder,true);
 }
 foreach(Transform unused in board.transform.Find("exp 1"))if(unused!=root)unused.gameObject.SetActive(false);
 templates=source.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
 foreach(var wire in board.steps.Select(s=>s.interaction).OfType<WireInteraction>())RetargetCurve(MatchWire(wire),wire);
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
 }
 [MenuItem("RE009/Validate EXP1 Template Repair")]
 public static void Validate(){
 var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var original=All<StoryboardSteps>().Single(b=>b.StoryboardID=="Speed Curve Characteristics");
 if(board.name!="Exp 1"||!board.transform.Find("exp 1/Steps")||board.steps.Count!=56)throw new Exception("EXP1 structure or guided sequence changed.");
 var wires=board.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();if(wires.Length!=36)throw new Exception("Expected 36 complete connections.");
 var meshes=new HashSet<Mesh>();int endpointsChecked=0;
 foreach(var w in wires){
 foreach(var end in new[]{(w.pointA,w.plugA,w.markerA),(w.pointB,w.plugB,w.markerB)}){
 var p=end.Item1;var mf=p.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh||!p.GetComponent<MeshRenderer>()||!p.GetComponent<BoxCollider>()||!p.GetComponent<OnMouseDownEvent>())throw new Exception("Incomplete template banana: "+p.name);
 if(!end.Item2||!end.Item2.transform.IsChildOf(p.transform)||!end.Item3||!end.Item3.transform.IsChildOf(p.transform))throw new Exception("Cross-assigned plug/marker: "+p.name);
 var free=p.GetComponent<FreeLabEndpoint>();if(free&&free.SelectionPlug!=end.Item2)throw new Exception("Selection plug mismatch: "+p.name);
 endpointsChecked++;
 }
 var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var e=c.GetComponent<SplineExtrude>();if(!e.targetMesh||!meshes.Add(e.targetMesh)||!AssetDatabase.GetAssetPath(e.targetMesh).Contains("Hybrid EXP1 Template Wires"))throw new Exception("A wire shares its mesh with a template: "+w.name);
 }
 var source=All<MultipleChoiceQuiz>().First(q=>Path(q.transform).Contains("/Middle Panel/Exp 1/")&&q.questions.Count>0);
 foreach(var q in board.steps.Where(s=>s.notesObject&&s.notesObject.GetComponent<MultipleChoiceQuiz>()).Select(s=>s.notesObject.GetComponent<MultipleChoiceQuiz>())){
 foreach(var pair in new[]{((RectTransform)source.transform,(RectTransform)q.transform),((RectTransform)source.transform.parent,(RectTransform)q.transform.parent)})
 if(pair.Item1.anchorMin!=pair.Item2.anchorMin||pair.Item1.anchorMax!=pair.Item2.anchorMax||pair.Item1.pivot!=pair.Item2.pivot||pair.Item1.sizeDelta!=pair.Item2.sizeDelta||Vector3.Distance(pair.Item1.anchoredPosition3D,pair.Item2.anchoredPosition3D)>.001f||pair.Item1.localScale!=pair.Item2.localScale)throw new Exception("Quiz moved away from the original EXP1 layout: "+q.name);
 }
 File.WriteAllText("Logs/HybridRepairBackup/template-validation.txt","PASS: original EXP1 duplicated; 56 guided entries; 36 independent copied wire meshes; "+endpointsChecked+" complete banana/socket meshes with original components; plugs and highlights owned by their endpoints; exact original quiz/parent anchors, positions, scales and dimensions. Original wind EXP1 retained as an inactive reference.");
 }
 [MenuItem("RE009/Show Original-Layout Hybrid Quiz For Review")]
 public static void ShowQuiz(){
 if(!Application.isPlaying)throw new Exception("Run the guided experiment first.");var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
 foreach(var row in board.steps)if(row.uiObject)row.uiObject.SetActive(false);
 var panel=board.steps.First(s=>s.notesObject&&s.notesObject.GetComponent<MultipleChoiceQuiz>()).notesObject;panel.SetActive(true);panel.GetComponent<MultipleChoiceQuiz>().questions[0].questionObject.SetActive(true);
 }
 static Vector3[] SampleWorld(SplineContainer c,Spline spline){
 var knots=spline.Select(k=>c.transform.TransformPoint((Vector3)k.Position)).ToArray();float length=0;for(int i=1;i<knots.Length;i++)length+=Vector3.Distance(knots[i-1],knots[i]);
 int count=Mathf.Clamp(Mathf.CeilToInt(length*1.5f/.1f),40,6000);return Enumerable.Range(0,count+1).Select(i=>c.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(spline,(float)i/count))).ToArray();
 }
 struct SamplePoint{public int wire;public Vector3 position;}
 static Vector3Int Grid(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x/.35f),Mathf.FloorToInt(p.y/.35f),Mathf.FloorToInt(p.z/.35f));
 [MenuItem("RE009/Check Copied EXP1 Wire Crossings")]
 public static void CheckCrossings()=>Crossings(false);
 [MenuItem("RE009/Separate Only Touching Copied EXP1 Wires")]
 public static void SeparateCrossings()=>Crossings(true);
 static void Crossings(bool repair){
 if(Application.isPlaying&&repair)throw new Exception("Stop Play Mode first.");var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var wires=board.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
 var grid=new Dictionary<Vector3Int,List<SamplePoint>>();var anchors=new List<Vector3[]>();var lines=new List<string>();int moved=0,failed=0;
 for(int i=0;i<wires.Length;i++){
 var w=wires[i];var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var basis=c.Spline;var basePoints=SampleWorld(c,basis);var a=basePoints[0];var b=basePoints[basePoints.Length-1];anchors.Add(new[]{a,b});Vector3[] chosenPoints=basePoints;Spline chosen=basis;string hit="";bool clear=false;
 for(int attempt=0;attempt<(repair?13:1);attempt++){
 var candidate=new Spline(basis);if(attempt>0)for(int k=2;k<candidate.Count-2;k++){var knot=candidate[k];knot.Position+=(float3)c.transform.InverseTransformVector(Vector3.back*(attempt*.28f));candidate[k]=knot;}
 var points=attempt==0?basePoints:SampleWorld(c,candidate);bool collided=false;
 foreach(var p in points){if(Vector3.Distance(p,a)<1.2f||Vector3.Distance(p,b)<1.2f)continue;var cell=Grid(p);
 for(int x=-1;x<=1&&!collided;x++)for(int y=-1;y<=1&&!collided;y++)for(int z=-1;z<=1&&!collided;z++)if(grid.TryGetValue(cell+new Vector3Int(x,y,z),out var bucket))foreach(var q in bucket){if(Vector3.Distance(q.position,anchors[q.wire][0])<1.2f||Vector3.Distance(q.position,anchors[q.wire][1])<1.2f)continue;if(Vector3.Distance(p,q.position)<.27f){collided=true;hit=wires[q.wire].name+" near "+p;break;}}
 if(collided)break;
 }
 if(!collided){chosen=candidate;chosenPoints=points;clear=true;if(attempt>0){moved++;lines.Add("ADJUSTED "+w.name+" depth="+(attempt*.28f));}else lines.Add("UNCHANGED "+w.name);break;}
 }
 if(!clear){failed++;lines.Add("REVIEW "+w.name+" vs "+hit);}
 if(repair&&clear&&chosen!=basis){c.Splines=new[]{chosen};var e=c.GetComponent<SplineExtrude>();e.Rebuild();Dirty(c);Dirty(e.targetMesh);}
 foreach(var p in chosenPoints){var cell=Grid(p);if(!grid.TryGetValue(cell,out var bucket))grid[cell]=bucket=new List<SamplePoint>();bucket.Add(new SamplePoint{wire=i,position=p});}
 }
 lines.Insert(0,"Wires="+wires.Length+" adjusted="+moved+" review="+failed+"; source EXP1 cable profile, .27 centerline clearance outside plug assemblies.");File.WriteAllLines("Logs/HybridRepairBackup/copied-wire-crossings.txt",lines);
 if(repair&&moved>0){AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);}
 }
 [MenuItem("RE009/Resolve Remaining Local EXP1 Cable Crossing")]
 public static void ResolveLocalCrossing(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var wires=board.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
 var target=wires.Single(w=>w.name=="Off grid inverter Ac Output N to AC Halogen N 1");var c=target.wireObject.GetComponentInChildren<SplineContainer>(true);var basis=c.Spline;var basePoints=SampleWorld(c,basis);var a=basePoints[0];var b=basePoints[basePoints.Length-1];
 var samples=new Dictionary<Vector3Int,List<(Vector3 p,Vector3 a,Vector3 b)>>();
 foreach(var w in wires.Where(w=>w!=target)){var other=w.wireObject.GetComponentInChildren<SplineContainer>(true);var points=SampleWorld(other,other.Spline);foreach(var p in points){var key=Grid(p);if(!samples.TryGetValue(key,out var bucket))samples[key]=bucket=new List<(Vector3,Vector3,Vector3)>();bucket.Add((p,points[0],points[points.Length-1]));}}
 foreach(var offset in Enumerable.Range(1,12).SelectMany(n=>new[]{Vector3.forward,Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.back}.Select(v=>v*(n*.28f)))){
 var trial=new Spline(basis);for(int k=2;k<trial.Count-2;k++){var knot=trial[k];knot.Position+=(float3)c.transform.InverseTransformVector(offset);trial[k]=knot;}
 bool hit=false;foreach(var p in SampleWorld(c,trial)){if(Vector3.Distance(p,a)<1.2f||Vector3.Distance(p,b)<1.2f)continue;var cell=Grid(p);for(int x=-1;x<=1&&!hit;x++)for(int y=-1;y<=1&&!hit;y++)for(int z=-1;z<=1&&!hit;z++)if(samples.TryGetValue(cell+new Vector3Int(x,y,z),out var bucket))foreach(var q in bucket){if(Vector3.Distance(q.p,q.a)<1.2f||Vector3.Distance(q.p,q.b)<1.2f)continue;if(Vector3.Distance(p,q.p)<.27f){hit=true;break;}}if(hit)break;}
 if(hit)continue;c.Splines=new[]{trial};var e=c.GetComponent<SplineExtrude>();e.Rebuild();Dirty(c);Dirty(e.targetMesh);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);File.WriteAllText("Logs/HybridRepairBackup/local-crossing.txt","PASS: "+target.name+" body offset="+offset+"; plug leads unchanged.");return;
 }
 throw new Exception("Remaining cable crossing requires manual control-point adjustment.");
 }
 static void CopyRect(RectTransform source,RectTransform target){
 target.anchorMin=source.anchorMin;target.anchorMax=source.anchorMax;target.pivot=source.pivot;target.sizeDelta=source.sizeDelta;target.anchoredPosition3D=source.anchoredPosition3D;target.localRotation=source.localRotation;target.localScale=source.localScale;Dirty(target);
 }
 [MenuItem("RE009/Rebuild From Original EXP1 Template")]
 public static void Repair(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode before repairing the saved scene.");
 var oldBoard=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);var source=All<StoryboardSteps>().Single(b=>b.StoryboardID=="Speed Curve Characteristics");
 if(oldBoard.name=="Exp 1")throw new Exception("EXP1 template repair has already been applied.");
 templates=source.steps.Select(s=>s.interaction).OfType<WireInteraction>().Where(w=>w.wireObject&&w.pointA&&w.pointB&&w.plugA&&w.plugB&&w.markerA&&w.markerB).ToArray();
 endpoints=templates.SelectMany(w=>new[]{new EndpointSource{point=w.pointA,plug=w.plugA,marker=w.markerA},new EndpointSource{point=w.pointB,plug=w.plugB,marker=w.markerB}}).ToArray();
 if(templates.Length<9||!new[]{"Red","Black","Yellow","Blue","Comm"}.All(c=>endpoints.Any(e=>ColorOf(e.plug)==c)))throw new Exception("Original EXP1 templates are incomplete.");
 replacements.Clear();var replacementRoot=Object.Instantiate(source.gameObject,source.transform.parent,false);replacementRoot.name="Exp 1 (Repair In Progress)";replacementRoot.SetActive(false);var board=replacementRoot.GetComponent<StoryboardSteps>();
 // Duplicate the original experiment first, retaining its Storyboard/audio/default settings and imported coordinate frame.
 var frame=replacementRoot.transform.Find("exp 1");var copiedSteps=frame.Find("Steps");Object.DestroyImmediate(copiedSteps.gameObject);
 foreach(Transform extra in replacementRoot.transform.Cast<Transform>().Where(t=>t!=frame).ToArray())Object.DestroyImmediate(extra.gameObject);
 var stepRoot=oldBoard.transform.Find("Steps");stepRoot.SetParent(frame,true);
 foreach(var child in oldBoard.transform.Cast<Transform>().ToArray())child.SetParent(replacementRoot.transform,true);
 board.steps=oldBoard.steps;board.freeSteps=oldBoard.freeSteps;
 HybridGuidedCompletion.CopyHybridSelectionFields(oldBoard,board);
 var oldBinding=oldBoard.GetComponent<HybridGuidedBindings>();var binding=replacementRoot.AddComponent<HybridGuidedBindings>();EditorUtility.CopySerialized(oldBinding,binding);
 Map(oldBoard,board);Map(oldBoard.gameObject,replacementRoot);Map(oldBoard.transform,replacementRoot.transform);Map(oldBinding,binding);
 var meshFolder="Assets/_Project/Models/Generated/Hybrid EXP1 Template Wires";if(!AssetDatabase.IsValidFolder(meshFolder))AssetDatabase.CreateFolder("Assets/_Project/Models/Generated","Hybrid EXP1 Template Wires");
 var unusedMeshes=new List<string>();var report=new List<string>();var replacedObjects=new List<GameObject>();
 var counts=new Dictionary<string,int>();
 foreach(var row in board.steps.Where(s=>s.interaction is WireInteraction).ToArray()){
 var prior=(WireInteraction)row.interaction;var donor=MatchWire(prior);var go=Object.Instantiate(donor.gameObject,prior.transform.parent,true);go.name=prior.name;var wire=go.GetComponent<WireInteraction>();
 var discardedA=wire.pointA.gameObject;var discardedB=wire.pointB.gameObject;
 var a=MatchPoint(prior.pointA,ColorOf(prior.plugA));var b=MatchPoint(prior.pointB,ColorOf(prior.plugB));
 wire.pointA=CopyEndpoint(a,prior.pointA,go.transform,out wire.plugA,out wire.markerA);wire.pointB=CopyEndpoint(b,prior.pointB,go.transform,out wire.plugB,out wire.markerB);
 if(discardedA.transform.IsChildOf(go.transform))Object.DestroyImmediate(discardedA);if(discardedB&&discardedB.transform.IsChildOf(go.transform))Object.DestroyImmediate(discardedB);
 foreach(var pair in new[]{(wire.pointA,wire.plugA),(wire.pointB,wire.plugB)}){
 string key=pair.Item1.transform.position.ToString("F3");counts.TryGetValue(key,out int slot);counts[key]=slot+1;
 if(slot>0){var filter=pair.Item2.GetComponentInChildren<MeshFilter>(true);float spacing=filter.transform.TransformVector(Vector3.forward*filter.sharedMesh.bounds.size.z).magnitude*.8f;pair.Item2.transform.position+=pair.Item1.transform.forward*spacing*slot;}
 }
 wire.onCompleted=prior.onCompleted;wire.onUndone=prior.onUndone;wire.onFirstPointClicked=prior.onFirstPointClicked;
 wire.wireObject=Object.Instantiate(donor.wireObject,prior.wireObject.transform.parent,true);wire.wireObject.name=prior.name;
 var extrusion=wire.wireObject.GetComponentInChildren<SplineExtrude>(true);var mesh=new Mesh{name=prior.name};AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(meshFolder+"/"+prior.name.Replace('/','-')+".asset"));extrusion.Container=wire.wireObject.GetComponentInChildren<SplineContainer>(true);extrusion.targetMesh=mesh;
 foreach(var definition in wire.wireObject.GetComponentsInChildren<FreeLabWireDefinition>(true)){definition.endpointA=wire.pointA.GetComponent<FreeLabEndpoint>();definition.endpointB=wire.pointB.GetComponent<FreeLabEndpoint>();definition.plugA=wire.plugA;definition.plugB=wire.plugB;definition.wireParentTemplate=wire.wireObject;}
 RetargetCurve(donor,wire);wire.wireObject.SetActive(false);wire.plugA.SetActive(false);wire.plugB.SetActive(false);
 var previousMesh=prior.wireObject.GetComponentInChildren<SplineExtrude>(true).targetMesh;unusedMeshes.Add(AssetDatabase.GetAssetPath(previousMesh));
 replacedObjects.Add(prior.wireObject);replacedObjects.Add(prior.gameObject);
 Map(prior,wire);Map(prior.gameObject,go);Map(prior.wireObject,wire.wireObject);Map(prior.plugA,wire.plugA);Map(prior.plugB,wire.plugB);Map(prior.markerA,wire.markerA);Map(prior.markerB,wire.markerB);
 row.interaction=wire;report.Add(wire.name+" <- "+Path(donor.transform));
 }
 RemapReferences();
 // Restore the exact original EXP1 parent and child transforms; only quiz content differs.
 var originalQuiz=All<MultipleChoiceQuiz>().First(q=>Path(q.transform).Contains("/Middle Panel/Exp 1/")&&q.questions.Count>0);
 var quizzes=board.steps.Where(s=>s.notesObject&&s.notesObject.GetComponent<MultipleChoiceQuiz>()).Select(s=>s.notesObject.GetComponent<MultipleChoiceQuiz>()).ToArray();
 foreach(var quiz in quizzes){CopyRect((RectTransform)originalQuiz.transform.parent,(RectTransform)quiz.transform.parent);CopyRect((RectTransform)originalQuiz.transform,(RectTransform)quiz.transform);}
 // Remove only the replaced hybrid proxies and their owned meshes, after all references were redirected.
 foreach(var oldObject in replacedObjects.Distinct())if(oldObject&&oldObject.transform.IsChildOf(replacementRoot.transform))Object.DestroyImmediate(oldObject);
 var originals=source.transform.parent.Find("Wind References");if(!originals){originals=new GameObject("Wind References").transform;originals.SetParent(source.transform.parent,false);}source.gameObject.name="Wind EXP1 Reference";source.transform.SetParent(originals,true);source.gameObject.SetActive(false);
 replacementRoot.name="Exp 1";Object.DestroyImmediate(oldBoard.gameObject);
 foreach(var path in unusedMeshes.Distinct().Where(p=>p.StartsWith("Assets/_Project/Models/Generated/Hybrid Wires/")))if(!All<SplineExtrude>().Any(e=>AssetDatabase.GetAssetPath(e.targetMesh)==path))AssetDatabase.DeleteAsset(path);
 Dirty(board);Dirty(binding);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
 File.WriteAllLines("Logs/HybridRepairBackup/rebuilt-from-exp1.txt",report);Selection.activeGameObject=replacementRoot;EditorGUIUtility.PingObject(replacementRoot);
 }
 [MenuItem("RE009/Inspect EXP1 Template Before Repair")]
 public static void Inspect(){
 var output=new StringBuilder();
 foreach(var b in All<StoryboardSteps>().Where(b=>b.name=="Exp 1"||b.StoryboardID==HybridExperimentSetup.Id)){
 output.AppendLine("BOARD "+Path(b.transform)+" ID="+b.StoryboardID+" rows="+b.steps.Count);
 foreach(var t in b.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==b.transform||t.name=="Steps"||t.name=="Step Objects"))output.AppendLine("FOLDER "+Path(t)+" active="+t.gameObject.activeSelf+" world="+t.position+" scale="+t.lossyScale+" rotation="+t.eulerAngles);
 foreach(var w in b.steps.Select(s=>s.interaction).OfType<WireInteraction>()){
 output.AppendLine("WIRE "+Path(w.transform));
 foreach(var end in new[]{(w.pointA,w.plugA),(w.pointB,w.plugB)}){
 var p=end.Item1;var plug=end.Item2;var mesh=p.GetComponent<MeshFilter>();output.AppendLine("POINT "+p.name+" position="+p.transform.position+" rotation="+p.transform.eulerAngles+" scale="+p.transform.lossyScale+" components="+string.Join(",",p.GetComponents<Component>().Select(c=>c.GetType().Name))+" mesh="+(mesh&&mesh.sharedMesh?mesh.sharedMesh.name:"NONE")+" plugLocal="+plug.transform.localPosition+" plugRotation="+plug.transform.localEulerAngles+" plugScale="+plug.transform.localScale);
 }
 var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var e=c.GetComponent<SplineExtrude>();output.AppendLine("CURVE scale="+c.transform.lossyScale+" radius="+e.Radius+" knots="+c.Spline.Count+" materials="+string.Join(",",c.GetComponent<Renderer>().sharedMaterials.Select(m=>m.name)));
 foreach(var k in c.Spline)output.AppendLine("KNOT "+c.transform.TransformPoint((Vector3)k.Position)+" in="+k.TangentIn+" out="+k.TangentOut+" mode="+c.Spline.GetTangentMode(c.Spline.ToList().IndexOf(k)));
 }
 }
 foreach(var q in All<MultipleChoiceQuiz>().Where(q=>Path(q.transform).Contains("/Middle Panel/Exp 1/")||Path(q.transform).Contains("/Hybrid Guided Popups/"))){
 output.AppendLine("QUIZ "+Path(q.transform)+" questions="+q.questions.Count);
 for(var t=q.transform;t&&t.name!="Main UI";t=t.parent){var r=t as RectTransform;output.AppendLine("QUIZFRAME "+Path(t)+" local="+t.localPosition+" scale="+t.localScale+" rotation="+t.localEulerAngles+(r?" anchors="+r.anchorMin+"/"+r.anchorMax+" pivot="+r.pivot+" size="+r.sizeDelta+" anchored="+r.anchoredPosition3D:""));}
 }
 var f=All<FreeLabWindController>().Single();foreach(var socket in f.sockets.Where(s=>s.plugs.Length>1).Take(8))output.AppendLine("STACK "+socket.endpoint.name+" pos="+socket.endpoint.transform.position+" plugs="+string.Join(";",socket.plugs.Where(p=>p).Select(p=>p.transform.position+" localFromPin="+socket.endpoint.transform.InverseTransformPoint(p.transform.position))));
 Directory.CreateDirectory("Logs/HybridRepairBackup");File.WriteAllText("Logs/HybridRepairBackup/template-audit.txt",output.ToString());
 }
}
