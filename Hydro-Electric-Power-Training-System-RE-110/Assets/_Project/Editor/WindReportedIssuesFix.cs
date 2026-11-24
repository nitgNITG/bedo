using System;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public static class WindReportedIssuesFix
{
    static StoryboardSteps Owner(string id) => Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(s => s.StoryboardID == id);
    static Transform Steps(StoryboardSteps o) => o.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Steps");
    static void Dirty(Object o) { EditorUtility.SetDirty(o); PrefabUtility.RecordPrefabInstancePropertyModifications(o); }
    static void Active(UnityEvent ev, GameObject target, bool active)
    {
        for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
            if (ev.GetPersistentTarget(i) == target && ev.GetPersistentMethodName(i) == "SetActive") UnityEventTools.RemovePersistentListener(ev, i);
        UnityEventTools.AddBoolPersistentListener(ev, target.SetActive, active);
    }
    [MenuItem("PV System/Fix Reported Wind Experiment Issues")]
    public static void Fix()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/_Project/Scenes/Hydro Lab.unity") throw new Exception("Stop play mode and open Hydro Lab.");
        if (scene.isDirty) EditorSceneManager.SaveScene(scene);
        var exp2 = Owner(WindExp2MenuSetup.Id);
        var exp3 = Owner("Regulating DC Output from Wind Turbine using MPPT Charger");
        foreach (var owner in new[] { exp2, exp3 })
        {
            var steps = Steps(owner); var binder = steps.GetComponentsInChildren<PVGuidedDataBinder>(true).Single(b => b.UsesWindModel);
            var power = steps.GetComponentsInChildren<SingleClickInteraction>(true).Single(s => s.name == "Power_Switch wind");
            var terminal = steps.Find("Step 6").GetComponentsInChildren<WireInteraction>(true).First(w => w.name.Contains("Halogen"));
            var point = terminal.pointA.name.Contains("Halogen") ? terminal.pointA : terminal.pointB;
            var lamps = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == "DC Halogen Lamp 2" && !t.GetComponentInParent<StoryboardSteps>()).ToArray();
            var lamp = lamps.OrderBy(t => t.GetComponentsInChildren<Renderer>(true).Min(r => Vector3.Distance(r.bounds.center, point.transform.position))).First();
            var light = lamp.Cast<Transform>().Single(t => t.name.Contains(" on")).gameObject;
            var so = new SerializedObject(binder); var old = so.FindProperty("connectedLampLights");
            foreach (var ev in new[] { power.onCompleted, power.onUndone })
                for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
                    if (Enumerable.Range(0, old.arraySize).Any(n => old.GetArrayElementAtIndex(n).objectReferenceValue == ev.GetPersistentTarget(i)) && ev.GetPersistentMethodName(i) == "SetActive") UnityEventTools.RemovePersistentListener(ev, i);
            old.arraySize = 1; old.GetArrayElementAtIndex(0).objectReferenceValue = light; so.ApplyModifiedPropertiesWithoutUndo();
            Active(power.onCompleted, light, true); Active(power.onUndone, light, false); light.SetActive(false);
            Dirty(light); Dirty(power); Dirty(binder);
            Debug.Log("LAMP FIX " + owner.name + ": DC Halogen Lamp 2/" + light.name);
        }
        var camera = Object.FindObjectsByType<DelayedEventTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(t => t.name == "Move camera to pos 11" && t.transform.parent.name == "Camera move");
        var row = exp2.steps[29];
        if (!row.interaction || row.interaction.name != "Speed control wind") throw new Exception("EXP2 element29 changed unexpectedly.");
        for (int i = row.onStepStart.GetPersistentEventCount() - 1; i >= 0; i--)
            if (row.onStepStart.GetPersistentTarget(i) is DelayedEventTrigger || row.onStepStart.GetPersistentTarget(i) is SmoothCameraMovement) UnityEventTools.RemovePersistentListener(row.onStepStart, i);
        UnityEventTools.AddVoidPersistentListener(row.onStepStart, camera.TriggerDelay); Dirty(exp2);
        var wires = Steps(exp3).Find("Step 5").GetComponentsInChildren<WireInteraction>(true);
        var negative = wires.Single(w => w.pointA.name == "MPPT Solar Charge Controller Battery -ve");
        var positive = wires.Single(w => w.pointB.name == "MPPT Solar Charge Controller Battery +ve");
        var pointers = Steps(exp3).Find("Pointers");
        var minus = pointers.Find("MPPT Solar Charge Controller Battery -ve (1)").gameObject;
        var plus = pointers.Find("MPPT Solar Charge Controller Battery +ve (1)").gameObject;
        var positions = new[] { minus.transform.position, plus.transform.position };
        // Preserve the authored arrow clearance, and assign each named pointer
        // to the matching terminal. Choose the existing position nearest its plug.
        var minusPosition = negative.plugA.transform.position + Vector3.up * 1.5f;
        var plusPosition = positive.plugB.transform.position + Vector3.up * 1.5f;
        float front = positions.Min(p => p.z);
        minusPosition.z = Mathf.Min(front, minusPosition.z - .15f);
        plusPosition.z = Mathf.Min(front, plusPosition.z - .15f);
        minus.transform.position = minusPosition; plus.transform.position = plusPosition;
        negative.markerA = minus; positive.markerB = plus;
        Dirty(minus.transform); Dirty(plus.transform); Dirty(negative); Dirty(positive);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("REPORTED WIND ISSUES SAVED: EXP2/3 bottom halogen; EXP2 element29 camera11; EXP3 matching MPPT battery pointers. EXP4 lamp selection preserved.");
    }

    [MenuItem("PV System/Test EXP4 Guided Step Transitions")]
    public static void TestGuided()
    {
        if (!Application.isPlaying || !InteractionManager.Instance.Guided) throw new Exception("Run in Guided Play Mode first.");
        var owner = Owner("OFF Grid System Using Wind Turbine");
        foreach (var other in Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(o => o != owner)) other.gameObject.SetActive(false);
        for (var t = Steps(owner); t; t = t.parent) t.gameObject.SetActive(true);
        double after = EditorApplication.timeSinceStartup + 1;
        EditorApplication.CallbackFunction run = null;
        run = () =>
        {
            if (EditorApplication.timeSinceStartup < after) return;
            EditorApplication.update -= run;
            try
            {
                owner.StopAllCoroutines();
                var settings = new SerializedObject(owner); settings.FindProperty("delayBetweenSteps").floatValue = 0; settings.ApplyModifiedPropertiesWithoutUndo();
                var binder = Steps(owner).GetComponentsInChildren<PVGuidedDataBinder>(true).Single(b => b.UsesWindOffGrid);
                binder.TurnOffGridSystemOff();
                owner.GoToStep(0);
                for (int i = 0; i <= 30; i++)
                {
                    if (owner.CurrentStep != i) throw new Exception("Unexpected guided row " + owner.CurrentStep + ", expected " + i);
                    var interaction = owner.steps[i].interaction;
                    if (interaction is WireInteraction wire)
                    {
                        wire.PointClicked(wire.pointA); wire.PointClicked(wire.pointB);
                        if (wire.IsCompleted || !wire.wireObject.activeSelf) throw new Exception("Expected completed flag reset and cable retained at " + i);
                    }
                    else if (interaction is SingleClickInteraction single) single.Execute();
                    else throw new Exception("Unexpected non-click row " + i);
                    binder.RefreshDisplay();
                    if (i == 18 && Mathf.Abs(binder.OffGridReading.batteryPower + 4.8f) > .001f) throw new Exception("Battery discharge lost across guided steps.");
                    if (i == 22 && Mathf.Abs(binder.OffGridReading.batteryPower + 100.8f) > .001f) throw new Exception("Lamp load lost across guided steps.");
                }
                var speed = Steps(owner).GetComponentsInChildren<WindTurbineSpeedController>(true).Single(); speed.SetSpeed(800); binder.RefreshDisplay();
                if (binder.OffGridReading.batteryCurrent <= 0 || binder.OffGridReading.dcLoadPower == 0 || binder.OffGridReading.acLoadPower == 0) throw new Exception("Meters stopped after wind step.");
                // Cross the authored monitor/quiz rows without answering the quiz.
                owner.StopAllCoroutines(); owner.GoToStep(34);
                for (int i = 34; i <= 35; i++) { var w = (WireInteraction)owner.steps[i].interaction; w.PointClicked(w.pointA); w.PointClicked(w.pointB); }
                binder.RefreshDisplay();
                if (binder.OffGridReading.batteryCurrent != 9 || binder.OffGridReading.gridCurrent <= 0) throw new Exception("Guided grid charging failed.");
                Debug.Log("EXP4 GUIDED TRANSITIONS PASSED: actual two-terminal clicks and automatic row advances; retained cables after IsCompleted resets; battery/load/wind/grid readings remain live.");
                owner.StopAllCoroutines(); binder.TurnOffGridSystemOff();
            }
            catch (Exception e) { Debug.LogException(e); }
        };
        EditorApplication.update += run;
    }
}
