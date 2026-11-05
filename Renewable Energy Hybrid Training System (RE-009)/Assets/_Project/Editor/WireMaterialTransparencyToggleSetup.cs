#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TutorialFramework.EditorTools
{
    public static class WireMaterialTransparencyToggleSetup
    {
        [MenuItem("Free Lab/Setup Wire Transparency Button")]
        private static void SetupButton()
        {
            GameObject button = null;
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.scene.IsValid() &&
                    candidate.name == "Wire Transparency btn")
                {
                    button = candidate;
                    break;
                }
            }

            if (button == null)
            {
                Debug.LogError("Free Lab: could not find 'Wire Transparency btn' in the open scene.");
                return;
            }

            WireMaterialTransparencyToggle toggle = button.GetComponent<
                WireMaterialTransparencyToggle
            >();
            if (toggle == null)
            {
                toggle = Undo.AddComponent<WireMaterialTransparencyToggle>(button);
            }

            Selection.activeGameObject = button;
            EditorUtility.SetDirty(toggle);
            EditorSceneManager.MarkSceneDirty(button.scene);
            Debug.Log("Free Lab: WireMaterialTransparencyToggle is ready on 'Wire Transparency btn'. Assign its wire materials in the Inspector.");
        }
    }
}
#endif
