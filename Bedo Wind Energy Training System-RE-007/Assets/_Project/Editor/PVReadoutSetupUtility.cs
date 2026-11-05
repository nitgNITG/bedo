using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
using TutorialFramework;
using Template;
using XCharts.Runtime;

/// <summary>
/// Configures only the existing PV Characteristic DC multimeter readouts.
/// Guided experiments receive PVGuidedDataBinder instances; Free Lab keeps its
/// own PVIntensityDataBinder. No Battery Charging or Off Grid UI is touched.
/// </summary>
public static class PVReadoutSetupUtility
{
    public static void VerifyWindExp1Assets()
    {
        VerifyWindModel(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Wind/WindExp1Data.bytes"));
        VerifyWindGraph(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Wind/WindExp1Graph.bytes"));
    }

    [MenuItem("PV System/Configure Wind EXP1 Only")]
    private static void ConfigureWindExp1()
    {
        if (Application.isPlaying) { Debug.LogError("Exit Play Mode before configuring EXP1."); return; }
        WindTurbineSpeedController authoredSpeed = FindAll<WindTurbineSpeedController>().Single(s => HasAncestor(s.transform, "Exp 1"));
        StoryboardSteps owner = authoredSpeed.GetComponentInParent<StoryboardSteps>(true);
        WindTurbineSpeedController speed = owner.GetComponentInChildren<WindTurbineSpeedController>(true);
        PVGraphController graph = owner.GetComponentInChildren<PVGraphController>(true);
        PVGuidedDataBinder binder = owner.GetComponentInChildren<PVGuidedDataBinder>(true);
        RheostatController load = owner.GetComponentInChildren<RheostatController>(true);
        Transform stepsRoot = speed.transform.parent.parent;
        Undo.SetTransformParent(binder.transform, stepsRoot, "Move EXP1 meter data into Steps");
        Undo.SetTransformParent(graph.transform, stepsRoot, "Keep EXP1 graph active across steps");
        WindSet(speed.GetComponent<SingleClickInteraction>(), "interaction", speed.GetComponent<Interaction>());
        Transform windUI = FindAll<Transform>().Single(t => SameName(t, "Wind UI"));
        Transform system = windUI.Find("Speed curve");
        Transform graphScreen = windUI.Find("Wind graph");
        var graphTab = owner.GetComponentsInChildren<SingleClickInteraction>(true).Single(t => t.name == "Button (2)" && t.transform.parent.name == "Step 14");
        UnityEventTools.AddBoolPersistentListener(graphTab.onCompleted, windUI.Find("Black Screen").gameObject.SetActive, false);
        Transform screenMesh = graphScreen.Find("Wind graph");
        // Unity does not import .csv as TextAsset in this project. Embed an
        // identical .bytes copy so the CSV also works in packaged/VR builds.
        System.IO.File.Copy("Assets/StreamingAssets/WindExp1Data.csv", "Assets/_Project/Data/Wind/WindExp1Data.bytes", true);
        AssetDatabase.ImportAsset("Assets/_Project/Data/Wind/WindExp1Data.bytes", ImportAssetOptions.ForceSynchronousImport);
        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Wind/WindExp1Data.bytes");
        if (speed == null || graph == null || binder == null || load == null || screenMesh == null || csv == null)
            throw new InvalidOperationException($"EXP1 wind setup missing: speed={speed}, graph={graph}, binder={binder}, load={load}, screen={screenMesh}, csv={csv}; owner={owner.name}.");
        VerifyWindModel(csv);
        System.IO.File.Copy("Assets/StreamingAssets/WindExp1Graph.csv", "Assets/_Project/Data/Wind/WindExp1Graph.bytes", true);
        AssetDatabase.ImportAsset("Assets/_Project/Data/Wind/WindExp1Graph.bytes", ImportAssetOptions.ForceSynchronousImport);
        TextAsset graphCsv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Wind/WindExp1Graph.bytes");
        VerifyWindGraph(graphCsv);

        Transform existingUI = graphScreen.Find("EXP1 Wind Graph UI");
        Canvas canvas;
        if (existingUI != null) canvas = existingUI.GetComponent<Canvas>();
        else
        {
            // Reuse the old screen's orientation; fit the new mesh's actual
            // vertices in that plane rather than assuming a global axis.
            var oldGraph = new SerializedObject(graph).FindProperty("lineChartSpawnParent").objectReferenceValue as Transform;
            Canvas oldCanvas = oldGraph.GetComponentInParent<Canvas>(true);
            Quaternion rotation = oldCanvas.transform.rotation;
            Vector3 right = rotation * Vector3.right, up = rotation * Vector3.up;
            Vector3[] vertices = screenMesh.GetComponent<MeshFilter>().sharedMesh.vertices
                .Select(v => screenMesh.TransformPoint(v)).ToArray();
            float minX = vertices.Min(v => Vector3.Dot(v, right)), maxX = vertices.Max(v => Vector3.Dot(v, right));
            float minY = vertices.Min(v => Vector3.Dot(v, up)), maxY = vertices.Max(v => Vector3.Dot(v, up));
            Vector3 center = screenMesh.GetComponent<Renderer>().bounds.center;
            GameObject go = new GameObject("EXP1 Wind Graph UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Configure Wind EXP1");
            go.transform.SetParent(graphScreen, false);
            go.transform.rotation = rotation;
            go.transform.position = center - (rotation * Vector3.forward) * 0.03f;
            float scale = (maxX - minX) / 1000f;
            go.transform.localScale = Vector3.one * scale;
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            ((RectTransform)go.transform).sizeDelta = new Vector2(1000f, (maxY - minY) / scale);
            canvas.sortingOrder = 10;
        }
        RectTransform canvasRect = (RectTransform)canvas.transform;
        RectTransform chartParent = WindRect(canvasRect, "Speed Voltage Chart", new Vector2(.34f, .21f), new Vector2(.96f, .77f));
        TMP_Text xText = WindScroll(canvasRect, "RPM Readings", new Vector2(.04f, .20f), new Vector2(.14f, .74f));
        TMP_Text yText = WindScroll(canvasRect, "DC Voltage Readings", new Vector2(.14f, .20f), new Vector2(.24f, .74f));

        // Create a local variant of the established chart; no other experiment
        // or shared prefab is reconfigured.
        const string chartPath = "Assets/_Project/Prefabs/Graphs/Wind EXP1 Runtime.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(chartPath);
        if (prefab == null)
        {
            GameObject source = new SerializedObject(graph).FindProperty("lineChartPrefab").objectReferenceValue as GameObject;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            var chart = instance.GetComponent<LineChart>();
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            chart.RemoveAllSerie(); chart.ClearData();
            prefab = PrefabUtility.SaveAsPrefabAsset(instance, chartPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }
        PVExperimentExporter exporter = graphScreen.GetComponent<PVExperimentExporter>();
        if (exporter == null) exporter = Undo.AddComponent<PVExperimentExporter>(graphScreen.gameObject);
        WindSet(exporter, "windExport", true);
        OnMouseDownE clear = WindButton(graphScreen.Find("Clear"), graph.ClearRuntimeGraph);
        WindButton(graphScreen.Find("export data"), exporter.SaveDataCSV);
        WindButton(graphScreen.Find("save screen"), exporter.SaveGraphPNG);
        WindButton(system.Find("Up"), speed.IncreaseValue);
        WindButton(system.Find("Down"), speed.DecreaseValue);

        WindSet(speed, "windCalibration", csv);
        WindSet(speed, "loadRheostat", load);
        WindSet(speed, "dataBinder", binder);
        WindSet(speed, "graphController", graph);
        WindSet(speed, "initialSpeed", 696.5f);
        WindSet(speed, "maxSpeed", 990.2f);
        WindSet(load, "minResistance", 4f);
        WindSet(binder, "windSpeedController", speed);
        WindSet(binder, "intensitySlider", null);
        WindSet(binder, "voltageText", FindText(system.Find("Exp 1 Numbers/DC Multimeter"), "V DC"));
        WindSet(binder, "currentText", FindText(system.Find("Exp 1 Numbers/DC Multimeter"), "A DC"));
        WindSet(binder, "powerText", FindText(system.Find("Exp 1 Numbers/DC Multimeter"), "P DC"));
        WindSet(binder, "acVoltageText", FindText(system.Find("Exp 1 Numbers/3 Phase Power Meter"), "V DC"));
        WindSet(binder, "acCurrentText", FindText(system.Find("Exp 1 Numbers/3 Phase Power Meter"), "A DC"));
        WindSet(binder, "acPowerText", FindText(system.Find("Exp 1 Numbers/3 Phase Power Meter"), "P DC"));
        WindSet(binder, "speedText", FindText(system, "Intensity Txt"));
        WindSet(graph, "lineChartPrefab", prefab);
        WindSet(graph, "windGraphReference", graphCsv);
        WindSet(graph, "lineChartSpawnParent", chartParent);
        WindSet(graph, "xAxisLabel", "Speed (RPM)");
        WindSet(graph, "yAxisLabel", "DC voltage (V)");
        WindSet(graph, "exporter", exporter);
        WindSet(graph, "plottedXValuesText", xText);
        WindSet(graph, "plottedYValuesText", yText);
        var gs = new SerializedObject(graph);
        gs.FindProperty("clearButtons").arraySize = 1;
        gs.FindProperty("clearButtons").GetArrayElementAtIndex(0).objectReferenceValue = clear;
        gs.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Wind EXP1 configured: workbook model, AC/DC meters, local runtime chart, synchronized table and export. Other experiments and Free Mode unchanged.");
    }

    private static void WindSet(UnityEngine.Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (value is bool b) p.boolValue = b;
        else if (value is float f) p.floatValue = f;
        else if (value is string s) p.stringValue = s;
        else p.objectReferenceValue = value as UnityEngine.Object;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void VerifyWindModel(TextAsset csv)
    {
        int count = 0;
        foreach (string row in csv.text.Split('\n').Skip(1))
        {
            if (string.IsNullOrWhiteSpace(row)) continue;
            float[] v = row.Trim().Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            if (!PVCSVReader.TryCalculateWind(csv, v[4], v[0], out var p))
                throw new InvalidOperationException("Wind model could not evaluate workbook row.");
            float[] actual = { p.acVoltage, p.acCurrent, p.acPower, p.dcVoltage, p.dcCurrent, p.dcPower };
            for (int i = 0; i < actual.Length; i++)
                if (Mathf.Abs(actual[i] - v[i + 5]) > Mathf.Max(.0001f, Mathf.Abs(v[i + 5]) * .00001f))
                    throw new InvalidOperationException($"Wind workbook mismatch at row {count + 3}, column {i + 6}.");
            count++;
        }
        if (count != 36 || PVCSVReader.TryCalculateWind(csv, 100, 0, out _) ||
            PVCSVReader.TryCalculateWind(csv, float.NaN, 100, out _))
            throw new InvalidOperationException("Wind model boundary check failed.");
        if (!PVCSVReader.TryCalculateWind(csv, 350, 30, out var interpolated) ||
            Mathf.Abs(interpolated.dcVoltage - (0.055f * 350 / 1.2f * 1.45f)) > .0001f)
            throw new InvalidOperationException("Wind model interpolation check failed.");
        Debug.Log("Wind model verification passed: all 36 workbook rows (216 AC/DC outputs), invalid input and intermediate RPM/load checks.");
    }

    private static void VerifyWindGraph(TextAsset csv)
    {
        if (csv == null) throw new InvalidOperationException("Wind graph reference CSV missing.");
        int count = 0;
        foreach (string row in csv.text.Split('\n').Skip(1))
        {
            if (string.IsNullOrWhiteSpace(row)) continue;
            float[] p = row.Trim().Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            if (!PVCSVReader.TryCalculateWindGraph(csv, p[0], out float voltage) || Mathf.Abs(voltage - p[1]) > .0001f)
                throw new InvalidOperationException("Wind graph does not reproduce its reference table.");
            count++;
        }
        if (count != 14 || !PVCSVReader.TryCalculateWindGraph(csv, 713.95f, out float middle) ||
            Mathf.Abs(middle - 39.355f) > .0001f)
            throw new InvalidOperationException("Wind graph reference/interpolation check failed.");
        Debug.Log("Wind graph reference verified: all 14 RPM/voltage points and interpolation.");
    }

    [MenuItem("PV System/Test Wind EXP1 In Play Mode")]
    private static void TestWindExp1InPlayMode()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode to test EXP1.");
        WindTurbineSpeedController speed = FindAll<WindTurbineSpeedController>().Single(s => HasAncestor(s.transform, "Exp 1"));
        var owner = speed.GetComponentInParent<StoryboardSteps>(true);
        var manager = InteractionManager.Instance;
        manager.currentMode = ExperimentMode.Guided;
        manager.SetActiveStoryboard(owner);
        var graph = owner.GetComponentInChildren<PVGraphController>(true);
        var binder = owner.GetComponentInChildren<PVGuidedDataBinder>(true);
        var load = owner.GetComponentInChildren<RheostatController>(true);
        owner.GetComponentsInChildren<Transform>(true).Single(t => SameName(t, "Step Objects")).gameObject.SetActive(false);
        foreach (Transform leaf in new[] { speed.transform, graph.transform, load.transform })
            for (Transform t = leaf; t != null; t = t.parent) t.gameObject.SetActive(true);
        Transform windUI = FindAll<Transform>().Single(t => SameName(t, "Wind UI"));
        for (Transform t = windUI; t != null; t = t.parent) t.gameObject.SetActive(true);
        windUI.Find("Wind graph").gameObject.SetActive(true);
        windUI.Find("Speed curve").gameObject.SetActive(false);
        speed.gameObject.SetActive(false); // Power can turn on before the later speed step.
        // Let Start initialize the physical controls before exercising their
        // public methods. No scene changes made here are saved.
        Application.runInBackground = true;
        double after = EditorApplication.timeSinceStartup + 1;
        bool waitingForRotor = false;
        int rotorStartFrame = 0;
        speed.ResolveTurbineRotor();
        Quaternion rotorAtStartup = speed.TurbineRotor.localRotation;
        Quaternion rotorAtSwitchOn = Quaternion.identity;
        EditorApplication.CallbackFunction test = null;
        test = () =>
        {
            if (EditorApplication.timeSinceStartup < after || (waitingForRotor && Time.frameCount < rotorStartFrame + 5)) return;
            EditorApplication.update -= test;
            try
            {
                VerifyWindModel(new SerializedObject(speed).FindProperty("windCalibration").objectReferenceValue as TextAsset);
                if (!binder.isActiveAndEnabled || !graph.isActiveAndEnabled)
                    throw new InvalidOperationException("EXP1 still depends on disabled Step Objects.");
                PVCSVReader.WindReading offReading;
                if (!waitingForRotor)
                {
                var analyzer = new SerializedObject(binder).FindProperty("powerAnalyzerCanvas").objectReferenceValue as GameObject;
                if (analyzer == null || analyzer.activeSelf)
                    throw new InvalidOperationException("Power Analyzer must start inactive.");
                if (Quaternion.Angle(rotorAtStartup, speed.TurbineRotor.localRotation) > .001f)
                    throw new InvalidOperationException("Rotor moved before wind power was switched on.");
                if (speed.IsTurbineRunning || !speed.TryGetReading(out offReading) || offReading.acVoltage != 0 || offReading.dcVoltage != 0 || offReading.dcPower != 0)
                    throw new InvalidOperationException("Meters are not zero before turbine power on.");
                owner.GoToStep(12);
                var mainBoard = owner.GetComponentsInChildren<SingleClickInteraction>(true).Single(t => t.name == "on_button004 (1)" && t.transform.parent.name == "Step 8");
                mainBoard.interaction.Click();
                if (speed.IsTurbineRunning || !speed.TryGetReading(out offReading) || offReading.acVoltage != 0 || offReading.dcVoltage != 0)
                    throw new InvalidOperationException("Main board incorrectly started turbine generation.");
                owner.GoToStep(15);
                var powerOn = owner.GetComponentsInChildren<SingleClickInteraction>(true).Single(t => t.name == "Power_Switch wind");
                powerOn.interaction.Click();
                if (!speed.IsTurbineRunning || speed.TurbineRotor == null)
                    throw new InvalidOperationException("Turbine power event or axis_Circle_003 rotor is missing.");
                owner.GoToStep(16);
                var modeSelection = owner.GetComponentsInChildren<SingleClickInteraction>(true).Single(t => t.name == "Mode selection wind001");
                modeSelection.interaction.Click();
                var analyzerText = new SerializedObject(binder).FindProperty("powerAnalyzerText").objectReferenceValue as TMP_Text;
                speed.TryGetReading(out var analyzerReading);
                string expectedAnalyzer = $"Voltage: {analyzerReading.acVoltage:F2} V\nCurrent: {analyzerReading.acCurrent:F2} A\nPower: {analyzerReading.acPower:F2} W";
                if (!analyzer.activeInHierarchy || analyzerText == null || !analyzerText.gameObject.activeInHierarchy || analyzerText.text != expectedAnalyzer)
                    throw new InvalidOperationException("Mode selection did not reveal the CSV Power Analyzer readings.");
                speed.gameObject.SetActive(false); // Storyboard activation must not be required for rotor motion.
                rotorAtSwitchOn = speed.TurbineRotor.localRotation;
                waitingForRotor = true;
                rotorStartFrame = Time.frameCount;
                after = EditorApplication.timeSinceStartup + .3;
                EditorApplication.update += test;
                return;
                }
                if (Quaternion.Angle(rotorAtSwitchOn, speed.TurbineRotor.localRotation) < .1f)
                    throw new InvalidOperationException("Rotor did not spin: running=" + speed.IsTurbineRunning + "; binder=" + binder.isActiveAndEnabled + "; model=" + binder.UsesWindModel + "; dt=" + Time.deltaTime + "; scale=" + Time.timeScale);
                Debug.Log("EXP1 rotor: " + speed.TurbineRotor.name + "; local mesh bounds=" + speed.TurbineRotor.GetComponent<MeshFilter>()?.sharedMesh.bounds.size);
                owner.GoToStep(19);
                var graphTab = owner.GetComponentsInChildren<SingleClickInteraction>(true).Single(t => t.name == "Button (2)" && t.transform.parent.name == "Step 14");
                graphTab.interaction.Click();
                if (windUI.Find("Black Screen").gameObject.activeSelf || !windUI.Find("Wind graph").gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Graph tab did not reveal the wind graph screen.");
                speed.gameObject.SetActive(true);
                owner.GoToStep(22); // Serialized step index used for Speed control wind.
                var interaction = speed.GetComponent<Interaction>();
                if (!interaction.Allow || speed.GetComponent<SingleClickInteraction>().interaction != interaction)
                    throw new InvalidOperationException("Speed step did not enable its own Interaction.Allow.");
                interaction.Click();
                var speedCanvas = speed.transform.parent.Find("Canvas (1)");
                if (!speedCanvas.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Speed interaction did not open its controls.");
                graph.UpdateWindMeasurement();
                var serialized = new SerializedObject(graph);
                VerifyWindGraph(serialized.FindProperty("windGraphReference").objectReferenceValue as TextAsset);
                var chart = serialized.FindProperty("lineChart").objectReferenceValue as LineChart;
                var x = serialized.FindProperty("plottedXValuesText").objectReferenceValue as TMP_Text;
                var y = serialized.FindProperty("plottedYValuesText").objectReferenceValue as TMP_Text;
                var exporter = serialized.FindProperty("exporter").objectReferenceValue as PVExperimentExporter;
                if (chart == null) throw new InvalidOperationException("EXP1 runtime chart did not spawn.");
                windUI.Find("Wind graph/Clear").GetComponent<OnMouseDownE>().mouseDown.Invoke();
                if (x.text != "" || y.text != "" || exporter.BuildWindCSV().Split('\n').Length != 2)
                    throw new InvalidOperationException("Wind clear left stale table/export data.");
                var plus = speedCanvas.Find("Plus").GetComponent<Button>();
                var minus = speedCanvas.Find("Minus").GetComponent<Button>();
                graph.UpdateWindMeasurement();
                for (int i = 0; i < 13; i++) plus.onClick.Invoke();
                for (int i = 0; i < 13; i++) minus.onClick.Invoke();
                graph.UpdateWindMeasurement();
                if (x.text.Split('\n').Length != 14 || y.text.Split('\n').Length != 14 ||
                    exporter.BuildWindCSV().Split('\n').Length != 16 || chart.GetSerie(0).data.Count != 14)
                    throw new InvalidOperationException("Wind chart, table and export counts do not match.");
                string[] referenceRows = (serialized.FindProperty("windGraphReference").objectReferenceValue as TextAsset).text.Trim().Split('\n').Skip(1).ToArray();
                for (int i = 0; i < 14; i++)
                {
                    var values = referenceRows[i].Trim().Split(',');
                    if (x.text.Split('\n')[i] != values[0] || y.text.Split('\n')[i] != values[1] || exporter.BuildWindCSV().Trim().Split('\n')[i + 1] != values[0] + "," + values[1])
                        throw new InvalidOperationException("Recorded rows differ from the right-hand workbook table.");
                }
                if (chart.GetChartComponent<XAxis>().max != 1200 || chart.GetChartComponent<YAxis>().max != 60)
                    throw new InvalidOperationException("Wind chart bounds are incorrect.");
                Canvas.ForceUpdateCanvases();
                var sx = x.GetComponentInParent<ScrollRect>();
                var sy = y.GetComponentInParent<ScrollRect>();
                sx.content.anchoredPosition += new Vector2(0, 80);
                sx.onValueChanged.Invoke(Vector2.zero);
                if (Mathf.Abs(sx.content.anchoredPosition.y - sy.content.anchoredPosition.y) > .01f)
                    throw new InvalidOperationException("Wind table scrolling is not synchronized.");
                Quaternion rotorBefore = speed.TurbineRotor.localRotation;
                Quaternion rotorExpected = rotorBefore * Quaternion.AngleAxis(speed.CurrentSpeed * 6f * .05f * Time.deltaTime, Vector3.right);
                speed.TickRotor();
                if (Quaternion.Angle(rotorBefore, speed.TurbineRotor.localRotation) <= .0001f || Quaternion.Angle(rotorExpected, speed.TurbineRotor.localRotation) > .05f)
                    throw new InvalidOperationException("Turbine rotor did not rotate while powered.");
                var offButton = speed.transform.parent.parent.Find("Step 8/Off_button004 (1)");
                if (offButton.GetComponent<Collider>() == null) throw new InvalidOperationException("Physical Off button cannot receive clicks.");
                offButton.GetComponent<OnMouseDownE>().mouseDown.Invoke();
                rotorBefore = speed.TurbineRotor.localRotation;
                speed.TickRotor();
                if (speed.IsTurbineRunning || Quaternion.Angle(rotorBefore, speed.TurbineRotor.localRotation) > .0001f ||
                    !speed.TryGetReading(out offReading) || offReading.acPower != 0 || offReading.dcCurrent != 0)
                    throw new InvalidOperationException("Physical Off did not stop rotor and zero meters.");
                var meterFields = new SerializedObject(binder);
                foreach (string field in new[] { "voltageText", "currentText", "powerText", "acVoltageText", "acCurrentText", "acPowerText" })
                    if ((meterFields.FindProperty(field).objectReferenceValue as TMP_Text).text != "0.00")
                        throw new InvalidOperationException("A visible meter remains nonzero while powered off.");
                foreach (var button in new[] { windUI.Find("Wind graph/export data"), windUI.Find("Wind graph/Clear") })
                    if (button.GetComponent<OnMouseDownE>().mouseDown.GetPersistentEventCount() == 0)
                        throw new InvalidOperationException("Graph export or clear button is not wired.");
                Selection.activeGameObject = chart.gameObject;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.rotation = chart.GetComponentInParent<Canvas>().transform.rotation;
                SceneView.lastActiveSceneView?.Frame(new Bounds(chart.transform.position, Vector3.one * 55), false);
                Debug.Log("Wind EXP1 PLAY TEST PASSED: power On/Off, rotor, zero meters while off, 14 exact reference rows without duplicates, 1200/60 graph bounds, export/Clear buttons and synchronized scrolling.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
        };
        EditorApplication.update += test;
    }

    private static OnMouseDownE WindButton(Transform transform, UnityEngine.Events.UnityAction action)
    {
        if (transform == null) throw new InvalidOperationException("Wind EXP1 button is missing.");
        var button = transform.GetComponent<OnMouseDownE>();
        if (button == null) button = Undo.AddComponent<OnMouseDownE>(transform.gameObject);
        if (button.mouseDown == null) button.mouseDown = new UnityEngine.Events.UnityEvent();
        // These are EXP1's new button objects. Replace only their obsolete calls.
        while (button.mouseDown.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.mouseDown, 0);
        UnityEventTools.AddPersistentListener(button.mouseDown, action);
        if (transform.GetComponent<Collider>() == null)
            Undo.AddComponent<BoxCollider>(transform.gameObject);
        EditorUtility.SetDirty(button);
        return button;
    }

    private static RectTransform WindRect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null ? (RectTransform)existing :
            new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
        return rect;
    }

    private static TMP_Text WindScroll(RectTransform parent, string name, Vector2 min, Vector2 max)
    {
        RectTransform root = WindRect(parent, name, min, max);
        var existing = root.GetComponentInChildren<TMP_Text>(true);
        if (existing != null) return existing;
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        root.gameObject.AddComponent<Image>().color = Color.clear;
        RectTransform viewport = WindRect(root, "Viewport", Vector2.zero, Vector2.one);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = WindRect(viewport, "Content", new Vector2(0, 1), Vector2.one);
        content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform textRect = WindRect(content, "Values", Vector2.zero, Vector2.one);
        var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14; text.color = Color.black;
        text.alignment = TextAlignmentOptions.Top;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false; text.margin = new Vector4(4, 2, 4, 0);
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return text;
    }

    [MenuItem("PV System/Configure Guided Exp 1-5 + Free PV Readouts")]
    private static void Configure()
    {
        PVCSVReader dataReader = FindAll<PVCSVReader>().FirstOrDefault();
        if (dataReader == null)
        {
            Debug.LogError("PV readout setup: no PVCSVReader was found.");
            return;
        }

        int configured = 0;
        for (int number = 1; number <= 5; number++)
            configured += ConfigureGuidedExperiment(number, dataReader) ? 1 : 0;

        configured += ConfigureFreeLab(dataReader) ? 1 : 0;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"PV readout setup: configured {configured} independent PV display systems.");
    }

    private static bool ConfigureGuidedExperiment(int number, PVCSVReader dataReader)
    {
        Transform experiment = FindAll<Transform>().FirstOrDefault(transform =>
            SameName(transform, $"Exp {number}") && HasAncestor(transform, "ScriptHolders"));
        if (experiment == null)
        {
            Debug.LogWarning($"PV readout setup: Guided Exp {number} was not found.");
            return false;
        }

        Transform pvData = FindOrCreateGuidedPvData(experiment);
        if (!TryGetReadouts(pvData, out TMP_Text voltage, out TMP_Text current, out TMP_Text power))
        {
            Debug.LogWarning($"PV readout setup: Guided Exp {number} has no complete V/A/P DC Multimeter fields.");
            return false;
        }

        // Guided Mode must not use the Free Lab-only binder on the same PV Data object.
        foreach (PVIntensityDataBinder freeBinder in pvData.GetComponents<PVIntensityDataBinder>())
            Undo.DestroyObjectImmediate(freeBinder);

        PVGuidedDataBinder binder = pvData.GetComponent<PVGuidedDataBinder>();
        if (binder == null)
            binder = Undo.AddComponent<PVGuidedDataBinder>(pvData.gameObject);

        ApplyBinderReferences(binder, dataReader, voltage, current, power, experiment, pvData);
        AssignGraphBinder(experiment, binder, null);
        return true;
    }

    private static bool ConfigureFreeLab(PVCSVReader dataReader)
    {
        Transform freeLab = FindAll<Transform>().FirstOrDefault(transform => SameName(transform, "Free Lab"));
        if (freeLab == null)
        {
            Debug.LogWarning("PV readout setup: Free Lab was not found.");
            return false;
        }

        Transform pvData = FindPreferredPvData(freeLab);
        if (!TryGetReadouts(pvData, out TMP_Text voltage, out TMP_Text current, out TMP_Text power))
        {
            Debug.LogWarning("PV readout setup: Free Lab has no complete V/A/P DC Multimeter fields.");
            return false;
        }

        PVIntensityDataBinder binder = pvData.GetComponent<PVIntensityDataBinder>();
        if (binder == null)
            binder = Undo.AddComponent<PVIntensityDataBinder>(pvData.gameObject);

        SerializedObject serialized = new SerializedObject(binder);
        serialized.FindProperty("dataReader").objectReferenceValue = dataReader;
        serialized.FindProperty("voltageText").objectReferenceValue = voltage;
        serialized.FindProperty("currentText").objectReferenceValue = current;
        serialized.FindProperty("powerText").objectReferenceValue = power;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        AssignGraphBinder(freeLab, null, binder);
        return true;
    }

    private static void ApplyBinderReferences(
        PVGuidedDataBinder binder,
        PVCSVReader dataReader,
        TMP_Text voltage,
        TMP_Text current,
        TMP_Text power,
        Transform experiment,
        Transform pvData)
    {
        SerializedObject serialized = new SerializedObject(binder);
        serialized.FindProperty("dataReader").objectReferenceValue = dataReader;
        serialized.FindProperty("voltageText").objectReferenceValue = voltage;
        serialized.FindProperty("currentText").objectReferenceValue = current;
        serialized.FindProperty("powerText").objectReferenceValue = power;

        RheostatController rheostat = experiment.GetComponentsInChildren<RheostatController>(true).FirstOrDefault();
        serialized.FindProperty("rheostat").objectReferenceValue = rheostat;

        Transform ui = pvData.parent;
        Slider intensity = ui != null ? ui.GetComponentInChildren<Slider>(true) : null;
        serialized.FindProperty("intensitySlider").objectReferenceValue = intensity;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignGraphBinder(Transform scope, PVGuidedDataBinder guided, PVIntensityDataBinder free)
    {
        foreach (PVGraphController graph in scope.GetComponentsInChildren<PVGraphController>(true))
        {
            SerializedObject serialized = new SerializedObject(graph);
            serialized.FindProperty("guidedDataBinder").objectReferenceValue = guided;
            serialized.FindProperty("freeModeDataBinder").objectReferenceValue = free;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Transform FindPreferredPvData(Transform scope)
    {
        List<Transform> candidates = scope.GetComponentsInChildren<Transform>(true)
            .Where(transform => SameName(transform, "PV Data"))
            .ToList();

        return candidates
            .Where(candidate => TryGetReadouts(candidate, out _, out _, out _))
            .OrderByDescending(transform => HasAncestor(transform, "UI Step"))
            .ThenByDescending(transform => HasAncestor(transform, "Step 6"))
            .FirstOrDefault();
    }

    /// <summary>
    /// Early guided experiments have the PV-characteristic UI but did not
    /// originally have a separate PV Data holder. Create that holder directly
    /// under their existing UI, matching the later guided experiments, rather
    /// than placing a Free Lab component anywhere in their hierarchy.
    /// </summary>
    private static Transform FindOrCreateGuidedPvData(Transform scope)
    {
        Transform existing = FindPreferredPvData(scope);
        if (existing != null)
            return existing;

        Transform multimeter = scope.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(transform => SameName(transform, "DC Multimeter texts"));
        if (multimeter == null)
            return null;

        Transform ui = FindClosestAncestor(multimeter, "UI");
        if (ui == null)
            return null;

        GameObject holder = new GameObject("PV Data");
        Undo.RegisterCreatedObjectUndo(holder, "Create Guided PV Data");
        holder.transform.SetParent(ui, false);
        return holder.transform;
    }

    private static bool TryGetReadouts(Transform pvData, out TMP_Text voltage, out TMP_Text current, out TMP_Text power)
    {
        voltage = current = power = null;
        if (pvData == null || pvData.parent == null)
            return false;

        Transform ui = pvData.parent;
        Transform multimeter = ui.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(transform => SameName(transform, "DC Multimeter texts"));
        if (multimeter == null)
            return false;

        voltage = FindText(multimeter, "V DC");
        current = FindText(multimeter, "A DC");
        power = FindText(multimeter, "P DC");
        return voltage != null && current != null && power != null;
    }

    private static TMP_Text FindText(Transform root, string name) =>
        root.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => SameName(text.transform, name));

    private static IEnumerable<T> FindAll<T>() where T : Component =>
        SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true));

    private static bool SameName(Transform transform, string name) =>
        string.Equals(transform.name.Trim(), name, StringComparison.OrdinalIgnoreCase);

    private static bool HasAncestor(Transform transform, string name)
    {
        for (Transform current = transform.parent; current != null; current = current.parent)
            if (SameName(current, name)) return true;
        return false;
    }

    private static Transform FindClosestAncestor(Transform transform, string name)
    {
        for (Transform current = transform.parent; current != null; current = current.parent)
            if (SameName(current, name)) return current;
        return null;
    }
}

