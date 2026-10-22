using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using XCharts.Runtime;
using SFB;
using Bedo.Core.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif


public class PVExperimentExporter : MonoBehaviour
{
    [Header("EXP1 wind export (optional)")]
    [SerializeField] private bool windExport;
    [SerializeField] private bool freeWindExport;
    [SerializeField] private Renderer windCaptureSurface;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void Download(string base64, string fileName);
#endif
    private readonly List<PVCSVReader.WindReading> windPoints = new List<PVCSVReader.WindReading>();
    private readonly List<float> windGraphVoltages = new List<float>();
    private bool CanExportInCurrentMode()
    {
        bool free = TutorialFramework.InteractionManager.Instance != null && TutorialFramework.InteractionManager.Instance.Free;
        if (freeWindExport) return free;
        return !free || TutorialFramework.FreeLabWindController.Instance == null ||
            TutorialFramework.FreeLabWindController.Instance.exporter == this;
    }
    public void AddWindMeasurement(PVCSVReader.WindReading reading, float graphVoltage)
    {
        windPoints.Add(reading);
        windGraphVoltages.Add(graphVoltage);
    }
    public void ClearWindMeasurements() { windPoints.Clear(); windGraphVoltages.Clear(); }

    public string BuildWindCSV()
    {
        var text = new System.Text.StringBuilder("Speed (RPM),DC voltage (V)\n");
        for (int row = 0; row < windPoints.Count; row++)
        {
            var p = windPoints[row];
            float[] values = { p.rpm, windGraphVoltages[row] };
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) text.Append(',');
                text.Append(values[i].ToString("0.##", CultureInfo.InvariantCulture));
            }
            text.Append('\n');
        }
        return text.ToString();
    }
    [Header("Graph")]
    [SerializeField] private LineChart lineChart;


    private List<PVPoint> points = new List<PVPoint>();


    [System.Serializable]
    public class PVPoint
    {
        public float resistance;
        public float voltage;
        public float current;
    }



    public void AddMeasurement(
        float resistance,
        float voltage,
        float current)
    {
        points.Add(new PVPoint()
        {
            resistance = resistance,
            voltage = voltage,
            current = current
        });


        Debug.Log(
            $"Stored: {resistance}Ω {voltage}V {current}A"
        );
    }




    // ==========================
    // SAVE CSV ONLY
    // ==========================

    public void SaveDataCSV()
    {
        if (!CanExportInCurrentMode()) return;
        if (windExport)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(BuildWindCSV());
            SaveWindFile(bytes, "Wind_Speed_Curve", "csv");
            return;
        }

        string folder =
            StandaloneFileBrowser.OpenFolderPanel(
                "Choose CSV Save Location",
                "",
                false
            )[0];


        if (string.IsNullOrEmpty(folder))
            return;



        string path =
            Path.Combine(
                folder,
                "PV_Data.csv"
            );



        using (StreamWriter writer =
            new StreamWriter(path))
        {

            writer.WriteLine(
                "Resistance (Ohm),Voltage (V),Current (A)"
            );


            foreach (var p in points)
            {
                writer.WriteLine(
                    $"{p.resistance},{p.voltage},{p.current}"
                );
            }

        }



        Debug.Log(
            "CSV saved: " + path
        );
    }







    // ==========================
    // SAVE GRAPH ONLY
    // ==========================

    public void SaveGraphPNG()
    {
        if (!CanExportInCurrentMode()) return;
        if (windExport)
        {
            StartCoroutine(CaptureWindScreen());
            return;
        }

        string folder =
            StandaloneFileBrowser.OpenFolderPanel(
                "Choose Image Save Location",
                "",
                false
            )[0];


        if (string.IsNullOrEmpty(folder))
            return;



        StartCoroutine(
            CaptureGraph(folder)
        );

    }





    private IEnumerator CaptureWindScreen()
    {
        yield return new WaitForEndOfFrame();
        if (windCaptureSurface == null)
        {
            Debug.LogError("Assign the wind monitor surface before saving its image.", this);
            yield break;
        }
        byte[] bytes = MapCameraScreenshotButton_RT.CaptureSurfaceToPngBytes(windCaptureSurface, 2048);
        SaveWindFile(bytes, "Wind_Screen", "png");
    }

    private static void SaveWindFile(byte[] bytes, string name, string extension)
    {
        if (bytes == null || bytes.Length == 0) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        Download(System.Convert.ToBase64String(bytes), name + "." + extension);
#else
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanel("Save Wind " + extension.ToUpperInvariant(), "", name + "." + extension, extension);
#else
        string path = StandaloneFileBrowser.SaveFilePanel("Save Wind " + extension.ToUpperInvariant(), "", name, extension);
#endif
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, bytes);
            Debug.Log("Saved: " + path);
        }
#endif
    }

    private IEnumerator CaptureGraph(string folder)
    {

        yield return new WaitForEndOfFrame();



        string path =
            Path.Combine(
                folder,
                windExport ? "Wind_Graph.png" : "PV_Graph.png"
            );



        Texture2D texture =
            new Texture2D(
                Screen.width,
                Screen.height,
                TextureFormat.RGB24,
                false
            );



        texture.ReadPixels(
            new Rect(
                0,
                0,
                Screen.width,
                Screen.height
            ),
            0,
            0
        );



        texture.Apply();



        byte[] bytes =
            texture.EncodeToPNG();



        File.WriteAllBytes(
            path,
            bytes
        );



        Destroy(texture);



        Debug.Log(
            "Graph saved: " + path
        );
    }
}
