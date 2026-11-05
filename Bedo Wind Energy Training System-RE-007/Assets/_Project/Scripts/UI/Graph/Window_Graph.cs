using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Window_Graph : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private Sprite circleSprite;

    [SerializeField] private float lineThickness = 1f;

    [Header("Graph Values")]
    [SerializeField] private float xMaximum = 20f; // Voltage
    [SerializeField] private float yMaximum = 1.1f; // Current

    private RectTransform graphContainer;

    private GameObject lastCircleGameObject;

    private List<GameObject> circleList = new List<GameObject>();
    private List<GameObject> connectionList = new List<GameObject>();

    private void Awake()
    {
        graphContainer =
            transform.Find("graphContainer").GetComponent<RectTransform>();
    }

    // =========================================================
    // ADD A NEW MEASUREMENT
    // =========================================================

    public void AddDataPoint(float voltage, float current)
    {
        // Clamp values so they stay inside graph
        voltage = Mathf.Clamp(voltage, 0f, xMaximum);
        current = Mathf.Clamp(current, 0f, yMaximum);

        // Get graph dimensions
        float graphWidth = graphContainer.rect.width;
        float graphHeight = graphContainer.rect.height;

        // Convert actual voltage/current into graph position
        float xPosition =
            (voltage / xMaximum) * graphWidth;

        float yPosition =
            (current / yMaximum) * graphHeight;

        Vector2 pointPosition =
            new Vector2(xPosition, yPosition);

        // Create new point
        GameObject circleGameObject =
            CreateCircle(pointPosition);

        circleList.Add(circleGameObject);

        // Connect to previous point
        if (lastCircleGameObject != null)
        {
            Vector2 previousPosition =
                lastCircleGameObject
                    .GetComponent<RectTransform>()
                    .anchoredPosition;

            GameObject connection =
                CreateDotConnection(
                    previousPosition,
                    pointPosition
                );

            connectionList.Add(connection);
        }

        // Remember this point
        lastCircleGameObject =
            circleGameObject;
    }


    // =========================================================
    // CREATE POINT
    // =========================================================

    private GameObject CreateCircle(
        Vector2 anchoredPosition)
    {
        GameObject gameObject =
            new GameObject(
                "MeasurementPoint",
                typeof(Image)
            );

        gameObject.transform.SetParent(
            graphContainer,
            false
        );

        RectTransform rectTransform =
            gameObject.GetComponent<RectTransform>();

        rectTransform.anchorMin =
            new Vector2(0, 0);

        rectTransform.anchorMax =
            new Vector2(0, 0);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition =
            anchoredPosition;

        rectTransform.sizeDelta =
            new Vector2(6f, 6f);

        Image image =
            gameObject.GetComponent<Image>();

        image.sprite =
            circleSprite;

        return gameObject;
    }


    // =========================================================
    // CREATE LINE BETWEEN POINTS
    // =========================================================

    private GameObject CreateDotConnection(
        Vector2 dotPositionA,
        Vector2 dotPositionB)
    {
        GameObject gameObject =
            new GameObject(
                "GraphLine",
                typeof(Image)
            );

        gameObject.transform.SetParent(
            graphContainer,
            false
        );

        RectTransform rectTransform =
            gameObject.GetComponent<RectTransform>();

        rectTransform.anchorMin =
            new Vector2(0, 0);

        rectTransform.anchorMax =
            new Vector2(0, 0);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        // Calculate direction
        Vector2 direction =
            dotPositionB - dotPositionA;

        // Calculate distance
        float distance =
            direction.magnitude;

        // Put line between points
        rectTransform.anchoredPosition =
            (dotPositionA + dotPositionB) / 2f;

        // Line length and thickness
        rectTransform.sizeDelta =
            new Vector2(
                distance,
                lineThickness
            );

        // Rotate line
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        rectTransform.localEulerAngles =
            new Vector3(
                0,
                0,
                angle
            );

        Image image =
            gameObject.GetComponent<Image>();

        image.color =
            Color.white;

        return gameObject;
    }


    // =========================================================
    // CLEAR GRAPH
    // =========================================================

    public void ClearGraph()
    {
        foreach (GameObject circle in circleList)
        {
            Destroy(circle);
        }

        foreach (GameObject connection in connectionList)
        {
            Destroy(connection);
        }

        circleList.Clear();
        connectionList.Clear();

        lastCircleGameObject = null;
    }
}