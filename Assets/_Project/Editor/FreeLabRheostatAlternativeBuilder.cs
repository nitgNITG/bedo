using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Converts the authored 800 Ohm Free Lab setup into safe 100/150/800 Ohm
/// alternatives. It never changes an existing wire's transform; new template
/// copies inherit the 800 Ohm template transform for manual visual adjustment.
/// </summary>
public static class FreeLabRheostatAlternativeBuilder
{
    private static readonly string[] RheostatValues = { "100", "150", "800" };

    [MenuItem("Free Lab/Build rheostat alternatives from 800 Ohm")]
    public static void Build()
    {
        FreeLabManager lab = Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
        {
            Debug.LogError("Free Lab builder: no FreeLabManager was found in the open scene.");
            return;
        }

        FreeLabEndpoint[] endpoints = lab.GetComponentsInChildren<FreeLabEndpoint>(true);
        Dictionary<string, FreeLabEndpoint> endpointsById = endpoints
            .Where(endpoint => endpoint != null && !string.IsNullOrWhiteSpace(endpoint.EndpointId))
            .GroupBy(endpoint => Key(endpoint.EndpointId))
            .ToDictionary(group => group.Key, group => group.First());

        List<FreeLabWireDefinition> definitions = lab
            .GetComponentsInChildren<FreeLabWireDefinition>(true)
            .Where(definition => definition != null &&
                definition.endpointA != null && definition.endpointB != null)
            .ToList();

        int createdWires = CreateMissingWireDefinitions(
            definitions, endpointsById);
        definitions = lab.GetComponentsInChildren<FreeLabWireDefinition>(true)
            .Where(definition => definition != null &&
                definition.endpointA != null && definition.endpointB != null)
            .ToList();

        Transform groupRoot = GetOrCreateGroupRoot(lab.transform);
        Dictionary<string, FreeLabWireDefinition> definitionsByPair = definitions
            .GroupBy(PairKey)
            .ToDictionary(group => group.Key, group => group.First());

        int convertedRequirements = 0;
        foreach (FreeLabExperimentEvaluator evaluator in lab
            .GetComponentsInChildren<FreeLabExperimentEvaluator>(true))
        {
            convertedRequirements += ConvertConfigurations(
                evaluator, "states", groupRoot, endpointsById,
                definitionsByPair);
        }

        foreach (FreeLabControl control in lab
            .GetComponentsInChildren<FreeLabControl>(true))
        {
            convertedRequirements += ConvertConfigurations(
                control, "enableConfigurations", groupRoot, endpointsById,
                definitionsByPair);
        }

        foreach (FreeLabSliderControl slider in lab
            .GetComponentsInChildren<FreeLabSliderControl>(true))
        {
            convertedRequirements += ConvertConfigurations(
                slider, "adjustmentConfigurations", groupRoot, endpointsById,
                definitionsByPair);
        }

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        EditorSceneManager.SaveScene(lab.gameObject.scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "Free Lab rheostat alternatives complete. Created " +
            createdWires + " wire templates and converted " +
            convertedRequirements + " requirement sets.",
            lab);
    }

