using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GraphScreenshot : MonoBehaviour
{
    [Header("Graph Area")]
    [SerializeField] private RectTransform graphRect;

    public void SaveGraphImage()
    {
        StartCoroutine(CaptureGraph());
    }


    private IEnumerator CaptureGraph()
    {
        yield return new WaitForEndOfFrame();


        Vector3[] corners = new Vector3[4];
        graphRect.GetWorldCorners(corners);


        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(
            null,
            corners[0]
        );


        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(
            null,
            corners[2]
        );


        int width = Mathf.RoundToInt(
            topRight.x - bottomLeft.x
        );

        int height = Mathf.RoundToInt(
            topRight.y - bottomLeft.y
        );


        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGB24,
            false
        );


        texture.ReadPixels(
            new Rect(
                bottomLeft.x,
                bottomLeft.y,
                width,
                height
            ),
            0,
            0
        );


        texture.Apply();


        byte[] bytes = texture.EncodeToPNG();


        Destroy(texture);
        PVExperimentExporter.SaveWindFile(bytes, "Wind_Graph", "png");
    }
}