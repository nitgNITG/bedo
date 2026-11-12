using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TutorialFramework;
[InitializeOnLoad]
public static class WindLocalizationRegression
{
 const string Prefix="WindLocalizationRegression.";
 static int Stage {get=>SessionState.GetInt(Prefix+"stage",-1);set=>SessionState.SetInt(Prefix+"stage",value);}
 static double Due {get=>SessionState.GetFloat(Prefix+"due",0);set=>SessionState.SetFloat(Prefix+"due",(float)value);}
 static int Index {get=>SessionState.GetInt(Prefix+"index",0);set=>SessionState.SetInt(Prefix+"index",value);}
 static string Report="Temp/Localization/runtime.txt";
 static readonly string[] Ids={"Speed Curve Characteristics","Regulating DC Output from Wind Turbine using PWM Charger","Regulating DC Output from Wind Turbine using MPPT Charger","OFF Grid System Using Wind Turbine"};
 static WindLocalizationRegression(){EditorApplication.update+=Tick;}
 static void Log(string s)=>File.AppendAllText(Report,s+"\n");
 static void Tick(){
  if(File.Exists("Temp/Localization/stop.flag") && !EditorApplication.isCompiling){File.Delete("Temp/Localization/stop.flag");Stage=6;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;return;}

  if(File.Exists("Temp/Localization/visual.flag") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode){
   File.Delete("Temp/Localization/visual.flag");Stage=10;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+8;return;
  }

  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(Stage<0 && File.Exists("Temp/Localization/test.flag") && !EditorApplication.isPlayingOrWillChangePlaymode){
   File.Delete("Temp/Localization/test.flag");if(EditorSceneManager.GetActiveScene().isDirty){Log("BLOCKED unsaved scene");return;}
   File.WriteAllText(Report,"Localization regression: "+DateTime.Now+"\n");Stage=0;Index=0;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+7;return;
  }
  if(Stage<0||EditorApplication.timeSinceStartup<Due)return;
  try{
   if(Stage==5){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");Stage=0;EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+6;return;}
   if(Stage==6){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");Stage=-1;Log("COMPLETE; no scene assets saved.");return;}
   if(!EditorApplication.isPlaying)throw new Exception("Play mode stopped");
   var loc=WindLocalization.Instance;if(!loc){Due=EditorApplication.timeSinceStartup+2;return;}
   if(Stage==10){loc.SetLanguage("Arabic");Stage=11;Due=EditorApplication.timeSinceStartup+4;return;}
   if(Stage==11){Verify("Visual Menu"); ScreenCapture.CaptureScreenshot("Temp/Localization/visual-menu.png");Stage=-1;return;}
   if(Stage==0){
    loc.SetLanguage("English");
    var toggle=Resources.FindObjectsOfTypeAll<Button>().Single(x=>x.gameObject.scene==SceneManager.GetActiveScene()&&x.name=="EN AR Language Button");
    toggle.onClick.Invoke();if(!WindLocalization.Arabic)throw new Exception("EN/AR button failed");
    toggle.onClick.Invoke();if(WindLocalization.Arabic)throw new Exception("EN/AR restore failed");
    Coverage("Main Menu"); loc.SetLanguage("Arabic");Verify("Main Menu"); ScreenCapture.CaptureScreenshot("Temp/Localization/menu-ar.png");
    var launch=Resources.FindObjectsOfTypeAll<MainMenuExperimentLauncher>().Single(x=>x.gameObject.scene==SceneManager.GetActiveScene()&&new SerializedObject(x).FindProperty("storyboardId").stringValue==Ids[Index]);
    InteractionManager.Instance.currentMode=ExperimentMode.Guided;launch.GetComponent<Button>().onClick.Invoke();Stage=1;Due=EditorApplication.timeSinceStartup+8;return;
   }
   if(Stage==1){
    if(!WindLocalization.Arabic)throw new Exception("Language lost during scene change");
    loc.SetLanguage("English"); Coverage("EXP"+(Index+1)); loc.SetLanguage("Arabic"); Verify("EXP"+(Index+1)+" Guided"); ScreenCapture.CaptureScreenshot("Temp/Localization/exp"+(Index+1)+"-ar.png");
    var boards=Resources.FindObjectsOfTypeAll<StoryboardSteps>().Where(x=>x.gameObject.scene==SceneManager.GetActiveScene());var b=boards.Single(x=>x.StoryboardID==Ids[Index]);
    foreach(var step in b.steps)if(step.uiObject){
     foreach(var t in step.uiObject.GetComponentsInChildren<TMP_Text>(true))if(System.Text.RegularExpressions.Regex.IsMatch(t.text??"","[A-Za-z]{3}"))Log("CHECK subtitle: "+t.text);
    }
    InteractionManager.Instance.SetFree();Stage=2;Due=EditorApplication.timeSinceStartup+7;return;
   }
   if(Stage==2){Verify("EXP"+(Index+1)+" Free");loc.SetLanguage("English");Stage=3;Due=EditorApplication.timeSinceStartup+2;return;}
   if(Stage==3){
    int ar=Resources.FindObjectsOfTypeAll<TMP_Text>().Count(t=>t.gameObject.scene==SceneManager.GetActiveScene()&&t.text.Any(c=>c>=0xFE70&&c<=0xFEFF));
    Log("EXP"+(Index+1)+" English restored; Arabic-shaped displays="+ar);
    if(ar>0)throw new Exception("Arabic display not restored");
    Index++;Stage=Index==4?6:5;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;
   }
  }catch(Exception e){Log("FAIL "+e);Stage=6;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}
 }
 static void Coverage(string label){
  foreach(var t in Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()))
   if(System.Text.RegularExpressions.Regex.IsMatch(t.text??"", "[A-Za-z]{3}")&&!WindLocalization.TryTranslate(t.text,out var translated))
    File.AppendAllText("Temp/Localization/missing.txt",label+"|"+t.name+"|"+t.text.Replace("\n"," ")+"\n");
 }
 static void Verify(string label){
  var scene=SceneManager.GetActiveScene();var ts=Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t=>t.gameObject.scene==scene).ToArray();
  int ar=ts.Count(t=>t.text.Any(c=>c>=0xFE70&&c<=0xFEFF));
  var buttons=Resources.FindObjectsOfTypeAll<Button>().Where(b=>b.gameObject.scene==scene&&b.name=="EN AR Language Button").ToArray();
  Log(label+": Arabic displays="+ar+"; language controls="+buttons.Length+"; Lean phrases="+Lean.Localization.LeanLocalization.CurrentTranslations.Count);
  if(ar==0||buttons.Length!=1)throw new Exception("Arabic UI/control missing");
  foreach(var t in ts.Where(t=>t.isActiveAndEnabled&&t.text.Any(c=>c>=0xFE70&&c<=0xFEFF))) {
   t.ForceMeshUpdate();if(t.isTextOverflowing)Log("OVERFLOW "+t.name+" size="+t.rectTransform.rect.size+" font="+t.fontSize+" margin="+t.margin+" pref="+t.GetPreferredValues(t.text,t.rectTransform.rect.width,float.PositiveInfinity)+" firstOverflow="+t.firstOverflowCharacterIndex+" text="+t.text);
  }
 }
}
