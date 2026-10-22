using UnityEngine;

public class PVExperimentController : MonoBehaviour
{
    [SerializeField]
    private Window_Graph graph;

    [Header("Current Rheostat Value")]
    [Range(0f, 1f)]
    [SerializeField]
    private float rheostatValue = 1f;

    private float lastMeasurementValue = -1f;

    private void Update()
    {
        // Only record a new point when the rheostat changes
        if (Mathf.Abs(rheostatValue - lastMeasurementValue) > 0.05f)
        {
            TakeMeasurement();

            lastMeasurementValue =
                rheostatValue;
        }
    }

    private void TakeMeasurement()
    {
        // Convert rheostat value into voltage/current
        float voltage =
            Mathf.Lerp(
                0.5f,
                20f,
                rheostatValue
            );

        float current =
            Mathf.Lerp(
                1.05f,
                0f,
                rheostatValue
            );

        // Add measurement to graph
        graph.AddDataPoint(
            voltage,
            current
        );

        Debug.Log(
            "PV Measurement | Voltage: "
            + voltage.ToString("F2")
            + " V | Current: "
            + current.ToString("F2")
            + " A"
        );
    }
}