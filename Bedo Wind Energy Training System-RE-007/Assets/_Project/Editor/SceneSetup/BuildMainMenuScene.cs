using System.Collections.Generic;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time Unity editor utility that splits the current combined scene into:
/// - Main Menu: the existing experiment-selection UI only
/// - PV Lab: the complete physical lab and experiment scenes
///
/// This intentionally uses Unity's scene API, so all retained references are
/// remapped by Unity rather than by editing YAML scene files directly.
/// </summary>
public static class BuildMainMenuScene
{
    private const string SourceScene =
        "Assets/_Reference/Scenes/PVTemplate/Plc_Lab-solar.unity";

    private const string LabScene =
        "Assets/_Project/Scenes/PV Lab.unity";

    private const string MenuScene =
        "Assets/_Project/Scenes/Main Menu.unity";

    [MenuItem("Tools/PV Lab/Create Main Menu and Lab Scenes")]
    public static void CreateScenes()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LabScene) != null ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene) != null)
        {
            Debug.LogError(
                "Scene split stopped: Main Menu or PV Lab already exists. " +
                "This command never overwrites existing scenes."
            );
            return;
        }


        if (!AssetDatabase.CopyAsset(SourceScene, LabScene) ||
            !AssetDatabase.CopyAsset(SourceScene, MenuScene))
        {
            Debug.LogError("Scene split stopped: Unity could not duplicate the source scene.");
            return;
        }


        AssetDatabase.SaveAssets();


        Scene menu = EditorSceneManager.OpenScene(
            MenuScene,
            OpenSceneMode.Single
        );


        ConfigureSceneManager(menu, "PV Lab", "Main Menu");
        StripPhysicalLabFromMenu(menu);
        EditorSceneManager.SaveScene(menu);


        Scene lab = EditorSceneManager.OpenScene(
            LabScene,
            OpenSceneMode.Single
        );


        ConfigureSceneManager(lab, "PV Lab", "Main Menu");
        EditorSceneManager.SaveScene(lab);


        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(LabScene, true)
        };


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        Debug.Log(
            "Scene split complete. Build starts at Main Menu; " +
            "selecting an experiment opens PV Lab."
        );
    }


    [MenuItem("Tools/PV Lab/Rebuild Main Menu Scene")]
    public static void RebuildMainMenuScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene) == null)
        {
            Debug.LogError("Main Menu scene was not found. Create the scene split first.");
            return;
        }


        Scene menu = EditorSceneManager.OpenScene(
            MenuScene,
            OpenSceneMode.Single
        );


        ConfigureSceneManager(menu, "PV Lab", "Main Menu");
        StripPhysicalLabFromMenu(menu);
        EditorSceneManager.SaveScene(menu);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        Debug.Log("Main Menu rebuilt as a UI-only scene.");
    }


    private static void ConfigureSceneManager(
        Scene scene,
        string labSceneName,
        string mainMenuSceneName
    )
    {
        foreach (InteractionManager manager in
                 Object.FindObjectsByType<InteractionManager>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (manager.gameObject.scene != scene)
                continue;


            SerializedObject serializedManager =
                new SerializedObject(manager);

            serializedManager.FindProperty("scenarioSceneName")
                .stringValue = labSceneName;
            serializedManager.FindProperty("mainMenuSceneName")
                .stringValue = mainMenuSceneName;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }


    private static void StripPhysicalLabFromMenu(Scene menu)
    {
        // These roots contain the physical rack, bananas/wires, auxiliary 3D
        // objects, and the runtime Free Lab system.  The experiment-selection
        // UI stays under Experiments, along with its existing button wiring.
        string[] physicalRoots =
        {
            "Scene",
            "Plugs",
            "Accessories",
            "Camera move",
            "PV Data Manager"
        };


        foreach (string rootName in physicalRoots)
        {
            GameObject root = FindRoot(menu, rootName);

            if (root != null)
                Object.DestroyImmediate(root);
        }


        // Convert the visible menu's existing experiment buttons into simple
        // ID-based launchers before removing all Exp 1..Exp 8 objects.  This
        // preserves the menu artwork and its buttons without retaining the
        // heavy storyboard, wiring, evaluator, and free-lab hierarchies.
        CreateMenuLaunchers(menu);


        GameObject experimentsRoot = FindRoot(menu, "Experiments");

        if (experimentsRoot != null)
            Object.DestroyImmediate(experimentsRoot);


    }


    private static void CreateMenuLaunchers(Scene menu)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (button.gameObject.scene != menu)
                continue;


            SerializedObject serializedButton =
                new SerializedObject(button);
            SerializedProperty calls = serializedButton.FindProperty(
                "m_OnClick.m_PersistentCalls.m_Calls"
            );


            string storyboardId = null;
            string experimentName = null;
            bool isExperimentButton = false;


            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                Object target = call.FindPropertyRelative("m_Target")
                    .objectReferenceValue;
                string method = call.FindPropertyRelative("m_MethodName")
                    .stringValue;
                SerializedProperty arguments = call.FindPropertyRelative(
                    "m_Arguments"
                );


                if (target is ExperimentSelector &&
                    method == "SelectExperiment")
                {
                    StoryboardSteps storyboard = arguments.FindPropertyRelative(
                        "m_ObjectArgument"
                    ).objectReferenceValue as StoryboardSteps;


                    if (storyboard != null)
                    {
                        storyboardId = storyboard.StoryboardID;
                        isExperimentButton = !string.IsNullOrWhiteSpace(
                            storyboardId
                        );
                    }
                }


                if (target is StoryboardSteps &&
                    method == "SetExperimentName")
                {
                    experimentName = arguments.FindPropertyRelative(
                        "m_StringArgument"
                    ).stringValue;
                }
            }


            if (!isExperimentButton)
                continue;


            MainMenuExperimentLauncher launcher =
                button.GetComponent<MainMenuExperimentLauncher>();


            if (launcher == null)
                launcher = button.gameObject.AddComponent<
                    MainMenuExperimentLauncher
                >();


            launcher.Configure(storyboardId, experimentName);


            // These callbacks target the Exp 1..Exp 8 objects we remove
            // below.  The launcher replaces them completely in Main Menu.
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(
                    button.onClick,
                    0
                );
            }
        }
    }


    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name)
                    return item.gameObject;
        }


        return null;
    }
}
