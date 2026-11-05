using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates editable device-alternative holders only. It deliberately does not
/// guess a lamp's cable pairs; assign each device's Required Wires in the
/// Inspector from the authored FreeLabWireDefinition components.
/// </summary>
public static class FreeLabDeviceAlternativeBuilder
{
    [MenuItem("Free Lab/Create editable AC and DC lamp alternatives")]
    private static void CreateLampAlternatives()
    {
        FreeLabManager lab = Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
        {
            Debug.LogError("Free Lab device setup: no FreeLabManager was found.");
            return;
        }

        Transform managerRoot = GetOrCreate(lab.transform, "Free Lab Device Manager");
        FreeLabDeviceManager deviceManager = managerRoot.GetComponent<FreeLabDeviceManager>();
        if (deviceManager == null)
            deviceManager = Undo.AddComponent<FreeLabDeviceManager>(managerRoot.gameObject);

        List<FreeLabWireDefinition> wires = lab.GetComponentsInChildren<
            FreeLabWireDefinition>(true).Where(wire => wire != null).ToList();

        FreeLabDeviceGroup dcGroup = CreateGroup(managerRoot, "DC Lamp Alternatives", new[]
        {
            "DC LED Lamp Device", "DC Halogen Lamp Device"
        });
        FreeLabDeviceGroup acGroup = CreateGroup(managerRoot, "AC Lamp Alternatives", new[]
        {
            "AC LED Lamp Device", "AC Halogen Lamp Device"
        });

        deviceManager.Configure(new[] { dcGroup, acGroup });
        EditorUtility.SetDirty(deviceManager);

        ConfigureDevice(dcGroup, "DC LED Lamp Device", wires,
            "Battery Output to DC LED Lamp Red", "Battery Output to DC LED Lamp Black");
        ConfigureDevice(dcGroup, "DC Halogen Lamp Device", wires,
            "Battery Output to DC Halogen Lamp Red", "Battery Output to DC Halogen Lamp Black");
        ConfigureDevice(acGroup, "AC LED Lamp Device", wires,
            "Main Board to AC LED Lamp Red", "Main Board to AC LED Lamp Black");
        ConfigureDevice(acGroup, "AC Halogen Lamp Device", wires,
            "Main Board to AC Halogen Lamp Red", "Main Board to AC Halogen Lamp Black");

        ConfigureExp8Alternatives(lab, dcGroup, acGroup, wires);

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Created AC/DC lamp alternatives and assigned the existing two-terminal lamp wires. The Exp 8 Free Lab evaluator now accepts either device in each lamp group.", managerRoot);
    }

    private static FreeLabDeviceGroup CreateGroup(Transform parent, string name, string[] deviceNames)
    {
        Transform groupRoot = GetOrCreate(parent, name);
        FreeLabDeviceGroup group = groupRoot.GetComponent<FreeLabDeviceGroup>();
        if (group == null)
            group = Undo.AddComponent<FreeLabDeviceGroup>(groupRoot.gameObject);

        List<FreeLabDevice> devices = new List<FreeLabDevice>();
        foreach (string deviceName in deviceNames)
        {
            Transform deviceRoot = GetOrCreate(groupRoot, deviceName);
            FreeLabDevice device = deviceRoot.GetComponent<FreeLabDevice>();
            if (device == null)
                device = Undo.AddComponent<FreeLabDevice>(deviceRoot.gameObject);
            devices.Add(device);
        }

        group.Configure(name, devices);
        EditorUtility.SetDirty(group);
        return group;
    }

    private static void ConfigureDevice(FreeLabDeviceGroup group,
        string deviceName, List<FreeLabWireDefinition> allWires,
        params string[] requiredNameFragments)
    {
        FreeLabDevice device = group.Devices.FirstOrDefault(candidate =>
            candidate != null && candidate.gameObject.name == deviceName);
        if (device == null)
            return;

        List<FreeLabWireDefinition> matched = new List<FreeLabWireDefinition>();
        foreach (string fragment in requiredNameFragments)
        {
            FreeLabWireDefinition wire = allWires.FirstOrDefault(candidate =>
                candidate != null && candidate.gameObject.name.IndexOf(fragment,
                    System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (wire != null)
                matched.Add(wire);
            else
                Debug.LogWarning("Free Lab device setup could not find the wire: " + fragment, group);
        }

        device.ConfigureRequiredWires(matched);
        EditorUtility.SetDirty(device);
    }

    private static void ConfigureExp8Alternatives(FreeLabManager lab,
        FreeLabDeviceGroup dcGroup, FreeLabDeviceGroup acGroup,
        List<FreeLabWireDefinition> lampWires)
    {
        FreeLabExperimentEvaluator evaluator = lab.GetComponentsInChildren<
            FreeLabExperimentEvaluator>(true).FirstOrDefault(candidate =>
            candidate != null && candidate.ExperimentId.IndexOf("8",
                System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (evaluator == null)
            return;

        SerializedObject serialized = new SerializedObject(evaluator);
        SerializedProperty states = serialized.FindProperty("states");
        for (int stateIndex = 1; stateIndex < states.arraySize; stateIndex++)
        {
            SerializedProperty state = states.GetArrayElementAtIndex(stateIndex);
            RemoveLampConnections(state.FindPropertyRelative("requiredConnections"), lampWires);
            EnsureGroup(state.FindPropertyRelative("requiredDeviceGroups"), dcGroup);
            EnsureGroup(state.FindPropertyRelative("requiredDeviceGroups"), acGroup);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(evaluator);
    }

    private static void RemoveLampConnections(SerializedProperty connections,
        List<FreeLabWireDefinition> lampWires)
    {
        if (connections == null)
            return;

        for (int index = connections.arraySize - 1; index >= 0; index--)
        {
            SerializedProperty entry = connections.GetArrayElementAtIndex(index);
            FreeLabEndpoint endpointA = entry.FindPropertyRelative("endpointA").objectReferenceValue as FreeLabEndpoint;
            FreeLabEndpoint endpointB = entry.FindPropertyRelative("endpointB").objectReferenceValue as FreeLabEndpoint;
            bool isLampConnection = lampWires.Any(wire => wire != null &&
                ((wire.endpointA == endpointA && wire.endpointB == endpointB) ||
                 (wire.endpointA == endpointB && wire.endpointB == endpointA)));
            if (isLampConnection)
                connections.DeleteArrayElementAtIndex(index);
        }
    }

    private static void EnsureGroup(SerializedProperty groups,
        FreeLabDeviceGroup group)
    {
        if (groups == null || group == null)
            return;

        for (int index = 0; index < groups.arraySize; index++)
        {
            if (groups.GetArrayElementAtIndex(index).FindPropertyRelative("deviceGroup")
                    .objectReferenceValue == group)
            {
                return;
            }
        }

        int newIndex = groups.arraySize++;
        groups.GetArrayElementAtIndex(newIndex).FindPropertyRelative("deviceGroup")
            .objectReferenceValue = group;
    }

    private static Transform GetOrCreate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Create Free Lab device alternative");
        created.transform.SetParent(parent, false);
        return created.transform;
    }
}