    [MenuItem("Free Lab/Configure active rheostat data sources")]
    public static void ConfigureActiveRheostatDataSources()
    {
        FreeLabManager lab = Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
        {
            Debug.LogError("Free Lab data setup: no FreeLabManager was found in the open scene.");
            return;
        }

        FreeLabRheostatDataAdapter[] adapters = lab.GetComponentsInChildren<
            FreeLabRheostatDataAdapter>(true);
        FreeLabRheostatDataAdapter source800 = adapters.FirstOrDefault(adapter =>
            adapter.gameObject.name.Contains("800"));
        FreeLabConnectionGroup sourceGroup = source800 != null
            ? source800.GetComponent<FreeLabConnectionGroup>()
            : null;
        if (source800 == null || sourceGroup == null ||
            sourceGroup.RequiredWires.Count == 0)
        {
            Debug.LogError("Free Lab data setup needs the existing 800 Ohm rheostat's red/black connection group.");
            return;
        }

        Dictionary<string, FreeLabEndpoint> endpointsById = lab
            .GetComponentsInChildren<FreeLabEndpoint>(true)
            .Where(endpoint => endpoint != null)
            .GroupBy(endpoint => Key(endpoint.EndpointId))
            .ToDictionary(group => group.Key, group => group.First());
        List<FreeLabWireDefinition> definitions = lab
            .GetComponentsInChildren<FreeLabWireDefinition>(true).ToList();
        Dictionary<string, FreeLabWireDefinition> definitionsByPair = definitions
            .Where(definition => definition != null && definition.endpointA != null &&
                definition.endpointB != null)
            .GroupBy(PairKey).ToDictionary(group => group.Key, group => group.First());

        foreach (FreeLabRheostatDataAdapter adapter in adapters)
        {
            Match match = Regex.Match(adapter.gameObject.name, @"\b(100|150|800)\b");
            if (!match.Success)
                continue;

            string value = match.Groups[1].Value;
            List<FreeLabWireDefinition> wires = new List<FreeLabWireDefinition>();
            foreach (FreeLabWireDefinition sourceWire in sourceGroup.RequiredWires)
            {
                FreeLabEndpoint endpointA = TranslateEndpoint(
                    sourceWire.endpointA, value, endpointsById);
                FreeLabEndpoint endpointB = TranslateEndpoint(
                    sourceWire.endpointB, value, endpointsById);
                if (endpointA != null && endpointB != null &&
                    definitionsByPair.TryGetValue(PairKey(endpointA, endpointB),
                        out FreeLabWireDefinition wire))
                {
                    wires.Add(wire);
                }
            }

            if (wires.Count != sourceGroup.RequiredWires.Count)
            {
                Debug.LogWarning("Free Lab data setup could not find every red/black wire for " + adapter.gameObject.name, adapter);
                continue;
            }

            FreeLabConnectionGroup group = adapter.GetComponent<
                FreeLabConnectionGroup>();
            if (group == null)
                group = adapter.gameObject.AddComponent<FreeLabConnectionGroup>();
            group.Configure(adapter.gameObject.name + " data connection", wires);
            adapter.ConfigureConnectionGroup(group, float.Parse(value));
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(adapter);
        }

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        EditorSceneManager.SaveScene(lab.gameObject.scene);
        Debug.Log("Free Lab active rheostat data sources are configured.", lab);
    }

    [MenuItem("Free Lab/Make rheostats freely adjustable")]
    public static void MakeRheostatsFreelyAdjustable()
    {
        FreeLabManager lab = Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
        {
            Debug.LogError("Free Lab rheostat setup: no FreeLabManager was found in the open scene.");
            return;
        }

        int changed = 0;
        foreach (FreeLabControl control in lab.GetComponentsInChildren<
            FreeLabControl>(true))
        {
            if (control == null || !Regex.IsMatch(
                control.gameObject.name, @"Rheostat 0 to (100|150|800)"))
            {
                continue;
            }

            SerializedObject serializedControl = new SerializedObject(control);
            SerializedProperty configurations = serializedControl.FindProperty(
                "enableConfigurations");
            SerializedProperty requiresConnection = serializedControl.FindProperty(
                "requiresAtLeastOneConnection");
            if (configurations != null)
                configurations.ClearArray();
            if (requiresConnection != null)
                requiresConnection.boolValue = false;
            serializedControl.ApplyModifiedProperties();
            EditorUtility.SetDirty(control);
            changed++;
        }

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        EditorSceneManager.SaveScene(lab.gameObject.scene);
        Debug.Log("Free Lab rheostats are freely adjustable. Their red/black cable groups now affect data selection only (" + changed + " controls).", lab);
    }

