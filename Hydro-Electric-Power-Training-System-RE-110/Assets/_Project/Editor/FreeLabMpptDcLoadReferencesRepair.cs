using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Corrects only the existing Free Exp 8 MPPT DC Load/DC LED wire definitions.
/// It deliberately does not touch wire splines, transforms, colliders, child objects,
/// or Guided Mode objects.
/// </summary>
public static class FreeLabMpptDcLoadReferencesRepair
{
    [MenuItem("Free Lab/Set Existing MPPT DC Load References Only")]
    private static void Apply()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (manager == null)
            throw new InvalidOperationException("FreeLabManager was not found.");

        var endpoints = manager.GetComponentsInChildren<FreeLabEndpoint>(true);
        FreeLabEndpoint Endpoint(string endpointId) => endpoints
            .FirstOrDefault(endpoint => endpoint != null && endpoint.EndpointId == endpointId)
            ?? throw new InvalidOperationException($"Free Lab endpoint '{endpointId}' was not found.");

        var loadPlus = Endpoint("MPPT DC Load +ve [Free Exp 8]");
        var loadMinus = Endpoint("MPPT DC Load -ve [Free Exp 8]");
        // The Exp 8 MPPT load uses the already shared Free Lab DC LED sockets.
        var ledPlus = Endpoint("DC LED +ve [Free Exp 7]");
        var ledMinus = Endpoint("DC LED -ve [Free Exp 7]");

        var definitions = manager.GetComponentsInChildren<FreeLabWireDefinition>(true);
        var positive = definitions.FirstOrDefault(definition =>
            definition != null && definition.gameObject.name == "Free Exp 8 - MPPT DC Load +ve to DC LED -ve");
        var negative = definitions.FirstOrDefault(definition =>
            definition != null && definition.gameObject.name == "Free Exp 8 - MPPT DC Load -ve to DC LED +ve");

        if (positive == null || negative == null)
            throw new InvalidOperationException("One of the two existing Free Exp 8 MPPT DC Load wire parents is missing.");

        Undo.RecordObjects(new UnityEngine.Object[] { positive, negative, positive.gameObject, negative.gameObject },
            "Correct MPPT DC Load polarity references");

        positive.gameObject.name = "Free Exp 8 - MPPT DC Load +ve to DC LED +ve";
        positive.endpointA = loadPlus;
        positive.endpointB = ledPlus;
        positive.plugA = loadPlus.SelectionPlug;
        positive.plugB = ledPlus.SelectionPlug;
        positive.gameObject.SetActive(true);

        negative.gameObject.name = "Free Exp 8 - MPPT DC Load -ve to DC LED -ve";
        negative.endpointA = loadMinus;
        negative.endpointB = ledMinus;
        negative.plugA = loadMinus.SelectionPlug;
        negative.plugB = ledMinus.SelectionPlug;
        negative.gameObject.SetActive(true);

        RedirectPair(manager, loadPlus, ledMinus, loadPlus, ledPlus);
        RedirectPair(manager, loadMinus, ledPlus, loadMinus, ledMinus);

        EditorUtility.SetDirty(positive);
        EditorUtility.SetDirty(negative);
        EditorUtility.SetDirty(positive.gameObject);
        EditorUtility.SetDirty(negative.gameObject);
        EditorSceneManager.MarkSceneDirty(positive.gameObject.scene);
        Debug.Log("Corrected the two existing MPPT DC Load/DC LED Free Mode references and plugs only.");
    }

    private static void RedirectPair(FreeLabManager manager, FreeLabEndpoint oldA,
        FreeLabEndpoint oldB, FreeLabEndpoint newA, FreeLabEndpoint newB)
    {
        foreach (var evaluator in manager.GetComponentsInChildren<FreeLabExperimentEvaluator>(true))
        {
            var serialized = new SerializedObject(evaluator);
            var states = serialized.FindProperty("states");
            var changed = false;
            for (var stateIndex = 0; stateIndex < states.arraySize; stateIndex++)
            {
                var requirements = states.GetArrayElementAtIndex(stateIndex)
                    .FindPropertyRelative("requiredConnections");
                for (var requirementIndex = 0; requirementIndex < requirements.arraySize; requirementIndex++)
                {
                    var pair = requirements.GetArrayElementAtIndex(requirementIndex);
                    var a = pair.FindPropertyRelative("endpointA");
                    var b = pair.FindPropertyRelative("endpointB");
                    var first = a.objectReferenceValue as FreeLabEndpoint;
                    var second = b.objectReferenceValue as FreeLabEndpoint;
                    if (!((first == oldA && second == oldB) || (first == oldB && second == oldA))) continue;
                    a.objectReferenceValue = newA;
                    b.objectReferenceValue = newB;
                    changed = true;
                }
            }
            if (!changed) continue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(evaluator);
        }
    }
}
