using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class WindExp4MenuAndStoryboardSetup
{
    const string Id="OFF Grid System Using Wind Turbine";
    const string Display="Experiment 4: OFF Grid System Using Wind Turbine";
    static Transform Card(Transform t){while(t.parent!=null&&t.parent.GetComponent<HorizontalLayoutGroup>()==null)t=t.parent;return t;}
    static void SetCard(Transform card)
    {
        card.name=Display;card.gameObject.SetActive(true);PrefabUtility.RecordPrefabInstancePropertyModifications(card.gameObject);
        foreach(var text in card.GetComponentsInChildren<TMP_Text>(true))
        {
            if(text.text.Length>100)text.text="Explore an off-grid wind energy system. Connect the turbine, rectifier, inverter, battery and lamp modules, then follow the guided experiment to test the system.";
            else if(text.text.Contains("Setting up PV")||text.text.Contains("Regulating DC")||text.text.Contains("Optimum Alignment"))text.text=Id;
            EditorUtility.SetDirty(text);PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }
    [MenuItem("PV System/Configure EXP4 Storyboard And Both Menus")]
    public static void Configure()
    {
        if(Application.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene edits and stop Play Mode first.");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hydro Lab.unity");
        WindExp4Step1Setup.OnlyNewBlack=true;
        try{WindExp4Step1Setup.Configure();}finally{WindExp4Step1Setup.OnlyNewBlack=false;}
        var owner=Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(o=>o.name=="Exp 4"&&o.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Steps"));
        var steps=owner.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Steps");
        var ordered=steps.Find("Step 1").Cast<Transform>().Select(t=>t.GetComponent<WireInteraction>()).ToArray();
        if(ordered.Length!=10||ordered.Any(w=>w==null))throw new InvalidOperationException("Expected ten Step 1 wire interactions.");
        int ethernet=owner.steps.FindIndex(s=>s.interaction!=null&&s.interaction.name.Contains("ethernet"));
        if(ethernet<0)throw new InvalidOperationException("Ethernet step missing.");
        var preservedTail=owner.steps.Skip(ethernet).ToList();
        var ui=owner.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Exp 4 UI");
        var ui1=ui.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Subtitle Step 1").gameObject;
        var ui2=ui.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Subtitle Step 2").gameObject;
        var prefix=new List<StoryboardSteps.Step>();
        foreach(var wire in ordered)
        {
            var original=owner.steps.Take(ethernet).FirstOrDefault(s=>s.interaction==wire);
            var row=original??new StoryboardSteps.Step{stepType=StepType.Interaction,interaction=wire};row.uiObject=ui1;prefix.Add(row);
        }
        preservedTail[0].uiObject=ui2;prefix.AddRange(preservedTail);owner.steps=prefix;
        var so=new SerializedObject(owner);so.FindProperty("storyboardID").stringValue=Id;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(owner);
        var manager=Object.FindObjectsByType<InteractionManager>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
        var selector=Object.FindObjectsByType<ExperimentSelector>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
        var start=Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(b=>Card(b.transform).name.StartsWith("Experiment 4:")&&b.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text.Trim().Equals("Start",StringComparison.OrdinalIgnoreCase)));
        while(start.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(start.onClick,0);
        UnityEventTools.AddStringPersistentListener(start.onClick,manager.SetExperimentName,Display);
        UnityEventTools.AddObjectPersistentListener(start.onClick,selector.SelectExperiment,owner);
        EditorUtility.SetDirty(start);PrefabUtility.RecordPrefabInstancePropertyModifications(start);SetCard(Card(start.transform));
        var ss=new SerializedObject(selector);var list=ss.FindProperty("experiments");
        if(!Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).objectReferenceValue==owner))
        {list.InsertArrayElementAtIndex(list.arraySize);list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=owner;ss.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(selector);}
        if(owner.steps[10].interaction!=preservedTail[0].interaction||!owner.steps.Take(10).Select(s=>s.interaction).SequenceEqual(ordered.Cast<BaseInteraction>()))throw new InvalidOperationException("Storyboard order mismatch.");
        Debug.Log("EXP4 STORYBOARD VERIFIED: ten Step1 connections in exact hierarchy order using EXP4 Subtitle Step1; original Ethernet at element10 using EXP4 Subtitle Step2; all later rows preserved.");
        EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");
        var launcher=Object.FindObjectsByType<MainMenuExperimentLauncher>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(l=>new SerializedObject(l).FindProperty("experimentName").stringValue.StartsWith("Experiment 4:"));
        launcher.Configure(Id,Display);EditorUtility.SetDirty(launcher);PrefabUtility.RecordPrefabInstancePropertyModifications(launcher);SetCard(Card(launcher.transform));
        EditorSceneManager.MarkSceneDirty(launcher.gameObject.scene);EditorSceneManager.SaveScene(launcher.gameObject.scene);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hydro Lab.unity");
        Debug.Log("EXP4 BOTH MENUS SAVED: "+Display+" launches the new EXP4 via unique storyboard ID "+Id);
    }
}
