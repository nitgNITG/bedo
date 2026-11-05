#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TutorialFramework.EditorTools
{
    /// <summary>
    /// Makes one project-owned material copy per unique wire material and
    /// assigns it to every guided and Free Lab wire renderer.
    /// </summary>
    public static class WireMaterialCopySetup
    {
        private const string Folder = "Assets/_Project/Materials/Wires";

        [MenuItem("Free Lab/Create and Assign Wire Material Copies")]
        private static void CreateAndAssign()
        {
            EnsureFolder();
            Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
            HashSet<Renderer> wireRenderers = CollectWireRenderers();

            foreach (Renderer renderer in wireRenderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null) continue;
                    Material copy = GetOrCreateCopy(source, copies);
                    if (copy != source)
                    {
                        materials[i] = copy;
                        changed = true;
                    }
                }
                if (changed)
                {
                    Undo.RecordObject(renderer, "Assign Wire Material Copy");
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }

            WireMaterialTransparencyToggle toggle = FindTransparencyToggle();
            if (toggle != null)
            {
                SerializedObject serializedToggle = new SerializedObject(toggle);
                SerializedProperty materials = serializedToggle.FindProperty("wireMaterials");
                materials.arraySize = copies.Values.Count;
                int index = 0;
                foreach (Material copy in copies.Values)
                    materials.GetArrayElementAtIndex(index++).objectReferenceValue = copy;
                serializedToggle.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(toggle);
            }

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Free Lab: assigned " + copies.Count +
                " shared wire material copies to " + wireRenderers.Count +
                " guided/Free Mode wire renderers.");
        }

        [MenuItem("Free Lab/Reset Wire Material Copies To Opaque")]
        private static void ResetCopiesToOpaque()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { Folder });
            foreach (string guid in guids)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                if (material == null) continue;
                SetAlpha(material, 1f);
                SetFloat(material, "_Mode", 0f); SetFloat(material, "_SrcBlend", (float)BlendMode.One);
                SetFloat(material, "_DstBlend", (float)BlendMode.Zero); SetFloat(material, "_ZWrite", 1f);
                SetFloat(material, "_Surface", 0f); SetFloat(material, "_AlphaClip", 0f);
                material.SetOverrideTag("RenderType", ""); material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        private static HashSet<Renderer> CollectWireRenderers()
        {
            HashSet<Renderer> result = new HashSet<Renderer>();
            foreach (WireInteraction wire in Resources.FindObjectsOfTypeAll<WireInteraction>())
                AddRenderers(result, wire != null ? wire.wireObject : null);

            foreach (FreeLabWireDefinition definition in
                Resources.FindObjectsOfTypeAll<FreeLabWireDefinition>())
            {
                AddRenderers(result, definition != null ? definition.wireParentTemplate : null);
            }

            foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (gameObject.scene.IsValid() && gameObject.name == "Free Lab Wires")
                    AddRenderers(result, gameObject);
            }
            return result;
        }

        private static void AddRenderers(HashSet<Renderer> renderers, GameObject wireObject)
        {
            if (wireObject == null || !wireObject.scene.IsValid()) return;
            foreach (Renderer renderer in wireObject.GetComponentsInChildren<Renderer>(true))
                if (renderer != null) renderers.Add(renderer);
        }

        private static Material GetOrCreateCopy(Material source, Dictionary<Material, Material> copies)
        {
            if (copies.TryGetValue(source, out Material cached)) return cached;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (sourcePath.StartsWith(Folder + "/"))
            {
                copies.Add(source, source);
                return source;
            }

            string guid = AssetDatabase.AssetPathToGUID(sourcePath);
            string suffix = string.IsNullOrEmpty(guid) ? source.GetInstanceID().ToString() : guid.Substring(0, 8);
            string fileName = Sanitize(source.name) + "_Wire_" + suffix + ".mat";
            string copyPath = Folder + "/" + fileName;
            Material copy = AssetDatabase.LoadAssetAtPath<Material>(copyPath);
            if (copy == null)
            {
                copy = new Material(source) { name = Path.GetFileNameWithoutExtension(fileName) };
                AssetDatabase.CreateAsset(copy, copyPath);
            }
            copies.Add(source, copy);
            return copy;
        }

        private static WireMaterialTransparencyToggle FindTransparencyToggle()
        {
            foreach (WireMaterialTransparencyToggle toggle in
                Resources.FindObjectsOfTypeAll<WireMaterialTransparencyToggle>())
            {
                if (toggle != null && toggle.gameObject.scene.IsValid()) return toggle;
            }
            return null;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials/Wires"))
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "Wires");
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "WireMaterial" : value;
        }

        private static void SetAlpha(Material material, float alpha)
        {
            if (material.HasProperty("_Color")) { Color color = material.color; color.a = alpha; material.color = color; }
            if (material.HasProperty("_BaseColor")) { Color color = material.GetColor("_BaseColor"); color.a = alpha; material.SetColor("_BaseColor", color); }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }
    }
}
#endif
