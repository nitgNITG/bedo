using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
[InitializeOnLoad] public static class LocalizationPolishCheck {
 const string Report="Temp/localization-polish-test.txt";
 static int Stage {get=>SessionState.GetInt("LocalePolish.Stage",-1);set=>SessionState.SetInt("LocalePolish.Stage",value);}
 static double Due {get=>SessionState.GetFloat("LocalePolish.Due",0);set=>SessionState.SetFloat("LocalePolish.Due",(float)value);}
 static Dictionary<TMP_Text,string> snapshots=new Dictionary<TMP_Text,string>();static int samples,changes;
 static LocalizationPolishCheck(){EditorApplication.update+=Tick;}
 static void Log(string s)=>File.AppendAllText(Report,s+"\n");
 static string Snapshot(TMP_Text t)=>t.text+"|"+t.fontSize.ToString("F3")+"|"+t.rectTransform.rect+"|"+t.textInfo.lineCount;
 static void Tick(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(Stage<0&&File.Exists("Temp/localization-polish-test.flag")&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete("Temp/localization-polish-test.flag");if(EditorSceneManager.GetActiveScene().isDirty){File.WriteAllText(Report,"BLOCKED unsaved scene");return;}File.WriteAllText(Report,"Localization polish regression\n");Stage=0;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+6;return;}
 if(Stage<0||EditorApplication.timeSinceStartup<Due)return;
 try{
 if(Stage==5){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");Stage=-1;Log("COMPLETE; no scene assets saved");return;}
 if(!EditorApplication.isPlaying)return;var loc=WindLocalization.Instance;if(!loc)return;
 if(Stage==0){loc.SetLanguage("Arabic");Stage=1;Due=EditorApplication.timeSinceStartup+4;return;}
 if(Stage==1||Stage==3){
  CheckButton();snapshots=Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()&&t.isActiveAndEnabled&&t.transform.parent&&t.transform.parent.name!="Icon"&&t.text.Any(c=>c>=0xFE70&&c<=0xFEFF)).ToDictionary(t=>t,Snapshot);samples=changes=0;Stage++;return;
 }
 if(Stage==2||Stage==4){
  foreach(var pair in snapshots.ToArray())if(pair.Key&&pair.Key.isActiveAndEnabled){string now=Snapshot(pair.Key);if(now!=pair.Value){changes++;snapshots[pair.Key]=now;}}
  samples++;if(samples<180)return;
  Log(SceneManager.GetActiveScene().name+": sampled "+snapshots.Count+" active Arabic labels across "+samples+" updates; text/font/rectangle/line-count changes="+changes);
  if(changes>0)throw new Exception("Arabic layout is not stable");
  if(Stage==2){var launch=Resources.FindObjectsOfTypeAll<MainMenuExperimentLauncher>().Single(x=>x.gameObject.scene==SceneManager.GetActiveScene()&&new SerializedObject(x).FindProperty("storyboardId").stringValue=="Regulating DC Output from Wind Turbine using PWM Charger");launch.GetComponent<Button>().onClick.Invoke();Stage=3;Due=EditorApplication.timeSinceStartup+8;return;}
  var toggle=Resources.FindObjectsOfTypeAll<Button>().Single(x=>x.gameObject.scene==SceneManager.GetActiveScene()&&x.name=="EN AR Language Button");toggle.onClick.Invoke();if(WindLocalization.Arabic)throw new Exception("English toggle failed");toggle.onClick.Invoke();if(!WindLocalization.Arabic)throw new Exception("Arabic toggle failed");loc.SetLanguage("English");Log("PASS language switching and Reload callback isolation");Stage=5;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;
 }
 }catch(Exception e){Log("FAIL "+e);Stage=5;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}
 }
 static void CheckButton(){
 var scene=SceneManager.GetActiveScene();var buttons=Resources.FindObjectsOfTypeAll<Button>().Where(b=>b.gameObject.scene==scene).ToArray();var language=buttons.Single(b=>b.name=="EN AR Language Button");var reload=buttons.Single(b=>b.name=="Restart Button Variant"&&b.transform.parent&&b.transform.parent.name=="Right Panel");
 var a=language.transform.Find("Icon").GetComponent<RectTransform>();var brect=reload.transform.Find("Icon").GetComponent<RectTransform>();
 var canvas=language.GetComponentInParent<Canvas>().rootCanvas.transform;var la=canvas.InverseTransformPoint(a.TransformPoint(a.rect.center));var rb=canvas.InverseTransformPoint(brect.TransformPoint(brect.rect.center));
 Log(scene.name+": language center="+la+"; Reload center="+rb+"; gap="+(la-rb)+"; active="+language.gameObject.activeInHierarchy+"; inherited Reload handlers="+language.onClick.GetPersistentEventCount());
 if(!language.gameObject.activeInHierarchy||Mathf.Abs(la.x-rb.x)>.1f||la.y<=rb.y||language.onClick.GetPersistentEventCount()!=0)throw new Exception("Incorrect language-button position or callback");
 var old=reload.GetComponentsInChildren<Image>(true);var next=language.GetComponentsInChildren<Image>(true);if(old.Length!=next.Length||!old.Select(i=>i.sprite).SequenceEqual(next.Select(i=>i.sprite)))throw new Exception("Button visual style not reused");
 }
}
