using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Extracts only independent equipment assemblies. It never unpacks a prefab,
/// moves a scene object, or changes experiment logic. Scene dependencies remain
/// explicit instance overrides rather than hidden references in reusable assets.
/// </summary>
public static class EquipmentPrefabConversion
{
    const string Folder = "Assets/_Project/Prefabs/Equipment";
    const string Reports = EquipmentPrefabWorkflow.ReportFolder;
    static readonly string[] PrefabFields = { "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset" };
    [Serializable] public class Entry { public string key, hash; }
    [Serializable] public class Snapshot { public string scene; public int transforms, missing; public List<Entry> entries = new List<Entry>(); }
    sealed class Link { public Object owner, target; public string property; }

    public static void DiscardUnsavedTrial()
    {
        var scene = SceneManager.GetActiveScene();
        if (File.ReadAllText(scene.path) != File.ReadAllText(Reports + "/PV Lab.before-prefabs.unity.txt"))
            throw new InvalidOperationException("The scene file changed since the baseline; refusing automatic trial rollback.");
        EditorSceneManager.OpenScene(scene.path);
        foreach (var path in new[] { Folder + "/Rheostats/Rheostat 150 Ohm.prefab", Folder + "/Rheostats/Rheostat Assembly.prefab" })
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        VerifyBaseline(SceneManager.GetActiveScene());
        File.AppendAllText(Reports + "/Conversion.txt", "Trial rejected before save: differently named children were not matched. Original scene restored and verified.\n");
    }

    public static void Baseline()
    {
        var scene = SceneManager.GetActiveScene();
        RequireClean(scene);
        Directory.CreateDirectory(Reports);
        File.Copy(scene.path, Reports + "/PV Lab.before-prefabs.unity.txt", true);
        var baseline = Capture(scene);
        File.WriteAllText(Reports + "/Baseline.json", JsonUtility.ToJson(baseline, true));
        File.WriteAllText(Reports + "/Conversion.txt", "Baseline captured; " + baseline.transforms + " transforms, " + baseline.missing + " pre-existing missing components.\n");
    }

    public static void Rheostats()
    {
        var scene = SceneManager.GetActiveScene(); RequireClean(scene);
        VerifyBaseline(scene);
        var root = scene.GetRootGameObjects().Single(g => g.name == "Experiments").transform;
        var first = root.Find("Rheostat 150"); var second = root.Find("rheostat 800");
        if (!first || !second) throw new InvalidOperationException("Expected authored rheostat assemblies were not found.");
        ConvertFamily(new[] { first, second }, "Rheostats", "Rheostat Assembly", new[] { "Rheostat 150 Ohm", "Rheostat 800 Ohm" });
        Finish(scene, "Rheostats");
    }

    public static void Lamps()
    {
        var scene = SceneManager.GetActiveScene(); RequireClean(scene); VerifyBaseline(scene);
        var outputs = EquipmentPrefabWorkflow.All(scene).Single(t => t.name == "Outputs" && t.parent && t.parent.name == "Wind Lab");
        var lamps = outputs.Cast<Transform>().Where(t => t.childCount == 2 && t.name.Contains("Lamp") &&
            t.GetComponentsInChildren<MeshRenderer>(true).Length == 2).ToArray();
        if (lamps.Length != 8) throw new InvalidOperationException("Expected eight authored lamp presentations; found " + lamps.Length);
        var free = EquipmentPrefabWorkflow.All(scene).Select(t => t.GetComponent<TutorialFramework.FreeLabWindController>()).First(c => c);
        var names = lamps.Select(t => {
            var binding = free.lamps.FirstOrDefault(l => l.light && l.light.transform.parent == t);
            if (binding == null) throw new InvalidOperationException("No lamp mapping for " + EquipmentPrefabWorkflow.PathOf(t));
            return Regex.Replace(binding.name, @"[^\w\- ]", "").Trim();
        }).ToArray();
        if (names.Distinct().Count() != names.Length) throw new InvalidOperationException("Lamp variant names must be unique.");
        ConvertFamily(lamps, "Lamps", "Lamp On Off Assembly", names);
        Finish(scene, "Lamps");
    }

    public static void Connectors()
    {
        var scene = SceneManager.GetActiveScene(); RequireClean(scene); VerifyBaseline(scene);
        var root = scene.GetRootGameObjects().Single(g => g.name == "Experiments").transform;
        ConvertFamily(new[] { root.Find("DC Halogen Lamp Module power Connectors"), root.Find("AC LED Lamp Module power Connectors") },
            "Connectors", "Lamp Power Connector Assembly", new[] { "DC Halogen Power Connectors", "AC LED Power Connectors" });
        Finish(scene, "Connectors");
    }

