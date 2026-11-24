using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public static class WindExp4MathSetup
{
    static void ConnectForTest(WireInteraction wire)
    {
        wire.Begin(); wire.PointClicked(wire.pointA); wire.PointClicked(wire.pointB);
        wire.ResetInteraction(); // The guided storyboard clears this flag too.
        if (wire.IsCompleted || !wire.wireObject.activeSelf || !wire.plugA.activeSelf || !wire.plugB.activeSelf)
            throw new Exception("Connected cable did not survive guided reset.");
    }
    static void DisconnectForTest(WireInteraction wire)
    {
        wire.wireObject.SetActive(false); wire.plugA.SetActive(false); wire.plugB.SetActive(false); wire.ResetInteraction(); wire.onUndone.Invoke();
    }
    const string Id = "OFF Grid System Using Wind Turbine";
    static StoryboardSteps Owner(string id) => Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => x.StoryboardID == id);
    static Transform Steps(StoryboardSteps owner) => owner.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Steps");
    static PVGuidedDataBinder Binder(StoryboardSteps owner) => Steps(owner).GetComponentsInChildren<PVGuidedDataBinder>(true).Single(x => x.UsesWindModel);
    static void Dirty(Object obj) { EditorUtility.SetDirty(obj); PrefabUtility.RecordPrefabInstancePropertyModifications(obj); }
    static void Ref(SerializedObject so, string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
    static void Refs(SerializedObject so, string field, IEnumerable<Object> values)
    {
        var list = values.ToArray(); var prop = so.FindProperty(field); prop.arraySize = list.Length;
        for (int i = 0; i < list.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
    }
    static void Void(UnityEvent ev, UnityAction action)
    {
        for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
            if (ev.GetPersistentTarget(i) == action.Target as Object && ev.GetPersistentMethodName(i) == action.Method.Name) UnityEventTools.RemovePersistentListener(ev, i);
        UnityEventTools.AddVoidPersistentListener(ev, action);
    }
    static void Active(UnityEvent ev, GameObject go, bool value)
    {
        for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
            if (ev.GetPersistentTarget(i) == go && ev.GetPersistentMethodName(i) == "SetActive") UnityEventTools.RemovePersistentListener(ev, i);
        UnityEventTools.AddBoolPersistentListener(ev, go.SetActive, value);
    }
    static TMP_Text[] Triplet(Transform root, string name) => new[] { "V DC", "A DC", "P DC" }.Select(n => root.Find(name + "/" + n).GetComponent<TMP_Text>()).ToArray();
    static Transform CopyMeter(Transform root, string name, Transform source, Vector3 offset)
    {
        var t = root.Find(name);
        if (!t) { t = Object.Instantiate(source.gameObject, root, false).transform; t.name = name; t.localPosition += offset; }
        t.gameObject.SetActive(true); return t;
    }
    static GameObject Lamp(string type, int number, Vector3 terminal)
    {
        var candidates = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name == type + " Lamp " + number && t.GetComponentInParent<StoryboardSteps>() == null).ToArray();
        if (candidates.Length == 0) throw new Exception("Missing physical " + type + " lamp " + number);
        float Distance(Transform t) { var rs = t.GetComponentsInChildren<Renderer>(true); return rs.Length == 0 ? Vector3.Distance(t.position, terminal) : rs.Min(r => Vector3.Distance(r.bounds.center, terminal)); }
        var lamp = candidates.OrderBy(Distance).First();
        return lamp.Cast<Transform>().Single(t => t.name.IndexOf(" on", StringComparison.OrdinalIgnoreCase) >= 0).gameObject;
    }
    static GameObject[] Lamps(StoryboardSteps owner)
    {
        var wires = Steps(owner).GetComponentsInChildren<WireInteraction>(true).Where(w => owner.steps.Any(s => s.interaction == w)).ToArray();
        var result = new HashSet<GameObject>();
        foreach (var w in wires)
        {
            if (w.name.IndexOf("DC Halogen", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var p = w.pointA.name.IndexOf("Halogen", StringComparison.OrdinalIgnoreCase) >= 0 ? w.pointA : w.pointB;
                result.Add(Lamp("DC Halogen", 2, p.transform.position));
            }
            if (w.name.IndexOf("DC LED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var p = w.pointA.name.IndexOf("LED", StringComparison.OrdinalIgnoreCase) >= 0 ? w.pointA : w.pointB;
                result.Add(Lamp("DC LED", 1, p.transform.position));
            }
            if (w.transform.parent.name == "Step 9" && w.name.StartsWith("AC Halogen"))
            {
                int number = w.name.Contains(" 2 ") ? 2 : 1;
                var p = w.pointA.name.StartsWith("AC Halogen") ? w.pointA : w.pointB;
                result.Add(Lamp("AC Halogen", number, p.transform.position));
            }
        }
        return result.ToArray();
    }

    [MenuItem("PV System/Setup EXP4 Math And Wind Lamp Lights")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/_Project/Scenes/Wind Lab.unity") throw new Exception("Open Wind Lab and stop Play Mode first.");
        var owner = Owner(Id); var steps = Steps(owner); var binder = Binder(owner);
        var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Wind UI").Find("Diversion control/Exp 4 Numbers");
        if (!root) throw new Exception("Missing EXP4 number group.");
        AssetDatabase.ImportAsset("Assets/_Project/Data/Wind/WindExp4Data.bytes", ImportAssetOptions.ForceUpdate);
        var model = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Wind/WindExp4Data.bytes");
        if (!model) throw new Exception("Import WindExp4Data.csv first.");
        var so = new SerializedObject(binder);
        so.FindProperty("windPWM").boolValue = false; so.FindProperty("windOffGrid").boolValue = true;
        Ref(so, "offGridModel", model);
        foreach (string f in new[] { "pwmBatteryTexts", "pwmLoadTexts", "pwmDumpTexts", "pwmUnusedTexts" }) Refs(so, f, new Object[0]);
        foreach (string f in new[] { "pwmBatteryScreen", "pwmBatteryScreenText", "powerAnalyzerCanvas", "powerAnalyzerText" }) Ref(so, f, null);
        var ac = Triplet(root, "3 Phase Power Meter"); var dc = Triplet(root, "DC Multimeter Rectifier Module");
        Ref(so, "acVoltageText", ac[0]); Ref(so, "acCurrentText", ac[1]); Ref(so, "acPowerText", ac[2]);
        Ref(so, "voltageText", dc[0]); Ref(so, "currentText", dc[1]); Ref(so, "powerText", dc[2]);
        Ref(so, "speedText", root.Find("Intensity Txt").GetComponent<TMP_Text>());
        Refs(so, "offGridBatteryTexts", Triplet(root, "DC Multimeter Battery Bank"));
        Refs(so, "offGridDCLoadTexts", Triplet(root, "DC Multimeter 1"));
        Refs(so, "offGridDumpTexts", Triplet(root, "DC Multimeter dump load"));
        var template = root.Find("DC Multimeter 1");
        CopyMeter(root, "AC Multimeter Off-grid Output", template, new Vector3(0, 2, 0));
        CopyMeter(root, "DC Multimeter Inverter Input", template, new Vector3(0, 4, 0));
        CopyMeter(root, "AC Multimeter Grid Input", template, new Vector3(0, 6, 0));
        Refs(so, "offGridACLoadTexts", Triplet(root, "AC Multimeter Off-grid Output"));
        Refs(so, "offGridInverterTexts", Triplet(root, "DC Multimeter Inverter Input"));
        Refs(so, "offGridGridTexts", Triplet(root, "AC Multimeter Grid Input"));
        var charge = root.Find("Available Battery Charge Current (A)");
        if (!charge) { charge = Object.Instantiate(template.Find("A DC").gameObject, root, false).transform; charge.name = "Available Battery Charge Current (A)"; charge.localPosition += Vector3.up * 8; }
        Ref(so, "offGridChargeCurrentText", charge.GetComponent<TMP_Text>());
        var wires = owner.steps.Select(s => s.interaction).OfType<WireInteraction>().ToArray();
        WireInteraction[] At(int n) => wires.Where(w => w.transform.parent.name == "Step " + n).ToArray();
        if (At(3).Length != 2 || At(4).Length != 2 || At(9).Length != 4 || At(10).Length != 3 || At(11).Length != 2 || At(16).Length != 2) throw new Exception("Unexpected EXP4 wiring. No guessing connection groups.");
        Refs(so, "offGridBatteryWires", At(3)); Refs(so, "offGridDCLoadWires", At(4));
        Refs(so, "offGridWindWires", At(10).Concat(At(11))); Refs(so, "offGridGridWires", At(16));
        Refs(so, "offGridACLamp1Wires", At(9).Where(w => w.name.Contains(" 1 ")));
        Refs(so, "offGridACLamp2Wires", At(9).Where(w => w.name.Contains(" 2 ")));
        so.ApplyModifiedPropertiesWithoutUndo(); binder.name = "Wind EXP4 Meter Data"; Dirty(binder);
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) { text.text = "0.00"; Dirty(text); }
        var speed = steps.GetComponentsInChildren<WindTurbineSpeedController>(true).Single(); var speedSO = new SerializedObject(speed);
        Ref(speedSO, "windCalibration", model); speedSO.FindProperty("defaultResistance").floatValue = 4;
        speedSO.FindProperty("initialSpeed").floatValue = 200; speedSO.ApplyModifiedPropertiesWithoutUndo(); Dirty(speed);
        // Remove inherited PWM callbacks only on EXP4. Preserve animations,
        // camera moves, colliders, and the authored storyboard order/UI.
        foreach (var interaction in steps.GetComponentsInChildren<BaseInteraction>(true))
        {
            foreach (var ev in new[] { interaction.onCompleted, interaction.onUndone })
                for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
                    if (ev.GetPersistentTarget(i) is PVGuidedDataBinder) UnityEventTools.RemovePersistentListener(ev, i);
            Dirty(interaction);
        }
        var main = steps.Find("Step 7").GetComponentsInChildren<SingleClickInteraction>(true).Single();
        Void(main.onCompleted, binder.TurnOffGridSystemOn); Void(main.onUndone, binder.TurnOffGridSystemOff); Dirty(main);
        var inverter = steps.Find("Step 8").GetComponentsInChildren<SingleClickInteraction>(true).Single();
        Void(inverter.onCompleted, binder.TurnOffGridInverterOn); Void(inverter.onUndone, binder.TurnOffGridInverterOff); Dirty(inverter);
        foreach (var w in wires) { Void(w.onCompleted, binder.RefreshDisplay); Void(w.onUndone, binder.RefreshDisplay); Dirty(w); }
        foreach (string id in new[] { WindExp2MenuSetup.Id, "Regulating DC Output from Wind Turbine using MPPT Charger", Id })
        {
            var o = Owner(id); var b = Binder(o); var lamps = Lamps(o);
            if (lamps.Length == 0) throw new Exception("No lamp mapping for " + id);
            var bs = new SerializedObject(b); Refs(bs, "connectedLampLights", lamps); bs.ApplyModifiedPropertiesWithoutUndo(); Dirty(b);
            var power = Steps(o).GetComponentsInChildren<SingleClickInteraction>(true).Single(i => i.name == "Power_Switch wind");
            foreach (var lamp in lamps) { Active(power.onCompleted, lamp, true); Active(power.onUndone, lamp, false); lamp.SetActive(false); Dirty(lamp); }
            Void(power.onUndone, b.ResetConnectedLampLights); Dirty(power);
            // Reset shared lamp visuals when the experiment begins/restarts.
            Void(o.steps[0].onStepStart, b.ResetConnectedLampLights); Dirty(o);
            Debug.Log("WIND LAMPS: " + o.name + " => " + string.Join(", ", lamps.Select(l => l.transform.parent.name + "/" + l.name)));
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("EXP4 MATH SETUP SAVED: isolated EXP4 meters, source CSV, battery/inverter/grid/load states, and EXP2/3/4 power-switch lamp callbacks.");
    }

    [MenuItem("PV System/Verify EXP4 Formula Reference Rows")]
    public static void VerifyModel()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Wind/WindExp4Data.bytes");
        var rows = asset.text.Split('\n').Skip(1).Where(r => !string.IsNullOrWhiteSpace(r)).ToArray();
        foreach (string row in rows)
        {
            float[] c = row.Trim().Split(',').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            if (!PVCSVReader.TryCalculateWind(asset, c[4], c[0], out var source)) throw new Exception("Wind model failed.");
            float[] actual = { source.acVoltage, source.acCurrent, source.acPower, source.dcVoltage, source.dcCurrent, source.dcPower };
            for (int i = 0; i < 6; i++) if (Mathf.Abs(actual[i] - c[i + 5]) > .001f) throw new Exception("Source reference mismatch at " + c[4]);
            PVCSVReader.TryCalculateOffGrid(asset, source.dcCurrent, 1, 1, true, out var grid);
            if (Mathf.Abs(grid.gridCurrent - c[23]) > .0001f || grid.batteryCurrent != c[21]) throw new Exception("Grid reference mismatch.");
            PVCSVReader.TryCalculateOffGrid(asset, source.dcCurrent, 1, 1, false, out var wind);
            // Last workbook row sets S to 9 manually. Equation rows use the
            // documented zero floor/9 A ceiling, rather than that override.
            if (c[19] != 9 && Mathf.Abs(wind.availableChargeCurrent - Mathf.Clamp(c[18], 0, 9)) > .0001f) throw new Exception("Wind charging mismatch.");
        }
        Debug.Log("EXP4 MODEL PASSED: seven workbook source rows, W/X grid flow, V 9 A charging, S charging limits, separate AC display/DC-equivalent load currents.");
    }

    [MenuItem("PV System/Test EXP4 Live Math And Lamp Switches")]
    public static void TestLive()
    {
        if (!Application.isPlaying) throw new Exception("Enter Play Mode first.");
        var owner = Owner(Id);
        foreach (var other in Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(o => o != owner)) other.gameObject.SetActive(false);
        owner.gameObject.SetActive(true);
        var steps = Steps(owner); for (var t = steps; t; t = t.parent) t.gameObject.SetActive(true);
        double after = EditorApplication.timeSinceStartup + 1;
        EditorApplication.CallbackFunction check = null;
        check = () =>
        {
            if (EditorApplication.timeSinceStartup < after) return;
            EditorApplication.update -= check;
            try
            {
                owner.StopAllCoroutines();
                var binder = Binder(owner); var speed = steps.GetComponentsInChildren<WindTurbineSpeedController>(true).Single();
                var wires = owner.steps.Select(s => s.interaction).OfType<WireInteraction>().ToArray();
                foreach (var w in wires) DisconnectForTest(w);
                binder.TurnOffGridSystemOff();
                if (binder.OffGridReading.batteryPower != 0 || speed.IsTurbineRunning || Lamps(owner).Any(l => l.activeSelf)) throw new Exception("EXP4 startup not zero/off.");
                foreach (var w in wires.Where(w => w.transform.parent.name == "Step 3" || w.transform.parent.name == "Step 4")) ConnectForTest(w);
                steps.Find("Step 7").GetComponentInChildren<SingleClickInteraction>(true).onCompleted.Invoke();
                steps.Find("Step 8").GetComponentInChildren<SingleClickInteraction>(true).onCompleted.Invoke();
                if (Mathf.Abs(binder.OffGridReading.batteryPower + 4.8f) > .001f) throw new Exception("DC LED battery discharge mismatch.");
                foreach (var w in wires.Where(w => w.transform.parent.name == "Step 9")) ConnectForTest(w);
                if (Mathf.Abs(binder.OffGridReading.batteryPower + 100.8f) > .001f || Mathf.Abs(binder.OffGridReading.acLoadCurrent - .4f) > .0001f) throw new Exception("Two AC lamp load mismatch.");
                foreach (var w in wires.Where(w => w.transform.parent.name == "Step 10" || w.transform.parent.name == "Step 11")) ConnectForTest(w);
                var power = steps.Find("Step 14").GetComponentsInChildren<SingleClickInteraction>(true).Single(i => i.name == "Power_Switch wind");
                power.onCompleted.Invoke();
                if (!speed.IsTurbineRunning || Lamps(owner).Any(l => !l.activeSelf)) throw new Exception("EXP4 wind switch/lamp callbacks failed.");
                foreach (float rpm in new[] { 200f, 350f, 600f, 800f, 999f })
                {
                    speed.SetSpeed(rpm); binder.RefreshDisplay();
                    float expected = .055f * rpm / 1.2f / 4 - 4.2f;
                    if (Mathf.Abs(binder.OffGridReading.batteryCurrent - expected) > .0001f) throw new Exception("Live wind battery balance mismatch at " + rpm);
                }
                // Grid must wait for BOTH L and N.
                var grid = wires.Where(w => w.transform.parent.name == "Step 16").ToArray();
                ConnectForTest(grid[0]);
                if (binder.OffGridReading.gridCurrent != 0) throw new Exception("Incomplete grid incorrectly enabled.");
                ConnectForTest(grid[1]);
                if (binder.OffGridReading.batteryCurrent != 9 || binder.OffGridReading.gridCurrent <= 0) throw new Exception("Grid charging failed.");
                DisconnectForTest(grid[0]);
                if (binder.OffGridReading.gridCurrent != 0 || binder.OffGridReading.batteryCurrent == 9) throw new Exception("Grid disconnect failed.");
                var disconnected = wires.First(w => w.transform.parent.name == "Step 9"); DisconnectForTest(disconnected);
                if (Mathf.Abs(binder.OffGridReading.acLoadCurrent - .2f) > .0001f) throw new Exception("AC lamp disconnect failed.");
                power.onUndone.Invoke(); binder.RefreshDisplay();
                if (Lamps(owner).Any(l => l.activeSelf) || Mathf.Abs(binder.OffGridReading.batteryCurrent + 2.2f) > .0001f) throw new Exception("Power-off lamp reset/battery supply failed.");
                foreach (string id in new[] { WindExp2MenuSetup.Id, "Regulating DC Output from Wind Turbine using MPPT Charger" })
                {
                    var o = Owner(id); var p = Steps(o).GetComponentsInChildren<SingleClickInteraction>(true).Single(i => i.name == "Power_Switch wind");
                    p.onCompleted.Invoke(); if (Lamps(o).Any(l => !l.activeSelf)) throw new Exception(o.name + " lamp switch failed.");
                    p.onUndone.Invoke(); if (Lamps(o).Any(l => l.activeSelf)) throw new Exception(o.name + " lamp reset failed.");
                }
                Debug.Log("EXP4 LIVE PASSED: startup zero; DC LED -4.8W; LED plus two AC lamps -100.8W; variable wind sharing; grid both-pin gating/9A charge/disconnect; individual lamp-load disconnect; EXP2/3/4 lamp callbacks and resets.");
                binder.TurnOffGridSystemOff();
            }
            catch (Exception e) { Debug.LogException(e); }
        };
        EditorApplication.update += check;
    }
}
