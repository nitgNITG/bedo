using System;
using System.IO;
using System.Linq;
using System.Text;
using TutorialFramework;
using UnityEngine;
using UnityEditor;

public static partial class HybridGuidedCompletion
{
 [MenuItem("RE009/Audit Storyboard Hardware Corrections")]
 public static void AuditCorrections(){
 var o=new StringBuilder();var equipment=At("Equipment/Wind Lab");
 foreach(var t in equipment.Cast<Transform>())o.AppendLine("ROOT "+Path(t)+" "+t.position);
 foreach(var r in equipment.GetComponentsInChildren<Renderer>(true)){
 var p=Path(r.transform);
 if((p.Contains("Inputs_hydro")||p.Contains("Outputs_hydro")||p.Contains("Unused_hydro")||p.Contains("INPUTS (solar panel)")||p.Contains("Bananas (Modules)"))&&!p.Contains("Solar panel 2")&&!p.Contains("Solar panel 3"))
 o.AppendLine("PHYSICAL "+p+" p="+r.transform.position+" e="+r.transform.eulerAngles+" bounds="+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name)));
 }
 foreach(var s in All<SingleClickInteraction>().Where(s=>Path(s.transform).StartsWith("Experiments/Exp 4/")||Path(s.transform).StartsWith("Experiments/Hybrid Experiment 1/")||Path(s.transform).StartsWith("Equipment/UnGuided/Step Objects/Step 4/"))){
 o.AppendLine("CONTROL "+Path(s.transform)+" p="+s.transform.position+" local="+s.transform.localPosition+" rot="+s.transform.localEulerAngles+" parent="+s.transform.parent.position+" animator="+(s.animator?Path(s.animator.transform)+" clip="+s.animationName:"none"));
 if(s.animator&&s.animator.runtimeAnimatorController)foreach(var clip in s.animator.runtimeAnimatorController.animationClips)foreach(var binding in AnimationUtility.GetCurveBindings(clip)){var curve=AnimationUtility.GetEditorCurve(clip,binding);o.AppendLine("CLIP "+clip.name+" "+binding.path+" "+binding.propertyName+" "+string.Join(",",curve.keys.Select(k=>k.time+":"+k.value)));}
 for(int i=0;i<s.onCompleted.GetPersistentEventCount();i++)o.AppendLine("EVENT "+s.onCompleted.GetPersistentMethodName(i)+" -> "+(s.onCompleted.GetPersistentTarget(i) is Component c?Path(c.transform):s.onCompleted.GetPersistentTarget(i)?.name));
 }
 File.WriteAllText("Temp/RE009Planning/correction-audit.txt",o.ToString());
 }
}
