using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Template;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XCharts.Runtime;

/// <summary>
/// Converts only the heavy scene copies of LineChart_Smooth (1) into one
/// prefab. It deliberately preserves every existing PVGraphController and
/// gives each one direct references to its own spawn parent and Clear button.
/// </summary>
public static class PVRuntimeSolarGraphSetup
{
    private const string PrefabPath =
        "Assets/_Project/Prefabs/Graphs/Solar Graph Runtime.prefab";

    [MenuItem("PV System/Convert LineChart_Smooth To Runtime Prefab")]
    private static void ConvertCharts()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Exit Play Mode before converting the Solar Graph charts.");
            return;
        }

        List<GraphRecord> records = FindGraphRecords();
        if (records.Count == 0)
        {
            Debug.LogError("No PVGraphController with an assigned LineChart was found.");
            return;
        }

        GraphRecord source = records.FirstOrDefault(record =>
            record.chart.name == "LineChart_Smooth (1)" &&
            record.ownerStoryboard != null &&
            string.Equals(record.ownerStoryboard.StoryboardID, "Exp 1",
                StringComparison.OrdinalIgnoreCase));

        // Some authored graph controllers sit beside their storyboard rather
        // than beneath it. The chart itself is still under the Exp 1 branch.
        if (source == null)
        {
            source = records.FirstOrDefault(record =>
                record.chart.name == "LineChart_Smooth (1)" &&
                HasAncestorNamed(record.chart.transform, "Exp 1"));
        }

        if (source == null)
        {
            Debug.LogError(
                "Could not find Exp 1's LineChart_Smooth (1). No scene changes were made."
            );
            return;
        }

        string directory = Path.GetDirectoryName(PrefabPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            source.chart.gameObject,
            PrefabPath
        );
        if (prefab == null)
        {
            Debug.LogError("Solar Graph prefab could not be created.");
            return;
        }

        HashSet<GameObject> oldChartObjects = new HashSet<GameObject>();
        int configured = 0;

        foreach (GraphRecord record in records)
        {
            Transform container = FindGraphContainer(record.chart.transform);
            if (container == null)
            {
                Debug.LogWarning(
                    "Skipped " + record.controller.name +
                    ": no local child named 'graph' was found for its chart."
                );
                continue;
            }

            SerializedObject serialized = new SerializedObject(record.controller);
            serialized.FindProperty("lineChart").objectReferenceValue = null;
            serialized.FindProperty("lineChartPrefab").objectReferenceValue = prefab;
            serialized.FindProperty("lineChartSpawnParent").objectReferenceValue = container;
            serialized.FindProperty("owningStoryboard").objectReferenceValue =
                record.ownerStoryboard;
            AssignClearButtons(
                serialized.FindProperty("clearButtons"),
                FindClearButtons(record.chart)
            );
            serialized.ApplyModifiedPropertiesWithoutUndo();

            oldChartObjects.Add(record.chart.gameObject);
            configured++;
        }

        foreach (GameObject oldChartObject in oldChartObjects)
            Undo.DestroyObjectImmediate(oldChartObject);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log(
            "Solar Graph runtime conversion complete. Created one prefab and " +
            "configured " + configured + " existing PVGraphController components."
        );
    }

    private static List<GraphRecord> FindGraphRecords()
    {
        List<GraphRecord> records = new List<GraphRecord>();
        foreach (PVGraphController controller in FindSceneObjects<PVGraphController>())
        {
            SerializedObject serialized = new SerializedObject(controller);
            LineChart chart = serialized.FindProperty("lineChart")
                .objectReferenceValue as LineChart;
            if (chart == null)
                continue;

            StoryboardSteps owner = controller.GetComponentInParent<StoryboardSteps>(true);
            if (owner == null)
            {
                PVGuidedDataBinder guidedBinder = serialized
                    .FindProperty("guidedDataBinder").objectReferenceValue as PVGuidedDataBinder;
                if (guidedBinder != null)
                    owner = guidedBinder.GetComponentInParent<StoryboardSteps>(true);
            }

            records.Add(new GraphRecord
            {
                controller = controller,
                chart = chart,
                ownerStoryboard = owner
            });
        }

        return records;
    }

    private static Transform FindGraphContainer(Transform chart)
    {
        for (Transform current = chart.parent; current != null; current = current.parent)
        {
            if (string.Equals(current.name.Trim(), "graph",
                    StringComparison.OrdinalIgnoreCase))
                return current;
        }

        return null;
    }

    private static bool HasAncestorNamed(Transform transform, string name)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (string.Equals(current.name.Trim(), name,
                    StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static OnMouseDownE[] FindClearButtons(LineChart chart)
    {
        return FindSceneObjects<OnMouseDownE>()
            .Where(button => CallsMethodOnChart(button, chart, "ClearData"))
            .ToArray();
    }

    // Compatible with this project's Unity version and includes inactive
    // authored objects, while excluding prefab assets outside the scene.
    private static IEnumerable<T> FindSceneObjects<T>() where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .Where(component => component != null &&
                component.gameObject.scene.IsValid());
    }

    private static bool CallsMethodOnChart(
        OnMouseDownE button,
        LineChart chart,
        string methodName)
    {
        for (int index = 0; index < button.mouseDown.GetPersistentEventCount(); index++)
        {
            if (button.mouseDown.GetPersistentTarget(index) == chart &&
                button.mouseDown.GetPersistentMethodName(index) == methodName)
                return true;
        }

        return false;
    }

    private static void AssignClearButtons(
        SerializedProperty property,
        OnMouseDownE[] buttons)
    {
        property.arraySize = buttons.Length;
        for (int index = 0; index < buttons.Length; index++)
            property.GetArrayElementAtIndex(index).objectReferenceValue = buttons[index];
    }

    private sealed class GraphRecord
    {
        public PVGraphController controller;
        public LineChart chart;
        public StoryboardSteps ownerStoryboard;
    }
}
