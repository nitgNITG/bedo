using System;
using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Imports the authored Guided wire interactions from experiments 7, 8 and 9
/// into the separate Free Lab. It never edits a Guided object. Missing Free
/// Lab endpoints and wire parents are copied at their current world pose.
/// </summary>
public static class FreeLabExperiment789Importer
{
    private const float PositionTolerance = 0.002f;
    private static readonly string[] ExperimentTokens = { "7", "8", "9" };

    [MenuItem("Free Lab/Import Guided Experiments 7, 8 and 9")]
    private static void ImportExperiments()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
            throw new InvalidOperationException("No FreeLabManager was found in the open scene.");

        Undo.SetCurrentGroupName("Import experiments 7, 8 and 9 into Free Lab");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            Transform endpointRoot = GetOrCreateChild(lab.transform, "Imported Endpoints");
            Transform wireRoot = GetOrCreateChild(lab.transform, "Free Lab Wires");
            Transform evaluatorRoot = GetOrCreateChild(lab.transform, "Experiment Evaluators");

            List<FreeLabEndpoint> endpoints = lab.GetComponentsInChildren<FreeLabEndpoint>(true).ToList();
            List<FreeLabWireDefinition> definitions = lab.GetComponentsInChildren<FreeLabWireDefinition>(true).ToList();
            Dictionary<FreeLabEndpoint, int> endpointUsage = CountExistingEndpointUsage(definitions);
            List<string> report = new List<string>();

            foreach (string token in ExperimentTokens)
            {
                StoryboardSteps storyboard = FindStoryboard(token);
                if (storyboard == null)
                {
                    report.Add("Exp " + token + ": storyboard not found.");
                    continue;
                }

                ImportStoryboard(
                    storyboard,
                    token,
                    endpointRoot,
                    wireRoot,
                    evaluatorRoot,
                    endpoints,
                    definitions,
                    endpointUsage,
                    report
                );
            }