    public static void OutputModelVariant()
    {
        var scene = SceneManager.GetActiveScene(); RequireClean(scene); VerifyBaseline(scene);
        var root = EquipmentPrefabWorkflow.All(scene).Single(t => t.name == "Outputs" && t.parent && t.parent.name == "Wind Lab");
        if (PrefabUtility.GetPrefabAssetType(root.gameObject) != PrefabAssetType.Model)
            throw new InvalidOperationException("Expected the original Outputs model connection.");
        var path = Folder + "/Lamps/Wind Lamp Outputs.prefab";
        if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
        var links = ExternalLinks(root);
        var asset = PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, path, InteractionMode.AutomatedAction, out bool success);
        if (!success || !asset || PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.Variant)
            throw new InvalidOperationException("Expected a variant of the Outputs FBX model.");
        Restore(links);
        Finish(scene, "Outputs model variant with nested lamp variants");
    }

    static void ConvertFamily(Transform[] roots, string family, string baseName, string[] names)
    {
        var directory = Folder + "/" + family; Directory.CreateDirectory(directory); AssetDatabase.Refresh();
        foreach (var root in roots)
        {
            if (!root || PrefabUtility.IsPartOfPrefabInstance(root.gameObject)) throw new InvalidOperationException("Refusing to unpack or replace an existing prefab.");
            if (root.GetComponentsInChildren<Transform>(true).Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0))
                throw new InvalidOperationException("Cannot extract an assembly containing missing scripts.");
        }
        var basePath = directory + "/" + baseName + ".prefab";
        if (File.Exists(basePath)) throw new InvalidOperationException("Refusing to overwrite equipment assets: " + basePath);
        var baseAsset = PrefabUtility.SaveAsPrefabAsset(roots[0].gameObject, basePath, out bool saved);
        if (!saved || !baseAsset) throw new InvalidOperationException("Could not save equipment base: " + basePath);
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i]; var links = ExternalLinks(root);
            var before = Capture(root.gameObject.scene);
            var originalObjects = root.GetComponentsInChildren<Transform>(true).SelectMany(t => new Object[] { t.gameObject }.Concat(t.GetComponents<Component>())).Where(o => o).ToArray();
            var originalNames = new Dictionary<GameObject, string>();
            MatchNames(root, baseAsset.transform, originalNames);
            PrefabUtility.ConvertToPrefabInstance(root.gameObject, baseAsset, new ConvertToPrefabInstanceSettings {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                changeRootNameToAssetName = false,
                recordPropertyOverridesOfMatches = true,
                componentsNotMatchedBecomesOverride = true,
                gameObjectsNotMatchedBecomesOverride = true,
                logInfo = false
            }, InteractionMode.AutomatedAction);
            foreach (var pair in originalNames) { pair.Key.name = pair.Value; PrefabUtility.RecordPrefabInstancePropertyModifications(pair.Key); }
            Restore(links);
            Check(before, Capture(root.gameObject.scene), "base connection " + names[i]);
            var variant = directory + "/" + names[i] + ".prefab";
            if (File.Exists(variant)) throw new InvalidOperationException("Refusing to overwrite " + variant);
            var asset = PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, variant, InteractionMode.AutomatedAction, out saved);
            if (!saved || !asset || PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.Variant)
                throw new InvalidOperationException("Expected a connected prefab variant: " + variant);
            Restore(links);
            if (originalObjects.Any(o => !o)) throw new InvalidOperationException("A conversion replaced an authored object.");
            Check(before, Capture(root.gameObject.scene), "variant connection " + names[i]);
            File.AppendAllText(Reports + "/Conversion.txt", "PASS " + EquipmentPrefabWorkflow.PathOf(root) + " -> " + variant + "; preserved " + originalObjects.Length + " objects and " + links.Count + " external bindings.\n");
        }
    }

    static void MatchNames(Transform instance, Transform template, Dictionary<GameObject, string> originalNames)
    {
        var actualTypes = instance.GetComponents<Component>().Select(c => c.GetType()).ToArray();
        var requiredTypes = template.GetComponents<Component>().Select(c => c.GetType()).ToArray();
        if (instance.childCount != template.childCount || requiredTypes.Distinct().Any(type =>
            actualTypes.Count(t => t == type) < requiredTypes.Count(t => t == type)))
            throw new InvalidOperationException("Equipment structures differ; cannot safely match " + instance.name);
        for (int i = 0; i < instance.childCount; i++)
        {
            var child = instance.GetChild(i); var match = template.GetChild(i);
            originalNames.Add(child.gameObject, child.name); child.name = match.name;
            MatchNames(child, match, originalNames);
        }
    }

    static List<Link> ExternalLinks(Transform root)
    {
        var links = new List<Link>();
        foreach (var c in root.GetComponentsInChildren<Component>(true).Where(c => c))
        {
            if (c is Transform) continue;
            using (var so = new SerializedObject(c))
            {
                var p = so.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var target = p.objectReferenceValue;
                    if (!target || EditorUtility.IsPersistent(target)) continue;
                    var t = target is GameObject go ? go.transform : (target as Component)?.transform;
                    if (t && !t.IsChildOf(root)) links.Add(new Link { owner = c, property = p.propertyPath, target = target });
                }
            }
        }
        return links;
    }

    static void Restore(List<Link> links)
    {
        foreach (var link in links)
        {
            if (!link.owner || !link.target) throw new InvalidOperationException("A live scene binding was lost.");
            using (var so = new SerializedObject(link.owner))
            {
                var p = so.FindProperty(link.property);
                if (p == null) throw new InvalidOperationException("Binding property was lost: " + link.property);
                p.objectReferenceValue = link.target; so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(link.owner);
        }
    }

    static void Finish(Scene scene, string family)
    {
        VerifyBaseline(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save scene.");
        AssetDatabase.SaveAssets();
        scene = EditorSceneManager.OpenScene(scene.path);
        // UI layout is rebuilt over editor frames when a scene is reopened.
        // Compare after that rebuild rather than treating transient driven
        // RectTransform/scrollbar values as an authored change.
        EquipmentPrefabRequest.BeginSettledVerification(family);
    }

    static void RequireClean(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/PV Lab.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved PV Lab scene outside Play Mode; unsaved edits will never be overwritten.");
    }

    public static void VerifyBaseline(Scene scene)
        => Check(JsonUtility.FromJson<Snapshot>(File.ReadAllText(Reports + "/Baseline.json")), Capture(scene), "full scene");

    static void Check(Snapshot expected, Snapshot actual, string label)
    {
        var a = expected.entries.ToDictionary(e => e.key, e => e.hash);
        var b = actual.entries.ToDictionary(e => e.key, e => e.hash);
        var differences = a.Keys.Union(b.Keys).Where(k => !a.ContainsKey(k) || !b.ContainsKey(k) || a[k] != b[k]).ToArray();
        if (expected.transforms != actual.transforms || expected.missing != actual.missing || differences.Length > 0)
        {
            File.WriteAllText(Reports + "/Mismatch.txt", label + "\n" + string.Join("\n", differences));
            throw new InvalidOperationException("Prefab verification failed: " + label + "; " + differences.Length + " differences. No further changes made.");
        }
    }

    public static Snapshot Capture(Scene scene)
    {
        Canvas.ForceUpdateCanvases();
        var snapshot = new Snapshot { scene = scene.path };
        var transforms = EquipmentPrefabWorkflow.All(scene).ToArray(); snapshot.transforms = transforms.Length;
        var objects = new List<Object>();
        foreach (var t in transforms)
        {
            snapshot.missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            objects.Add(t.gameObject); objects.AddRange(t.GetComponents<Component>().Where(c => c));
        }
        var keys = objects.ToDictionary(o => o.GetInstanceID(), Key);
        using (var sha = SHA256.Create())
        foreach (var obj in objects)
        {
            var json = EditorJsonUtility.ToJson(obj);
            foreach (var field in PrefabFields) json = Regex.Replace(json, "\\\"" + field + "\\\":\\{[^}]*\\}", "\"" + field + "\":{}");
            json = Regex.Replace(json, "\\\"instanceID\\\":(-?\\d+)", m => {
                int id = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                if (id == 0) return "\"object\":\"null\"";
                if (keys.TryGetValue(id, out string key)) return "\"object\":\"" + key.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                var value = EditorUtility.InstanceIDToObject(id);
                if (value && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long local)) return "\"asset\":\"" + guid + ":" + local + "\"";
                return "\"unknown\":" + id;
            });
            snapshot.entries.Add(new Entry { key = keys[obj.GetInstanceID()], hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))) });
        }
        return snapshot;
    }

    static string Key(Object obj)
    {
        if (obj is GameObject go) return EquipmentPrefabWorkflow.PathOf(go.transform) + "|GameObject";
        var c = (Component)obj;
        return EquipmentPrefabWorkflow.PathOf(c.transform) + "|" + c.GetType().FullName + "[" + Array.IndexOf(c.GetComponents<Component>(), c) + "]";
    }
}
