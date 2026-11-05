using System;
using System.IO;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static partial class HybridGuidedCompletion
{
 [MenuItem("RE009/Remove Inverter Ethernet Instruction And Renumber")]
 public static void RemoveInverterEthernetInstruction(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");
 var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
 var stepRoot=board.transform.Find("Steps");var panelRoot=At("Interface/Main UI/Popups/Instructions/Hybrid Experiment UI");
 var obsolete=board.steps.Where(s=>s.interaction&&s.interaction.name=="Off-grid Inverter Module Comm.In to Inverter Device COM").ToArray();
 if(obsolete.Length==0){if(board.steps.Count!=56)throw new Exception("Unexpected sequence; no changes made.");return;}
 if(obsolete.Length!=1||panelRoot.childCount!=25)throw new Exception("Unexpected pre-removal sequence.");
 var wire=(WireInteraction)obsolete[0].interaction;var mesh=wire.wireObject.GetComponentInChildren<SplineExtrude>(true).targetMesh;var meshPath=AssetDatabase.GetAssetPath(mesh);
 board.steps.Remove(obsolete[0]);Object.DestroyImmediate(wire.wireObject);Object.DestroyImmediate(stepRoot.Find("Step 6").gameObject);Object.DestroyImmediate(panelRoot.Find("Subtitle Step 6").gameObject);
 var socket=At("Equipment/Wind Lab/UNUSED (MDOULE SYSTEM)/off grid").Find("Hybrid Off-grid Inverter Comm.In Socket");if(socket)Object.DestroyImmediate(socket.gameObject);
 foreach(var click in board.GetComponentsInChildren<SingleClickInteraction>(true)){
 var so=new SerializedObject(click);var calls=so.FindProperty("onCompleted.m_PersistentCalls.m_Calls");
 for(int i=0;i<calls.arraySize;i++){var call=calls.GetArrayElementAtIndex(i);if(call.FindPropertyRelative("m_MethodName").stringValue=="CompleteInstruction"){
 var n=call.FindPropertyRelative("m_Arguments.m_IntArgument");if(n.intValue>6)n.intValue--;}}
 so.ApplyModifiedPropertiesWithoutUndo();Dirty(click);
 }
 for(int n=7;n<=25;n++){
 var folder=stepRoot.Find("Step "+n);if(folder)folder.name="Step "+(n-1);
 var panel=panelRoot.Find("Subtitle Step "+n);if(!panel)throw new Exception("Missing subtitle "+n);panel.name="Subtitle Step "+(n-1);
 foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.transform.parent&&t.transform.parent.name=="Index Number")){text.text=(n-1).ToString();Dirty(text);}
 }
 var binding=board.GetComponent<HybridGuidedBindings>();
 foreach(var note in binding.notesGroup.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Notification After Step "))){int n=int.Parse(note.name.Substring("Notification After Step ".Length));if(n>6)note.name="Notification After Step "+(n-1);}
 foreach(var t in All<Transform>().Where(t=>t.name.StartsWith("Quiz After Step ")||t.name.StartsWith("Quiz Completion "))){
 if(!t.IsChildOf(board.transform)&&!Path(t).Contains("/Hybrid Guided Popups/"))continue;
 string prefix=t.name.StartsWith("Quiz After")?"Quiz After Step ":"Quiz Completion ";int n=int.Parse(t.name.Substring(prefix.Length));if(n>6)t.name=prefix+(n-1);
 }
 var definition=JsonUtility.FromJson<Definition>(File.ReadAllText("Assets/_Project/Data/Hybrid/RE009Storyboard.json"));
 var list=All<StepListPopulator>().Single(t=>Path(t.transform).Contains("/Steps/Exp 1/"));list.steps=definition.instructions.Select(s=>s.english).ToList();list.popups=definition.notes.Select(n=>new StepListPopulator.PopupData{afterStep=n.afterInstruction,text=n.english}).ToList();Dirty(list);
 var sys=board.GetComponentInChildren<HybridTrainingController>(true);var enable=sys.monitor.GetComponentInChildren<HybridEnableSwitch>(true);enable.instructionIndex=board.steps.FindIndex(s=>s.interaction==enable.instruction);Dirty(enable);
 var speed=new SerializedObject(sys.windSpeed);speed.FindProperty("speedStepIndex").intValue=board.steps.FindIndex(s=>s.interaction==binding.speedWait);speed.ApplyModifiedPropertiesWithoutUndo();Dirty(sys.windSpeed);
 var movement=All<SmoothCameraMovement>().Single();var cameras=new SerializedObject(movement);var poses=cameras.FindProperty("cameraPositions");
 if(poses.arraySize==19&&poses.GetArrayElementAtIndex(18).objectReferenceValue is Transform pose&&pose.name=="Hybrid Inverter COM Port"){poses.arraySize=18;cameras.ApplyModifiedPropertiesWithoutUndo();Object.DestroyImmediate(pose.gameObject);Dirty(movement);}
 if(!string.IsNullOrEmpty(meshPath)&&meshPath.StartsWith("Assets/_Project/Models/Generated/Hybrid Wires/")&&!All<SplineExtrude>().Any(e=>e.targetMesh==mesh))AssetDatabase.DeleteAsset(meshPath);
 if(board.steps.Count!=56||board.steps.Count(s=>s.interaction is WireInteraction)!=36||panelRoot.childCount!=24)throw new Exception("Renumbered counts are incorrect.");
 Dirty(board);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
 Directory.CreateDirectory("Temp/RE009Planning");File.WriteAllText("Temp/RE009Planning/step-removal.txt","PASS: inverter Ethernet row, cable, added socket, generated mesh and unused COM camera removed. 23 instructions + finished; 56 framework entries; 36 wires; notes/quizzes/index labels/events/Enable/slider references renumbered.");
 }
}
