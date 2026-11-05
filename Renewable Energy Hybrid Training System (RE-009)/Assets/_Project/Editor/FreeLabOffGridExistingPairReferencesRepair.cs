using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Changes only the references/names of the two existing Free Exp 9 Off-Grid wire parents.</summary>
public static class FreeLabOffGridExistingPairReferencesRepair
{
    [MenuItem("Free Lab/Set Existing Off-Grid Pair References Only")]
    private static void Apply()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null) throw new InvalidOperationException("FreeLabManager is missing.");
        FreeLabEndpoint[] endpoints = lab.GetComponentsInChildren<FreeLabEndpoint>(true);
        FreeLabEndpoint Endpoint(string id) => endpoints.FirstOrDefault(e => e != null && e.EndpointId == id);
        FreeLabEndpoint inverterPlus = Endpoint("Off grid inverter +ve [Free Exp 9]");
        FreeLabEndpoint inverterMinus = Endpoint("Off grid inverter -ve [Free Exp 9]");
        FreeLabEndpoint batteryPlus = Endpoint("Battery Module +ve [Free Exp 7]");
        FreeLabEndpoint batteryMinus = Endpoint("Battery Module -ve [Free Exp 7]");
        if (new[] { inverterPlus, inverterMinus, batteryPlus, batteryMinus }.Any(e => e == null))
            throw new InvalidOperationException("A required Off-Grid/Battery Free Mode endpoint is missing.");

        FreeLabWireDefinition[] definitions = lab.GetComponentsInChildren<FreeLabWireDefinition>(true);
        FreeLabWireDefinition negative = definitions.FirstOrDefault(w => w != null &&
            w.gameObject.name.Equals("Free Exp 9 - Off grid inverter +ve to battery -ve", StringComparison.OrdinalIgnoreCase));
        FreeLabWireDefinition positive = definitions.FirstOrDefault(w => w != null &&
            w.gameObject.name.Equals("Free Exp 9 - MPPT Battery +ve to Battery +ve", StringComparison.OrdinalIgnoreCase));
        if (negative == null || positive == null)
            throw new InvalidOperationException("One of the two existing Free Exp 9 wire parents is missing.");

        Undo.SetCurrentGroupName("Set existing Free Exp 9 Off-Grid references");
        int group = Undo.GetCurrentGroup();
        Undo.RecordObjects(new UnityEngine.Object[] { negative, positive }, "Set existing Off-Grid endpoints and plugs");

        negative.gameObject.name = "Free Exp 9 - Off grid inverter -ve to battery -ve";
        negative.endpointA = inverterMinus;
        negative.endpointB = batteryMinus;
        negative.plugA = inverterMinus.SelectionPlug;
        negative.plugB = batteryMinus.SelectionPlug;
        negative.gameObject.SetActive(true);

        positive.gameObject.name = "Free Exp 9 - Off grid inverter +ve to battery +ve";
        positive.endpointA = inverterPlus;
        positive.endpointB = batteryPlus;
        positive.plugA = inverterPlus.SelectionPlug;
        positive.plugB = batteryPlus.SelectionPlug;
        positive.gameObject.SetActive(true);

        Redirect(lab, inverterPlus, batteryMinus, inverterPlus, batteryPlus);
        Redirect(lab, inverterMinus, batteryPlus, inverterMinus, batteryMinus);

        EditorUtility.SetDirty(negative);
        EditorUtility.SetDirty(positive);
        EditorUtility.SetDirty(lab);
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Undo.CollapseUndoOperations(group);
        Debug.Log("Updated exactly the two existing Free Exp 9 Off-Grid wire parents. No transforms, splines, children, or new objects were changed/created.", lab);
    }

    private static void Redirect(FreeLabManager lab, FreeLabEndpoint oldA, FreeLabEndpoint oldB,
        FreeLabEndpoint nextA, FreeLabEndpoint nextB)
    {
        foreach (FreeLabExperimentEvaluator evaluator in lab.GetComponentsInChildren<FreeLabExperimentEvaluator>(true))
        {
            SerializedObject serialized = new SerializedObject(evaluator);
            SerializedProperty states = serialized.FindProperty("states");
            bool changed = false;
            for (int i = 0; i < states.arraySize; i++)
            {
                SerializedProperty requirements = states.GetArrayElementAtIndex(i).FindPropertyRelative("requiredConnections");
                for (int j = 0; j < requirements.arraySize; j++)
                {
                    SerializedProperty pair = requirements.GetArrayElementAtIndex(j);
                    SerializedProperty a = pair.FindPropertyRelative("endpointA");
                    SerializedProperty b = pair.FindPropertyRelative("endpointB");
                    FreeLabEndpoint first = a.objectReferenceValue as FreeLabEndpoint;
                    FreeLabEndpoint second = b.objectReferenceValue as FreeLabEndpoint;
                    if (!((first == oldA && second == oldB) || (first == oldB && second == oldA))) continue;
                    a.objectReferenceValue = nextA;
                    b.objectReferenceValue = nextB;
                    changed = true;
                }
            }
            if (changed) { serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(evaluator); }
        }
    }
}
