using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
[InitializeOnLoad] public static class ArabicPanelInspect {
 static int Stage{get=>SessionState.GetInt("ArabicPanelInspect.stage",-1);set=>SessionState.SetInt("ArabicPanelInspect.stage",value);}
 static double Due{get=>SessionState.GetFloat("ArabicPanelInspect.due",0);set=>SessionState.SetFloat("ArabicPanelInspect.due",(float)value);}
 static ArabicPanelInspect(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(File.Exists("Temp/arabic-panel-restart.flag")){File.Delete("Temp/arabic-panel-restart.flag");Stage=-1;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
 if(Stage<0&&File.Exists("Temp/arabic-panel-inspect.flag")){if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/arabic-panel-inspect.flag");if(EditorSceneManager.GetActiveScene().isDirty)return;File.WriteAllText("Temp/arabic-panel-inspect.txt","");Stage=0;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");EditorApplication.EnterPlaymode();Due=EditorApplication.timeSinceStartup+6;return;}
 if(Stage<0||EditorApplication.timeSinceStartup<Due)return;
 if(Stage==4){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");Stage=-1;return;}
 if(!EditorApplication.isPlaying||!WindLocalization.Instance)return;
 try{
 if(Stage==0){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var l=Resources.FindObjectsOfTypeAll<MainMenuExperimentLauncher>().Single(x=>x.gameObject.scene==scene&&new SerializedObject(x).FindProperty("storyboardId").stringValue=="Speed Curve Characteristics");l.GetComponent<Button>().onClick.Invoke();Stage=1;Due=EditorApplication.timeSinceStartup+8;return;}
 if(Stage==1){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var panel=Resources.FindObjectsOfTypeAll<CanvasGroup>().First(x=>x.gameObject.scene==scene&&x.name=="Wind Speed Panel");panel.gameObject.SetActive(true);panel.alpha=1;panel.interactable=panel.blocksRaycasts=true;WindLocalization.Instance.SetLanguage("Arabic");Stage=2;Due=EditorApplication.timeSinceStartup+3;return;}
 if(Stage==2){Dump("Wind Speed Panel");ScreenCapture.CaptureScreenshot("Temp/arabic-panel-after.png");Stage=3;Due=EditorApplication.timeSinceStartup+2;return;}
 if(Stage==3){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 var panel=Resources.FindObjectsOfTypeAll<CanvasGroup>().First(x=>x.gameObject.scene==scene&&x.name=="Wind Speed Panel");panel.alpha=0;
 var quiz=Resources.FindObjectsOfTypeAll<MultipleChoiceQuiz>().First(q=>q.gameObject.scene==scene&&PathOf(q.transform).Contains("/Exp 1/"));
 for(var t=quiz.transform;t;t=t.parent){t.gameObject.SetActive(true);var g=t.GetComponent<CanvasGroup>();if(g)g.alpha=1;var a=t.GetComponent<Animator>();if(a)a.enabled=false;}
 for(int i=0;i<quiz.questions.Count;i++)if(quiz.questions[i].questionObject)quiz.questions[i].questionObject.SetActive(i==0);
 WindLocalization.Instance.SetLanguage("Arabic");Stage=5;Due=EditorApplication.timeSinceStartup+3;return;
 }
 if(Stage==5){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 foreach(var quiz in Resources.FindObjectsOfTypeAll<MultipleChoiceQuiz>().Where(q=>q.gameObject.scene==scene))foreach(var t in quiz.GetComponentsInChildren<TMP_Text>(true))DumpText(t);
 ScreenCapture.CaptureScreenshot("Temp/arabic-quiz-after.png");Stage=6;Due=EditorApplication.timeSinceStartup+2;return;
 }
 if(Stage==6){WindLocalization.Instance.SetLanguage("English");File.AppendAllText("Temp/arabic-panel-inspect.txt","PASS English restored\n");Stage=4;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}
 }catch(Exception e){File.AppendAllText("Temp/arabic-panel-inspect.txt","FAIL "+e);Stage=4;EditorApplication.ExitPlaymode();Due=EditorApplication.timeSinceStartup+2;}}
 static void Dump(string owner){foreach(var t in Resources.FindObjectsOfTypeAll<TMP_Text>())if(t.gameObject.scene.IsValid()&&PathOf(t.transform).Contains(owner))DumpText(t);}
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static void DumpText(TMP_Text t){t.ForceMeshUpdate(true);string s=PathOf(t.transform)+"\nTEXT "+t.text+"\nALIGN "+t.alignment+" spacing="+t.characterSpacing+" wordSpacing="+t.wordSpacing+" style="+t.fontStyle+" font="+(t.font?t.font.name:"")+" features="+string.Join(",",t.fontFeatures)+" size="+t.fontSize+" rect="+t.rectTransform.rect+"\n";for(int i=0;i<t.textInfo.characterCount;i++){var c=t.textInfo.characterInfo[i];s+=$"{(int)c.character:X4} origin={c.origin:F2} advance={c.xAdvance:F2} visible={c.isVisible} ";}File.AppendAllText("Temp/arabic-panel-inspect.txt",s+"\n");}
}
