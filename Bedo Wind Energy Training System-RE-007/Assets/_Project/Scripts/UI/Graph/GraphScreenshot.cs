using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GraphScreenshot : MonoBehaviour
{
    [Header("Graph Area")]
    [SerializeField] private RectTransform graphRect;

    private int imageCounter = 0;


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


        string path =
            Application.persistentDataPath +
            "/PV_Graph_" +
            imageCounter +
            ".png";


        System.IO.File.WriteAllBytes(
            path,
            bytes
        );


        imageCounter++;


        Debug.Log(
            "Graph saved at: " + path
        );


        Destroy(texture);
    }
}