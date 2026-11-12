using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
[InitializeOnLoad] public static class LocalizationVerticalCheck {
 static int Stage{get=>SessionState.GetInt("VerticalCheck.stage",-1);set=>SessionState.SetInt("VerticalCheck.stage",value);}
 static double Due{get=>SessionState.GetFloat("VerticalCheck.due",0);set=>SessionState.SetFloat("VerticalCheck.due",(float)value);}
 const string Output="Temp/localization-vertical-check.txt";
 static LocalizationVerticalCheck(){EditorApplication.update+=Tick;}
 static void Log(string x)=>File.AppendAllText(Output,x+"\n");
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(File.Exists("Temp/localization-stop.flag")){File.Delete("Temp/localization-stop.flag");if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();Stage=-1;return;}
  if(Stage<0&&File.Exists("Temp/localization-vertical-check.flag")&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete("Temp/localization-vertical-check.flag");if(EditorSceneManager.GetActiveScene().isDirty){Log("BLOCKED unsaved scene");return;}File.WriteAllText(Output,"Vertical labels and language badge\n");EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");Stage=0;EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+6;return;}
  if(Stage<0||EditorApplication.timeSinceStartup<Due)return;
  try{
   if(Stage==4){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");Stage=-1;Log("COMPLETE");return;}
   if(!EditorApplication.isPlaying||!WindLocalization.Instance)return;
   if(Stage==0){CheckBadge();CheckLabels("Main Menu");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var launch=Resources.FindObjectsOfTypeAll<MainMenuExperimentLauncher>().Single(x=>x.gameObject.scene==scene&&new SerializedObject(x).FindProperty("storyboardId").stringValue=="Speed Curve Characteristics");launch.GetComponent<Button>().onClick.Invoke();Stage=1;Due=EditorApplication.timeSinceStartup+8;return;}
   if(Stage==1){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var experiments=Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(x=>x.gameObject.scene==scene&&x.name=="Experiments Button");if(experiments)experiments.onClick.Invoke();WindLocalization.Instance.SetLanguage("Arabic");Stage=2;Due=EditorApplication.timeSinceStartup+3;return;}
   if(Stage==2){CheckBadge();CheckLabels("PV Lab");ScreenCapture.CaptureScreenshot("Temp/localization-vertical-pv.png");Stage=3;Due=EditorApplication.timeSinceStartup+3;return;}
   if(Stage==3){WindLocalization.Instance.SetLanguage("English");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(Resources.FindObjectsOfTypeAll<TMP_Text>().Any(t=>t.gameObject.scene==scene&&t.name=="Arabic Vertical Label"&&t.gameObject.activeSelf))throw new Exception("Arabic labels still active in English");Log("PASS English title restoration");Stage=4;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}
  }catch(Exception e){Log("FAIL "+e);Stage=4;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}
 }
 static void CheckBadge(){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var b=Resources.FindObjectsOfTypeAll<Button>().Single(x=>x.gameObject.scene==scene&&x.name=="EN AR Language Button");WindLocalization.Instance.SetLanguage("English");if(b.GetComponentInChildren<TMP_Text>(true).text!="EN")throw new Exception("Expected EN");b.onClick.Invoke();if(b.GetComponentInChildren<TMP_Text>(true).text!="AR")throw new Exception("Expected AR");Log(scene.name+" PASS EN/AR current-language indicator");}
 static bool CurrentExperiment(Transform t){for(var p=t;p;p=p.parent)if(System.Text.RegularExpressions.Regex.IsMatch(p.name,@"^Experiment [1-4]:"))return true;return false;}
 static void CheckLabels(string sceneName){Canvas.ForceUpdateCanvases();var labels=Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t=>t.gameObject.scene.name==sceneName&&t.name=="Arabic Vertical Label"&&CurrentExperiment(t.transform)).ToArray();if(labels.Length<8)throw new Exception("Missing vertical labels");foreach(var t in labels){t.ForceMeshUpdate(true);var owner=(RectTransform)t.transform.parent;var size=t.rectTransform.rect.size;Log(sceneName+" "+owner.name+" owner="+owner.rect.size+" label="+size+" font="+t.fontSize+" lines="+t.textInfo.lineCount+" angle="+t.rectTransform.localEulerAngles.z);if(Mathf.Abs(t.rectTransform.localEulerAngles.z-90)>.1f||size.x<60||size.y<15||t.text.Contains("\n")||t.textInfo.lineCount>1)throw new Exception("Invalid vertical-label geometry");}}
}