    private static int CreateMissingWireDefinitions(
        List<FreeLabWireDefinition> definitions,
        Dictionary<string, FreeLabEndpoint> endpointsById)
    {
        int created = 0;
        List<FreeLabWireDefinition> sources = definitions.Where(
            definition => ReferencesRheostat(definition, "800")).ToList();

        foreach (FreeLabWireDefinition source in sources)
        {
            foreach (string targetValue in RheostatValues)
            {
                if (targetValue == "800")
                    continue;

                FreeLabEndpoint targetA = TranslateEndpoint(
                    source.endpointA, targetValue, endpointsById);
                FreeLabEndpoint targetB = TranslateEndpoint(
                    source.endpointB, targetValue, endpointsById);
                if (targetA == null || targetB == null)
                {
                    Debug.LogWarning(
                        "Free Lab builder skipped " + source.name +
                        ": matching " + targetValue +
                        " Ohm endpoint was not found.", source);
                    continue;
                }

                if (definitions.Any(definition => PairKey(definition) ==
                    PairKey(targetA, targetB)))
                {
                    continue;
                }

                if (source.wireParentTemplate == null)
                {
                    Debug.LogWarning(
                        "Free Lab builder skipped " + source.name +
                        ": it has no authored wire template.", source);
                    continue;
                }

                GameObject clone = Object.Instantiate(
                    source.wireParentTemplate,
                    source.wireParentTemplate.transform.parent);
                clone.name = "Free Template - " + targetA.EndpointId +
                    " to " + targetB.EndpointId;
                clone.transform.SetSiblingIndex(
                    source.wireParentTemplate.transform.GetSiblingIndex() + 1);
                Undo.RegisterCreatedObjectUndo(
                    clone, "Create rheostat wire alternative");

                FreeLabWireDefinition cloneDefinition = clone
                    .GetComponent<FreeLabWireDefinition>();
                if (cloneDefinition == null)
                    cloneDefinition = clone.AddComponent<FreeLabWireDefinition>();

                cloneDefinition.endpointA = targetA;
                cloneDefinition.endpointB = targetB;
                cloneDefinition.wireParentTemplate = clone;
                EditorUtility.SetDirty(cloneDefinition);
                definitions.Add(cloneDefinition);
                created++;
            }
        }

        return created;
    }

    private static int ConvertConfigurations(
        Object owner,
        string configurationsPropertyName,
        Transform groupRoot,
        Dictionary<string, FreeLabEndpoint> endpointsById,
        Dictionary<string, FreeLabWireDefinition> definitionsByPair)
    {
        SerializedObject serializedOwner = new SerializedObject(owner);
        SerializedProperty configurations = serializedOwner.FindProperty(
            configurationsPropertyName);
        if (configurations == null)
            return 0;

        int converted = 0;
        for (int configurationIndex = 0;
            configurationIndex < configurations.arraySize;
            configurationIndex++)
        {
            SerializedProperty configuration = configurations
                .GetArrayElementAtIndex(configurationIndex);
            SerializedProperty connections = configuration.FindPropertyRelative(
                "requiredConnections");
            SerializedProperty alternatives = configuration.FindPropertyRelative(
                "anyConnectionGroups");
            if (connections == null || alternatives == null ||
                alternatives.arraySize > 0)
            {
                continue;
            }

            List<ConnectionPair> sourcePairs = new List<ConnectionPair>();
            for (int connectionIndex = 0;
                connectionIndex < connections.arraySize;
                connectionIndex++)
            {
                SerializedProperty connection = connections
                    .GetArrayElementAtIndex(connectionIndex);
                FreeLabEndpoint endpointA = connection.FindPropertyRelative(
                    "endpointA").objectReferenceValue as FreeLabEndpoint;
                FreeLabEndpoint endpointB = connection.FindPropertyRelative(
                    "endpointB").objectReferenceValue as FreeLabEndpoint;
                if (ReferencesRheostat(endpointA, "800") ||
                    ReferencesRheostat(endpointB, "800"))
                {
                    sourcePairs.Add(new ConnectionPair(
                        connectionIndex, endpointA, endpointB));
                }
            }

            if (sourcePairs.Count == 0)
                continue;

            List<List<FreeLabWireDefinition>> wireAlternatives =
                new List<List<FreeLabWireDefinition>>();
            bool canConvert = true;
            foreach (string value in RheostatValues)
            {
                List<FreeLabWireDefinition> wires = new List<FreeLabWireDefinition>();
                foreach (ConnectionPair sourcePair in sourcePairs)
                {
                    FreeLabEndpoint targetA = TranslateEndpoint(
                        sourcePair.endpointA, value, endpointsById);
                    FreeLabEndpoint targetB = TranslateEndpoint(
                        sourcePair.endpointB, value, endpointsById);
                    if (targetA == null || targetB == null ||
                        !definitionsByPair.TryGetValue(
                            PairKey(targetA, targetB), out FreeLabWireDefinition wire))
                    {
                        canConvert = false;
                        break;
                    }

                    if (!wires.Contains(wire))
                        wires.Add(wire);
                }

                if (!canConvert)
                    break;
                wireAlternatives.Add(wires);
            }

            if (!canConvert)
            {
                Debug.LogWarning(
                    "Free Lab builder left an 800 Ohm requirement unchanged on " +
                    owner.name + " because one or more matching authored wires " +
                    "could not be found.", owner);
                continue;
            }

            for (int index = sourcePairs.Count - 1; index >= 0; index--)
                connections.DeleteArrayElementAtIndex(sourcePairs[index].index);

            for (int index = 0; index < RheostatValues.Length; index++)
            {
                string label = owner.name + " - " + configurationsPropertyName +
                    " " + configurationIndex + " - " + RheostatValues[index] +
                    " Ohm alternative";
                FreeLabConnectionGroup group = CreateGroup(
                    groupRoot, label, wireAlternatives[index]);
                int destination = alternatives.arraySize;
                alternatives.InsertArrayElementAtIndex(destination);
                alternatives.GetArrayElementAtIndex(destination)
                    .objectReferenceValue = group;
            }

            converted++;
        }

        if (converted > 0)
        {
            serializedOwner.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner);
        }

