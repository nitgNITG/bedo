using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class LocalizationPolishInspect {
 static LocalizationPolishInspect(){EditorApplication.update+=Tick;}
 static string P(Transform t)=>t.parent?P(t.parent)+"/"+t.name:t.name;
 static void Tick(){if(!File.Exists("Temp/localization-polish-inspect.flag")||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 File.Delete("Temp/localization-polish-inspect.flag");if(EditorSceneManager.GetActiveScene().isDirty)return;string original=EditorSceneManager.GetActiveScene().path;string report="";
 foreach(string scene in new[]{"Main Menu","PV Lab"}){var s=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+scene+".unity");foreach(var r in s.GetRootGameObjects())foreach(var b in r.GetComponentsInChildren<Button>(true))if(P(b.transform).Contains("Right Panel")){
 var rect=(RectTransform)b.transform;report+= "active="+b.gameObject.activeSelf+" parentActive="+b.transform.parent.gameObject.activeSelf+" target="+(b.targetGraphic?b.targetGraphic.name:"null")+" transition="+b.transition+"\n"; report+=scene+"|"+P(b.transform)+"|"+rect.anchoredPosition+"|"+rect.sizeDelta+"|"+string.Join(",",b.GetComponents<Component>().Select(c=>c?c.GetType().Name:"missing"))+"\n";
 foreach(var g in b.GetComponentsInChildren<Graphic>(true))report+="  "+P(g.transform)+"|"+g.GetType().Name+"|"+g.color+"|rect="+g.rectTransform.anchoredPosition+" anchors="+g.rectTransform.anchorMin+"/"+g.rectTransform.anchorMax+" size="+g.rectTransform.sizeDelta+"\n";
 }}File.WriteAllText("Temp/localization-polish-inspect.txt",report);EditorSceneManager.OpenScene(original);}
}
