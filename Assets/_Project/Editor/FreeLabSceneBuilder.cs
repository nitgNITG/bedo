#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TutorialFramework.EditorTools
{
    /// <summary>
    /// Builds a separate Free Lab hierarchy without altering Guided Mode
    /// storyboards. Select the physical experiment hierarchy to use as the
    /// base, then run Tools > Free Lab > Build From Selected Experiment.
    /// </summary>
    public static class FreeLabSceneBuilder
    {
        [MenuItem("Tools/Free Lab/Build From Selected Experiment")]
        private static void BuildFromSelectedExperiment()
        {
            GameObject selected = Selection.activeGameObject;
            StoryboardSteps baseStoryboard = selected != null
                ? selected.GetComponentInParent<StoryboardSteps>()
                : null;

            if (baseStoryboard == null)
            {
                EditorUtility.DisplayDialog(
                    "Free Lab Builder",
                    "Select an Experiment GameObject with StoryboardSteps first.",
                    "OK"
                );
                return;
            }

            GameObject previous = GameObject.Find("Free Lab");
            if (previous != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "Replace Free Lab?",
                    "A Free Lab hierarchy already exists. Replace it?",
                    "Replace",
                    "Cancel"))
                {
                    return;
                }

                Object.DestroyImmediate(previous);
            }

            // A real copy/paste: same parent, sibling level and local
            // transform as the chosen experiment. Do not re-parent it under
            // a new helper hierarchy before its transform is established.
            GameObject freeLab = Object.Instantiate(
                baseStoryboard.gameObject,
                baseStoryboard.transform.parent
            );

            freeLab.transform.SetSiblingIndex(
                baseStoryboard.transform.GetSiblingIndex() + 1
            );

            freeLab.name = "Free Lab";
            Undo.RegisterCreatedObjectUndo(
                freeLab,
                "Create Free Lab"
            );

            foreach (StoryboardSteps storyboard in
                freeLab.GetComponentsInChildren<StoryboardSteps>(true))
            {
                Object.DestroyImmediate(storyboard);
            }

            // The copied base is physical lab content only. Existing Guided
            // wire scripts and visuals must start disabled; FreeLabManager
            // enables endpoints and instantiates only selected templates.
            foreach (WireInteraction wire in
                freeLab.GetComponentsInChildren<WireInteraction>(true))
            {
                wire.enabled = false;

                if (wire.wireObject != null)
                    wire.wireObject.SetActive(false);

                if (wire.plugA != null)
                    wire.plugA.SetActive(false);

                if (wire.plugB != null)
                    wire.plugB.SetActive(false);
            }

            FreeLabSceneRoot sceneRoot =
                freeLab.AddComponent<FreeLabSceneRoot>();
            FreeLabManager manager =
                freeLab.AddComponent<FreeLabManager>();

            // This is deliberately empty. Place and position your authored
            // wire parents manually inside it, then add a
            // FreeLabWireDefinition to each parent. The builder must never
            // generate or reposition authored wires.
            GameObject templates = new GameObject("Free Lab Wires");
            templates.transform.SetParent(freeLab.transform, false);

            foreach (Interaction interaction in
                freeLab.GetComponentsInChildren<Interaction>(true))
            {
                GetOrAddEndpoint(interaction);
            }

            freeLab.SetActive(false);
            Selection.activeGameObject = freeLab;
            EditorUtility.SetDirty(freeLab);
            EditorSceneManager.MarkSceneDirty(
                freeLab.scene
            );

            Debug.Log(
                "Free Lab created. Add your own authored wire parents under " +
                "Free Lab Wires, then assign their endpoints and plugs."
            );
        }

        private static FreeLabEndpoint GetOrAddEndpoint(
            Interaction interaction)
        {
            if (interaction == null)
                return null;

            FreeLabEndpoint endpoint =
                interaction.GetComponent<FreeLabEndpoint>();

            if (endpoint == null)
                endpoint = interaction.gameObject.AddComponent<FreeLabEndpoint>();

            return endpoint;
        }

    }
}
#endif
