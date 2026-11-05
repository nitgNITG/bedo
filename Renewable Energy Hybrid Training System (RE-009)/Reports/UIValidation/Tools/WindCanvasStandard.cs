using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
[InitializeOnLoad]
public static class WindCanvasStandard
{
 static WindCanvasStandard(){EditorApplication.update+=Tick;}
 static void Tick(){
  if(File.Exists("Temp/ui-probe.flag")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating){File.Delete("Temp/ui-probe.flag");EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");Canvas.ForceUpdateCanvases();File.WriteAllText("Temp/ui-probe.txt","");foreach(var image in Resources.FindObjectsOfTypeAll<Image>().Where(i=>i.gameObject.scene==SceneManager.GetActiveScene()&&i.rectTransform.rect.width>1000&&i.rectTransform.rect.height>650))File.AppendAllText("Temp/ui-probe.txt",EquipmentPrefabWorkflow.PathOf(image.transform)+" rect="+image.rectTransform.rect+" anchors="+image.rectTransform.anchorMin+":"+image.rectTransform.anchorMax+" color="+image.color+"\n");return;}
  if(!File.Exists("Temp/ui-scale.flag")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
  File.Delete("Temp/ui-scale.flag");var output="Reports/UIValidation/CanvasChanges.txt";File.WriteAllText(output,"");
  try{
   if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene before applying canvas standard");
   foreach(var path in new[]{"Assets/_Project/Scenes/Main Menu.unity","Assets/_Project/Scenes/PV Lab.unity"}){
    var scene=EditorSceneManager.OpenScene(path);int count=0;
    foreach(var scaler in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CanvasScaler>(true))){var canvas=scaler.GetComponent<Canvas>();if(!canvas||canvas.renderMode==RenderMode.WorldSpace)continue;
     scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
     EditorUtility.SetDirty(scaler);PrefabUtility.RecordPrefabInstancePropertyModifications(scaler);count++;
     File.AppendAllText(output,scene.name+": "+EquipmentPrefabWorkflow.PathOf(scaler.transform)+" -> ScaleWithScreenSize,1600x900,match0.5\n");
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.AppendAllText(output,"Updated "+count+" screen-space scalers; world-space equipment canvases excluded.\n");
   }
   File.AppendAllText(output,"FINISHED\n");
  }catch(Exception e){File.AppendAllText(output,"FAIL "+e+"\n");}
 }
}
