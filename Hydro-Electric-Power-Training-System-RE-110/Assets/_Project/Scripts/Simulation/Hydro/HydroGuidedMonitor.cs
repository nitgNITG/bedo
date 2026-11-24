using System.Collections.Generic;
using TMPro;
using TutorialFramework;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;

namespace Bedo.Hydro
{
    public sealed class HydroGuidedMonitor : MonoBehaviour
    {
        [SerializeField] private GameObject chartPrefab;
        [SerializeField] private Transform chartParent;
        [SerializeField] private TMP_Text speedColumn;
        [SerializeField] private TMP_Text voltageColumn;
        [SerializeField] private TMP_Text currentColumn;
        [SerializeField] private bool separateLoadCurves;
        private readonly SortedDictionary<float, float> samples = new SortedDictionary<float, float>();
        private readonly SortedDictionary<float, float> currents = new SortedDictionary<float, float>();
        private readonly SortedDictionary<float, float> loadedSamples = new SortedDictionary<float, float>();
        private readonly SortedDictionary<float, float> loadedCurrents = new SortedDictionary<float, float>();
        private LineChart chart;
        private bool showingCurrent;
        [SerializeField] private HydroGuidedSpeed speed;
        [SerializeField] private Image voltageSelector;
        [SerializeField] private Button voltageButton;
        [SerializeField] private Button currentButton;
        [SerializeField] private bool initializeOnEnable;
        [SerializeField] private PVExperimentExporter exporter;

        private void OnEnable() { if (initializeOnEnable) OpenGraph(); }

        public void AddReading(float rpm, float voltage) =>
            AddDcReading(rpm, voltage, speed ? speed.CurrentReading.DcCurrent : 0f);
        public void AddDcReading(float rpm, float voltage, float current) =>
            AddDcReading(rpm, voltage, current, speed && speed.IsLoaded);

        public void AddDcReading(float rpm, float voltage, float current, bool loaded)
        {
            if (rpm <= 0f) return;
            var volts = separateLoadCurves && loaded ? loadedSamples : samples;
            var amps = separateLoadCurves && loaded ? loadedCurrents : currents;
            // Keep exact measured values, with one latest sample per 25-RPM interval.
            // This limits recording density without quantizing the speed control.
            int interval = Mathf.RoundToInt(rpm / 25f);
            float previousKey = -1f;
            foreach (var key in volts.Keys)
                if (Mathf.RoundToInt(key / 25f) == interval) { previousKey = key; break; }
            if (previousKey >= 0f && previousKey != rpm)
            {
                volts.Remove(previousKey);
                amps.Remove(previousKey);
            }
            volts[rpm] = voltage;
            amps[rpm] = current;
            if (exporter)
            {
                exporter.ClearWindMeasurements();
                ExportSamples(samples, currents);
                ExportSamples(loadedSamples, loadedCurrents);
            }
            Refresh();
        }

        private void ExportSamples(SortedDictionary<float, float> volts, SortedDictionary<float, float> amps)
        {
            foreach (var item in volts)
                exporter.AddWindMeasurement(new PVCSVReader.WindReading
                    { rpm = item.Key, dcVoltage = item.Value, dcCurrent = amps[item.Key] }, item.Value);
        }

        public void OpenGraph() => SelectGraph(showingCurrent);
        public void ShowGraph() => SelectGraph(false);
        public void ShowCurrentGraph() => SelectGraph(true);

        private void SelectGraph(bool current)
        {
            showingCurrent = current;
            if (!chart && chartPrefab && chartParent)
            {
                var instance = Instantiate(chartPrefab, chartParent, false);
                var rect = (RectTransform)instance.transform;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.anchoredPosition3D = new Vector3(0, 0, -2f);
                chart = instance.GetComponentInChildren<LineChart>(true);
                if (chart)
                {
                    chart.RemoveAllSerie();
                    chart.AddSerie<Line>(separateLoadCurves ? "Without load" : "Speed curve");
                    if (separateLoadCurves)
                        chart.AddSerie<Line>("With load");
                    for (int i = 0; i < (separateLoadCurves ? 2 : 1); i++)
                    {
                        chart.GetSerie(i).symbol.show = true;
                        chart.GetSerie(i).symbol.size = 2;
                    }
                    var legend = chart.GetChartComponent<Legend>();
                    if (legend != null) legend.show = true;
                    var xAxis = chart.GetChartComponent<XAxis>();
                    ConfigureAxis(xAxis, "Speed (RPM)", 1200f);
                    if (xAxis != null)
                    {
                        xAxis.splitNumber = 6;
                        xAxis.axisName.labelStyle.position = LabelStyle.Position.Middle;
                        xAxis.axisName.labelStyle.offset = new Vector3(0, -32, 0);
                    }
                    var grid = chart.GetChartComponent<GridCoord>();
                    if (grid != null) grid.right = .07f;
                    var zoom = chart.GetChartComponent<DataZoom>();
                    if (zoom != null) zoom.enable = false;
                }
            }
            Refresh();
        }

        public void ClearReadings()
        {
            samples.Clear();
            currents.Clear();
            loadedSamples.Clear();
            loadedCurrents.Clear();
            if (exporter) exporter.ClearWindMeasurements();
            Refresh();
        }

        public void SaveGraphImage() { if (chart) chart.SaveAsImage(); }

        private static void ConfigureAxis(Axis axis, string label, float max)
        {
            if (axis == null) return;
            axis.type = Axis.AxisType.Value;
            axis.minMaxType = Axis.AxisMinMaxType.Custom;
            if (axis is YAxis) axis.splitNumber = 5;
            axis.min = 0;
            axis.max = max;
            axis.axisName.show = true;
            axis.axisName.name = label;
            axis.axisLabel.numericFormatter = "0.##";
        }

        private void Refresh()
        {
            var speeds = new List<string>();
            var volts = new List<string>();
            var amps = new List<string>();
            float maxValue = showingCurrent ? .1f : 60f;
            if (chart) chart.ClearData();
            for (int series = 0; series < (separateLoadCurves ? 2 : 1); series++)
            {
                var voltageSamples = series == 0 ? samples : loadedSamples;
                var currentSamples = series == 0 ? currents : loadedCurrents;
                foreach (var item in voltageSamples)
                {
                    float current = currentSamples[item.Key];
                    speeds.Add(item.Key.ToString("0.##"));
                    volts.Add(item.Value.ToString("0.##"));
                    amps.Add(current.ToString("0.##"));
                    float value = showingCurrent ? current : item.Value;
                    maxValue = Mathf.Max(maxValue, value);
                    if (chart) chart.AddData(series, item.Key, value);
                }
            }
            if (speedColumn) speedColumn.text = string.Join("\n", speeds);
            if (voltageColumn) voltageColumn.text = string.Join("\n", volts);
            if (currentColumn) currentColumn.text = string.Join("\n", amps);
            if (voltageButton) voltageButton.interactable = showingCurrent;
            if (currentButton) currentButton.interactable = !showingCurrent;
            if (!chart) return;
            var axis = chart.GetChartComponent<YAxis>();
            ConfigureAxis(axis, showingCurrent ? "DC current (A)" : "DC voltage (V)",
                showingCurrent ? Mathf.Ceil(maxValue * 10f) / 10f : Mathf.Ceil(maxValue / 12f) * 12f);
            chart.RefreshChart();
        }
    }
}
