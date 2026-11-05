using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TutorialFramework;
[InitializeOnLoad] public static class GuidedWiringGuardRegression
{
 const string Prefix="GuidedWireGuard.";const string Report="Reports/GuidedWiringGuard/runtime.txt";
 static readonly string[] Ids={"Speed Curve Characteristics","Regulating DC Output from Wind Turbine using PWM Charger","Regulating DC Output from Wind Turbine using MPPT Charger","OFF Grid System Using Wind Turbine"};
 static int Stage{get=>SessionState.GetInt(Prefix+"stage",-1);set=>SessionState.SetInt(Prefix+"stage",value);}
 static int Index{get=>SessionState.GetInt(Prefix+"index",0);set=>SessionState.SetInt(Prefix+"index",value);}
 static double Due{get=>SessionState.GetFloat(Prefix+"due",0);set=>SessionState.SetFloat(Prefix+"due",(float)value);}
 static GuidedWiringGuardRegression(){EditorApplication.update+=Tick;}
 static void Log(string s)=>File.AppendAllText(Report,s+"\n");
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static T[] All<T>()where T:Component=>Resources.FindObjectsOfTypeAll<T>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()).ToArray();
 static void Tick(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(Stage<0&&File.Exists("Temp/guided-wire-guard.flag")){
  if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
  File.Delete("Temp/guided-wire-guard.flag");if(EditorSceneManager.GetActiveScene().isDirty){Log("BLOCKED unsaved scene");return;}
  File.WriteAllText(Report,"Guided wire permission / Free Mode regression\n");Index=0;Stage=0;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+6;return;
 }
 if(Stage<0||EditorApplication.timeSinceStartup<Due)return;
 try{
  if(Stage==4){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");Stage=-1;Log("COMPLETE; no scene assets saved");return;}
  if(Stage==3){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");Stage=0;EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+6;return;}
  if(!EditorApplication.isPlaying||!InteractionManager.Instance)return;
  var manager=InteractionManager.Instance;
  if(Stage==0){Require(SceneManager.GetActiveScene().name=="Main Menu","Expected menu");var launch=All<MainMenuExperimentLauncher>().Single(l=>new SerializedObject(l).FindProperty("storyboardId").stringValue==Ids[Index]);manager.currentMode=ExperimentMode.Guided;launch.GetComponent<Button>().onClick.Invoke();Stage=1;Due=EditorApplication.timeSinceStartup+8;return;}
  if(Stage==1){var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==Ids[Index]);CheckGuided(board);manager.SetFree();Stage=2;Due=EditorApplication.timeSinceStartup+8;return;}
  if(Stage==2){CheckFree();Index++;Stage=Index==4?4:3;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}
 }catch(Exception e){Log("FAIL EXP"+(Index+1)+": "+e);Stage=4;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}
 }
 static void CheckGuided(StoryboardSteps board){
  Require(InteractionManager.Instance.Guided,"Expected Guided mode");
  var serialized=new SerializedObject(board);serialized.FindProperty("delayBetweenSteps").floatValue=10;serialized.ApplyModifiedPropertiesWithoutUndo();
  var wires=board.steps.Select((s,i)=>new{step=s,index=i,wire=s.interaction as WireInteraction}).Where(x=>x.wire).ToArray();int done=0;
  Require(wires.Length>0,"No guided wires");
  foreach(var entry in wires){
   board.StopAllCoroutines();board.GoToStep(entry.index);var w=entry.wire;
   Require(w.pointA&&w.pointB&&w.pointA.GetComponentInParent<WireInteraction>()==w&&w.pointB.GetComponentInParent<WireInteraction>()==w,"Endpoint ownership changed: "+w.name);
   int clicks=0,first=0,completed=0,undone=0;
   UnityEngine.Events.UnityAction onClick=()=>clicks++, onFirst=()=>first++,onComplete=()=>completed++,onUndo=()=>undone++;
   w.pointA.onClicked.AddListener(onClick);w.pointB.onClicked.AddListener(onClick);w.onFirstPointClicked.AddListener(onFirst);w.onCompleted.AddListener(onComplete);w.onUndone.AddListener(onUndo);
   // Stale Allow flags must not authorize a future wire or the second endpoint first.
   var other=wires.FirstOrDefault(x=>x.wire!=w)?.wire;
   if(other){int wrong=0;UnityEngine.Events.UnityAction listener=()=>wrong++;other.pointA.onClicked.AddListener(listener);other.pointA.Enable();other.pointA.Click();other.PointClicked(other.pointA);InteractionManager.Instance.ProcessInteraction(other.pointA);Require(wrong==0,"Wrong endpoint emitted click events: "+other.name);other.pointA.onClicked.RemoveListener(listener);other.pointA.Disable();}
   w.pointB.Enable();w.pointB.Click();w.PointClicked(w.pointB);Require(clicks==0&&first==0&&!w.IsCompleted,"Second endpoint accepted before first: "+w.name);w.pointB.Disable();
   w.pointA.Click();Require(first==1&&clicks==1&&w.pointB.Allow,"Correct first endpoint blocked: "+w.name+" active="+w.isActiveAndEnabled+" A="+w.pointA.isActiveAndEnabled);
   w.pointA.Enable();w.pointA.Click();Require(first==1&&clicks==1,"Repeated first endpoint accepted: "+w.name);w.pointA.Disable();
   w.pointB.Click();Require(w.IsCompleted&&completed==1&&clicks==2,"Correct second endpoint blocked: "+w.name);Require(w.wireObject.activeSelf&&w.plugA.activeSelf&&w.plugB.activeSelf,"Connection visuals missing: "+w.name);Require(!w.pointA.Allow&&!w.pointB.Allow,"Completed guided endpoints left enabled");
   w.pointA.Enable();w.pointB.Enable();w.pointA.Click();w.PointClicked(w.pointB);InteractionManager.Instance.ProcessInteraction(w.pointA);Require(w.IsCompleted&&w.wireObject.activeSelf&&completed==1&&undone==0&&clicks==2,"Guided wire was removed or repeated");
   board.StopAllCoroutines();board.GoToStep(entry.index+1);
   Require(w.wireObject.activeSelf&&w.plugA.activeSelf&&w.plugB.activeSelf&&!w.pointA.Allow&&!w.pointB.Allow,"Advancing changed the physical connection");
   w.pointA.Enable();w.pointA.Click();w.PointClicked(w.pointA);Require(w.wireObject.activeSelf&&clicks==2&&first==1&&undone==0,"Previous endpoint accepted after step advance");w.pointA.Disable();
   w.pointA.onClicked.RemoveListener(onClick);w.pointB.onClicked.RemoveListener(onClick);w.onFirstPointClicked.RemoveListener(onFirst);w.onCompleted.RemoveListener(onComplete);w.onUndone.RemoveListener(onUndo);done++;
  }
  board.StopAllCoroutines();Log("PASS EXP"+(Index+1)+": "+done+" guided wire steps; wrong/future/repeated/out-of-order clicks blocked before callbacks; both legitimate clicks work; removal blocked before and after advancing; plugs/wires preserved.");
 }
 static void CheckFree(){
  Require(InteractionManager.Instance.Free,"Expected Free mode");var wind=All<FreeLabWindController>().Single();Require(wind.IsReady,"Free lab not ready");
  var definition=wind.definitions.First(d=>d&&d.endpointA&&d.endpointB&&d.endpointA.Interaction&&d.endpointB.Interaction);
  int before=wind.lab.ConnectionCount;definition.endpointA.Interaction.Click();definition.endpointB.Interaction.Click();Require(wind.lab.ConnectionCount==before+1,"Free endpoint clicks failed");
  var connection=wind.lab.Connections.Single(w=>(w.EndpointA==definition.endpointA&&w.EndpointB==definition.endpointB)||(w.EndpointB==definition.endpointA&&w.EndpointA==definition.endpointB));
  Require(connection.RemoveFromFreeLab()&&wind.lab.ConnectionCount==before,"Free wire removal failed");
  Log("PASS EXP"+(Index+1)+": Free Mode connects through normal banana clicks and removes through the existing wire click handler.");
 }
}
