using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Adds the Exp 2 Solar Panel 3 negative alternatives to Main Board power.</summary>
public static class AddExp2RheostatPowerAlternatives
{
    private const int MainBoardConfigurationIndex = 0;

    [MenuItem("Free Lab/Setup/Add Exp 2 Rheostat Power Alternatives")]
    private static void AddAlternatives()
    {
        FreeLabControl mainBoard = Resources.FindObjectsOfTypeAll<FreeLabControl>()
            .FirstOrDefault(control => control != null &&
                control.ControlId == "Main Board On/Off");
        if (mainBoard == null)
        {
            Debug.LogError("Could not find the Free Lab Main Board On/Off control.");
            return;
        }

        SerializedObject boardData = new SerializedObject(mainBoard);
        SerializedProperty configurations = boardData.FindProperty("enableConfigurations");
        if (configurations == null || configurations.arraySize <= MainBoardConfigurationIndex)
        {
            Debug.LogError("Main Board On/Off Enable Configuration 0 was not found.");
            return;
        }

        SerializedProperty alternatives = configurations
            .GetArrayElementAtIndex(MainBoardConfigurationIndex)
            .FindPropertyRelative("anyConnectionGroups");
        if (alternatives == null)
        {
            Debug.LogError("Main Board On/Off Alternative Connection Groups were not found.");
            return;
        }

        FreeLabConnectionGroup existingGroup = alternatives.arraySize > 0
            ? alternatives.GetArrayElementAtIndex(0).objectReferenceValue as FreeLabConnectionGroup
            : null;
        Transform parent = existingGroup != null ? existingGroup.transform.parent : mainBoard.transform.parent;

        AddAlternative(alternatives, parent,
            "Main Board On/Off - enableConfigurations 0 - 100 Ohm Exp 2 alternative",
            "Main board to 100 Ohm Red",
            "Main board to 100 Ohm Black",
            "Free Template - 100 Ohm Terminal 1 to Solar Panel 1 +ve",
            "Free Template - 100 Ohm Terminal 3 to Solar Panel 3 -ve");

        AddAlternative(alternatives, parent,
            "Main Board On/Off - enableConfigurations 0 - 150 Ohm Exp 2 alternative",
            "Main board to 150 Ohm Red",
            "Main board to 150 Ohm Black",
            "Free Template - 150 Ohm Terminal 1 to Solar Panel 1 +ve",
            "Free Template - 150 Ohm Terminal 3 to Solar Panel 3 -ve");

        AddAlternative(alternatives, parent,
            "Main Board On/Off - enableConfigurations 0 - 800 Ohm Exp 2 alternative",
            "Main board to 800 Ohm Red",
            "Main board to 800 Ohm Black",
            "Solar Panel 1 +ve to 800Ohm Terminal 1",
            "800 Ohm Terminal 3 to Solar Panel 3 -ve");

        boardData.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(mainBoard.gameObject.scene);
        Debug.Log("Added the 100, 150, and 800 Ohm Exp 2 power alternatives. Existing alternatives were left unchanged.");
    }

    [MenuItem("Free Lab/Add Exp 2 Rheostat Power Alternatives Now", false, 1)]
    private static void AddAlternativesNow()
    {
        AddAlternatives();
    }

    private static void AddAlternative(SerializedProperty alternatives, Transform parent,
        string groupName, params string[] wireNames)
    {
        if (ContainsGroup(alternatives, groupName))
            return;

        List<FreeLabWireDefinition> wires = new List<FreeLabWireDefinition>();
        foreach (string wireName in wireNames)
        {
            FreeLabWireDefinition wire = FindWireInActiveScene(wireName);
            if (wire == null)
            {
                Debug.LogError($"Exp 2 rheostat alternative not created: wire '{wireName}' was not found.");
                return;
            }
            wires.Add(wire);
        }

        GameObject groupObject = new GameObject(groupName);
        if (parent != null)
            groupObject.transform.SetParent(parent, false);

        FreeLabConnectionGroup group = groupObject.AddComponent<FreeLabConnectionGroup>();
        group.Configure(groupName, wires);

        alternatives.arraySize++;
        alternatives.GetArrayElementAtIndex(alternatives.arraySize - 1)
            .objectReferenceValue = group;
    }

    private static FreeLabWireDefinition FindWireInActiveScene(string wireName)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            FreeLabWireDefinition wire = root
                .GetComponentsInChildren<FreeLabWireDefinition>(true)
                .FirstOrDefault(item => item != null &&
                    string.Equals(item.gameObject.name.Trim(), wireName.Trim(),
                        System.StringComparison.Ordinal));
            if (wire != null)
                return wire;
        }

        return null;
    }

    private static bool ContainsGroup(SerializedProperty alternatives, string groupName)
    {
        for (int i = 0; i < alternatives.arraySize; i++)
        {
            FreeLabConnectionGroup group = alternatives.GetArrayElementAtIndex(i)
                .objectReferenceValue as FreeLabConnectionGroup;
            if (group != null && group.name == groupName)
                return true;
        }
        return false;
    }
}
