using System.Collections.Generic;
using TMPro;
using TutorialFramework;
using UnityEngine;
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
        private readonly SortedDictionary<float, float> samples = new SortedDictionary<float, float>();
        private LineChart chart;
        [SerializeField] private HydroGuidedSpeed speed;
        [SerializeField] private UnityEngine.UI.Image voltageSelector;
        [SerializeField] private UnityEngine.UI.Image currentSelector;
        private readonly SortedDictionary<float, float> currents = new SortedDictionary<float, float>();
        private bool showCurrent;
        [SerializeField] private bool initializeOnEnable;

        private void OnEnable()
        {
            if (initializeOnEnable) SelectGraph(showCurrent);
        }
        [SerializeField] private PVExperimentExporter exporter;

        public void AddReading(float rpm, float voltage) => AddDcReading(rpm, voltage, speed ? speed.CurrentReading.DcCurrent : 0f);

        public void AddDcReading(float rpm, float voltage, float current)
        {
            if (rpm <= 0f)
                return;
            samples[rpm] = voltage;
            currents[rpm] = current;
            if (exporter)
            {
                exporter.ClearWindMeasurements();
                foreach (var item in samples) exporter.AddWindMeasurement(new PVCSVReader.WindReading { rpm = item.Key, dcVoltage = item.Value, dcCurrent = currents[item.Key] }, item.Value);
            }
            Refresh();
        }

        public void ShowGraph() => SelectGraph(false);
        public void ShowCurrentGraph() => SelectGraph(true);

        private void SelectGraph(bool current)
        {
            showCurrent = current;
            if (voltageSelector) voltageSelector.color = current ? Color.clear : new Color(0, 0, 0, .22f);
            if (currentSelector) currentSelector.color = current ? new Color(0, 0, 0, .22f) : Color.clear;
            if (!chart && chartPrefab && chartParent)
            {
                var instance = Instantiate(chartPrefab, chartParent, false);
                var position = instance.transform.localPosition;
                position.x -= 112f;
                position.z = -2f;
                instance.transform.localPosition = position;
                ((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 440f);
                chart = instance.GetComponentInChildren<LineChart>(true);
                if (chart)
                {
                    chart.RemoveAllSerie();
                    chart.AddSerie<Line>("Speed curve");
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
                    ConfigureAxis(chart.GetChartComponent<YAxis>(), "DC voltage (V)", 60f);
                }
            }
            Refresh();
        }

        public void ClearReadings()
        {
            samples.Clear();
            currents.Clear();
            if (exporter) exporter.ClearWindMeasurements();
            Refresh();
        }

        public void SaveGraphImage()
        {
            if (chart)
                chart.SaveAsImage();
        }

        private static void ConfigureAxis(Axis axis, string label, float max)
        {
            if (axis == null)
                return;
            axis.type = Axis.AxisType.Value;
            axis.minMaxType = Axis.AxisMinMaxType.Custom;
            if (axis is YAxis) axis.splitNumber = 5;
            axis.min = 0;
            axis.max = max;
            axis.axisName.show = true;
            axis.axisName.name = label;
        }

        private void Refresh()
        {
            var speeds = new List<string>();
            var volts = new List<string>();
            var amps = new List<string>();
            float maxValue = showCurrent ? 1f : 60f;
            if (chart)
                chart.ClearData();
            foreach (var item in samples)
            {
                speeds.Add(item.Key.ToString("0.##"));
                volts.Add(item.Value.ToString("0.##"));
                float current = currents[item.Key];
                amps.Add(current.ToString("0.##"));
                float value = showCurrent ? current : item.Value;
                maxValue = Mathf.Max(maxValue, value);
                if (chart)
                    chart.AddData(0, item.Key, value);
            }
            if (speedColumn) speedColumn.text = string.Join("\n", speeds);
            if (voltageColumn) voltageColumn.text = string.Join("\n", volts);
            if (currentColumn) currentColumn.text = string.Join("\n", amps);
            if (!chart)
                return;
            var axis = chart.GetChartComponent<YAxis>();
            if (axis != null)
                ConfigureAxis(axis, showCurrent ? "DC current (A)" : "DC voltage (V)", showCurrent ? Mathf.Ceil(maxValue) : Mathf.Ceil(maxValue / 12f) * 12f);
            chart.RefreshChart();
        }
    }
}