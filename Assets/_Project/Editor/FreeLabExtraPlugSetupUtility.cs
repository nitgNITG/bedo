#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TutorialFramework.EditorTools
{
    /// <summary>
    /// Creates exactly one extra plug for each Free Lab endpoint that has two
    /// or more authored wire definitions. The original plug is always first;
    /// the duplicate is used only for a second simultaneous connection.
    /// </summary>
    public static class FreeLabExtraPlugSetupUtility
    {
        [MenuItem("Free Lab/Rebuild Second Connection Plugs")]
        private static void RebuildSecondConnectionPlugs()
        {
            FreeLabManager manager = Object.FindFirstObjectByType<FreeLabManager>();
            if (manager == null)
            {
                Debug.LogError("Free Lab: no FreeLabManager was found in the open scene.");
                return;
            }

            Dictionary<FreeLabEndpoint, int> requiredSlots =
                new Dictionary<FreeLabEndpoint, int>();

            foreach (FreeLabWireDefinition wire in
                manager.GetComponentsInChildren<FreeLabWireDefinition>(true))
            {
                AddRequiredSlot(requiredSlots, wire != null ? wire.endpointA : null);
                AddRequiredSlot(requiredSlots, wire != null ? wire.endpointB : null);
            }

            Transform existingHolder = manager.transform.Find("Free Lab Extra Connection Plugs");
            ClearExistingGeneratedPlugs(manager, existingHolder);
            int created = 0;
            int skipped = 0;

            foreach (KeyValuePair<FreeLabEndpoint, int> entry in requiredSlots)
            {
                FreeLabEndpoint endpoint = entry.Key;
                if (endpoint == null)
                    continue;

                // The source must be the physical plug under this banana,
                // never a wire-template plug. Template fields are reused by
                // multiple connections and can therefore all point to one
                // shared position.
                GameObject sourcePlug = FindPhysicalEndpointPlug(endpoint);
                if (sourcePlug == null)
                    sourcePlug = endpoint.SelectionPlug;

                // Keep the first connection on this physical plug too.
                endpoint.SetSelectionPlug(sourcePlug);
                if (sourcePlug == null)
                {
                    skipped++;
                    Debug.LogWarning(
                        "Free Lab: '" + endpoint.name +
                        "' needs extra plugs but has no Selection Plug assigned.",
                        endpoint
                    );
                    continue;
                }

                EditorUtility.SetDirty(endpoint);

                // One original plug plus one duplicate is the intentional
                // maximum. A third authored wire sharing this endpoint is not
                // a supported simultaneous connection in this lab.
                if (entry.Value < 2)
                    continue;

                // Keep the duplicate under the exact same banana as its
                // original. Instantiating with that parent copies the local
                // transform exactly; no position, rotation, scale, or parent
                // conversion is performed afterward.
                GameObject extraPlug = Object.Instantiate(
                    sourcePlug,
                    sourcePlug.transform.parent
                );
                Undo.RegisterCreatedObjectUndo(
                    extraPlug,
                    "Create Free Lab Second Connection Plug"
                );

                extraPlug.name = endpoint.name + " - Free Lab Second Plug";
                extraPlug.SetActive(false);
                endpoint.AddAdditionalConnectionPlug(extraPlug);
                created++;
            }

            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Debug.Log(
                "Free Lab: rebuilt " + created +
                " second-connection plugs under their original endpoints. " +
                "Endpoints skipped because they have no source plug: " +
                skipped + "."
            );
        }

        private static void AddRequiredSlot(
            Dictionary<FreeLabEndpoint, int> requiredSlots,
            FreeLabEndpoint endpoint)
        {
            if (endpoint == null)
                return;

            if (!requiredSlots.ContainsKey(endpoint))
                requiredSlots.Add(endpoint, 0);

            requiredSlots[endpoint]++;
        }

        private static void ClearExistingGeneratedPlugs(
            FreeLabManager manager,
            Transform existingHolder)
        {
            foreach (FreeLabEndpoint endpoint in
                manager.GetComponentsInChildren<FreeLabEndpoint>(true))
            {
                if (endpoint == null)
                    continue;

                endpoint.ClearAdditionalConnectionPlugs();
                EditorUtility.SetDirty(endpoint);
            }

            if (existingHolder == null)
                return;

            // The holder is dedicated to prior generated plugs, so removing
            // it cannot touch any original authored plug.
            Undo.DestroyObjectImmediate(existingHolder.gameObject);
        }

        private static GameObject FindEndpointSpecificPlug(
            FreeLabManager manager,
            FreeLabEndpoint endpoint)
        {
            foreach (FreeLabWireDefinition wire in
                manager.GetComponentsInChildren<FreeLabWireDefinition>(true))
            {
                if (wire == null)
                    continue;

                if (wire.endpointA == endpoint && wire.plugA != null)
                    return wire.plugA;

                if (wire.endpointB == endpoint && wire.plugB != null)
                    return wire.plugB;
            }

            return null;
        }

        private static GameObject FindPhysicalEndpointPlug(FreeLabEndpoint endpoint)
        {
            // The project convention is Banana -> Plug. Search direct children
            // first so another banana's nested plug can never be selected.
            foreach (Transform child in endpoint.transform)
            {
                if (child.name.IndexOf("plug", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child.gameObject;
            }

            // Some authored bananas use one visual wrapper before the Plug.
            // Still require that the candidate belongs to this endpoint.
            foreach (Transform child in endpoint.GetComponentsInChildren<Transform>(true))
            {
                if (child == endpoint.transform)
                    continue;

                if (child.name.IndexOf("plug", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child.gameObject;
            }

            return null;
        }
    }
}
#endif
