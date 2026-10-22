using UnityEngine;

/// <summary>
/// Compatibility bridge for the existing PDFViewer -- EventListener prefab.
/// Its serialized fields are retained so the object no longer has a missing
/// component, while opening remains owned by PDFViewerEventListener.
/// </summary>
public class PDFViewerDownloadBridge : MonoBehaviour
{
    [SerializeField] private bool useCustomResources = true;
    [SerializeField] private ScriptableObject downloadEvent;
    [SerializeField] private string resourcesEventName =
        "Ui_V3_OnDownloadPdfButtonPressed";
    [SerializeField] private string relativePathInStreamingAssets;
    [SerializeField] private string suggestedFileName = "manual.pdf";

    public void OpenAssignedPdf()
    {
        foreach (PDFViewerEventListener listener in
            Resources.FindObjectsOfTypeAll<PDFViewerEventListener>())
        {
            if (listener != null && listener.gameObject.scene.IsValid())
            {
                listener.OpenPdfViewer();
                return;
            }
        }
    }
}
