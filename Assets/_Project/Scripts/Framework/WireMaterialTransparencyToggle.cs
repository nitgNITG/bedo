using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TutorialFramework
{
    /// <summary>
    /// Applies temporary material instances to every renderer using an assigned
    /// wire material. The material assets are never changed during Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public class WireMaterialTransparencyToggle : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private List<Material> wireMaterials = new List<Material>();
        [SerializeField, Range(0.02f, 0.95f)] private float transparentAlpha = 0.22f;

        private readonly Dictionary<Material, Material> runtimeCopies = new Dictionary<Material, Material>();
        private readonly List<RendererState> rendererStates = new List<RendererState>();
        private bool transparent;

        private sealed class RendererState
        {
            public Renderer renderer;
            public Material[] originalMaterials;
        }

        private void Reset() => button = GetComponent<Button>();

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            CreateRuntimeCopiesAndAssign();
            if (button != null) button.onClick.AddListener(ToggleTransparency);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(ToggleTransparency);
            RestoreOriginalRendererMaterials();
        }

        public void ToggleTransparency()
        {
            transparent = !transparent;
            foreach (Material material in runtimeCopies.Values)
            {
                if (transparent) MakeTransparent(material);
                else MakeOpaque(material);
            }
        }

        public void SetTransparent(bool value)
        {
            if (transparent != value) ToggleTransparency();
        }

        private void CreateRuntimeCopiesAndAssign()
        {
            foreach (Material source in wireMaterials)
            {
                if (source == null || runtimeCopies.ContainsKey(source)) continue;
                Material copy = new Material(source)
                {
                    name = source.name + " (Runtime Wire Transparency)",
                    hideFlags = HideFlags.DontSave
                };
                MakeOpaque(copy);
                runtimeCopies.Add(source, copy);
            }

            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (renderer == null || !renderer.gameObject.scene.IsValid()) continue;
                Material[] originals = renderer.sharedMaterials;
                Material[] replacements = null;
                for (int i = 0; i < originals.Length; i++)
                {
                    // Unrelated scene renderers can have empty material slots.
                    // Dictionary lookup rejects null keys and would abort Awake
                    // before the transparency button's listener is registered.
                    if (originals[i] == null) continue;
                    if (!runtimeCopies.TryGetValue(originals[i], out Material copy)) continue;
                    if (replacements == null) replacements = (Material[])originals.Clone();
                    replacements[i] = copy;
                }
                if (replacements == null) continue;
                rendererStates.Add(new RendererState { renderer = renderer, originalMaterials = originals });
                renderer.sharedMaterials = replacements;
            }
        }

        private void RestoreOriginalRendererMaterials()
        {
            foreach (RendererState state in rendererStates)
                if (state.renderer != null) state.renderer.sharedMaterials = state.originalMaterials;
            rendererStates.Clear();

            foreach (Material copy in runtimeCopies.Values)
                if (copy != null) Destroy(copy);
            runtimeCopies.Clear();
        }

        private void MakeTransparent(Material material)
        {
            SetAlpha(material, transparentAlpha);
            SetFloat(material, "_Mode", 3f); SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha); SetFloat(material, "_ZWrite", 0f);
            SetFloat(material, "_Surface", 1f); SetFloat(material, "_Blend", 0f); SetFloat(material, "_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", "Transparent"); material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void MakeOpaque(Material material)
        {
            SetAlpha(material, 1f);
            SetFloat(material, "_Mode", 0f); SetFloat(material, "_SrcBlend", (float)BlendMode.One);
            SetFloat(material, "_DstBlend", (float)BlendMode.Zero); SetFloat(material, "_ZWrite", 1f);
            SetFloat(material, "_Surface", 0f); SetFloat(material, "_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", ""); material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }

        private static void SetAlpha(Material material, float alpha)
        {
            if (material.HasProperty("_Color")) { Color c = material.color; c.a = alpha; material.color = c; }
            if (material.HasProperty("_BaseColor")) { Color c = material.GetColor("_BaseColor"); c.a = alpha; material.SetColor("_BaseColor", c); }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }
    }
}
