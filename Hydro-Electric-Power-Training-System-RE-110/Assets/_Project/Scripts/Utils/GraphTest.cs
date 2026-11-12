using UnityEngine;
using XCharts.Runtime;

public class GraphTest : MonoBehaviour
{
    [SerializeField] private LineChart chart;

    private void Start()
    {
        chart.ClearData();

        for (int i = 0; i < 20; i++)
        {
            chart.AddData(0, Random.Range(0f, 100f));
        }
    }
}