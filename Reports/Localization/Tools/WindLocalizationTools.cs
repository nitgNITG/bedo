using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class WindLocalizationTools
{
 [Serializable] public class Item { public string scene,path,text,kind; }
 [Serializable] public class Inventory {public List<Item> items=new List<Item>();}
 static WindLocalizationTools(){EditorApplication.update+=Tick;}
 static void Tick(){
  if(File.Exists("Temp/Localization/setup.flag") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode){
   File.Delete("Temp/Localization/setup.flag");
   Directory.CreateDirectory("Assets/_Project/Resources/WindLocalization"); AssetDatabase.Refresh();
   const string fontPath="Assets/_Project/Resources/WindLocalization/Arabic Font.asset";
   var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
   if(!font){
    font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/RTLTMPro/Fonts/segoeui.ttf"),90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
    font.name="Wind Arabic Segoe UI"; font.isMultiAtlasTexturesEnabled=true;
    AssetDatabase.CreateAsset(font,fontPath);
    var chars=new System.Text.StringBuilder();for(int n=32;n<127;n++)chars.Append((char)n);for(int n=0x600;n<0x700;n++)chars.Append((char)n);for(int n=0xFE70;n<0xFEFF;n++)chars.Append((char)n);
    font.TryAddCharacters(chars.ToString(),out string missing);
    AssetDatabase.AddObjectToAsset(font.material,font);
    foreach(var atlas in font.atlasTextures)if(atlas&&!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,font);
    EditorUtility.SetDirty(font);
   }
   var config=AssetDatabase.LoadAssetAtPath<WindLocalizationSettings>("Assets/_Project/Resources/WindLocalization/Settings.asset");
   if(!config){config=ScriptableObject.CreateInstance<WindLocalizationSettings>();AssetDatabase.CreateAsset(config,"Assets/_Project/Resources/WindLocalization/Settings.asset");}
   config.arabicFont=font;config.legacyFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/RTLTMPro/Fonts/segoeui.ttf");EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
   File.WriteAllText("Temp/Localization/setup-result.txt","Arabic font and Lean catalogue settings created.");return;
  }

  const string flag="Temp/Localization/inventory.flag";
  if(!File.Exists(flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
  File.Delete(flag);
  if(EditorSceneManager.GetActiveScene().isDirty){File.WriteAllText("Temp/Localization/blocked.txt","Unsaved scene; no changes made.");return;}
  string original=EditorSceneManager.GetActiveScene().path;var data=new Inventory();
  foreach(string scene in new[]{"Main Menu","PV Lab"}){
   var s=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+scene+".unity");
   foreach(var root in s.GetRootGameObjects()){
    foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)){
     if(!c || c is TMP_Text) continue;
     var so=new SerializedObject(c);var prop=so.GetIterator();
     while(prop.NextVisible(true))if(prop.propertyType==SerializedPropertyType.String && System.Text.RegularExpressions.Regex.IsMatch(prop.name,"title|description|tooltip",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      data.items.Add(new Item{scene="Tooltip",path=PathOf(c.transform)+"/"+prop.propertyPath,text=prop.stringValue,kind=c.GetType().Name});
    }
    foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))data.items.Add(new Item{scene=scene,path=PathOf(t.transform),text=t.text,kind="TMP"});
    foreach(var t in root.GetComponentsInChildren<Text>(true))data.items.Add(new Item{scene=scene,path=PathOf(t.transform),text=t.text,kind="UI.Text"});
   }
  }
  foreach(var guid in AssetDatabase.FindAssets("t:TooltipComponentDescription")){
   var a=AssetDatabase.LoadAssetAtPath<TooltipComponentDescription>(AssetDatabase.GUIDToAssetPath(guid));
   data.items.Add(new Item{scene="Tooltip",path=AssetDatabase.GetAssetPath(a),text=a.Title,kind="Title"});
   data.items.Add(new Item{scene="Tooltip",path=AssetDatabase.GetAssetPath(a),text=a.Description,kind="Description"});
  }
  foreach(var guid in AssetDatabase.FindAssets("t:MechaTooltipScriptableObject")){
   var a=AssetDatabase.LoadAssetAtPath<Bedo.Core.MechaTooltipScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
   if(a.EnglishDesciptionStringVariable)data.items.Add(new Item{scene="Tooltip",path=AssetDatabase.GetAssetPath(a),text=a.EnglishDesciptionStringVariable.Value,kind="MechaEnglish"});
   if(a.ArabicDesciptionStringVariable)data.items.Add(new Item{scene="TooltipArabic",path=AssetDatabase.GetAssetPath(a),text=a.ArabicDesciptionStringVariable.Value,kind="MechaArabic"});
  }
  foreach(var guid in AssetDatabase.FindAssets("t:TooltipData")){
   var a=AssetDatabase.LoadAssetAtPath<TooltipData>(AssetDatabase.GUIDToAssetPath(guid));
   data.items.Add(new Item{scene="Tooltip",path=AssetDatabase.GetAssetPath(a),text=a.Title,kind="Title"});
   data.items.Add(new Item{scene="Tooltip",path=AssetDatabase.GetAssetPath(a),text=a.Description,kind="Description"});
  }
  File.WriteAllText("Temp/Localization/inventory.json",JsonUtility.ToJson(data,true));
  if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
 }
 static string PathOf(Transform t){return t.parent?PathOf(t.parent)+"/"+t.name:t.name;}
}
