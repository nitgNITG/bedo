using System;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WindExp2MathVerification
{
    [MenuItem("PV System/Test Wind EXP3 MPPT Math In Play Mode")]
    public static void TestMPPT()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var owners = Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var owner = owners.Single(s => s.StoryboardID == "Regulating DC Output from Wind Turbine using MPPT Charger");
        foreach (var other in owners.Where(s => s != owner && (s.StoryboardID == "Speed Curve Characteristics" || s.StoryboardID == WindExp2MenuSetup.Id))) other.gameObject.SetActive(false);
        owner.gameObject.SetActive(true);
        var binder = owner.GetComponentsInChildren<PVGuidedDataBinder>(true).Single(s => s.UsesWindPWM);
        for (var t = binder.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
        var settings = new SerializedObject(binder);
        var speed = settings.FindProperty("windSpeedController").objectReferenceValue as WindTurbineSpeedController;
        var screen = settings.FindProperty("pwmBatteryScreen").objectReferenceValue as GameObject;
        var screenText = settings.FindProperty("pwmBatteryScreenText").objectReferenceValue as TMP_Text;
        var batteryPower = settings.FindProperty("pwmBatteryTexts").GetArrayElementAtIndex(2).objectReferenceValue as TMP_Text;
        double after = EditorApplication.timeSinceStartup + 1;
        EditorApplication.CallbackFunction check = null;
        check = () =>
        {
            if (EditorApplication.timeSinceStartup < after) return;
            EditorApplication.update -= check;
            try
            {
                owner.StopAllCoroutines(); binder.TurnPWMSystemOff();
                if (screen.name != "MPPT Solar Charge Controller Screen" || screen.activeSelf || batteryPower.text != "0.00") throw new Exception("MPPT startup/screen mapping mismatch.");
                ((SingleClickInteraction)owner.steps[20].interaction).onCompleted.Invoke();
                ((SingleClickInteraction)owner.steps[21].interaction).onCompleted.Invoke();
                if (!screen.activeSelf || batteryPower.text != "-48.00") throw new Exception("MPPT load switch must show battery discharge and enable its screen.");
                ((SingleClickInteraction)owner.steps[24].interaction).onCompleted.Invoke();
                ((SingleClickInteraction)owner.steps[25].interaction).onCompleted.Invoke();
                float[] expected = { -2, .29166667f, 2.58333333f, 4.875f, 7.16666667f, 9 };
                for (int i = 0; i < expected.Length; i++)
                {
                    speed.SetSpeed(i * 200); binder.RefreshDisplay();
                    if (Mathf.Abs(binder.PWMBatteryCurrent - expected[i]) > .0001f) throw new Exception("MPPT model mismatch at " + i * 200 + " RPM.");
                }
                if (Mathf.Abs(binder.PWMDumpCurrent - .45833333f) > .0001f) throw new Exception("MPPT dump/cap mismatch.");
                speed.SetSpeed(600);
                var stored = typeof(PVGuidedDataBinder).GetField("pwmScreenCurrent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                stored.SetValue(binder, 4.875f); binder.RefreshDisplay();
                if (screenText.text != "54.17%" || batteryPower.text != "117.00") throw new Exception("MPPT screen percentage/power mismatch.");
                stored.SetValue(binder, 0f);
                double screenAfter = EditorApplication.timeSinceStartup + 1;
                EditorApplication.CallbackFunction finish = null;
                finish = () =>
                {
                    if (EditorApplication.timeSinceStartup < screenAfter) return;
                    EditorApplication.update -= finish;
                    float displayed = (float)stored.GetValue(binder);
                    if (displayed <= 0 || displayed >= 4.875f) Debug.LogError("MPPT percentage must change gradually.");
                    else Debug.Log("WIND EXP3 MPPT MATH PASSED: switch callbacks, CSV battery values, signed power, 9 A cap/dump load, correct MPPT screen and gradual percentage.");
                    binder.TurnPWMSystemOff();
                };
                EditorApplication.update += finish;
            }
            catch (Exception e) { Debug.LogException(e); }
        };
        EditorApplication.update += check;
    }

    [MenuItem("PV System/Reload Saved Wind Scene")]
    public static void Reload()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save your scene edits before reloading.");
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/PV Lab.unity");
    }
    private static T Find<T>(ulong id) where T : Object
    {
        return Resources.FindObjectsOfTypeAll<T>().Single(x =>
            GlobalObjectId.GetGlobalObjectIdSlow(x).targetObjectId == id);
    }

    [MenuItem("PV System/Test Wind EXP2 Math In Play Mode")]
    public static void Test()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var owner = Find<StoryboardSteps>(1816162570);
        var binder = Find<PVGuidedDataBinder>(739654807);
        var speed = Find<WindTurbineSpeedController>(732324371);
        Find<StoryboardSteps>(2010696064).gameObject.SetActive(false);
        owner.gameObject.SetActive(true);
        for (Transform t = binder.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
        double after = EditorApplication.timeSinceStartup + 1;
        EditorApplication.CallbackFunction check = null;
        check = () =>
        {
            if (EditorApplication.timeSinceStartup < after) return;
            EditorApplication.update -= check;
            try
            {
                owner.StopAllCoroutines();
                binder.TurnPWMSystemOff();
                if (binder.PWMBatteryCurrent != 0 || speed.IsTurbineRunning) throw new Exception("Startup must be off.");
                Find<SingleClickInteraction>(1536426827).onCompleted.Invoke();
                Find<SingleClickInteraction>(2138150982).onCompleted.Invoke();
                if (binder.PWMBatteryCurrent != -2 || Find<TMP_Text>(1183660659).text != "-48.00") throw new Exception("Battery discharge/load switch mismatch: PWM=" + binder.UsesWindPWM + ", I=" + binder.PWMBatteryCurrent + ", P=" + Find<TMP_Text>(1183660659).text + ", calls=" + Find<SingleClickInteraction>(1536426827).onCompleted.GetPersistentEventCount());
                if (!Find<GameObject>(23514451).activeSelf) throw new Exception("PWM screen did not turn on.");
                Find<SingleClickInteraction>(1855147412).onCompleted.Invoke();
                Find<SingleClickInteraction>(376975195).onCompleted.Invoke();
                float[] battery = { -2, .291666667f, 2.58333333f, 4.875f, 7.16666667f, 9 };
                for (int i = 0; i < battery.Length; i++)
                {
                    speed.SetSpeed(i * 200); binder.RefreshDisplay();
                    if (Mathf.Abs(binder.PWMBatteryCurrent - battery[i]) > .0001f) throw new Exception("CSV battery mismatch at " + i * 200);
                    speed.TryGetReading(out var reading);
                    if (Mathf.Abs(reading.dcVoltage - .055f * i * 200 / 1.2f) > .0001f) throw new Exception("DC voltage mismatch.");
                    if (Find<TMP_Text>(1897760993).text != "24.00" || Find<TMP_Text>(1824274500).text != "2.00" || Find<TMP_Text>(467342791).text != "48.00") throw new Exception("Halogen load mismatch.");
                }
                if (Mathf.Abs(binder.PWMDumpCurrent - .45833333f) > .0001f) throw new Exception("Dump load/cap mismatch.");
                speed.SetSpeed(350); binder.RefreshDisplay();
                if (Mathf.Abs(binder.PWMBatteryCurrent - 2.01041667f) > .0001f) throw new Exception("Intermediate speed mismatch.");
                Find<SingleClickInteraction>(376975195).onUndone.Invoke();
                if (binder.PWMBatteryCurrent != -2) throw new Exception("Output disable must restore discharge.");
                binder.TurnPWMSystemOff();
                if (Find<TMP_Text>(621517827).text != "0.00" || Find<TMP_Text>(166819497).text != "0.00%") throw new Exception("Power-off reset mismatch.");
                if (Find<PVGuidedDataBinder>(1684839060).UsesWindPWM) throw new Exception("EXP1 must not use PWM calculations.");
                binder.TurnPWMSystemOn(); binder.TurnPWMLoadOn(); speed.TurnOn(); binder.EnablePWMOutput(); speed.SetSpeed(600);
                var stored = typeof(PVGuidedDataBinder).GetField("pwmScreenCurrent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                stored.SetValue(binder, 4.875f); binder.RefreshDisplay();
                if (Find<TMP_Text>(166819497).text != "54.17%") throw new Exception("PWM screen percentage mismatch.");
                stored.SetValue(binder, 0f);
                double screenAfter = EditorApplication.timeSinceStartup + 1;
                EditorApplication.CallbackFunction screenCheck = null;
                screenCheck = () =>
                {
                    if (EditorApplication.timeSinceStartup < screenAfter) return;
                    EditorApplication.update -= screenCheck;
                    float displayed = (float)stored.GetValue(binder);
                    if (displayed <= 0 || displayed >= 4.875f) Debug.LogError("PWM screen must change gradually while charging.");
                    else Debug.Log("WIND EXP2 MATH PASSED: switch events, six CSV speeds, intermediate RPM, signed battery power, 24 V/2 A halogen, 9 A cap/dump, PWM screen activation, gradual percentage and reset. EXP1 PWM branch remains disabled.");
                    binder.TurnPWMSystemOff();
                };
                EditorApplication.update += screenCheck;
            }
            catch (Exception e) { Debug.LogException(e); }
        };
        EditorApplication.update += check;
    }
}
