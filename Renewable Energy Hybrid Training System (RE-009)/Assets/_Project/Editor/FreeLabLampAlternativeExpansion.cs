using System;
using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Free Mode-only lamp alternative setup.  It creates a second authored wire
/// from an existing cable without changing the source cable or any Guided
/// Mode object.  Clones stay under the same Free Lab Wires parent and retain
/// the source wire's local transform exactly; their spline shape is left for
/// the author to position later.
/// </summary>
public static class FreeLabLampAlternativeExpansion
{
    private sealed class Role
    {
        public string id;
        public string[] original;
        public string[] alternative;
        public string[] endpointReference;

        public Role(string id, string[] original, string[] alternative,
            string[] endpointReference)
        {
            this.id = id;
            this.original = original;
            this.alternative = alternative;
            this.endpointReference = endpointReference;
        }
    }

    [MenuItem("Free Lab/Expand LED and Halogen lamp alternatives %&l")]
    private static void Expand()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null)
        {
            Debug.LogError("Lamp alternatives: no FreeLabManager was found.");
            return;
        }

        List<FreeLabWireDefinition> wires = lab.GetComponentsInChildren<FreeLabWireDefinition>(true).ToList();
        Transform manager = GetOrCreate(lab.transform, "Free Lab Device Manager");
        Transform groupsRoot = GetOrCreate(manager, "Lamp Wiring Alternatives");
        var groups = new Dictionary<string, FreeLabConnectionGroup>();
        var report = new List<string>();

        foreach (Role role in Roles())
        {
            List<FreeLabWireDefinition> original = FindAll(wires, role.original, role.id + " original", report);
            if (original.Count != role.original.Length)
                continue;

            List<FreeLabWireDefinition> alternative = new List<FreeLabWireDefinition>();
            for (int i = 0; i < role.original.Length; i++)
            {
                FreeLabWireDefinition source = original[i];
                string referenceName = role.endpointReference[i];
                FreeLabWireDefinition reference = referenceName.StartsWith("@endpoint:")
                    ? null : FindWire(wires, referenceName);
                if (reference == null && !referenceName.StartsWith("@endpoint:"))
                {
                    report.Add("MISSING ENDPOINT REFERENCE: " + referenceName);
                    continue;
                }

                FreeLabEndpoint sourceLamp = FindLampEndpoint(source);
                FreeLabEndpoint targetLamp = referenceName.StartsWith("@endpoint:")
                    ? FindEndpoint(referenceName.Substring("@endpoint:".Length))
                    : FindLampEndpoint(reference);
                if (sourceLamp == null || targetLamp == null)
                {
                    report.Add("MISSING LAMP ENDPOINT: " + role.alternative[i] +
                        " | source=" + DescribeEndpoints(source) +
                        " | target=" + (reference != null ? DescribeEndpoints(reference) : referenceName));
                    continue;
                }

                FreeLabEndpoint other = source.endpointA == sourceLamp ? source.endpointB : source.endpointA;
                FreeLabWireDefinition target = FindWire(wires, role.alternative[i]);
                if (target == null)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent, false);
                    clone.name = role.alternative[i];
                    clone.transform.localPosition = source.transform.localPosition;
                    clone.transform.localRotation = source.transform.localRotation;
                    clone.transform.localScale = source.transform.localScale;
                    Undo.RegisterCreatedObjectUndo(clone, "Create Free Lab lamp alternative wire");
                    target = clone.GetComponent<FreeLabWireDefinition>();
                    wires.Add(target);
                    report.Add("CREATED (unmoved): " + target.name);
                }

                target.endpointA = other;
                target.endpointB = targetLamp;
                target.plugA = FindPlug(wires, other);
                target.plugB = FindPlug(wires, targetLamp);
                target.wireParentTemplate = target.gameObject;
                EditorUtility.SetDirty(target);
                alternative.Add(target);
            }

            if (alternative.Count != role.alternative.Length)
            {
                report.Add("INCOMPLETE ROLE (not assigned): " + role.id);
                continue;
            }

            groups[role.id + " LED"] = ConfigureGroup(groupsRoot, role.id + " LED", original);
            groups[role.id + " Halogen"] = ConfigureGroup(groupsRoot, role.id + " Halogen", alternative);
        }

        ReplaceExactLampRequirements(lab, groups, report);
        ConfigureDevicePaths(manager, groups);
        ConfigureLoadRouter(manager, lab);
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Debug.Log("Free Lab lamp alternatives completed.\n" + string.Join("\n", report), lab);
    }

    private static IEnumerable<Role> Roles()
    {
        // Each role is a full terminal pair for one particular circuit
        // position.  This deliberately prevents an off-grid pair from being
        // used as the requirement for a PWM/MPPT configuration.
        yield return new Role("Exp 7 Battery DC", A("Free Exp 7 - Battery Output to DC LED Lamp Red", "Free Exp 7 - Battery Output to DC LED Lamp Black"), A("Free Exp 7 - Battery Output to DC Halogen Lamp Red", "Free Exp 7 - Battery Output to DC Halogen Lamp Black"), A("Free Exp 8 - Battery Output to DC Halogen Lamp Red", "Free Exp 8 - Battery Output to DC Halogen Lamp Black"));
        yield return new Role("Exp 7 PWM DC", A("Free Exp 7 - PMW DC Load +ve to DC LED +ve", "Free Exp 7 - PMW DC Load -ve to DC LED -ve"), A("Free Exp 7 - PMW DC Load +ve to DC Halogen +ve", "Free Exp 7 - PMW DC Load -ve to DC Halogen -ve"), A("@endpoint:DC Halogen +ve [Free Exp 9]", "@endpoint:DC Halogen -ve [Free Exp 9]"));
        yield return new Role("Exp 8 Battery DC", A("Free Exp 8 - Battery Output to DC Halogen Lamp Red", "Free Exp 8 - Battery Output to DC Halogen Lamp Black"), A("Free Exp 8 - Battery Output to DC LED Lamp Red", "Free Exp 8 - Battery Output to DC LED Lamp Black"), A("Free Exp 7 - Battery Output to DC LED Lamp Red", "Free Exp 7 - Battery Output to DC LED Lamp Black"));
        yield return new Role("Exp 8 MPPT DC", A("Free Exp 8 - MPPT DC Load +ve to DC LED +ve", "Free Exp 8 - MPPT DC Load -ve to DC LED -ve"), A("Free Exp 8 - MPPT DC Load +ve to DC Halogen +ve", "Free Exp 8 - MPPT DC Load -ve to DC Halogen -ve"), A("@endpoint:DC Halogen +ve [Free Exp 9]", "@endpoint:DC Halogen -ve [Free Exp 9]"));
        yield return new Role("Exp 8 Main Board AC", A("Free Exp 8 - Main Board to AC Halogen Lamp Red (1)", "Free Exp 8 - Main Board to AC Halogen Lamp Black (1)"), A("Free Exp 8 - Main Board to AC LED Lamp Red", "Free Exp 8 - Main Board to AC LED Lamp Black"), A("Free Exp 9 - Main Board to AC LED Lamp Red", "Free Exp 9 - Main Board to AC LED Lamp Black"));
        yield return new Role("Exp 8 Inverter Output AC", A("Free Exp 8 - AC Halogen Lamp Output to Off Grid Inverter Red (1)", "Free Exp 8 - AC Halogen Lamp Output to Off Grid Inverter Black (1)"), A("Free Exp 8 - AC LED Lamp Output to Off Grid Inverter Red", "Free Exp 8 - AC LED Lamp Output to Off Grid Inverter Black"), A("Free Exp 9 - AC LED Lamp Output to Off Grid Inverter Red", "Free Exp 9 - AC LED Lamp Output to Off Grid Inverter Black"));
        yield return new Role("Exp 9 Main Board AC", A("Free Exp 9 - Main Board to AC LED Lamp Red", "Free Exp 9 - Main Board to AC LED Lamp Black"), A("Free Exp 9 - Main Board to AC Halogen Lamp Red", "Free Exp 9 - Main Board to AC Halogen Lamp Black"), A("Free Exp 8 - Main Board to AC Halogen Lamp Red (1)", "Free Exp 8 - Main Board to AC Halogen Lamp Black (1)"));
        yield return new Role("Exp 9 Inverter Output AC", A("Free Exp 9 - AC LED Lamp Output to Off Grid Inverter Red", "Free Exp 9 - AC LED Lamp Output to Off Grid Inverter Black"), A("Free Exp 9 - AC Halogen Lamp Output to Off Grid Inverter Red", "Free Exp 9 - AC Halogen Lamp Output to Off Grid Inverter Black"), A("Free Exp 8 - AC Halogen Lamp Output to Off Grid Inverter Red (1)", "Free Exp 8 - AC Halogen Lamp Output to Off Grid Inverter Black (1)"));
        yield return new Role("Exp 9 Inverter Input DC", A("Free Exp 9 - Off grid inverter AC input L to DC Halogen +ve", "Free Exp 9 - Off grid inverter AC input N to DC Halogen -ve (1)"), A("Free Exp 9 - Off grid inverter AC input L to DC LED +ve", "Free Exp 9 - Off grid inverter AC input N to DC LED -ve"), A("Free Exp 7 - PMW DC Load +ve to DC LED +ve", "Free Exp 7 - PMW DC Load -ve to DC LED -ve"));
        yield return new Role("Exp 9 Inverter Output AC L N", A("Free Exp 9 - Off grid inverter AC output L to AC Halogen L", "Free Exp 9 - Off grid inverter AC output N to AC Halogen N"), A("Free Exp 9 - Off grid inverter AC output L to AC LED L", "Free Exp 9 - Off grid inverter AC output N to AC LED N"), A("@endpoint:AC LED L [Free Exp 9] (1)", "@endpoint:AC LED N [Free Exp 9] (1)"));
    }

    private static string[] A(params string[] values) { return values; }

    private static List<FreeLabWireDefinition> FindAll(List<FreeLabWireDefinition> wires, IEnumerable<string> names, string label, List<string> report)
    {
        List<FreeLabWireDefinition> result = new List<FreeLabWireDefinition>();
        foreach (string name in names)
        {
            FreeLabWireDefinition wire = FindWire(wires, name);
            if (wire == null) report.Add("MISSING SOURCE: " + name);
            else result.Add(wire);
        }
        return result;
    }

    private static FreeLabWireDefinition FindWire(IEnumerable<FreeLabWireDefinition> wires, string name)
    {
        return wires.FirstOrDefault(w => w != null && string.Equals(w.gameObject.name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static FreeLabEndpoint FindLampEndpoint(FreeLabWireDefinition wire)
    {
        if (wire == null) return null;
        bool aLamp = IsLamp(wire.endpointA);
        bool bLamp = IsLamp(wire.endpointB);
        return aLamp && !bLamp ? wire.endpointA : bLamp && !aLamp ? wire.endpointB : null;
    }

    private static bool IsLamp(FreeLabEndpoint endpoint)
    {
        if (endpoint == null) return false;
        // EndpointId can be a copied wire label (and therefore mention both
        // ends).  The authored GameObject name is the reliable physical-socket
        // identity for deciding which end belongs to the lamp.
        string value = endpoint.gameObject.name;
        return value.IndexOf("lamp", StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("led", StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("halogen", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static FreeLabEndpoint FindEndpoint(string name)
    {
        // Resources also includes inactive objects and endpoints nested under
        // disabled Free Lab hierarchy branches, which is how authored Free
        // Mode sockets are stored in this scene.
        FreeLabEndpoint[] endpoints = Resources.FindObjectsOfTypeAll<FreeLabEndpoint>();
        return endpoints.FirstOrDefault(endpoint => endpoint != null &&
            (string.Equals(endpoint.gameObject.name.Trim(), name.Trim(),
                StringComparison.OrdinalIgnoreCase) ||
             string.Equals(endpoint.EndpointId.Trim(), name.Trim(),
                StringComparison.OrdinalIgnoreCase)));
    }

    private static string DescribeEndpoints(FreeLabWireDefinition wire)
    {
        if (wire == null) return "<none>";
        return (wire.endpointA != null ? wire.endpointA.gameObject.name : "<null>") +
            " <> " + (wire.endpointB != null ? wire.endpointB.gameObject.name : "<null>");
    }

    private static GameObject FindPlug(IEnumerable<FreeLabWireDefinition> wires, FreeLabEndpoint endpoint)
    {
        foreach (FreeLabWireDefinition wire in wires)
        {
            if (wire == null) continue;
            if (wire.endpointA == endpoint && wire.plugA != null) return wire.plugA;
            if (wire.endpointB == endpoint && wire.plugB != null) return wire.plugB;
        }
        return endpoint != null ? endpoint.SelectionPlug : null;
    }

    private static FreeLabConnectionGroup ConfigureGroup(Transform root, string id, List<FreeLabWireDefinition> wires)
    {
        Transform child = GetOrCreate(root, id);
        FreeLabConnectionGroup group = child.GetComponent<FreeLabConnectionGroup>();
        if (group == null) group = Undo.AddComponent<FreeLabConnectionGroup>(child.gameObject);
        group.Configure(id, wires);
        EditorUtility.SetDirty(group);
        return group;
    }

    private static void ConfigureDevicePaths(Transform manager, Dictionary<string, FreeLabConnectionGroup> groups)
    {
        ConfigureDevice(manager, "DC LED Lamp Device", groups.Where(pair => pair.Key.EndsWith(" LED", StringComparison.Ordinal) && pair.Key.IndexOf(" DC", StringComparison.Ordinal) >= 0).Select(pair => pair.Value));
        ConfigureDevice(manager, "DC Halogen Lamp Device", groups.Where(pair => pair.Key.EndsWith(" Halogen", StringComparison.Ordinal) && pair.Key.IndexOf(" DC", StringComparison.Ordinal) >= 0).Select(pair => pair.Value));
        ConfigureDevice(manager, "AC LED Lamp Device", groups.Where(pair => pair.Key.EndsWith(" LED", StringComparison.Ordinal) && pair.Key.IndexOf(" AC", StringComparison.Ordinal) >= 0).Select(pair => pair.Value));
        ConfigureDevice(manager, "AC Halogen Lamp Device", groups.Where(pair => pair.Key.EndsWith(" Halogen", StringComparison.Ordinal) && pair.Key.IndexOf(" AC", StringComparison.Ordinal) >= 0).Select(pair => pair.Value));
    }

    private static void ConfigureLoadRouter(Transform manager, FreeLabManager lab)
    {
        FreeLabDeviceGroup dc = manager.GetComponentsInChildren<FreeLabDeviceGroup>(true)
            .FirstOrDefault(group => group != null && group.GroupId == "DC Lamp Alternatives");
        FreeLabDeviceGroup ac = manager.GetComponentsInChildren<FreeLabDeviceGroup>(true)
            .FirstOrDefault(group => group != null && group.GroupId == "AC Lamp Alternatives");
        FreeLabPVOffGridBinder binder = lab.GetComponentsInChildren<FreeLabPVOffGridBinder>(true)
            .FirstOrDefault();
        if (dc == null || ac == null || binder == null)
        {
            Debug.LogWarning("Lamp alternatives: load router was not assigned because the DC/AC device group or FreeLabPVOffGridBinder is missing.", manager);
            return;
        }

        FreeLabLampLoadRouter router = manager.GetComponent<FreeLabLampLoadRouter>();
        if (router == null) router = Undo.AddComponent<FreeLabLampLoadRouter>(manager.gameObject);
        router.Configure(dc, ac, binder);
        EditorUtility.SetDirty(router);
    }

    private static void ReplaceExactLampRequirements(FreeLabManager lab,
        Dictionary<string, FreeLabConnectionGroup> groups, List<string> report)
    {
        List<Role> roles = Roles().ToList();
        int replacements = 0;
        foreach (FreeLabControl control in lab.GetComponentsInChildren<FreeLabControl>(true))
        {
            SerializedObject serialized = new SerializedObject(control);
            SerializedProperty configurations = serialized.FindProperty("enableConfigurations");
            if (configurations == null) continue;
            for (int i = 0; i < configurations.arraySize; i++)
                replacements += ReplaceInRequirementContainer(configurations.GetArrayElementAtIndex(i), roles, groups);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (replacements > 0) EditorUtility.SetDirty(control);
        }

        foreach (FreeLabExperimentEvaluator evaluator in lab.GetComponentsInChildren<FreeLabExperimentEvaluator>(true))
        {
            SerializedObject serialized = new SerializedObject(evaluator);
            SerializedProperty states = serialized.FindProperty("states");
            if (states == null) continue;
            int evaluatorReplacements = 0;
            for (int i = 0; i < states.arraySize; i++)
                evaluatorReplacements += ReplaceInRequirementContainer(states.GetArrayElementAtIndex(i), roles, groups);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (evaluatorReplacements > 0)
            {
                replacements += evaluatorReplacements;
                EditorUtility.SetDirty(evaluator);
            }
        }
        report.Add("Updated exact Free Mode lamp requirements: " + replacements);
    }

    private static int ReplaceInRequirementContainer(SerializedProperty container,
        List<Role> roles, Dictionary<string, FreeLabConnectionGroup> groups)
    {
        SerializedProperty connections = container.FindPropertyRelative("requiredConnections");
        SerializedProperty rows = container.FindPropertyRelative("requiredAlternativeConnectionGroups");
        if (connections == null || rows == null) return 0;
        int replaced = 0;
        foreach (Role role in roles)
        {
            FreeLabConnectionGroup led;
            FreeLabConnectionGroup halogen;
            if (!groups.TryGetValue(role.id + " LED", out led) ||
                !groups.TryGetValue(role.id + " Halogen", out halogen) ||
                !ContainsEveryWirePair(connections, led.RequiredWires))
                continue;

            RemoveWirePairs(connections, led.RequiredWires);
            int index = rows.arraySize++;
            SerializedProperty alternatives = rows.GetArrayElementAtIndex(index)
                .FindPropertyRelative("alternatives");
            alternatives.arraySize = 2;
            alternatives.GetArrayElementAtIndex(0).objectReferenceValue = led;
            alternatives.GetArrayElementAtIndex(1).objectReferenceValue = halogen;
            replaced++;
        }
        return replaced;
    }

    private static bool ContainsEveryWirePair(SerializedProperty connections,
        IReadOnlyList<FreeLabWireDefinition> wires)
    {
        if (wires == null || wires.Count == 0) return false;
        foreach (FreeLabWireDefinition wire in wires)
        {
            if (wire == null || !ContainsPair(connections, wire.endpointA, wire.endpointB))
                return false;
        }
        return true;
    }

    private static bool ContainsPair(SerializedProperty connections,
        FreeLabEndpoint a, FreeLabEndpoint b)
    {
        for (int i = 0; i < connections.arraySize; i++)
        {
            SerializedProperty item = connections.GetArrayElementAtIndex(i);
            FreeLabEndpoint first = item.FindPropertyRelative("endpointA").objectReferenceValue as FreeLabEndpoint;
            FreeLabEndpoint second = item.FindPropertyRelative("endpointB").objectReferenceValue as FreeLabEndpoint;
            if ((first == a && second == b) || (first == b && second == a)) return true;
        }
        return false;
    }

    private static void RemoveWirePairs(SerializedProperty connections,
        IReadOnlyList<FreeLabWireDefinition> wires)
    {
        for (int i = connections.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty item = connections.GetArrayElementAtIndex(i);
            FreeLabEndpoint first = item.FindPropertyRelative("endpointA").objectReferenceValue as FreeLabEndpoint;
            FreeLabEndpoint second = item.FindPropertyRelative("endpointB").objectReferenceValue as FreeLabEndpoint;
            if (wires.Any(wire => wire != null && ((wire.endpointA == first && wire.endpointB == second) ||
                (wire.endpointA == second && wire.endpointB == first))))
                connections.DeleteArrayElementAtIndex(i);
        }
    }

    private static void ConfigureDevice(Transform manager, string name, IEnumerable<FreeLabConnectionGroup> groups)
    {
        Transform target = manager.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        if (target == null) return;
        FreeLabDevice device = target.GetComponent<FreeLabDevice>();
        if (device == null) return;
        device.ConfigureRequiredWires(Array.Empty<FreeLabWireDefinition>());
        device.ConfigureAlternativeConnectionGroups(groups);
        EditorUtility.SetDirty(device);
    }

    private static Transform GetOrCreate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing;
        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Create Free Lab lamp alternative holder");
        created.transform.SetParent(parent, false);
        return created.transform;
    }
}
