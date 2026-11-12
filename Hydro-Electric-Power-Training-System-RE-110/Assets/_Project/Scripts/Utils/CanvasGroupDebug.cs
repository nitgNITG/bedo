using UnityEngine;

public class CanvasGroupDebug : MonoBehaviour
{
    private CanvasGroup cg;
    private float lastAlpha;


    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();

        if (cg != null)
            lastAlpha = cg.alpha;
    }


    private void Update()
    {
        if (cg == null)
            return;


        if (cg.alpha != lastAlpha)
        {
            Debug.Log(
                "CanvasGroup alpha changed from "
                + lastAlpha +
                " to "
                + cg.alpha,
                this
            );

            lastAlpha = cg.alpha;
        }
    }
}