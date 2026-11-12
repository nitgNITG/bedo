using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;
using TMPro;
using Template;
using TutorialFramework;


public class PVGraphController : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private LineChart lineChart;
    [Tooltip("The Exp 1 LineChart_Smooth (1) template. It is instantiated only for this controller's active experiment.")]
    [SerializeField] private GameObject lineChartPrefab;
    [Tooltip("The local UI > Solar Graph > graph container for this experiment or Free Mode.")]
    [SerializeField] private Transform lineChartSpawnParent;
    [Tooltip("Leave empty only for the Free Mode graph controller.")]
    [SerializeField] private StoryboardSteps owningStoryboard;


    [Header("Data")]
    [SerializeField] private PVCSVReader dataReader;
    [Tooltip("Optional reference RPM steps for legacy controls. Plotted voltage uses the live wind model.")]
    [SerializeField] private TextAsset windGraphReference;

    [Header("Local display source")]
    [Tooltip("Guided Mode: use this experiment's own PV binder so the graph and DC multimeter use the identical point.")]
    [SerializeField] private PVGuidedDataBinder guidedDataBinder;
    [Tooltip("Free Mode: use the Free Lab binder so the graph and DC multimeter use the identical point.")]
    [SerializeField] private PVIntensityDataBinder freeModeDataBinder;
    [SerializeField] private FreeLabWindController freeWindController;

    [Header("Axis Labels")]
    [Tooltip("Text shown for the horizontal axis. Set this to the quantity plotted as X.")]
    [SerializeField] private string xAxisLabel = "DC Voltage";
    [Tooltip("Text shown for the vertical axis. Set this to the quantity plotted as Y.")]
    [SerializeField] private string yAxisLabel = "DC Current";

    [Header("Exporter")]
    [SerializeField] private PVExperimentExporter exporter;

    [Header("Plotted Point Lists")]
    [Tooltip("Optional TextMeshPro field. Writes every plotted X value on its own line.")]
    [SerializeField] private TMP_Text plottedXValuesText;
    [Tooltip("Optional TextMeshPro field. Writes every plotted Y value on its own line.")]
    [SerializeField] private TMP_Text plottedYValuesText;
    [SerializeField] private TMP_Text plottedCurrentValuesText;
    [SerializeField] private Button voltageGraphTab, acSpeedGraphTab;
    private bool showingACSpeedGraph;
    private readonly List<float> plottedACCurrentValues = new List<float>();
    private readonly List<float> plottedCurrentValues = new List<float>();
    [SerializeField] private string plottedValueFormat = "0.##";
    [Tooltip("Optional direct Clear buttons. When assigned, no scene-wide button search is performed.")]
    [SerializeField] private OnMouseDownE[] clearButtons;


    private const int PointSeries = 0;
    private readonly List<float> plottedXValues = new List<float>();
    private readonly List<float> plottedYValues = new List<float>();
    private readonly List<OnMouseDownE> linkedClearButtons = new List<OnMouseDownE>();
    private bool windGraphReady;
    private bool UsesWindModel => freeWindController != null || (guidedDataBinder != null && guidedDataBinder.UsesWindModel);
    public bool IsWindExperimentActive => UsesWindModel && IsActiveControllerForCurrentMode();
    public float[] GetWindReferenceSpeeds()
    {
        if (windGraphReference == null) return new float[0];
        return windGraphReference.text.Split('\n').Skip(1).Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => float.Parse(r.Split(',')[0], System.Globalization.CultureInfo.InvariantCulture)).OrderBy(r => r).ToArray();
    }

    private bool EnsureWindGraph()
    {
        if (!UsesWindModel || !IsActiveControllerForCurrentMode()) return false;
        if (windGraphReady) return lineChart != null;
        EnsureLineChart();
        if (lineChart == null) return false;
        SetupGraph();
        SetupReadoutControls();
        windGraphReady = true;
        return true;
    }

    public void UpdateWindMeasurement()
    {
        if (!EnsureWindGraph()) return;
        PVCSVReader.WindReading reading;
        if (freeWindController != null)
        { if (!freeWindController.TryGetWindReading(out reading)) return; }
        else
        { if (!guidedDataBinder.TryGetWindReading(out reading)) return; guidedDataBinder.RefreshDisplay(); }
        if (reading.rpm <= 0f) return;
        float graphVoltage = reading.dcVoltage;
        // A new load can produce a different voltage at an already recorded RPM.
        // Suppress repeated readings, rather than discarding every repeated RPM.
        for (int i = 0; i < plottedXValues.Count; i++)
            if (Mathf.Abs(plottedXValues[i] - reading.rpm) < .001f &&
                Mathf.Abs(plottedYValues[i] - graphVoltage) < .001f &&
                Mathf.Abs(plottedCurrentValues[i] - reading.dcCurrent) < .001f &&
                Mathf.Abs(plottedACCurrentValues[i] - reading.acCurrent) < .001f) return;

        // The loaded model can exceed the original reference curve's 60 V range.
        var yAxis = lineChart.GetChartComponent<YAxis>();
        float graphValue = showingACSpeedGraph ? reading.acCurrent : graphVoltage;
        if (yAxis != null && graphValue > yAxis.max)
            yAxis.max = showingACSpeedGraph ? Mathf.Ceil(graphValue * 10f) / 10f : Mathf.Ceil(graphValue / 12f) * 12f;
        lineChart.AddData(PointSeries, reading.rpm, graphValue);
        plottedXValues.Add(reading.rpm);
        plottedYValues.Add(graphVoltage);
        plottedCurrentValues.Add(reading.dcCurrent);
        plottedACCurrentValues.Add(reading.acCurrent);
        RefreshPlottedPointLists();
        if (exporter != null) exporter.AddWindMeasurement(reading, graphVoltage);
        lineChart.RefreshChart();
    }

    public void ShowVoltageGraph() => SelectWindGraph(false);
    public void ShowACSpeedGraph() => SelectWindGraph(true);

    private void SelectWindGraph(bool acSpeed)
    {
        if (!EnsureWindGraph()) return;
        showingACSpeedGraph = acSpeed;
        lineChart.RemoveAllSerie();
        lineChart.ClearData();
        var series = lineChart.AddSerie<Line>(acSpeed ? "AC-Speed curve" : "Speed curve");
        series.symbol.show = true;
        series.symbol.size = 2;
        var values = acSpeed ? plottedACCurrentValues : plottedYValues;
        var axis = lineChart.GetChartComponent<YAxis>();
        if (axis != null)
        {
            axis.axisName.name = acSpeed ? "AC current (A)" : yAxisLabel;
            axis.axisLabel.numericFormatter = "0.##";
            axis.min = 0;
            float maximum = values.Count > 0 ? values.Max() : 0;
            axis.max = acSpeed ? Mathf.Max(.1f, Mathf.Ceil(maximum * 10f) / 10f)
                : Mathf.Max(60, Mathf.Ceil(maximum / 12f) * 12f);
        }
        for (int i = 0; i < plottedXValues.Count; i++)
            lineChart.AddData(PointSeries, plottedXValues[i], values[i]);
        if (voltageGraphTab) voltageGraphTab.interactable = acSpeed;
        if (acSpeedGraphTab) acSpeedGraphTab.interactable = !acSpeed;
        lineChart.RefreshChart();
    }

    public bool TryGetWindGraphVoltage(float rpm, out float voltage) =>
        PVCSVReader.TryCalculateWindGraph(windGraphReference, rpm, out voltage);


    private void Start()
    {
        if (UsesWindModel)
        {
            EnsureWindGraph();
            return;
        }
        if (!IsActiveControllerForCurrentMode())
            return;

        EnsureLineChart();
        SetupGraph();
        SetupReadoutControls();
    }

    private bool IsActiveControllerForCurrentMode()
    {
        TutorialFramework.InteractionManager manager =
            TutorialFramework.InteractionManager.Instance;
        if (manager == null)
            return false;

        if (freeWindController != null)
            return manager.Free && freeWindController.isActiveAndEnabled;

        // The Free Mode controller is identified by its existing Free binder.
        if (freeModeDataBinder != null)
            return manager.Free;

        return manager.Guided && owningStoryboard != null &&
            manager.IsExperimentActive(owningStoryboard);
    }

    private void EnsureLineChart()
    {
        if (lineChart != null || lineChartPrefab == null || lineChartSpawnParent == null)
            return;

        GameObject instance = Instantiate(lineChartPrefab, lineChartSpawnParent, false);
        instance.name = lineChartPrefab.name;
        lineChart = instance.GetComponent<LineChart>();
        if (lineChart == null)
            lineChart = instance.GetComponentInChildren<LineChart>(true);
        if (UsesWindModel && instance.transform is RectTransform rect)
        {
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.anchoredPosition3D = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        if (voltageGraphTab) voltageGraphTab.onClick.RemoveListener(ShowVoltageGraph);
        if (acSpeedGraphTab) acSpeedGraphTab.onClick.RemoveListener(ShowACSpeedGraph);
        foreach (var clearButton in linkedClearButtons)
        {
            if (clearButton != null)
                clearButton.mouseDown.RemoveListener(ClearRuntimeGraph);
        }
    }



    private void SetupGraph()
    {
        if (lineChart == null)
        {
            Debug.LogError("LineChart not assigned");
            return;
        }


        if (dataReader == null && !UsesWindModel)
        {
            Debug.LogError("PVCSVReader not assigned");
            return;
        }


        // Remove the prefab's old series (for example "Set Point (T(C))"),
        // not merely its point data. This graph owns one series only.
        lineChart.RemoveAllSerie();
        lineChart.ClearData();
        plottedXValues.Clear();
        plottedYValues.Clear();
        plottedCurrentValues.Clear();
        plottedACCurrentValues.Clear();
        RefreshPlottedPointLists();

        // Replace prefab labels such as "Set Point (T(C))" with labels for
        // the actual quantities plotted by this controller.
        var xAxis = lineChart.GetChartComponent<XAxis>();
        var yAxis = lineChart.GetChartComponent<YAxis>();
        if (xAxis != null)
        {
            xAxis.axisName.show = !string.IsNullOrWhiteSpace(xAxisLabel);
            xAxis.axisName.name = xAxisLabel;
        }
        if (yAxis != null)
        {
            yAxis.axisName.show = !string.IsNullOrWhiteSpace(yAxisLabel);
            yAxis.axisName.name = yAxisLabel;
        }

        if (UsesWindModel)
        {
            // Reserve room inside the chart for the complete final RPM label.
            var grid = lineChart.GetChartComponent<GridCoord>();
            if (grid != null) grid.right = .07f;
            if (xAxis != null)
            {
                xAxis.type = Axis.AxisType.Value;
                xAxis.minMaxType = Axis.AxisMinMaxType.Custom;
                xAxis.min = 0;
                xAxis.max = 1200;
                xAxis.splitNumber = 6;
                xAxis.axisName.labelStyle.position = LabelStyle.Position.Middle;
                xAxis.axisName.labelStyle.offset = new Vector3(0, -32, 0);
            }
            if (yAxis != null)
            {
                yAxis.type = Axis.AxisType.Value;
                yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
                yAxis.min = 0;
                yAxis.max = 60;
                yAxis.splitNumber = 5;
            }
            var zoom = lineChart.GetChartComponent<DataZoom>();
            if (zoom != null) zoom.enable = false;
        }


        // Create empty operating point series
        string seriesName = UsesWindModel ? "Speed curve" : "Operating Point";
        lineChart.AddSerie<Line>(seriesName);


        var serie =
            lineChart.GetSerie(seriesName);


        serie.symbol.show = true;
        serie.symbol.size = 2;


        lineChart.RefreshChart();


        Debug.Log("Graph ready - waiting for rheostat");
    }



    public void UpdateMeasurement(float resistance)
    {
        UpdateMeasurement(resistance, null);
    }

    /// <summary>
    /// Records one reading. In Free Mode, only the rheostat whose authored
    /// cable group is complete may submit a point.
    /// </summary>
    public void UpdateMeasurement(float resistance, RheostatController sourceRheostat)
    {
        if (UsesWindModel)
        {
            // Read the physical load used by the meters and record the same voltage.
            UpdateWindMeasurement();
            return;
        }
        float voltage = 0f;
        float current = 0f;

        if (guidedDataBinder != null)
        {
            guidedDataBinder.SetGraphResistance(resistance);
            if (!guidedDataBinder.TryComputePoint(out voltage, out current, out float _))
            {
                Debug.LogWarning($"Could not compute point for resistance {resistance}Ω");
                return;
            }
        }
        else if (freeModeDataBinder != null)
        {
            if (sourceRheostat != null &&
                !FreeLabRheostatDataAdapter.IsActiveRheostat(sourceRheostat))
            {
                return;
            }

            if (!FreeLabRheostatDataAdapter.HasActiveSource)
            {
                return;
            }

            // Use the exact resistance from the active physical controller.
            // The CSV reader then applies the workbook equations to it.
            if (!freeModeDataBinder.TryGetCurrentPoint(resistance, out PVPoint point))
            {
                Debug.LogWarning("Free mode: no matching point found.");
                return;
            }
            voltage = point.voltage;
            current = point.current;
        }
        else
        {
            Debug.LogWarning("No data binder assigned.");
            return;
        }

        var serie = lineChart.GetSerie("Operating Point");
        if (serie == null)
        {
            lineChart.AddSerie<Line>("Operating Point");
            serie = lineChart.GetSerie("Operating Point");
            serie.symbol.show = true;
            serie.symbol.size = 15;
        }

        lineChart.AddData(PointSeries, voltage, current);

        plottedXValues.Add(voltage);
        plottedYValues.Add(current);
        RefreshPlottedPointLists();

        if (exporter != null)
            exporter.AddMeasurement(resistance, voltage, current);

        lineChart.RefreshChart();

        Debug.Log($"Added Point | R={resistance}Ω V={voltage:F2} I={current:F2}");
    }



    private float ConvertRheostatToResistance(int value)
    {
        switch (value)
        {
            case 10:
                return 100;

            case 9:
                return 80;

            case 8:
                return 60;

            case 7:
                return 40;

            case 6:
                return 20;

            default:
                return 13;
        }
    }



    public void SaveGraphImage()
    {
        if (lineChart != null)
        {
            lineChart.SaveAsImage();
            Debug.Log("Graph saved!");
        }
    }

    private void RefreshPlottedPointLists()
    {
        if (plottedXValuesText != null)
            plottedXValuesText.text = FormatValues(plottedXValues);

        if (plottedYValuesText != null)
            plottedYValuesText.text = FormatValues(plottedYValues);
        if (plottedCurrentValuesText != null)
            plottedCurrentValuesText.text = FormatValues(plottedCurrentValues);
    }

    private string FormatValues(List<float> values)
    {
        return string.Join(
            "\n",
            values.Select(value => value.ToString(plottedValueFormat))
        );
    }


    public void ClearPlottedValues()
    {
        plottedXValues.Clear();
        plottedYValues.Clear();
        plottedCurrentValues.Clear();
        plottedACCurrentValues.Clear();
        RefreshPlottedPointLists();
    }

    /// <summary>
    /// Clears the instantiated chart and its paired numeric readouts.
    /// </summary>
    public void ClearRuntimeGraph()
    {
        if (lineChart != null)
        {
            lineChart.ClearData();
            lineChart.RefreshChart();
        }

        ClearPlottedValues();
        if (UsesWindModel && exporter != null) exporter.ClearWindMeasurements();
    }

    // Existing Clear buttons already call XCharts' ClearData. Find those buttons
    // at runtime and add clearing for the two accompanying text readouts too.
    // This avoids changing their authored animations or existing events.
    private void SetupReadoutControls()
    {
        var voltageScroll = FindParentScrollRect(plottedXValuesText);
        var currentScroll = FindParentScrollRect(plottedYValuesText);
        if (voltageScroll != null && currentScroll != null)
        {
            GameObject scrollOwner = UsesWindModel ? voltageScroll.transform.parent.gameObject : gameObject;
            var scrollSync = scrollOwner.GetComponent<PVGraphReadoutScrollSync>();
            if (scrollSync == null)
                scrollSync = scrollOwner.AddComponent<PVGraphReadoutScrollSync>();
            scrollSync.Configure(voltageScroll, currentScroll, FindParentScrollRect(plottedCurrentValuesText));
        }

        if (voltageGraphTab) voltageGraphTab.onClick.AddListener(ShowVoltageGraph);
        if (acSpeedGraphTab) acSpeedGraphTab.onClick.AddListener(ShowACSpeedGraph);
        if (voltageGraphTab) voltageGraphTab.interactable = showingACSpeedGraph;
        if (acSpeedGraphTab) acSpeedGraphTab.interactable = !showingACSpeedGraph;
        if (lineChart == null)
            return;

        if (clearButtons != null && clearButtons.Length > 0)
        {
            foreach (OnMouseDownE button in clearButtons)
            {
                if (button == null)
                    continue;

                button.mouseDown.AddListener(ClearRuntimeGraph);
                linkedClearButtons.Add(button);
            }

            return;
        }

        foreach (var button in FindObjectsByType<OnMouseDownE>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!CallsClearDataOnThisGraph(button))
                continue;

            button.mouseDown.AddListener(ClearRuntimeGraph);
            linkedClearButtons.Add(button);
        }
    }

    private bool CallsClearDataOnThisGraph(OnMouseDownE button)
    {
        for (int index = 0; index < button.mouseDown.GetPersistentEventCount(); index++)
        {
            if (button.mouseDown.GetPersistentMethodName(index) == "ClearData" &&
                button.mouseDown.GetPersistentTarget(index) == lineChart)
                return true;
        }

        return false;
    }

    private static ScrollRect FindParentScrollRect(Component component)
    {
        if (component == null)
            return null;

        return component.GetComponentInParent<ScrollRect>(true);
    }
}
