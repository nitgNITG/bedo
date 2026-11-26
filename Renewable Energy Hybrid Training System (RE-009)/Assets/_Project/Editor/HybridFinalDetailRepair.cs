using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.Mathematics;
using TutorialFramework;
public static class HybridFinalDetailRepair {
static T[] All<T>() where T:Component=>HybridGuidedCompletion.All<T>();
static string P(Transform t)=>HybridGuidedCompletion.Path(t);
[MenuItem("RE009/Test Applicable Wind Checklist Regressions")]
static void Checklist(){
if(!Application.isPlaying)throw new Exception("Run the guided sequence first.");
var lines=new List<string>();void Check(bool pass,string item){lines.Add((pass?"PASS: ":"FAIL: ")+item);}
var sys=All<HybridTrainingController>().Single(s=>s.isActiveAndEnabled);var board=sys.storyboard;var bind=board.GetComponent<HybridGuidedBindings>();
var transparency=All<WireMaterialTransparencyToggle>().First(t=>t.isActiveAndEnabled);transparency.SetTransparent(true);
foreach(var w in board.steps.Select(s=>s.interaction).OfType<WireInteraction>()){
var r=w.wireObject.GetComponentInChildren<SplineExtrude>(true).GetComponent<Renderer>();Check(r.sharedMaterials.All(m=>m&&m.renderQueue==3000&&(!m.HasProperty("_Color")||m.color.a<.95f)),"5 wire transparency: "+w.name);
Check(new[]{w.markerA,w.markerB}.All(m=>m&&!m.GetComponent<BopTarget>()),"48 pin highlighting uses endpoint indicators: "+w.name);
}transparency.SetTransparent(false);
var controls=board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Where(c=>c.marker&&c.marker.GetComponent<BopTarget>()).ToArray();
Check(controls.All(c=>Vector3.Dot(c.marker.transform.parent.up,Vector3.up)>.9999f),"49/55 bobbing arrows have vertical animation frames");
Check(controls.All(c=>!c.marker.activeSelf),"49/55 completed controls leave no floating arrows");
var before=All<PanelToggleButton>().Where(c=>c.gameObject.activeInHierarchy&&new SerializedObject(c).FindProperty("exclusivePeers").arraySize>=2).ToArray();var panels=before.Select(b=>(GameObject)new SerializedObject(b).FindProperty("panel").objectReferenceValue).ToArray();var states=panels.Select(p=>p&&p.activeSelf).ToArray();foreach(var b in before){b.ClosePanel();b.TogglePanel();Check(panels.Count(p=>p&&p.activeSelf)==1,"24 only one bottom panel opens: "+b.name);}for(int i=0;i<panels.Length;i++)if(panels[i])panels[i].SetActive(states[i]);Check(before.Length>=3,"24 bottom panels have exclusive peer assignments");
var texts=bind.subtitleGroup.GetComponentsInChildren<AutoSizeSubtitleScrollContent>(true).Concat(bind.notesGroup.GetComponentsInChildren<AutoSizeSubtitleScrollContent>(true)).Distinct().ToArray();Check(texts.Length>=24,"46/54 hybrid subtitle scrolling components exist");foreach(var text in texts){var so=new SerializedObject(text);Check(so.FindProperty("measureAtCurrentFontSize").boolValue,"46/54 measures current font size: "+text.transform.parent.parent.name);var scroll=text.GetComponent<ScrollRect>();if(scroll.verticalScrollbar){var bar=(RectTransform)scroll.verticalScrollbar.transform;Check(bar.anchorMin.x>=.5f,"54 scrollbar is on right: "+text.transform.parent.parent.name);}}
Check(sys.recordGraphMeasurements&&!bind.graphPage.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Unused Reading Area Background"),"28/58 current request: live original graph and three readout columns, no cover");
Check(sys.storyboard.steps.Where(s=>s.notesObject&&s.notesObject.name.StartsWith("Notification After Step")).All(s=>s.uiObject),"44 notifications attached to their instruction panels");
Check(All<ScrollRect>().Where(s=>P(s.transform).EndsWith("/Chapter List")).All(s=>!s.horizontal&&!s.horizontalScrollbar),"23 Hybrid Lab single-experiment scrolling disabled");
Check(All<AudioSource>().Any(a=>a.isActiveAndEnabled&&a.loop&&a.clip&&a.clip.name.ToLowerInvariant().Contains("wind")),"33 running turbine audio is looped");
lines.Add("NOT CERTIFIED: Windows player save dialogs, first-load timing, perceptual screen tearing, every aspect ratio and both languages require a built-player/device review.");
lines.Add("EXCLUDED: Free Mode is deferred. Wind-only rheostat/current-graph requirements are superseded by the hybrid storyboard and the explicit empty-graph instruction. Final monitor artwork and replacement PDF source quality remain external dependencies.");
File.WriteAllLines("Logs/HybridReview/checklist-regressions.txt",lines);Debug.Log("RE009 checklist regression audit: "+lines.Count(x=>x.StartsWith("FAIL"))+" failed checks.");
}
[MenuItem("RE009/Add Enable Bobbing Arrow")]
static void EnableArrow(){
if(Application.isPlaying)throw new Exception("Stop Play first.");
var system=All<HybridTrainingController>().Single();var board=system.storyboard;
var enable=system.monitor.GetComponentInChildren<HybridEnableSwitch>(true);
var source=board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().First(c=>c.marker&&c.marker.GetComponent<BopTarget>());
var holder=board.transform.Find("Bobbing Control Pointers");
if(enable.instruction.marker)UnityEngine.Object.DestroyImmediate(enable.instruction.marker);
var marker=UnityEngine.Object.Instantiate(source.marker,holder,true);marker.name="Enable PV Output Pointer";
var buttonBounds=enable.GetComponent<Renderer>().bounds;
var arrow=marker.GetComponentsInChildren<Renderer>(true);var bounds=arrow[0].bounds;foreach(var r in arrow.Skip(1))bounds.Encapsulate(r.bounds);
marker.transform.position+=new Vector3(buttonBounds.center.x-bounds.center.x,buttonBounds.max.y+.65f-bounds.min.y,buttonBounds.center.z-.4f-bounds.center.z);
marker.SetActive(false);enable.instruction.marker=marker;EditorUtility.SetDirty(enable.instruction);
EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
}
[MenuItem("RE009/Repair Main Menu Experiment Scrollbar")]
static void MainScroll(){var original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");try{File.WriteAllLines("Logs/HybridReview/main-scroll.txt",All<ScrollRect>().Select(s=>P(s.transform)+" content="+(s.content?P(s.content):"none")+" scrollbar="+(s.horizontalScrollbar?s.horizontalScrollbar.name:"none")).Concat(All<Slider>().Select(s=>"SLIDER "+P(s.transform))));DisableScroll();}finally{EditorSceneManager.OpenScene(original);}}
[MenuItem("RE009/Preview Selector And Ethernet Details")]
static void Preview(){var board=All<HybridTrainingController>().Single().storyboard;var selector=board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Single(c=>c.name=="PV Run Selector");var camGo=new GameObject("Temporary detail preview");camGo.hideFlags=HideFlags.HideAndDontSave;var camera=camGo.AddComponent<Camera>();camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;var rt=new RenderTexture(1000,800,24);camera.targetTexture=rt;
void Capture(string name,Vector3 center,Vector3 normal,float size){camera.transform.position=center+normal*18;camera.transform.rotation=Quaternion.LookRotation(-normal,Vector3.up);camera.orthographicSize=size;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1000,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,800),0,0);tex.Apply();File.WriteAllBytes("Logs/HybridReview/"+name+".png",tex.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);}
var changed=new Dictionary<GameObject,bool>();void Show(Transform t){for(;t;t=t.parent){if(!changed.ContainsKey(t.gameObject))changed.Add(t.gameObject,t.gameObject.activeSelf);t.gameObject.SetActive(true);}}
Show(selector.transform);
try{Capture("pv-selector-manual",selector.transform.position,new Vector3(0,.73135f,-.681998f),4);var animator=selector.animator;var saved=selector.transform.localRotation;animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="Manual To Remote").SampleAnimation(selector.gameObject,.4f);Capture("pv-selector-remote",selector.transform.position,new Vector3(0,.73135f,-.681998f),4);selector.transform.localRotation=saved;foreach(var w in board.steps.Select(s=>s.interaction).OfType<WireInteraction>().Where(w=>w.name.Contains("Comm"))){foreach(var p in new[]{w.plugA,w.plugB}){var objects=new[]{p,w.wireObject};var states=objects.Select(g=>g.activeSelf).ToArray();foreach(var g in objects)Show(g.transform);var center=p.GetComponentsInChildren<Renderer>(true).First().bounds.center;Capture("ethernet-"+w.transform.parent.name+"-"+(p==w.plugA?"a":"b"),center,Vector3.back,2);for(int i=0;i<objects.Length;i++)objects[i].SetActive(states[i]);}}}finally{foreach(var kv in changed)if(kv.Key)kv.Key.SetActive(kv.Value);camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camGo);}}
[MenuItem("RE009/Repair Ethernet Arrows Selector And Single Experiment Scroll")]
static void Repair(){
if(Application.isPlaying)throw new Exception("Stop Play first.");
var sys=All<HybridTrainingController>().Single();var board=sys.storyboard;var bind=board.GetComponent<HybridGuidedBindings>();
foreach(var t in bind.graphPage.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Unused Reading Area Background").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
foreach(var r in bind.graphReadouts)if(r)r.SetActive(false);
// These arrows are visual markers, not wire endpoint indicators. A level frame
// lets the original BopTarget animate local Y along world up.
var holder=board.transform.Find("Bobbing Control Pointers");if(!holder){holder=new GameObject("Bobbing Control Pointers").transform;holder.SetParent(board.transform,false);}holder.rotation=Quaternion.identity;
foreach(var c in board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Distinct().Where(c=>c.marker&&c.marker.GetComponentInChildren<BopTarget>(true))){var m=c.marker.transform;m.SetParent(holder,true);var renderers=c.GetComponentsInChildren<Renderer>(true);var bounds=renderers.Length>0?renderers[0].bounds:new Bounds(c.transform.position,Vector3.one);foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);var markerBounds=m.GetComponentsInChildren<Renderer>(true).Select(r=>r.bounds).ToArray();if(markerBounds.Length>0){float bottom=markerBounds.Min(b=>b.min.y);m.position+=Vector3.up*(bounds.max.y+.65f-bottom);}m.gameObject.SetActive(false);EditorUtility.SetDirty(m);}
foreach(var w in board.steps.Select(s=>s.interaction).OfType<WireInteraction>().Where(w=>w.name.Contains("Comm"))){var container=w.wireObject.GetComponentInChildren<SplineContainer>(true);var spline=new Spline(container.Spline);var first=container.transform.TransformPoint((Vector3)spline[0].Position);var plugs=new[]{w.plugA,w.plugB}.OrderBy(p=>Vector3.Distance(first,p.GetComponentsInChildren<Renderer>(true).First().bounds.center)).ToArray();
for(int e=0;e<2;e++){var plug=plugs[e];var tail=plug.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(f=>f.sharedMesh.bounds.size.z).First();var b=tail.sharedMesh.bounds;var p=b.center;p.z=b.max.z;var exit=tail.transform.TransformPoint(p);var direction=tail.transform.TransformDirection(Vector3.forward).normalized;int end=e==0?0:spline.Count-1;int adjacent=e==0?1:spline.Count-2;foreach(var pair in new[]{(end,exit),(adjacent,exit+direction*.3f)}){var k=spline[pair.Item1];k.Position=(float3)container.transform.InverseTransformPoint(pair.Item2);k.Rotation=quaternion.identity;var tangent=container.transform.InverseTransformVector(direction*(e==0?.1f:-.1f));k.TangentIn=(float3)(-tangent);k.TangentOut=(float3)tangent;spline[pair.Item1]=k;}}
container.Splines=new[]{spline};var extrude=container.GetComponent<SplineExtrude>();extrude.Rebuild();EditorUtility.SetDirty(container);EditorUtility.SetDirty(extrude.targetMesh);}
var selector=board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Single(c=>c.name=="PV Run Selector");
var manual=Quaternion.Euler(313,0,180)*Quaternion.AngleAxis(180,Vector3.up);selector.transform.rotation=manual;var start=selector.transform.localEulerAngles;var remote=Quaternion.Inverse(selector.transform.parent.rotation)*(manual*Quaternion.AngleAxis(-45,Vector3.up));var finish=remote.eulerAngles;for(int i=0;i<3;i++)finish[i]=start[i]+Mathf.DeltaAngle(start[i],finish[i]);
const string folder="Assets/_Project/Animations/Hybrid PV Selector";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
AnimationClip Clip(string name,Vector3 a,Vector3 b,float duration){var path=folder+"/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}for(int i=0;i<3;i++)clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw."+"xyz"[i],AnimationCurve.EaseInOut(0,a[i],duration,b[i]));EditorUtility.SetDirty(clip);return clip;}
var idle=Clip("Manual",start,start,.5f);var move=Clip("Manual To Remote",start,finish,.4f);var cp=folder+"/PV Run Selector.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);if(!controller){controller=AnimatorController.CreateAnimatorControllerAtPath(cp);var sm=controller.layers[0].stateMachine;var a=sm.AddState("Manual");a.motion=idle;sm.defaultState=a;sm.AddState("SwitchAnim").motion=move;}selector.animator.runtimeAnimatorController=controller;selector.animationName="SwitchAnim";EditorUtility.SetDirty(selector);EditorUtility.SetDirty(selector.animator);EditorUtility.SetDirty(selector.transform);
DisableScroll();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);AssetDatabase.SaveAssets();}
[MenuItem("RE009/Disable Single Experiment List Scrollbar")]
static void DisableScroll(){foreach(var scroll in All<ScrollRect>().Where(s=>s.content&&(P(s.transform).Contains("/Experiments/")&&s.name=="Chapter List"||s.content.GetComponentsInChildren<MainMenuExperimentLauncher>(true).Length>0))){if(scroll.horizontalScrollbar)scroll.horizontalScrollbar.gameObject.SetActive(false);scroll.horizontalScrollbar=null;scroll.horizontal=false;scroll.vertical=false;scroll.velocity=Vector2.zero;EditorUtility.SetDirty(scroll);EditorSceneManager.MarkSceneDirty(scroll.gameObject.scene);}EditorSceneManager.SaveOpenScenes();}
[MenuItem("RE009/Audit Ethernet Pointers Selector And Scrollbars")]
static void Audit(){var log=new List<string>();var board=All<HybridTrainingController>().Single().storyboard;
foreach(var c in board.steps.Select(s=>s.interaction).OfType<SingleClickInteraction>().Distinct()){log.Add("CONTROL "+P(c.transform)+" rot="+c.transform.eulerAngles+" local="+c.transform.localEulerAngles+" animator="+(c.animator?AssetDatabase.GetAssetPath(c.animator.runtimeAnimatorController):"none"));if(c.marker){log.Add("MARKER "+P(c.marker.transform)+" pos="+c.marker.transform.position+" rot="+c.marker.transform.eulerAngles+" scale="+c.marker.transform.lossyScale);foreach(var b in c.marker.GetComponentsInChildren<BopTarget>(true))log.Add("BOP "+P(b.transform)+" parent rot="+b.transform.parent.eulerAngles);}}
foreach(var w in All<WireInteraction>().Where(w=>w.GetComponentInParent<StoryboardSteps>()==board&&w.name.Contains("Comm")&&w.plugA&&w.plugB&&w.wireObject)){log.Add("WIRE "+P(w.transform));foreach(var p in new[]{w.plugA,w.plugB}){log.Add("PLUG "+p.name+" pos="+p.transform.position+" rot="+p.transform.eulerAngles);foreach(var f in p.GetComponentsInChildren<MeshFilter>(true))log.Add(" MESH active="+f.gameObject.activeSelf+" enabled="+f.GetComponent<Renderer>().enabled+" localpos="+f.transform.localPosition+" localrot="+f.transform.localEulerAngles+" "+f.sharedMesh.name+" localbounds="+f.sharedMesh.bounds+" world="+f.GetComponent<Renderer>().bounds);}var s=w.wireObject.GetComponentInChildren<SplineContainer>(true);if(s)log.Add("ENDS "+s.transform.TransformPoint((Vector3)s.Spline[0].Position)+" / "+s.transform.TransformPoint((Vector3)s.Spline[s.Spline.Count-1].Position));}
foreach(var b in All<Scrollbar>().Where(b=>P(b.transform).EndsWith("Chapter List/Scrollbar")))log.Add("SCROLL "+P(b.transform)+" active="+b.gameObject.activeSelf);
foreach(var a in All<Animator>().Where(a=>a.name.Contains("Pump")||a.name.Contains("Run Selector")))log.Add("SELECTOR TEMPLATE "+P(a.transform)+" rot="+a.transform.eulerAngles+" controller="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController));
File.WriteAllLines("Logs/HybridReview/detail-audit.txt",log);}
}

