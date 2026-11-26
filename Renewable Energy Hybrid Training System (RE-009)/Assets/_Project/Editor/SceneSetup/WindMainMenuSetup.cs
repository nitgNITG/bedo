using System;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WindMainMenuSetup
{
    [MenuItem("Tools/Wind/Update Main Menu For EXP1")]
    public static void Configure()
    {
        var lab = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hybrid Lab.unity");
        var storyboard = UnityEngine.Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(s => s.StoryboardID == "Speed Curve Characteristics");
        string id = storyboard.StoryboardID;
        // Reuse the authored EXP1 introduction, objective, and card title.
        var labTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var title = labTexts.Single(t => t.text.Trim() == "Speed Curve Characteristics");
        Transform labCard = title.transform;
        while (labCard.parent != null && !labCard.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains("Welcome!")))
            labCard = labCard.parent;
        string TextKey(TMP_Text text)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(text);
            if (source != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId))
                return guid + ":" + localId;
            return AnimationUtility.CalculateTransformPath(text.transform, labCard);
        }
        var textByPath = labCard.GetComponentsInChildren<TMP_Text>(true)
            .ToDictionary(TextKey, t => t.text);

        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main Menu.unity");
        var launchers = UnityEngine.Object.FindObjectsByType<MainMenuExperimentLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var exp1 = launchers.Single(l => new SerializedObject(l).FindProperty("experimentName").stringValue.StartsWith("Experiment 1:"));
        Transform content = exp1.transform.parent;
        while (content != null && !launchers.All(l => l.transform.IsChildOf(content))) content = content.parent;
        if (content == null) throw new InvalidOperationException("Experiment cards do not share a content container.");
        Transform Card(Transform button)
        {
            while (button.parent != content) button = button.parent;
            return button;
        }
        Transform firstCard = Card(exp1.transform);
        foreach (var launcher in launchers) Card(launcher.transform).gameObject.SetActive(launcher == exp1);
        firstCard.gameObject.SetActive(true);
        firstCard.name = "Experiment 1: Speed Curve Characteristics";
        PrefabUtility.RecordPrefabInstancePropertyModifications(firstCard.gameObject);
        foreach (var text in firstCard.GetComponentsInChildren<TMP_Text>(true))
            if (textByPath.TryGetValue(TextKey(text), out var value))
            {
                text.text = value;
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
        exp1.Configure(id, "Experiment 1: Speed Curve Characteristics");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Wind main menu configured: only EXP1, storyboard ID = " + id + "; card=" + firstCard.name);
    }
}
