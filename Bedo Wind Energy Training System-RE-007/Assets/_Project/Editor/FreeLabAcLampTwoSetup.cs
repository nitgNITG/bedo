using System;
using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Free Mode-only setup for the second (bottom) AC lamp on both AC modules.
/// It duplicates the existing Lamp 1 L/N sockets, plugs, wires, and visuals
/// in place, then makes Lamp 1 and Lamp 2 mutually exclusive complete paths.
/// The duplicate sockets and splines deliberately remain over Lamp 1 so their
/// final physical placement can be authored afterwards.
/// </summary>
public static class FreeLabAcLampTwoSetup
{
    private const string LedTopL =
        "Free Exp 9 - Off grid inverter AC output L to AC LED L";
    private const string LedTopN =
        "Free Exp 9 - Off grid inverter AC output N to AC LED N";
    private const string HalogenTopL =
        "Free Exp 9 - Off grid inverter AC output L to AC Halogen L";
    private const string HalogenTopN =
        "Free Exp 9 - Off grid inverter AC output N to AC Halogen N";

    [MenuItem("Free Lab/Create AC Lamp 2 alternatives")]
    internal static void CreateAcLampTwoAlternatives()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Exit Play Mode before creating the AC Lamp 2 setup.");
            return;
        }

        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
        {
            Debug.LogError("AC Lamp 2 setup: no FreeLabManager was found.");
            return;
        }

        List<FreeLabWireDefinition> allWires = lab
            .GetComponentsInChildren<FreeLabWireDefinition>(true)
            .Where(wire => wire != null).ToList();

        FreeLabWireDefinition ledTopL = FindWire(allWires, LedTopL);
        FreeLabWireDefinition ledTopN = FindWire(allWires, LedTopN);
        FreeLabWireDefinition halogenTopL = FindWire(allWires, HalogenTopL);
        FreeLabWireDefinition halogenTopN = FindWire(allWires, HalogenTopN);
        if (ledTopL == null || ledTopN == null || halogenTopL == null ||
            halogenTopN == null)
        {
            Debug.LogError("AC Lamp 2 setup stopped: one or more authored Lamp 1 L/N wires are missing.", lab);
            return;
        }

        FreeLabWireDefinition ledBottomL = DuplicateLampWire(
            allWires, ledTopL,
            "Free Exp 9 - Off grid inverter AC output L to AC LED Lamp 2 L",
            "AC LED Lamp 2 L [Free Exp 9]");
        FreeLabWireDefinition ledBottomN = DuplicateLampWire(
            allWires, ledTopN,
            "Free Exp 9 - Off grid inverter AC output N to AC LED Lamp 2 N",
            "AC LED Lamp 2 N [Free Exp 9]");
        FreeLabWireDefinition halogenBottomL = DuplicateLampWire(
            allWires, halogenTopL,
            "Free Exp 9 - Off grid inverter AC output L to AC Halogen Lamp 2 L",
            "AC Halogen Lamp 2 L [Free Exp 9]");
        FreeLabWireDefinition halogenBottomN = DuplicateLampWire(
            allWires, halogenTopN,
            "Free Exp 9 - Off grid inverter AC output N to AC Halogen Lamp 2 N",
            "AC Halogen Lamp 2 N [Free Exp 9]");

        if (ledBottomL == null || ledBottomN == null || halogenBottomL == null ||
            halogenBottomN == null)
            return;

        Transform manager = GetOrCreate(lab.transform, "Free Lab Device Manager");
        Transform pathsRoot = GetOrCreate(manager, "AC Lamp Number Paths");

        FreeLabConnectionGroup ledTopLn = ConfigureGroup(pathsRoot,
            "AC LED Lamp 1 L N", ledTopL, ledTopN);
        FreeLabConnectionGroup ledBottomLn = ConfigureGroup(pathsRoot,
            "AC LED Lamp 2 L N", ledBottomL, ledBottomN);
        FreeLabConnectionGroup halogenTopLn = ConfigureGroup(pathsRoot,
            "AC Halogen Lamp 1 L N", halogenTopL, halogenTopN);
        FreeLabConnectionGroup halogenBottomLn = ConfigureGroup(pathsRoot,
            "AC Halogen Lamp 2 L N", halogenBottomL, halogenBottomN);

        FreeLabConnectionGroup ledTopExp8 = ConfigureGroup(pathsRoot,
            "AC LED Lamp 1 Complete Exp 8",
            FindWire(allWires, "Free Exp 8 - Main Board to AC LED Lamp Red"),
            FindWire(allWires, "Free Exp 8 - Main Board to AC LED Lamp Black"),
            FindWire(allWires, "Free Exp 8 - AC LED Lamp Output to Off Grid Inverter Red"),
            FindWire(allWires, "Free Exp 8 - AC LED Lamp Output to Off Grid Inverter Black"));
        FreeLabConnectionGroup ledTopExp9 = ConfigureGroup(pathsRoot,
            "AC LED Lamp 1 Complete Exp 9",
            FindWire(allWires, "Free Exp 9 - Main Board to AC LED Lamp Red"),
            FindWire(allWires, "Free Exp 9 - Main Board to AC LED Lamp Black"), ledTopL, ledTopN);
        FreeLabConnectionGroup ledBottomExp9 = ConfigureGroup(pathsRoot,
            "AC LED Lamp 2 Complete Exp 9",
            FindWire(allWires, "Free Exp 9 - Main Board to AC LED Lamp Red"),
            FindWire(allWires, "Free Exp 9 - Main Board to AC LED Lamp Black"), ledBottomL, ledBottomN);
        FreeLabConnectionGroup halogenTopExp8 = ConfigureGroup(pathsRoot,
            "AC Halogen Lamp 1 Complete Exp 8",
            FindWire(allWires, "Free Exp 8 - Main Board to AC Halogen Lamp Red (1)"),
            FindWire(allWires, "Free Exp 8 - Main Board to AC Halogen Lamp Black (1)"),
            FindWire(allWires, "Free Exp 8 - AC Halogen Lamp Output to Off Grid Inverter Red (1)"),
            FindWire(allWires, "Free Exp 8 - AC Halogen Lamp Output to Off Grid Inverter Black (1)"));
        FreeLabConnectionGroup halogenTopExp9 = ConfigureGroup(pathsRoot,
            "AC Halogen Lamp 1 Complete Exp 9",
            FindWire(allWires, "Free Exp 9 - Main Board to AC Halogen Lamp Red"),
            FindWire(allWires, "Free Exp 9 - Main Board to AC Halogen Lamp Black"), halogenTopL, halogenTopN);
        FreeLabConnectionGroup halogenBottomExp9 = ConfigureGroup(pathsRoot,
            "AC Halogen Lamp 2 Complete Exp 9",
            FindWire(allWires, "Free Exp 9 - Main Board to AC Halogen Lamp Red"),
            FindWire(allWires, "Free Exp 9 - Main Board to AC Halogen Lamp Black"), halogenBottomL, halogenBottomN);

        if (new[] { ledTopExp8, ledTopExp9, ledBottomExp9, halogenTopExp8,
            halogenTopExp9, halogenBottomExp9 }.Any(group => group == null))
            return;

        FreeLabDevice ledTopDevice = FindDevice(lab, "AC LED Lamp Device");
        FreeLabDevice halogenTopDevice = FindDevice(lab, "AC Halogen Lamp Device");
        FreeLabDeviceGroup alternatives = FindGroup(lab, "AC Lamp Alternatives");
        if (ledTopDevice == null || halogenTopDevice == null || alternatives == null)
        {
            Debug.LogError("AC Lamp 2 setup stopped: the existing AC lamp device group is missing.", lab);
            return;
        }

        FreeLabDevice ledBottomDevice = DuplicateDevice(ledTopDevice,
            "AC LED Lamp 2 Device", "AC LED Lamp 2");
        FreeLabDevice halogenBottomDevice = DuplicateDevice(halogenTopDevice,
            "AC Halogen Lamp 2 Device", "AC Halogen Lamp 2");

        ledTopDevice.ConfigureRequiredWires(Array.Empty<FreeLabWireDefinition>());
        ledTopDevice.ConfigureAlternativeConnectionGroups(new[] { ledTopExp8, ledTopExp9 });
        halogenTopDevice.ConfigureRequiredWires(Array.Empty<FreeLabWireDefinition>());
        halogenTopDevice.ConfigureAlternativeConnectionGroups(new[] { halogenTopExp8, halogenTopExp9 });
        ledBottomDevice.ConfigureRequiredWires(Array.Empty<FreeLabWireDefinition>());
        ledBottomDevice.ConfigureAlternativeConnectionGroups(new[] { ledBottomExp9 });
        halogenBottomDevice.ConfigureRequiredWires(Array.Empty<FreeLabWireDefinition>());
        halogenBottomDevice.ConfigureAlternativeConnectionGroups(new[] { halogenBottomExp9 });

        alternatives.Configure("AC Lamp Alternatives", new[]
        {
            ledTopDevice, ledBottomDevice, halogenTopDevice, halogenBottomDevice
        });

        ConfigurePairGuard(manager, ledTopDevice, ledBottomDevice,
            halogenTopDevice, halogenBottomDevice,
            ledTopExp9.RequiredWires, ledBottomExp9.RequiredWires,
            halogenTopExp9.RequiredWires, halogenBottomExp9.RequiredWires);

        ReplaceTopLnRequirements(lab, ledTopLn, ledBottomLn, ledTopL, ledTopN);
        ReplaceTopLnRequirements(lab, halogenTopLn, halogenBottomLn,
            halogenTopL, halogenTopN);

        EditorUtility.SetDirty(ledTopDevice);
        EditorUtility.SetDirty(ledBottomDevice);
        EditorUtility.SetDirty(halogenTopDevice);
        EditorUtility.SetDirty(halogenBottomDevice);
        EditorUtility.SetDirty(alternatives);
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);

        Debug.Log(
            "Created Free Mode AC Lamp 2 alternatives. New wires: " +
            "\n- " + ledBottomL.name + "\n- " + ledBottomN.name +
            "\n- " + halogenBottomL.name + "\n- " + halogenBottomN.name +
            "\nNew sockets and light visuals were cloned in place for author positioning.",
            lab);
    }

    internal static bool HasCreatedExpectedWires()
    {
        string[] expected =
        {
            "Free Exp 9 - Off grid inverter AC output L to AC LED Lamp 2 L",
            "Free Exp 9 - Off grid inverter AC output N to AC LED Lamp 2 N",
            "Free Exp 9 - Off grid inverter AC output L to AC Halogen Lamp 2 L",
            "Free Exp 9 - Off grid inverter AC output N to AC Halogen Lamp 2 N"
        };

        return expected.All(name => UnityEngine.Object
            .FindObjectsByType<FreeLabWireDefinition>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(wire => wire != null && wire.gameObject.name == name));
    }

    private static FreeLabWireDefinition DuplicateLampWire(
        List<FreeLabWireDefinition> allWires, FreeLabWireDefinition source,
        string newWireName, string newLampEndpointName)
    {
        FreeLabWireDefinition existing = FindWire(allWires, newWireName);
        if (existing != null)
            return existing;

        FreeLabEndpoint sourceLamp = FindLampEndpoint(source);
        if (sourceLamp == null)
        {
            Debug.LogError("AC Lamp 2 setup could not identify the lamp endpoint on " + source.name, source);
            return null;
        }

        FreeLabEndpoint bottomLamp = DuplicateEndpoint(sourceLamp, newLampEndpointName);
        if (bottomLamp == null)
            return null;

        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject,
            source.transform.parent, false);
        clone.name = newWireName;
        CopyLocalTransform(source.transform, clone.transform);
        Undo.RegisterCreatedObjectUndo(clone, "Create AC Lamp 2 wire");

        FreeLabWireDefinition definition = clone.GetComponent<FreeLabWireDefinition>();
        bool lampIsA = source.endpointA == sourceLamp;
        definition.endpointA = lampIsA ? bottomLamp : source.endpointA;
        definition.endpointB = lampIsA ? source.endpointB : bottomLamp;
        definition.plugA = lampIsA ? bottomLamp.SelectionPlug : source.plugA;
        definition.plugB = lampIsA ? source.plugB : bottomLamp.SelectionPlug;
        definition.wireParentTemplate = clone;
        EditorUtility.SetDirty(definition);
        allWires.Add(definition);
        return definition;
    }

    private static FreeLabEndpoint DuplicateEndpoint(FreeLabEndpoint source,
        string newName)
    {
        FreeLabEndpoint existing = Resources.FindObjectsOfTypeAll<FreeLabEndpoint>()
            .FirstOrDefault(endpoint => endpoint != null && endpoint.gameObject.name == newName);
        if (existing != null)
            return existing;

        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject,
            source.transform.parent, false);
        clone.name = newName;
        CopyLocalTransform(source.transform, clone.transform);
        Undo.RegisterCreatedObjectUndo(clone, "Create AC Lamp 2 endpoint");

        FreeLabEndpoint endpoint = clone.GetComponent<FreeLabEndpoint>();
        endpoint.SetEndpointId(newName);
        endpoint.ClearAdditionalConnectionPlugs();

        if (source.SelectionPlug != null)
        {
            GameObject plug = UnityEngine.Object.Instantiate(source.SelectionPlug,
                source.SelectionPlug.transform.parent, false);
            plug.name = newName + " Plug";
            CopyLocalTransform(source.SelectionPlug.transform, plug.transform);
            plug.SetActive(false);
            Undo.RegisterCreatedObjectUndo(plug, "Create AC Lamp 2 plug");
            endpoint.SetSelectionPlug(plug);
        }

        EditorUtility.SetDirty(endpoint);
        return endpoint;
    }

    private static FreeLabDevice DuplicateDevice(FreeLabDevice source,
        string newName, string visualPrefix)
    {
        FreeLabDevice existing = source.transform.parent.GetComponentsInChildren<FreeLabDevice>(true)
            .FirstOrDefault(device => device != null && device.gameObject.name == newName);
        if (existing != null)
            return existing;

        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject,
            source.transform.parent, false);
        clone.name = newName;
        CopyLocalTransform(source.transform, clone.transform);
        Undo.RegisterCreatedObjectUndo(clone, "Create AC Lamp 2 device");

        FreeLabDevice device = clone.GetComponent<FreeLabDevice>();
        DuplicateVisualList(source, device, "objectsToTurnOn", visualPrefix + " Light On");
        DuplicateVisualList(source, device, "objectsToTurnOff", visualPrefix + " Light Off");
        EditorUtility.SetDirty(device);
        return device;
    }

    private static void ConfigurePairGuard(Transform manager,
        FreeLabDevice ledTop, FreeLabDevice ledBottom,
        FreeLabDevice halogenTop, FreeLabDevice halogenBottom,
        IEnumerable<FreeLabWireDefinition> ledTopWires,
        IEnumerable<FreeLabWireDefinition> ledBottomWires,
        IEnumerable<FreeLabWireDefinition> halogenTopWires,
        IEnumerable<FreeLabWireDefinition> halogenBottomWires)
    {
        FreeLabAcLampPairGuard guard = manager.GetComponent<FreeLabAcLampPairGuard>();
        if (guard == null)
            guard = Undo.AddComponent<FreeLabAcLampPairGuard>(manager.gameObject);

        guard.Configure(new[]
        {
            CreateLampPath(ledTop, ledTopWires),
            CreateLampPath(ledBottom, ledBottomWires),
            CreateLampPath(halogenTop, halogenTopWires),
            CreateLampPath(halogenBottom, halogenBottomWires)
        });
        EditorUtility.SetDirty(guard);
    }

    private static FreeLabAcLampPairGuard.LampPath CreateLampPath(
        FreeLabDevice device, IEnumerable<FreeLabWireDefinition> wires)
    {
        return new FreeLabAcLampPairGuard.LampPath
        {
            device = device,
            // Use the precise authored visual reference on this device.
            // Several visuals have identical names in the scene, so a global
            // name lookup can cross-wire two physical lamps.
            onVisual = device != null && device.ObjectsToTurnOn.Count > 0
                ? device.ObjectsToTurnOn[0] : null,
            offVisual = device != null && device.ObjectsToTurnOff.Count > 0
                ? device.ObjectsToTurnOff[0] : null,
            requiredWires = wires != null
                ? wires.Where(wire => wire != null).ToList()
                : new List<FreeLabWireDefinition>()
        };
    }

    private static void DuplicateVisualList(FreeLabDevice source,
        FreeLabDevice destination, string propertyName, string newName)
    {
        SerializedObject sourceSerialized = new SerializedObject(source);
        SerializedObject destinationSerialized = new SerializedObject(destination);
        SerializedProperty sourceObjects = sourceSerialized.FindProperty(propertyName);
        SerializedProperty destinationObjects = destinationSerialized.FindProperty(propertyName);
        destinationObjects.arraySize = sourceObjects != null ? sourceObjects.arraySize : 0;

        for (int index = 0; sourceObjects != null && index < sourceObjects.arraySize; index++)
        {
            GameObject original = sourceObjects.GetArrayElementAtIndex(index)
                .objectReferenceValue as GameObject;
            if (original == null)
                continue;

            string indexedName = sourceObjects.arraySize == 1
                ? newName : newName + " " + (index + 1);
            GameObject visual = FindOrDuplicateVisual(original, indexedName);
            destinationObjects.GetArrayElementAtIndex(index).objectReferenceValue = visual;
        }

        destinationSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindOrDuplicateVisual(GameObject source, string newName)
    {
        Transform sibling = source.transform.parent.Find(newName);
        if (sibling != null)
            return sibling.gameObject;

        GameObject clone = UnityEngine.Object.Instantiate(source,
            source.transform.parent, false);
        clone.name = newName;
        CopyLocalTransform(source.transform, clone.transform);
        Undo.RegisterCreatedObjectUndo(clone, "Create AC Lamp 2 light visual");
        return clone;
    }

    private static void ReplaceTopLnRequirements(FreeLabManager lab,
        FreeLabConnectionGroup top, FreeLabConnectionGroup bottom,
        FreeLabWireDefinition topL, FreeLabWireDefinition topN)
    {
        foreach (FreeLabControl control in lab.GetComponentsInChildren<FreeLabControl>(true))
            ReplaceRequirements(new SerializedObject(control), "enableConfigurations",
                top, bottom, topL, topN, control);

        foreach (FreeLabExperimentEvaluator evaluator in lab
            .GetComponentsInChildren<FreeLabExperimentEvaluator>(true))
            ReplaceRequirements(new SerializedObject(evaluator), "states",
                top, bottom, topL, topN, evaluator);
    }

    private static void ReplaceRequirements(SerializedObject serialized,
        string configurationsName, FreeLabConnectionGroup top,
        FreeLabConnectionGroup bottom, FreeLabWireDefinition topL,
        FreeLabWireDefinition topN, UnityEngine.Object owner)
    {
        SerializedProperty containers = serialized.FindProperty(configurationsName);
        if (containers == null)
            return;

        bool changed = false;
        for (int index = 0; index < containers.arraySize; index++)
        {
            SerializedProperty container = containers.GetArrayElementAtIndex(index);
            SerializedProperty connections = container.FindPropertyRelative("requiredConnections");
            SerializedProperty rows = container.FindPropertyRelative(
                "requiredAlternativeConnectionGroups");
            if (connections == null || rows == null)
                continue;

            if (ContainsPair(connections, topL.endpointA, topL.endpointB) &&
                ContainsPair(connections, topN.endpointA, topN.endpointB))
            {
                RemovePair(connections, topL.endpointA, topL.endpointB);
                RemovePair(connections, topN.endpointA, topN.endpointB);
                AddAlternativeRow(rows, top, bottom);
                changed = true;
            }

            for (int rowIndex = 0; rowIndex < rows.arraySize; rowIndex++)
            {
                SerializedProperty alternatives = rows.GetArrayElementAtIndex(rowIndex)
                    .FindPropertyRelative("alternatives");
                if (alternatives == null || !ContainsEquivalentTopPair(alternatives,
                    topL, topN) || ContainsObject(alternatives, bottom))
                    continue;

                int newIndex = alternatives.arraySize++;
                alternatives.GetArrayElementAtIndex(newIndex).objectReferenceValue = bottom;
                changed = true;
            }
        }

        if (!changed)
            return;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(owner);
    }

    private static bool ContainsEquivalentTopPair(SerializedProperty alternatives,
        FreeLabWireDefinition first, FreeLabWireDefinition second)
    {
        for (int index = 0; index < alternatives.arraySize; index++)
        {
            FreeLabConnectionGroup group = alternatives.GetArrayElementAtIndex(index)
                .objectReferenceValue as FreeLabConnectionGroup;
            if (group != null && group.RequiredWires.Contains(first) &&
                group.RequiredWires.Contains(second))
                return true;
        }
        return false;
    }

    private static bool ContainsObject(SerializedProperty objects,
        UnityEngine.Object value)
    {
        for (int index = 0; index < objects.arraySize; index++)
            if (objects.GetArrayElementAtIndex(index).objectReferenceValue == value)
                return true;
        return false;
    }

    private static void AddAlternativeRow(SerializedProperty rows,
        FreeLabConnectionGroup top, FreeLabConnectionGroup bottom)
    {
        int index = rows.arraySize++;
        SerializedProperty alternatives = rows.GetArrayElementAtIndex(index)
            .FindPropertyRelative("alternatives");
        alternatives.arraySize = 2;
        alternatives.GetArrayElementAtIndex(0).objectReferenceValue = top;
        alternatives.GetArrayElementAtIndex(1).objectReferenceValue = bottom;
    }

    private static bool ContainsPair(SerializedProperty connections,
        FreeLabEndpoint a, FreeLabEndpoint b)
    {
        for (int index = 0; index < connections.arraySize; index++)
        {
            SerializedProperty item = connections.GetArrayElementAtIndex(index);
            FreeLabEndpoint first = item.FindPropertyRelative("endpointA")
                .objectReferenceValue as FreeLabEndpoint;
            FreeLabEndpoint second = item.FindPropertyRelative("endpointB")
                .objectReferenceValue as FreeLabEndpoint;
            if ((first == a && second == b) || (first == b && second == a))
                return true;
        }
        return false;
    }

    private static void RemovePair(SerializedProperty connections,
        FreeLabEndpoint a, FreeLabEndpoint b)
    {
        for (int index = connections.arraySize - 1; index >= 0; index--)
        {
            SerializedProperty item = connections.GetArrayElementAtIndex(index);
            FreeLabEndpoint first = item.FindPropertyRelative("endpointA")
                .objectReferenceValue as FreeLabEndpoint;
            FreeLabEndpoint second = item.FindPropertyRelative("endpointB")
                .objectReferenceValue as FreeLabEndpoint;
            if ((first == a && second == b) || (first == b && second == a))
                connections.DeleteArrayElementAtIndex(index);
        }
    }

    private static FreeLabConnectionGroup ConfigureGroup(Transform root,
        string id, params FreeLabWireDefinition[] wires)
    {
        if (wires.Any(wire => wire == null))
        {
            Debug.LogError("AC Lamp 2 setup stopped: a required source wire is missing for " + id + ".", root);
            return null;
        }

        Transform child = GetOrCreate(root, id);
        FreeLabConnectionGroup group = child.GetComponent<FreeLabConnectionGroup>();
        if (group == null)
            group = Undo.AddComponent<FreeLabConnectionGroup>(child.gameObject);
        group.Configure(id, wires);
        EditorUtility.SetDirty(group);
        return group;
    }

    private static FreeLabDevice FindDevice(FreeLabManager lab, string name)
    {
        return lab.GetComponentsInChildren<FreeLabDevice>(true)
            .FirstOrDefault(device => device != null && device.gameObject.name == name);
    }

    private static FreeLabDeviceGroup FindGroup(FreeLabManager lab, string id)
    {
        return lab.GetComponentsInChildren<FreeLabDeviceGroup>(true)
            .FirstOrDefault(group => group != null && group.GroupId == id);
    }

    private static FreeLabWireDefinition FindWire(
        IEnumerable<FreeLabWireDefinition> wires, string name)
    {
        return wires.FirstOrDefault(wire => wire != null &&
            string.Equals(wire.gameObject.name.Trim(), name.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    private static FreeLabEndpoint FindLampEndpoint(FreeLabWireDefinition wire)
    {
        if (wire == null)
            return null;

        return IsLampEndpoint(wire.endpointA) ? wire.endpointA :
            IsLampEndpoint(wire.endpointB) ? wire.endpointB : null;
    }

    private static bool IsLampEndpoint(FreeLabEndpoint endpoint)
    {
        if (endpoint == null)
            return false;

        string name = endpoint.gameObject.name;
        return name.IndexOf("AC LED", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("AC Halogen", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Transform GetOrCreate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(created, "Create AC Lamp 2 setup holder");
        return created.transform;
    }

    private static void CopyLocalTransform(Transform source, Transform destination)
    {
        destination.localPosition = source.localPosition;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }
}

/// <summary>
/// Executes the requested one-time scene setup after this editor script has
/// compiled. The completion marker prevents any future automatic edits.
/// </summary>
[InitializeOnLoad]
internal static class FreeLabAcLampTwoOneShotInstaller
{
    private static readonly string CompletionKey =
        "Bedo.FreeLabAcLampTwoSetup.PairGuardV3." + Application.dataPath;

    static FreeLabAcLampTwoOneShotInstaller()
    {
        EditorApplication.delayCall += InstallOnce;
    }

    private static void InstallOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorPrefs.GetBool(CompletionKey, false))
            return;

        FreeLabAcLampTwoSetup.CreateAcLampTwoAlternatives();
        if (FreeLabAcLampTwoSetup.HasCreatedExpectedWires())
            EditorPrefs.SetBool(CompletionKey, true);
    }
}