        return converted;
    }

    private static FreeLabConnectionGroup CreateGroup(
        Transform root,
        string label,
        IEnumerable<FreeLabWireDefinition> wires)
    {
        GameObject objectGroup = new GameObject(label);
        objectGroup.transform.SetParent(root, false);
        FreeLabConnectionGroup group = objectGroup.AddComponent<
            FreeLabConnectionGroup>();
        group.Configure(label, wires);
        Undo.RegisterCreatedObjectUndo(
            objectGroup, "Create rheostat requirement alternative");
        EditorUtility.SetDirty(group);
        return group;
    }

    private static Transform GetOrCreateGroupRoot(Transform labRoot)
    {
        Transform existing = labRoot.Find("Rheostat Requirement Alternatives");
        if (existing != null)
            return existing;

        GameObject root = new GameObject("Rheostat Requirement Alternatives");
        root.transform.SetParent(labRoot, false);
        Undo.RegisterCreatedObjectUndo(root, "Create rheostat requirement folder");
        return root.transform;
    }

    private static FreeLabEndpoint TranslateEndpoint(
        FreeLabEndpoint source,
        string targetValue,
        Dictionary<string, FreeLabEndpoint> endpointsById)
    {
        if (source == null || !ReferencesRheostat(source, "800"))
            return source;

        string translated = ReplaceRheostatValue(
            source.EndpointId, targetValue);
        endpointsById.TryGetValue(Key(translated), out FreeLabEndpoint result);
        return result;
    }

    private static bool ReferencesRheostat(
        FreeLabWireDefinition definition,
        string value)
    {
        return definition != null && (ReferencesRheostat(
            definition.endpointA, value) || ReferencesRheostat(
            definition.endpointB, value));
    }

    private static bool ReferencesRheostat(
        FreeLabEndpoint endpoint,
        string value)
    {
        return endpoint != null && Regex.IsMatch(
            endpoint.EndpointId ?? string.Empty,
            @"\b" + Regex.Escape(value) + @"\s*Ohm\b",
            RegexOptions.IgnoreCase);
    }

    private static string ReplaceRheostatValue(
        string endpointId,
        string newValue)
    {
        return Regex.Replace(endpointId ?? string.Empty,
            @"\b800(?=\s*Ohm\b)", newValue,
            RegexOptions.IgnoreCase);
    }

    private static string PairKey(FreeLabWireDefinition definition)
    {
        return PairKey(definition.endpointA, definition.endpointB);
    }

    private static string PairKey(
        FreeLabEndpoint endpointA,
        FreeLabEndpoint endpointB)
    {
        string first = Key(endpointA != null ? endpointA.EndpointId : string.Empty);
        string second = Key(endpointB != null ? endpointB.EndpointId : string.Empty);
        return string.CompareOrdinal(first, second) <= 0
            ? first + "|" + second
            : second + "|" + first;
    }

    private static string Key(string value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ")
            .Trim().ToLowerInvariant();
    }

    private struct ConnectionPair
    {
        public readonly int index;
        public readonly FreeLabEndpoint endpointA;
        public readonly FreeLabEndpoint endpointB;

        public ConnectionPair(
            int index,
            FreeLabEndpoint endpointA,
            FreeLabEndpoint endpointB)
        {
            this.index = index;
            this.endpointA = endpointA;
            this.endpointB = endpointB;
        }
    }
}
