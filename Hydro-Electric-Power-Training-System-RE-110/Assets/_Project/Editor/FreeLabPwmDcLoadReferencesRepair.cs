using System;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Free Mode only. Corrects existing PWM DC Load to DC LED endpoint and plug references.</summary>
public static class FreeLabPwmDcLoadReferencesRepair
{
    [MenuItem("Free Lab/Set Existing PWM DC Load References Only")]
    private static void Apply()
    {
        FreeLabManager lab = UnityEngine.Object.FindFirstObjectByType<FreeLabManager>();
        if (lab == null) throw new InvalidOperationException("FreeLabManager is missing.");
        FreeLabEndpoint[] endpoints = lab.GetComponentsInChildren<FreeLabEndpoint>(true);
        FreeLabEndpoint Endpoint(string id) => endpoints.FirstOrDefault(e => e != null && e.EndpointId == id);
        FreeLabEndpoint loadPlus = Endpoint("PWM DC Load +ve [Free Exp 7]");
        FreeLabEndpoint loadMinus = Endpoint("PWM DC Load -ve [Free Exp 7]");
        FreeLabEndpoint ledPlus = Endpoint("DC LED +ve [Free Exp 7]");
        FreeLabEndpoint ledMinus = Endpoint("DC LED -ve [Free Exp 7]");
        if (new[] { loadPlus, loadMinus, ledPlus, ledMinus }.Any(e => e == null))
            throw new InvalidOperationException("A required PWM DC Load/DC LED Free Mode endpoint is missing.");

        FreeLabWireDefinition[] definitions = lab.GetComponentsInChildren<FreeLabWireDefinition>(true);
        FreeLabWireDefinition positive = definitions.FirstOrDefault(w => w != null &&
            w.gameObject.name.Equals("Free Exp 7 - PMW DC Load +ve to DC LED -ve", StringComparison.OrdinalIgnoreCase));
        FreeLabWireDefinition negative = definitions.FirstOrDefault(w => w != null &&
            w.gameObject.name.Equals("Free Exp 7 - PMW DC Load -ve to DC LED +ve", StringComparison.OrdinalIgnoreCase));
        if (positive == null || negative == null)
            throw new InvalidOperationException("The two existing Free Exp 7 PMW DC Load wire parents are missing.");

        Undo.SetCurrentGroupName("Set existing PWM DC Load wire references");
        int group = Undo.GetCurrentGroup();
        Undo.RecordObjects(new UnityEngine.Object[] { positive, negative }, "Set PWM DC Load endpoints and plugs");

        positive.gameObject.name = "Free Exp 7 - PMW DC Load +ve to DC LED +ve";
        positive.endpointA = loadPlus;
        positive.endpointB = ledPlus;
        positive.plugA = loadPlus.SelectionPlug;
        positive.plugB = ledPlus.SelectionPlug;
        positive.gameObject.SetActive(true);

        negative.gameObject.name = "Free Exp 7 - PMW DC Load -ve to DC LED -ve";
        negative.endpointA = loadMinus;
        negative.endpointB = ledMinus;
        negative.plugA = loadMinus.SelectionPlug;
        negative.plugB = ledMinus.SelectionPlug;
        negative.gameObject.SetActive(true);

        Redirect(lab, loadPlus, ledMinus, loadPlus, ledPlus);
        Redirect(lab, loadMinus, ledPlus, loadMinus, ledMinus);

        EditorUtility.SetDirty(positive);
        EditorUtility.SetDirty(negative);
        EditorUtility.SetDirty(lab);
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        Undo.CollapseUndoOperations(group);
        Debug.Log("Corrected the two existing PWM DC Load/DC LED Free Mode references and plugs only.", lab);
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
