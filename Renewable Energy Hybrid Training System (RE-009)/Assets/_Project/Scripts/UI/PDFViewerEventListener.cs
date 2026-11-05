using System.Collections.Generic;
using Paroxe.PdfRenderer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Restores the behaviour expected by the existing PDFViewer -- EventListener
/// object. It keeps the authored Documents buttons intact and only adds a
/// listener to their existing Unity Button click event.
/// </summary>
public class PDFViewerEventListener : MonoBehaviour
{
    [SerializeField] private bool useCustomResources = true;
    [SerializeField] private bool sharpAnimations;
    [SerializeField] private PDFViewer PDFViewerBeh;
    [SerializeField] private GameObject PanelToShow;
    [SerializeField] private MonoBehaviour pdfDownloader;
    [SerializeField] private ScriptableObject IsPDFOpened_BoolVariable;
    [SerializeField] private ScriptableObject PDFFileName_StringVariable;

    private static readonly HashSet<Button> documentButtons =
        new HashSet<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDocumentButtonBootstrap()
    {
        GameObject bootstrap = new GameObject("PDF Documents Button Bootstrap");
        DontDestroyOnLoad(bootstrap);
        bootstrap.AddComponent<PDFDocumentsButtonBootstrap>();
    }

    private void Start()
    {
        RegisterSceneDocumentsButtons();
    }

    private void OnEnable()
    {
        RegisterSceneDocumentsButtons();
    }

    /// <summary>
    /// Opens the existing authored viewer panel. This can also be assigned in
    /// the Inspector to a button's On Click event if a later experiment uses a
    /// differently named document control.
    /// </summary>
    public void OpenPdfViewer()
    {
        if (PanelToShow != null)
            PanelToShow.SetActive(true);

        if (PDFViewerBeh != null && !PDFViewerBeh.gameObject.activeSelf)
            PDFViewerBeh.gameObject.SetActive(true);
    }

    internal static void RegisterSceneDocumentsButtons()
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (button == null ||
                !button.gameObject.scene.IsValid() ||
                !IsDocumentsButton(button.transform) ||
                !documentButtons.Add(button))
            {
                continue;
            }

            // Add instead of replacing: the button's normal Unity transition
            // and any authored click/animation events continue to run.
            button.onClick.AddListener(OpenAnyPdfViewer);
        }
    }

    private static void OpenAnyPdfViewer()
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

        // Fallback for a viewer whose old listener object was removed from a
        // future UI variation. It still opens the authored viewer itself.
        foreach (PDFViewer viewer in Resources.FindObjectsOfTypeAll<PDFViewer>())
        {
            if (viewer != null && viewer.gameObject.scene.IsValid())
            {
                viewer.gameObject.SetActive(true);
                return;
            }
        }
    }

    private static bool IsDocumentsButton(Transform candidate)
    {
        if (candidate == null || candidate.name != "Documents")
            return false;

        bool isInsideRightButtons = false;
        for (Transform parent = candidate.parent;
             parent != null;
             parent = parent.parent)
        {
            if (parent.name == "RightButtons")
                isInsideRightButtons = true;

            // The document panel must only be opened by an experiment card,
            // not another UI control which happens to use the same label.
            if (isInsideRightButtons &&
                (parent.name.StartsWith("Exp.") ||
                 parent.name.StartsWith("Experiment")))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// The viewer panel starts inactive, so its listener cannot be relied upon to
/// register UI buttons before the first click. This runtime-only helper binds
/// the existing Documents buttons after each scene loads.
/// </summary>
internal sealed class PDFDocumentsButtonBootstrap : MonoBehaviour
{
    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        PDFViewerEventListener.RegisterSceneDocumentsButtons();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PDFViewerEventListener.RegisterSceneDocumentsButtons();
    }
}
