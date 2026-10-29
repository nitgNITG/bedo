using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class WindWarningLocalizationCheck
{
 static int Stage {get=>SessionState.GetInt("WindWarningCheck",-1);set=>SessionState.SetInt("WindWarningCheck",value);}
 static double Due;
 static WindWarningLocalizationCheck(){EditorApplication.update+=Tick;}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(Stage<0&&File.Exists("Temp/Localization/warning-test.flag")&&!EditorApplication.isPlayingOrWillChangePlaymode){
   File.Delete("Temp/Localization/warning-test.flag");Stage=0;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+7;return;
  }
  if(Stage<0||EditorApplication.timeSinceStartup<Due)return;
  if(Stage==1){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");Stage=-1;return;}
  if(!EditorApplication.isPlaying||!WindLocalization.Instance)return;
  try{
   var entries=WindLocalization.Catalogue.entries.Where(e=>e.source=="Runtime interaction warning").ToArray();
   var targets=entries.Select(e=>{var g=new GameObject("Warning translation test",typeof(RectTransform),typeof(TextMeshProUGUI));var t=g.GetComponent<TextMeshProUGUI>();t.text=e.english;t.fontSize=24;t.rectTransform.sizeDelta=new Vector2(800,140);return t;}).ToArray();
   WindLocalization.Instance.SetLanguage("Arabic");
   if(targets.Any(t=>!t.text.Any(c=>c>=0xFE70&&c<=0xFEFF)))throw new Exception("Missing warning translation");
   WindLocalization.Instance.SetLanguage("English");
   for(int i=0;i<targets.Length;i++)if(targets[i].text!=entries[i].english)throw new Exception("Warning did not restore English");
   File.WriteAllText("Reports/Localization/warning-runtime-check.txt","PASS all "+entries.Length+" runtime warning strings rendered in Arabic and restored exactly to English; Lean keys="+Lean.Localization.LeanLocalization.CurrentTranslations.Count+".\n");
  }catch(Exception e){File.WriteAllText("Reports/Localization/warning-runtime-check.txt","FAIL "+e);}
  Stage=1;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;
 }
}
