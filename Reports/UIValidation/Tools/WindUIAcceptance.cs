using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TutorialFramework;

[InitializeOnLoad]
public static class WindUIAcceptance
{
 const string Prefix="WindUIAcceptance.";
 static int Stage {get=>SessionState.GetInt(Prefix+"Stage",-1);set=>SessionState.SetInt(Prefix+"Stage",value);}
 static int Index {get=>SessionState.GetInt(Prefix+"Index",0);set=>SessionState.SetInt(Prefix+"Index",value);}
 static double Due {get=>SessionState.GetFloat(Prefix+"Due",0);set=>SessionState.SetFloat(Prefix+"Due",(float)value);}
 static readonly int[] Width={1280,1600,1920,2560,3840};
 static readonly int[] Height={720,900,1080,1440,2160};
 const string Report="Reports/UIValidation/Runtime.txt";
 static WindUIAcceptance(){EditorApplication.update+=Tick;}
 static void Tick()
 {
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<Due)return;
  try{
   if(Stage<0){if(!File.Exists("Temp/ui-validation.flag")||EditorApplication.isPlayingOrWillChangePlaymode)return;
    File.Delete("Temp/ui-validation.flag");Directory.CreateDirectory("Reports/UIValidation/Screenshots");File.WriteAllText(Report,"UI resolution diagnostic; current views are evidence, not approved baselines.\n");
    if(SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene; refusing to switch");
    EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");
    SessionState.SetInt(Prefix+"OriginalSize",SelectedSize());Index=0;Stage=0;EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+8;return;}
   if(Stage==0){SetSize(Index);Stage=1;Due=EditorApplication.timeSinceStartup+3;return;}
   if(Stage==1){Capture("MainMenu");Index++;Due=EditorApplication.timeSinceStartup+2;if(Index<5){Stage=0;return;}Stage=11;return;}
   if(Stage==11){
    var launch=Resources.FindObjectsOfTypeAll<MainMenuExperimentLauncher>().Single(l=>l.gameObject.scene==SceneManager.GetActiveScene() && new SerializedObject(l).FindProperty("storyboardId").stringValue=="Speed Curve Characteristics");InteractionManager.Instance.currentMode=ExperimentMode.Guided;launch.GetComponent<Button>().onClick.Invoke();Index=0;Stage=2;Due=EditorApplication.timeSinceStartup+8;return;}
   if(Stage==2){SetSize(Index);Stage=3;Due=EditorApplication.timeSinceStartup+3;return;}
   if(Stage==3){Capture("PVLab");Index++;Due=EditorApplication.timeSinceStartup+2;if(Index<5){Stage=2;return;}Stage=12;return;}
   if(Stage==12){AuditGraph();Stage=4;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+3;return;}
   if(Stage==4){if(EditorApplication.isPlayingOrWillChangePlaymode)return;SetIndex(SessionState.GetInt(Prefix+"OriginalSize",0));EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");File.AppendAllText(Report,"FINISHED; scenes restored; browser zoom and human visual approval remain pending.\n");Stage=-1;}
  }catch(Exception e){File.AppendAllText(Report,"FAIL "+e+"\n");Stage=4;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+3;}
 }
 static Type GV=>typeof(Editor).Assembly.GetType("UnityEditor.GameView");
 static EditorWindow View=>EditorWindow.GetWindow(GV);
 static int SelectedSize()=>(int)GV.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(View);
 static void SetIndex(int i){GV.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(View,i);View.Repaint();}
 static void SetSize(int i){var a=typeof(Editor).Assembly;var type=a.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);var sizes=singleton.GetProperty("instance").GetValue(null);
  var groupType=type.GetProperty("currentGroupType").GetValue(sizes);var group=type.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
  var sizeType=a.GetType("UnityEditor.GameViewSize");var enumType=a.GetType("UnityEditor.GameViewSizeType");
  var size=Activator.CreateInstance(sizeType,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{Enum.ToObject(enumType,1),Width[i],Height[i],"BEDO QA "+Width[i]+"x"+Height[i]},null);
  int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});SetIndex(count);
 }
 static void Capture(string label){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("Reports/UIValidation/Screenshots/"+label+"-"+Width[Index]+"x"+Height[Index]+".png");
  File.AppendAllText(Report,label+" requested "+Width[Index]+"x"+Height[Index]+" actual "+Screen.width+"x"+Screen.height+"\n");
  foreach(var scaler in Resources.FindObjectsOfTypeAll<CanvasScaler>().Where(c=>c.gameObject.scene==SceneManager.GetActiveScene())){
   var canvas=scaler.GetComponent<Canvas>();if(!canvas||canvas.renderMode==RenderMode.WorldSpace)continue;
   bool matches=scaler.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize&&scaler.referenceResolution==new Vector2(1600,900)&&Mathf.Approximately(scaler.matchWidthOrHeight,.5f);
   File.AppendAllText(Report,(matches?"PASS":"REVIEW")+" scaler "+EquipmentPrefabWorkflow.PathOf(scaler.transform)+" mode="+scaler.uiScaleMode+" reference="+scaler.referenceResolution+" match="+scaler.matchWidthOrHeight+"\n");
  }
 }
 static void AuditGraph(){
  var scene=SceneManager.GetActiveScene();var board=Resources.FindObjectsOfTypeAll<StoryboardSteps>().First(b=>b.gameObject.scene==scene&&b.gameObject.activeInHierarchy&&b.StoryboardID!="Storyboard");
  var speed=board.GetComponentsInChildren<WindTurbineSpeedController>(true).Single(s=>s.HasWindModel);
  var graph=Resources.FindObjectsOfTypeAll<PVGraphController>().First(g=>g.gameObject.scene==scene&&g.IsWindExperimentActive);
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;graph.ClearRuntimeGraph();speed.TurnOn();speed.SetSpeed(400);graph.UpdateWindMeasurement();graph.UpdateWindMeasurement();
  var xs=(System.Collections.IList)typeof(PVGraphController).GetField("plottedXValues",flags).GetValue(graph);var ys=(System.Collections.IList)typeof(PVGraphController).GetField("plottedYValues",flags).GetValue(graph);
  if(xs.Count!=1||ys.Count!=1)throw new Exception("Duplicate graph rows");speed.TryGetReading(out var reading);if(!Mathf.Approximately((float)xs[0],reading.rpm)||!Mathf.Approximately((float)ys[0],reading.dcVoltage))throw new Exception("Graph/meter model disagreement");
  var exporter=(PVExperimentExporter)typeof(PVGraphController).GetField("exporter",flags).GetValue(graph);var csv=exporter.BuildWindCSV();if(csv.Trim().Split('\n').Length!=2||csv.Split('\n')[0].Trim()!="Speed (RPM),DC voltage (V)")throw new Exception("Export schema/row mismatch");
  File.WriteAllText("Reports/UIValidation/GraphSample.csv",csv);graph.ClearRuntimeGraph();if(xs.Count!=0||ys.Count!=0||exporter.BuildWindCSV().Trim().Split('\n').Length!=1)throw new Exception("Clear did not clear graph/export");
  File.AppendAllText(Report,"PASS live graph/rows use current RPM/DC voltage; repeated sample deduplicated; CSV has exactly two columns; Clear clears graph and export.\n");
 }
}