            EditorUtility.SetDirty(lab);
            EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("Free Lab experiment import complete.\n" + string.Join("\n", report), lab);
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
    }

    [MenuItem("Free Lab/Build Evaluator States for Experiments 7, 8 and 9")]
    private static void BuildEvaluatorStates()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
            throw new InvalidOperationException("No FreeLabManager was found in the open scene.");

        Transform evaluatorRoot = GetOrCreateChild(lab.transform, "Experiment Evaluators");
        List<FreeLabWireDefinition> definitions = lab.GetComponentsInChildren<FreeLabWireDefinition>(true).ToList();
        List<FreeLabControl> controls = lab.GetComponentsInChildren<FreeLabControl>(true).ToList();

        foreach (string token in ExperimentTokens)
        {
            StoryboardSteps storyboard = FindStoryboard(token);
            if (storyboard == null)
                continue;

            List<FreeLabExperimentEvaluator.ConnectionRequirement> connections =
                ReadExistingEvaluatorConnections(evaluatorRoot, token);
            if (connections.Count == 0)
            {
                connections = definitions
                    .Where(definition => definition != null && definition.gameObject.name.StartsWith("Free Exp " + token + " -", StringComparison.OrdinalIgnoreCase))
                    .Select(definition => NewRequirement(definition.endpointA, definition.endpointB))
                    .ToList();
            }
            List<FreeLabControl> storyboardControls = FindStoryboardControls(storyboard, controls);
            CreateOrReplaceEvaluator(storyboard, token, evaluatorRoot, connections, storyboardControls);
        }

        EditorUtility.SetDirty(lab);
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Free Lab evaluator states for experiments 7, 8 and 9 were rebuilt from their Guided storyboards.", lab);
    }

    [MenuItem("Free Lab/Assign Imported Endpoint IDs From Names")]
    private static void AssignImportedEndpointIds()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
            throw new InvalidOperationException("No FreeLabManager was found in the open scene.");

        HashSet<FreeLabEndpoint> imported = new HashSet<FreeLabEndpoint>();
        Transform importedRoot = lab.transform.Find("Imported Endpoints");
        if (importedRoot != null)
        {
            foreach (FreeLabEndpoint endpoint in importedRoot.GetComponentsInChildren<FreeLabEndpoint>(true))
                imported.Add(endpoint);
        }

        foreach (FreeLabWireDefinition definition in lab.GetComponentsInChildren<FreeLabWireDefinition>(true))
        {
            if (!definition.gameObject.name.StartsWith("Free Exp ", StringComparison.OrdinalIgnoreCase))
                continue;
            if (definition.endpointA != null)
                imported.Add(definition.endpointA);
            if (definition.endpointB != null)
                imported.Add(definition.endpointB);
        }

        Dictionary<string, List<FreeLabEndpoint>> byName = imported
            .Where(endpoint => endpoint != null)
            .GroupBy(endpoint => endpoint.gameObject.name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        List<string> duplicates = byName.Where(pair => pair.Value.Count > 1)
            .Select(pair => pair.Key).ToList();
        int assigned = 0;
        foreach (KeyValuePair<string, List<FreeLabEndpoint>> pair in byName)
        {
            if (pair.Value.Count != 1)
                continue;

            FreeLabEndpoint endpoint = pair.Value[0];
            SerializedObject serialized = new SerializedObject(endpoint);
            SerializedProperty id = serialized.FindProperty("endpointId");
            if (!string.IsNullOrWhiteSpace(id.stringValue))
                continue;

            Undo.RecordObject(endpoint, "Assign Free Lab endpoint ID");
            id.stringValue = pair.Key;
            serialized.ApplyModifiedProperties();
            assigned++;
        }

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        string duplicateText = duplicates.Count == 0
            ? "No duplicate imported endpoint names were found."
            : "Duplicate names left unchanged: " + string.Join(", ", duplicates);
        Debug.Log("Assigned " + assigned + " imported Endpoint IDs. " + duplicateText, lab);
    }

    [MenuItem("Free Lab/Add Missing Mesh Colliders to Free Lab Wire Splines")]
    private static void AddMissingMeshCollidersToFreeLabWireSplines()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
            throw new InvalidOperationException("No FreeLabManager was found in the open scene.");

        Transform wireRoot = lab.transform.Find("Free Lab Wires");
        if (wireRoot == null)
            throw new InvalidOperationException("Free Lab Wires was not found below the FreeLabManager.");

        int added = 0;
        int alreadyPresent = 0;
        foreach (Component component in wireRoot.GetComponentsInChildren<Component>(true))
        {
            // Avoid a hard package dependency in this editor helper while still
            // targeting the exact GameObject which owns the SplineContainer.
            if (component == null || component.GetType().Name != "SplineContainer")
                continue;

            if (component.gameObject.GetComponent<MeshCollider>() != null)
            {
                alreadyPresent++;
                continue;
            }

            Undo.AddComponent<MeshCollider>(component.gameObject);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Added MeshCollider to " + added + " Free Lab SplineContainer objects. " +
                  alreadyPresent + " already had one.", lab);
    }

    [MenuItem("Free Lab/Enable Copied Exp 7, 8 and 9 Endpoint Interactions")]
    private static void EnableCopiedEndpointInteractions()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
            throw new InvalidOperationException("No FreeLabManager was found in the open scene.");

        Transform endpointRoot = lab.transform.Find("Imported Endpoints");
        if (endpointRoot == null)
            throw new InvalidOperationException("Imported Endpoints was not found below the FreeLabManager.");

        List<string> enabled = new List<string>();
        foreach (Interaction interaction in endpointRoot.GetComponentsInChildren<Interaction>(true))
        {
            // The imported endpoint root can contain a visual child which owns
            // the clickable Interaction. Enable that real click target rather
            // than assuming it is on the FreeLabEndpoint holder itself.
            if (interaction == null || interaction is WireInteraction || interaction.Allow)
                continue;

            Undo.RecordObject(interaction, "Enable copied Free Lab endpoint interaction");
            interaction.Allow = true;
            EditorUtility.SetDirty(interaction);
            enabled.Add(interaction.gameObject.name);
        }

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Enabled copied endpoint interactions (" + enabled.Count + "): " +
                  (enabled.Count == 0 ? "none were disabled." : string.Join(", ", enabled.Distinct())), lab);
    }

    private static void ImportStoryboard(
        StoryboardSteps storyboard,
        string token,
        Transform endpointRoot,
        Transform wireRoot,
        Transform evaluatorRoot,
        List<FreeLabEndpoint> endpoints,
        List<FreeLabWireDefinition> definitions,
        Dictionary<FreeLabEndpoint, int> endpointUsage,
        List<string> report)
    {
        List<WireInteraction> guidedWires = storyboard.steps
            .Where(step => step != null && step.interaction is WireInteraction)
            .Select(step => step.interaction as WireInteraction)
            .Where(wire => wire != null && wire.pointA != null && wire.pointB != null)
            .Distinct()
            .ToList();

        List<FreeLabExperimentEvaluator.ConnectionRequirement> importedConnections =
            new List<FreeLabExperimentEvaluator.ConnectionRequirement>();

        int reused = 0;
        int created = 0;
        foreach (WireInteraction guidedWire in guidedWires)
        {
            FreeLabEndpoint endpointA = FindMatchingEndpoint(guidedWire.pointA, endpoints);
            FreeLabEndpoint endpointB = FindMatchingEndpoint(guidedWire.pointB, endpoints);

            if (endpointA == null)
                endpointA = CloneSourceEndpoint(guidedWire.pointA, endpointRoot, endpoints, token);
            if (endpointB == null)
                endpointB = CloneSourceEndpoint(guidedWire.pointB, endpointRoot, endpoints, token);

            if (FindDefinition(definitions, endpointA, endpointB) != null)
            {
                reused++;
                importedConnections.Add(NewRequirement(endpointA, endpointB));
                continue;
            }

            // One endpoint can support several wires, but every simultaneous
            // cable needs its own clickable banana. Copy an overlapping socket
            // exactly in place for the second and later cable.
            if (GetUsage(endpointUsage, endpointA) > 0)
                endpointA = CloneEndpoint(endpointA, endpointRoot, endpoints, token);
            if (GetUsage(endpointUsage, endpointB) > 0)
                endpointB = CloneEndpoint(endpointB, endpointRoot, endpoints, token);

            FreeLabWireDefinition definition = CreateDefinition(
                guidedWire,
                endpointA,
                endpointB,
                wireRoot,
                token
            );
            definitions.Add(definition);
            IncrementUsage(endpointUsage, endpointA);
            IncrementUsage(endpointUsage, endpointB);
            importedConnections.Add(NewRequirement(endpointA, endpointB));
            created++;
        }

        CreateOrReplaceEvaluator(
            storyboard,
            token,
            evaluatorRoot,
            importedConnections,
            FindStoryboardControls(
                storyboard,
                UnityEngine.Object.FindFirstObjectByType<FreeLabManager>()
                    .GetComponentsInChildren<FreeLabControl>(true).ToList()
            )
        );
        report.Add(
            "Exp " + token + ": " + guidedWires.Count + " Guided wire interactions; " +
            created + " new Free Lab definitions; " + reused + " existing definitions reused."
        );
    }

    private static StoryboardSteps FindStoryboard(string experimentNumber)
    {
        string exact = "Exp " + experimentNumber;
        StoryboardSteps[] all = UnityEngine.Object.FindObjectsOfType<StoryboardSteps>(true);
        return all.FirstOrDefault(storyboard =>
                   string.Equals(storyboard.StoryboardID, exact, StringComparison.OrdinalIgnoreCase))
               ?? all.FirstOrDefault(storyboard =>
                   storyboard.StoryboardID.IndexOf("Experiment " + experimentNumber, StringComparison.OrdinalIgnoreCase) >= 0)
               ?? all.FirstOrDefault(storyboard =>
                   storyboard.gameObject.name.IndexOf("Exp " + experimentNumber, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   storyboard.gameObject.name.IndexOf("Experiment " + experimentNumber, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static FreeLabEndpoint FindMatchingEndpoint(
        Interaction source,
        IEnumerable<FreeLabEndpoint> candidates)
    {
        string sourceName = NormalizeName(source.gameObject.name);
        Vector3 sourcePosition = source.transform.position;
        List<FreeLabEndpoint> atSamePosition = candidates.Where(candidate =>
            candidate != null && Vector3.Distance(candidate.transform.position, sourcePosition) <= PositionTolerance).ToList();

        FreeLabEndpoint exactName = atSamePosition.FirstOrDefault(candidate =>
            NormalizeName(candidate.gameObject.name) == sourceName);
        if (exactName != null)
            return exactName;

        return atSamePosition.Count == 1 ? atSamePosition[0] : null;
    }

    private static FreeLabEndpoint CloneSourceEndpoint(
        Interaction source,
        Transform parent,
        List<FreeLabEndpoint> endpoints,
        string token)
    {
        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent, true);
        clone.name = source.gameObject.name + " [Free Exp " + token + "]";
        Undo.RegisterCreatedObjectUndo(clone, "Copy Free Lab endpoint");

        FreeLabEndpoint endpoint = clone.GetComponent<FreeLabEndpoint>();
        if (endpoint == null)
            endpoint = Undo.AddComponent<FreeLabEndpoint>(clone);
        endpoints.Add(endpoint);
        return endpoint;
    }

    private static FreeLabEndpoint CloneEndpoint(
        FreeLabEndpoint source,
        Transform parent,
        List<FreeLabEndpoint> endpoints,
        string token)
    {
        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent, true);
        clone.name = source.gameObject.name + " [Free Exp " + token + " Extra Socket]";
        Undo.RegisterCreatedObjectUndo(clone, "Create additional Free Lab socket");
        FreeLabEndpoint endpoint = clone.GetComponent<FreeLabEndpoint>();
        endpoints.Add(endpoint);
        return endpoint;
    }

    private static FreeLabWireDefinition CreateDefinition(
        WireInteraction source,
        FreeLabEndpoint endpointA,
        FreeLabEndpoint endpointB,
        Transform wireRoot,
        string token)
    {
        GameObject sourceWire = source.wireObject != null ? source.wireObject : source.gameObject;
        GameObject clone = UnityEngine.Object.Instantiate(sourceWire, wireRoot, true);
        clone.name = "Free Exp " + token + " - " + sourceWire.name;
        clone.SetActive(false);
        Undo.RegisterCreatedObjectUndo(clone, "Copy Free Lab wire");

        foreach (WireInteraction guidedWire in clone.GetComponentsInChildren<WireInteraction>(true))
            guidedWire.enabled = false;

        FreeLabWireDefinition definition = Undo.AddComponent<FreeLabWireDefinition>(clone);
        definition.endpointA = endpointA;
        definition.endpointB = endpointB;
        definition.wireParentTemplate = clone;
        definition.plugA = EnsurePlug(endpointA, source.plugA);
        definition.plugB = EnsurePlug(endpointB, source.plugB);
        endpointA.SetSelectionPlugIfUnassigned(definition.plugA);
        endpointB.SetSelectionPlugIfUnassigned(definition.plugB);
        return definition;
    }

    private static GameObject EnsurePlug(FreeLabEndpoint endpoint, GameObject sourcePlug)
    {
        if (sourcePlug == null)
            return null;

        Transform existing = endpoint.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item != endpoint.transform &&
                NormalizeName(item.name) == NormalizeName(sourcePlug.name));
        if (existing != null)
            return existing.gameObject;

        GameObject clone = UnityEngine.Object.Instantiate(sourcePlug, endpoint.transform, true);
        clone.name = sourcePlug.name;
        clone.SetActive(false);
        Undo.RegisterCreatedObjectUndo(clone, "Copy Free Lab plug");
        return clone;
    }

    private static void CreateOrReplaceEvaluator(
        StoryboardSteps storyboard,
        string token,
        Transform parent,
        List<FreeLabExperimentEvaluator.ConnectionRequirement> connections,
        List<FreeLabControl> controls)
    {
        string name = "Experiment " + token + " Evaluator";
        Transform existing = parent.Find(name);
        GameObject evaluatorObject = existing != null
            ? existing.gameObject
            : new GameObject(name);
        if (existing == null)
        {
            evaluatorObject.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(evaluatorObject, "Create Free Lab evaluator");
        }

        FreeLabExperimentEvaluator evaluator = evaluatorObject.GetComponent<FreeLabExperimentEvaluator>();
        if (evaluator == null)
            evaluator = Undo.AddComponent<FreeLabExperimentEvaluator>(evaluatorObject);

        SerializedObject serialized = new SerializedObject(evaluator);
        serialized.FindProperty("experimentId").stringValue = "Experiment " + token;
        SerializedProperty states = serialized.FindProperty("states");
        states.arraySize = 2 + controls.Count;
        SetStateName(states.GetArrayElementAtIndex(0), "A - Initial");
        SetStateName(states.GetArrayElementAtIndex(1), "B - Wiring Complete");
        SetConnections(states.GetArrayElementAtIndex(0), new List<FreeLabExperimentEvaluator.ConnectionRequirement>());
        SetConnections(states.GetArrayElementAtIndex(1), connections);

        List<FreeLabControl> cumulativeControls = new List<FreeLabControl>();
        for (int index = 0; index < controls.Count; index++)
        {
            cumulativeControls.Add(controls[index]);
            SerializedProperty state = states.GetArrayElementAtIndex(index + 2);
            SetStateName(state, ((char)('C' + index)) + " - " + controls[index].ControlId);
            SetConnections(state, connections);
            SetControls(state, cumulativeControls);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(evaluator);
    }

    private static void SetStateName(SerializedProperty state, string value)
    {
        state.FindPropertyRelative("stateName").stringValue = value;
    }

    private static void SetConnections(
        SerializedProperty state,
        List<FreeLabExperimentEvaluator.ConnectionRequirement> requirements)
    {
        SerializedProperty property = state.FindPropertyRelative("requiredConnections");
        property.arraySize = requirements.Count;
        for (int index = 0; index < requirements.Count; index++)
        {
            property.GetArrayElementAtIndex(index).FindPropertyRelative("endpointA").objectReferenceValue = requirements[index].endpointA;
            property.GetArrayElementAtIndex(index).FindPropertyRelative("endpointB").objectReferenceValue = requirements[index].endpointB;
        }
    }

    private static void SetControls(
        SerializedProperty state,
        List<FreeLabControl> controls)
    {
        SerializedProperty property = state.FindPropertyRelative("requiredControls");
        property.arraySize = controls.Count;
        for (int index = 0; index < controls.Count; index++)
        {
            SerializedProperty requirement = property.GetArrayElementAtIndex(index);
            requirement.FindPropertyRelative("control").objectReferenceValue = controls[index];
            requirement.FindPropertyRelative("requiredValue").boolValue = true;
        }
    }

    private static List<FreeLabExperimentEvaluator.ConnectionRequirement> ReadExistingEvaluatorConnections(
        Transform parent,
        string token)
    {
        Transform existing = parent.Find("Experiment " + token + " Evaluator");
        FreeLabExperimentEvaluator evaluator = existing != null
            ? existing.GetComponent<FreeLabExperimentEvaluator>()
            : null;
        if (evaluator == null)
            return new List<FreeLabExperimentEvaluator.ConnectionRequirement>();

        SerializedObject serialized = new SerializedObject(evaluator);
        SerializedProperty states = serialized.FindProperty("states");
        if (states.arraySize < 2)
            return new List<FreeLabExperimentEvaluator.ConnectionRequirement>();

        SerializedProperty source = states.GetArrayElementAtIndex(1)
            .FindPropertyRelative("requiredConnections");
        List<FreeLabExperimentEvaluator.ConnectionRequirement> result =
            new List<FreeLabExperimentEvaluator.ConnectionRequirement>();
        for (int index = 0; index < source.arraySize; index++)
        {
            SerializedProperty requirement = source.GetArrayElementAtIndex(index);
            FreeLabEndpoint endpointA = requirement.FindPropertyRelative("endpointA").objectReferenceValue as FreeLabEndpoint;
            FreeLabEndpoint endpointB = requirement.FindPropertyRelative("endpointB").objectReferenceValue as FreeLabEndpoint;
            if (endpointA != null && endpointB != null)
                result.Add(NewRequirement(endpointA, endpointB));
        }
        return result;
    }

    private static List<FreeLabControl> FindStoryboardControls(
        StoryboardSteps storyboard,
        List<FreeLabControl> candidates)
    {
        List<FreeLabControl> result = new List<FreeLabControl>();
        foreach (StoryboardSteps.Step step in storyboard.steps)
        {
            if (step == null || step.interaction == null || step.interaction is WireInteraction)
                continue;

            Interaction source = step.interaction.GetComponent<Interaction>() ??
                                 step.interaction.GetComponentInChildren<Interaction>(true);
            if (source == null)
                continue;

            FreeLabControl control = candidates.FirstOrDefault(candidate =>
            {
                if (candidate == null || result.Contains(candidate))
                    return false;

                return candidate.GetComponentsInChildren<Interaction>(true)
                    .Any(candidateInteraction =>
                        NormalizeName(candidateInteraction.gameObject.name) == NormalizeName(source.gameObject.name) &&
                        Vector3.Distance(candidateInteraction.transform.position, source.transform.position) <= PositionTolerance);
            });

            if (control != null)
                result.Add(control);
        }

        return result;
    }

    private static Dictionary<FreeLabEndpoint, int> CountExistingEndpointUsage(
        IEnumerable<FreeLabWireDefinition> definitions)
    {
        Dictionary<FreeLabEndpoint, int> result = new Dictionary<FreeLabEndpoint, int>();
        foreach (FreeLabWireDefinition definition in definitions)
        {
            if (definition == null)
                continue;
            IncrementUsage(result, definition.endpointA);
            IncrementUsage(result, definition.endpointB);
        }
        return result;
    }

    private static FreeLabWireDefinition FindDefinition(
        IEnumerable<FreeLabWireDefinition> definitions,
        FreeLabEndpoint first,
        FreeLabEndpoint second)
    {
        return definitions.FirstOrDefault(definition => definition != null &&
            ((definition.endpointA == first && definition.endpointB == second) ||
             (definition.endpointA == second && definition.endpointB == first)));
    }

    private static int GetUsage(Dictionary<FreeLabEndpoint, int> usage, FreeLabEndpoint endpoint)
    {
        return endpoint != null && usage.TryGetValue(endpoint, out int result) ? result : 0;
    }

    private static void IncrementUsage(Dictionary<FreeLabEndpoint, int> usage, FreeLabEndpoint endpoint)
    {
        if (endpoint == null)
            return;
        usage[endpoint] = GetUsage(usage, endpoint) + 1;
    }

    private static FreeLabExperimentEvaluator.ConnectionRequirement NewRequirement(
        FreeLabEndpoint endpointA,
        FreeLabEndpoint endpointB)
    {
        return new FreeLabExperimentEvaluator.ConnectionRequirement
        {
            endpointA = endpointA,
            endpointB = endpointB
        };
    }

    private static Transform GetOrCreateChild(Transform parent, string name)
    {
        Transform result = parent.Find(name);
        if (result != null)
            return result;

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(child, "Create Free Lab folder");
        return child.transform;
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("(Clone)", string.Empty).Trim().ToLowerInvariant();
    }

    [MenuItem("Free Lab/Sync missing Guided connections from Exp 8")]
    private static void SyncMissingExp8Connections()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        StoryboardSteps storyboard = FindStoryboard("8");
        if (lab == null || storyboard == null)
        {
            Debug.LogError("Free Lab Exp 8 sync needs both FreeLabManager and Guided Exp 8.");
            return;
        }

        Transform endpointRoot = GetOrCreateChild(lab.transform, "Imported Endpoints");
        Transform wireRoot = GetOrCreateChild(lab.transform, "Free Lab Wires");
        List<FreeLabEndpoint> endpoints = lab.GetComponentsInChildren<FreeLabEndpoint>(true).ToList();
        List<FreeLabWireDefinition> definitions = lab.GetComponentsInChildren<FreeLabWireDefinition>(true).ToList();
        Dictionary<FreeLabEndpoint, int> usage = CountExistingEndpointUsage(definitions);
        List<FreeLabExperimentEvaluator.ConnectionRequirement> newlyAdded =
            new List<FreeLabExperimentEvaluator.ConnectionRequirement>();

        foreach (WireInteraction guidedWire in storyboard.steps
                     .Where(step => step != null && step.interaction is WireInteraction)
                     .Select(step => step.interaction as WireInteraction)
                     .Where(wire => wire != null && wire.pointA != null && wire.pointB != null)
                     .Distinct())
        {
            FreeLabEndpoint endpointA = FindMatchingEndpoint(guidedWire.pointA, endpoints) ??
                CloneSourceEndpoint(guidedWire.pointA, endpointRoot, endpoints, "8");
            FreeLabEndpoint endpointB = FindMatchingEndpoint(guidedWire.pointB, endpoints) ??
                CloneSourceEndpoint(guidedWire.pointB, endpointRoot, endpoints, "8");

            if (FindDefinition(definitions, endpointA, endpointB) != null)
                continue;

            if (GetUsage(usage, endpointA) > 0)
                endpointA = CloneEndpoint(endpointA, endpointRoot, endpoints, "8");
            if (GetUsage(usage, endpointB) > 0)
                endpointB = CloneEndpoint(endpointB, endpointRoot, endpoints, "8");

            FreeLabWireDefinition definition = CreateDefinition(guidedWire,
                endpointA, endpointB, wireRoot, "8");
            definitions.Add(definition);
            IncrementUsage(usage, endpointA);
            IncrementUsage(usage, endpointB);
            newlyAdded.Add(NewRequirement(endpointA, endpointB));
        }

        AppendConnectionsToExistingEvaluator(lab, "8", newlyAdded);
        EditorUtility.SetDirty(lab);
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Exp 8 sync added " + newlyAdded.Count +
            " missing Free Lab connection(s) and appended them to the existing Exp 8 wiring states.", lab);
    }

    private static void AppendConnectionsToExistingEvaluator(FreeLabManager lab,
        string token, List<FreeLabExperimentEvaluator.ConnectionRequirement> connections)
    {
        if (connections == null || connections.Count == 0)
            return;

        FreeLabExperimentEvaluator evaluator = lab.GetComponentsInChildren<
            FreeLabExperimentEvaluator>(true).FirstOrDefault(candidate =>
            candidate != null && candidate.ExperimentId.IndexOf(token,
                StringComparison.OrdinalIgnoreCase) >= 0);
        if (evaluator == null)
        {
            Debug.LogWarning("Exp 8 sync added wires but found no matching Free Lab evaluator to update.", lab);
            return;
        }

        SerializedObject serialized = new SerializedObject(evaluator);
        SerializedProperty states = serialized.FindProperty("states");
        for (int stateIndex = 1; stateIndex < states.arraySize; stateIndex++)
        {
            SerializedProperty required = states.GetArrayElementAtIndex(stateIndex)
                .FindPropertyRelative("requiredConnections");
            foreach (FreeLabExperimentEvaluator.ConnectionRequirement connection in connections)
            {
                bool exists = false;
                for (int index = 0; index < required.arraySize; index++)
                {
                    SerializedProperty entry = required.GetArrayElementAtIndex(index);
                    FreeLabEndpoint a = entry.FindPropertyRelative("endpointA").objectReferenceValue as FreeLabEndpoint;
                    FreeLabEndpoint b = entry.FindPropertyRelative("endpointB").objectReferenceValue as FreeLabEndpoint;
                    if ((a == connection.endpointA && b == connection.endpointB) ||
                        (a == connection.endpointB && b == connection.endpointA))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    int index = required.arraySize++;
                    required.GetArrayElementAtIndex(index).FindPropertyRelative("endpointA").objectReferenceValue = connection.endpointA;
                    required.GetArrayElementAtIndex(index).FindPropertyRelative("endpointB").objectReferenceValue = connection.endpointB;
                }
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(evaluator);
    }

    [MenuItem("Free Lab/Consolidate Imported Endpoints by World Position")]
    private static void ConsolidateImportedEndpointsByWorldPosition()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
            throw new InvalidOperationException("No FreeLabManager was found in the open scene.");

        Transform importedRoot = lab.transform.Find("Imported Endpoints");
        if (importedRoot == null)
            throw new InvalidOperationException("Imported Endpoints was not found below the FreeLabManager.");

        const float worldTolerance = 0.001f;
        HashSet<FreeLabEndpoint> imported = new HashSet<FreeLabEndpoint>(
            importedRoot.GetComponentsInChildren<FreeLabEndpoint>(true));
        List<FreeLabEndpoint> all = lab.GetComponentsInChildren<FreeLabEndpoint>(true)
            .Where(endpoint => endpoint != null).ToList();
        Dictionary<FreeLabEndpoint, FreeLabEndpoint> replacement =
            new Dictionary<FreeLabEndpoint, FreeLabEndpoint>();

        foreach (FreeLabEndpoint source in imported.OrderBy(endpoint => endpoint.gameObject.name))
        {
            if (!source.gameObject.activeInHierarchy || replacement.ContainsKey(source))
                continue;

            List<FreeLabEndpoint> coincident = all.Where(candidate =>
                candidate != source && candidate.gameObject.activeInHierarchy &&
                !replacement.ContainsKey(candidate) &&
                Vector3.Distance(candidate.transform.position, source.transform.position) <= worldTolerance).ToList();
            if (coincident.Count == 0)
                continue;

            // Existing Free Lab endpoint wins. If this group is imported-only,
            // retain one stable imported endpoint and redirect the rest to it.
            FreeLabEndpoint keeper = coincident.Where(candidate => !imported.Contains(candidate))
                .OrderBy(candidate => candidate.gameObject.name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? source;

            foreach (FreeLabEndpoint duplicate in coincident.Append(source)
                         .Where(candidate => candidate != keeper && imported.Contains(candidate)).Distinct())
                replacement[duplicate] = keeper;
        }

        int wireReferences = 0;
        foreach (FreeLabWireDefinition wire in lab.GetComponentsInChildren<FreeLabWireDefinition>(true))
        {
            if (replacement.TryGetValue(wire.endpointA, out FreeLabEndpoint endpointA))
            {
                Undo.RecordObject(wire, "Redirect Free Lab wire endpoint");
                wire.endpointA = endpointA;
                wireReferences++;
            }
            if (replacement.TryGetValue(wire.endpointB, out FreeLabEndpoint endpointB))
            {
                Undo.RecordObject(wire, "Redirect Free Lab wire endpoint");
                wire.endpointB = endpointB;
                wireReferences++;
            }
        }

        int evaluatorReferences = 0;
        foreach (FreeLabExperimentEvaluator evaluator in lab.GetComponentsInChildren<FreeLabExperimentEvaluator>(true))
        {
            SerializedObject serialized = new SerializedObject(evaluator);
            SerializedProperty states = serialized.FindProperty("states");
            bool changed = false;
            for (int stateIndex = 0; stateIndex < states.arraySize; stateIndex++)
            {
                SerializedProperty requirements = states.GetArrayElementAtIndex(stateIndex)
                    .FindPropertyRelative("requiredConnections");
                for (int requirementIndex = 0; requirementIndex < requirements.arraySize; requirementIndex++)
                foreach (string propertyName in new[] { "endpointA", "endpointB" })
                {
                    SerializedProperty endpoint = requirements.GetArrayElementAtIndex(requirementIndex)
                        .FindPropertyRelative(propertyName);
                    if (replacement.TryGetValue(endpoint.objectReferenceValue as FreeLabEndpoint,
                            out FreeLabEndpoint mapped))
                    {
                        endpoint.objectReferenceValue = mapped;
                        changed = true;
                        evaluatorReferences++;
                    }
                }
            }
            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(evaluator);
            }
        }

        foreach (FreeLabEndpoint duplicate in replacement.Keys)
            Undo.DestroyObjectImmediate(duplicate.gameObject);

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Merged " + replacement.Count + " imported endpoint duplicates by world position; redirected " +
            wireReferences + " wire references and " + evaluatorReferences + " evaluator references.", lab);
    }

    [MenuItem("Free Lab/Repair References to Inactive Endpoints")]
    private static void RepairReferencesToInactiveEndpoints()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
            throw new InvalidOperationException("No FreeLabManager was found in the open scene.");

        const float worldTolerance = 0.001f;
        List<FreeLabEndpoint> active = lab.GetComponentsInChildren<FreeLabEndpoint>(true)
            .Where(endpoint => endpoint != null && endpoint.gameObject.activeInHierarchy).ToList();
        Dictionary<FreeLabEndpoint, FreeLabEndpoint> replacement = new Dictionary<FreeLabEndpoint, FreeLabEndpoint>();
        foreach (FreeLabEndpoint inactive in lab.GetComponentsInChildren<FreeLabEndpoint>(true)
                     .Where(endpoint => endpoint != null && !endpoint.gameObject.activeInHierarchy))
        {
            FreeLabEndpoint match = active.FirstOrDefault(candidate =>
                Vector3.Distance(candidate.transform.position, inactive.transform.position) <= worldTolerance);
            if (match != null)
                replacement[inactive] = match;
        }

        int wireReferences = 0;
        foreach (FreeLabWireDefinition wire in lab.GetComponentsInChildren<FreeLabWireDefinition>(true))
        {
            if (replacement.TryGetValue(wire.endpointA, out FreeLabEndpoint endpointA)) { Undo.RecordObject(wire, "Repair inactive endpoint reference"); wire.endpointA = endpointA; wireReferences++; }
            if (replacement.TryGetValue(wire.endpointB, out FreeLabEndpoint endpointB)) { Undo.RecordObject(wire, "Repair inactive endpoint reference"); wire.endpointB = endpointB; wireReferences++; }
        }

        int evaluatorReferences = 0;
        foreach (FreeLabExperimentEvaluator evaluator in lab.GetComponentsInChildren<FreeLabExperimentEvaluator>(true))
        {
            SerializedObject serialized = new SerializedObject(evaluator);
            SerializedProperty states = serialized.FindProperty("states");
            bool changed = false;
            for (int stateIndex = 0; stateIndex < states.arraySize; stateIndex++)
            {
                SerializedProperty requirements = states.GetArrayElementAtIndex(stateIndex).FindPropertyRelative("requiredConnections");
                for (int requirementIndex = 0; requirementIndex < requirements.arraySize; requirementIndex++)
                foreach (string name in new[] { "endpointA", "endpointB" })
                {
                    SerializedProperty endpoint = requirements.GetArrayElementAtIndex(requirementIndex).FindPropertyRelative(name);
                    if (replacement.TryGetValue(endpoint.objectReferenceValue as FreeLabEndpoint, out FreeLabEndpoint mapped)) { endpoint.objectReferenceValue = mapped; changed = true; evaluatorReferences++; }
                }
            }
            if (changed) { serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(evaluator); }
        }
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Repaired " + wireReferences + " wire and " + evaluatorReferences + " evaluator references from inactive endpoints.", lab);
    }
}
