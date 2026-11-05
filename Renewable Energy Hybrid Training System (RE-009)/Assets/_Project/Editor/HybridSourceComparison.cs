using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using TutorialFramework;
public static class HybridSourceComparison {
static T[] All<T>()where T:Component=>HybridGuidedCompletion.All<T>();static string P(Transform t)=>HybridGuidedCompletion.Path(t);
[MenuItem("RE009/Inspect Original Power Button And Plug Templates")]
public static void Audit(){var log=new StringBuilder();var sys=All<HybridTrainingController>().Single();
foreach(var c in All<SingleClickInteraction>().Where(c=>(c.name=="on_button004 (1)"||c.name=="Power_Switch wind"||c.name=="on_button001 (1)"||c.name=="Main Board ON"||c.name=="PV ON"||c.name=="Wind ON")&&c.interaction)){
log.AppendLine("CONTROL "+P(c.transform)+" pos="+c.transform.position+" rot="+c.transform.eulerAngles+" parent="+c.transform.parent.name);
foreach(var r in c.transform.parent.GetComponentsInChildren<Renderer>(true))log.AppendLine("MESH "+P(r.transform)+" pos="+r.transform.position+" rot="+r.transform.eulerAngles+" scale="+r.transform.lossyScale+" enabled="+r.enabled+" active="+r.gameObject.activeSelf+" mats="+string.Join("|",r.sharedMaterials.Select(m=>m?AssetDatabase.GetAssetPath(m):"NULL")));
for(int i=0;i<c.onCompleted.GetPersistentEventCount();i++)log.AppendLine("EVENT "+c.onCompleted.GetPersistentMethodName(i)+" "+(c.onCompleted.GetPersistentTarget(i) is Component cc?P(cc.transform):c.onCompleted.GetPersistentTarget(i)?.name));
}
foreach(var save in sys.monitor.GetComponentsInChildren<Bedo.Core.UI.MapCameraScreenshotButton_RT>(true)){log.AppendLine("SAVE "+P(save.transform));foreach(var t in save.transform.parent.GetComponentsInChildren<Transform>(true))if(t.name.ToLower().Contains("plane")||t==save.transform)log.AppendLine("SAVECHILD "+P(t)+" pos="+t.position+" scale="+t.lossyScale+" active="+t.gameObject.activeSelf);var hover=save.GetComponent<HoverObjectToggle>();log.AppendLine("HOVER "+(hover?EditorJsonUtility.ToJson(hover):"NONE"));}
var wires=sys.storyboard.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();foreach(var w in wires.Take(12)){foreach(var e in new[]{(w.pointA,w.plugA),(w.pointB,w.plugB)})log.AppendLine("PLUG "+e.Item1.name+" pointrot="+e.Item1.transform.eulerAngles+" pos="+e.Item2.transform.localPosition+" rot="+e.Item2.transform.localEulerAngles+" world="+e.Item2.transform.eulerAngles);}
foreach(var r in All<Renderer>().Where(r=>P(r.transform).StartsWith("Equipment/")&&(r.name.ToLower().Contains("led_")||r.name.StartsWith("Off_button")||r.name.StartsWith("on_button"))))log.AppendLine("HARDWARE "+P(r.transform)+" pos="+r.transform.position+" enabled="+r.enabled+" scale="+r.transform.lossyScale);
var bg=sys.storyboard.GetComponent<HybridGuidedBindings>().graphPage.transform.Find("Wind graph").GetComponent<Renderer>();log.AppendLine("BACKGROUND "+AssetDatabase.GetAssetPath(bg.sharedMaterial)+" TEXTURE="+AssetDatabase.GetAssetPath(bg.sharedMaterial.mainTexture));
File.WriteAllText("Logs/HybridReview/source-templates.txt",log.ToString());}
}
