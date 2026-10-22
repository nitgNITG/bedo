#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TutorialFramework.EditorTools
{
    /// <summary>Creates a reusable indicator prefab and one centered instance per endpoint.</summary>
    public static class FreeLabPossibleConnectionIndicatorSetup
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string PrefabPath =
            PrefabFolder + "/FreeLabPossibleConnectionIndicator.prefab";
        private const string IndicatorName = "Possible Connection Indicator";

        [MenuItem("Free Lab/Setup Possible Connection Indicators")]
        private static void SetupIndicators()
        {
            FreeLabManager manager = Object.FindFirstObjectByType<FreeLabManager>();
            if (manager == null)
            {
                Debug.LogError("Free Lab: no FreeLabManager was found in the open scene.");
                return;
            }

            GameObject prefab = GetOrCreatePrefab();
            int created = 0;
            foreach (FreeLabEndpoint endpoint in
                manager.GetComponentsInChildren<FreeLabEndpoint>(true))
            {
                if (endpoint == null)
                    continue;

                Transform existing = endpoint.transform.Find(IndicatorName);
                GameObject indicator;
                if (existing != null)
                {
                    indicator = existing.gameObject;
                }
                else
                {
                    indicator = (GameObject)PrefabUtility.InstantiatePrefab(
                        prefab,
                        endpoint.transform
                    );
                    Undo.RegisterCreatedObjectUndo(
                        indicator,
                        "Create Possible Connection Indicator"
                    );
                    indicator.name = IndicatorName;
                    created++;
                }

                Transform indicatorTransform = indicator.transform;
                indicatorTransform.localPosition = Vector3.zero;
                indicatorTransform.localRotation = Quaternion.identity;
                indicatorTransform.localScale = Vector3.one;
                indicator.SetActive(false);
                endpoint.SetPossibleConnectionIndicator(indicator);
                EditorUtility.SetDirty(endpoint);
            }

            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Debug.Log("Free Lab: set up " + created +
                " possible-connection indicators.");
        }

        private static GameObject GetOrCreatePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
                return prefab;

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            template.name = IndicatorName;
            Object.DestroyImmediate(template.GetComponent<Collider>());
            template.transform.localScale = Vector3.one * 0.15f;
            prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
            Object.DestroyImmediate(template);
            return prefab;
        }
    }
}
#endif
