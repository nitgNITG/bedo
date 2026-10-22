using System;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class WindExp2MenuSetup
{
    [MenuItem("PV System/Add Wind EXP3 To Both Menus")]
    public static void ConfigureExp3()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save your current scene edits first.");
        const string id = "Regulating DC Output from Wind Turbine using MPPT Charger";
        string display = "Experiment 3: " + id;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");
        var owner = Object.FindObjectsByType<TutorialFramework.StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(s => s.StoryboardID == id);
        var manager = Object.FindObjectsByType<TutorialFramework.InteractionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var labSelector = Object.FindObjectsByType<ExperimentSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform candidate = button.transform;
            while (candidate.parent != null && candidate.parent.GetComponent<HorizontalLayoutGroup>() == null) candidate = candidate.parent;
            bool isThird = candidate.name.StartsWith("Experiment 3:") || candidate.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.IndexOf("parallel configuration", StringComparison.OrdinalIgnoreCase) >= 0);
            bool isStart = button.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Trim().Equals("Start", StringComparison.OrdinalIgnoreCase));
            if (isThird && isStart)
            {
                while (button.onClick.GetPersistentEventCount() > 0) UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);
                UnityEditor.Events.UnityEventTools.AddStringPersistentListener(button.onClick, manager.SetExperimentName, display);
                UnityEditor.Events.UnityEventTools.AddObjectPersistentListener(button.onClick, labSelector.SelectExperiment, owner);
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
                SetExp3Card(button.transform, id, display);
                continue;
            }
            var settings = new SerializedObject(button);
            var calls = settings.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            bool matches = false;
            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                var target = call.FindPropertyRelative("m_Arguments.m_ObjectArgument").objectReferenceValue as TutorialFramework.StoryboardSteps;
                if (call.FindPropertyRelative("m_MethodName").stringValue != "SelectExperiment") continue;
                Transform card = button.transform;
                while (card.parent != null && card.parent.GetComponent<HorizontalLayoutGroup>() == null) card = card.parent;
                bool thirdCard = card.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains("Parallel configuration")) || card.name.StartsWith("Experiment 3:");
                if (target != owner && !thirdCard) continue;
                call.FindPropertyRelative("m_Arguments.m_ObjectArgument").objectReferenceValue = owner;
                matches = true;
            }
            if (!matches) continue;
            settings.ApplyModifiedPropertiesWithoutUndo();
            int nameIndex = Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Where(i => button.onClick.GetPersistentMethodName(i) == "SetExperimentName").DefaultIfEmpty(-1).First();
            if (nameIndex >= 0) UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, nameIndex);
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(button.onClick, manager.SetExperimentName, display);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            SetExp3Card(button.transform, id, display);
        }
        foreach (var selector in Object.FindObjectsByType<ExperimentSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var settings = new SerializedObject(selector); var list = settings.FindProperty("experiments");
            if (!Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == owner))
            {
                list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = owner;
                settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(selector);
            }
        }
        EditorSceneManager.MarkSceneDirty(owner.gameObject.scene); EditorSceneManager.SaveScene(owner.gameObject.scene);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");
        var launcher = Object.FindObjectsByType<MainMenuExperimentLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(l => new SerializedObject(l).FindProperty("experimentName").stringValue.StartsWith("Experiment 3:"));
        launcher.Configure(id, display); EditorUtility.SetDirty(launcher);
        SetExp3Card(launcher.transform, id, display);
        EditorSceneManager.MarkSceneDirty(launcher.gameObject.scene); EditorSceneManager.SaveScene(launcher.gameObject.scene);
        Debug.Log("Wind EXP3 cards saved in both menus with the MPPT storyboard ID.");
    }

    private static void SetExp3Card(Transform leaf, string id, string display)
    {
        Transform card = leaf;
        while (card.parent != null && card.parent.GetComponent<HorizontalLayoutGroup>() == null) card = card.parent;
        if (card.parent == null) throw new InvalidOperationException("Experiment card container not found.");
        card.gameObject.SetActive(true); card.name = display;
        PrefabUtility.RecordPrefabInstancePropertyModifications(card.gameObject);
        foreach (var text in card.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text.Contains("PV Characteristic Curve")) text.text = id;
            else if (text.text.Length > 100)
                text.text = "Welcome! Learn how a wind turbine uses an MPPT charge controller to supply a DC halogen lamp and charge a 24 V battery.\n\nObjective of the Experiment\nObserve how turbine speed affects generated voltage, current, and power, and how the battery changes between discharging and charging.";
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }

    public const string Id = "Regulating DC Output from Wind Turbine using PWM Charger";

    [MenuItem("PV System/Add Wind EXP2 Main Menu Card")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save your current scene edits first.");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");
        var launcher = Object.FindObjectsByType<MainMenuExperimentLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(l => new SerializedObject(l).FindProperty("experimentName").stringValue.StartsWith("Experiment 2:"));
        launcher.Configure(Id, "Experiment 2: " + Id);
        EditorUtility.SetDirty(launcher);
        Transform card = launcher.transform;
        while (card.parent != null && card.parent.GetComponent<HorizontalLayoutGroup>() == null) card = card.parent;
        if (card.parent == null) throw new InvalidOperationException("Experiment card container not found.");
        card.gameObject.SetActive(true);
        card.name = "Experiment 2: " + Id;
        PrefabUtility.RecordPrefabInstancePropertyModifications(card.gameObject);
        foreach (var text in card.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text.Contains("PV Characteristic Curve")) text.text = Id;
            else if (text.text.Length > 100)
                text.text = "Welcome! Learn how a wind turbine uses a PWM charge controller to supply a DC halogen lamp and charge a 24 V battery.\n\nObjective of the Experiment\nObserve how changing turbine speed affects generated voltage, current, and power, and how the battery changes between discharging and charging.";
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
        EditorSceneManager.MarkSceneDirty(launcher.gameObject.scene);
        EditorSceneManager.SaveScene(launcher.gameObject.scene);
        Debug.Log("Wind EXP2 Main Menu card saved with the exact PWM storyboard ID. EXP1 card retained.");
    }
}
