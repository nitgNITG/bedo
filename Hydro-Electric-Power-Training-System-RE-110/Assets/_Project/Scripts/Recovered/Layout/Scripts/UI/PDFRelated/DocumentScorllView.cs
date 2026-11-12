//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using System.Linq;
using System.Collections;
//using Paroxe.PdfRenderer;

public class DocumentScorllView : MonoBehaviour
{

    [Header("Settings")]
    public bool useCustomResources = false;
    public bool sharpAnimations = false;

    [Header("UI Elements")]
    [SerializeField] Animator mWindowAnimator;
    [SerializeField] RectTransform ScrollContent;
    [SerializeField] RectTransform PanelToShow;

    [Header("Soap Elements")]
    [SerializeField] StringVariable DocumentFileName_StringVariable;
    [SerializeField] BoolVariable IsOpenDocument_BoolVariable;
    [SerializeField] BoolVariable IsDocumentScrollView_BoolVariable;
    [SerializeField] ScriptableEventScriptableListString OnDocumentsChoosenChanged;
    [SerializeField] ScriptableEventString OnDocumentOpened;

    [Header("Prefabs")]
    [SerializeField] GameObject DocumentScrollViewItemGameObject;

    const string IsPDFOpene_BoolVariableResourceName = "IsPDFOpened_BoolVariable";
    const string IsDocumentsPanelOpened_BoolVariableResourceName = "IsShowDocumentsScrollView_BoolVariable";
    const string PDFFileName_StringVariablResourceName = "PDFFileName_StringVariable";
    const string DocumentScrollViewItem_StringResourceName = "Document Item";

    List<string> DocumentsNamesToShowAsItems = new List<string>();
    //List<DocumentScrollViewItem> DocumentScrollViewItems = new List<DocumentScrollViewItem>();

    //PDFViewer MainPDFViewer;

    Coroutine DisableWindowCoro;

    private void OnEnable()
    {
       


        if (DocumentFileName_StringVariable == null)
        {
            DocumentFileName_StringVariable = Resources.Load<StringVariable>(PDFFileName_StringVariablResourceName);
        }

        if (IsOpenDocument_BoolVariable == null)
        {
            IsOpenDocument_BoolVariable = Resources.Load<BoolVariable>(IsPDFOpene_BoolVariableResourceName);
        }

        if(DocumentScrollViewItemGameObject == null)
        {
            DocumentScrollViewItemGameObject = Resources.Load<GameObject>(DocumentScrollViewItem_StringResourceName);
        }

        if(IsDocumentScrollView_BoolVariable == null)
        {
            IsDocumentScrollView_BoolVariable = Resources.Load<BoolVariable>(IsDocumentsPanelOpened_BoolVariableResourceName);
        }
        //if(MainPDFViewer == null)
        //{
        //    MainPDFViewer = Object.FindAnyObjectByType<PDFViewer>(FindObjectsInactive.Include); ;
        //}
        OnDocumentsChoosenChanged.OnRaised += UpdateScrollList;
        IsDocumentScrollView_BoolVariable.OnValueChanged += IsDocumentScrollView_BoolVariable_OnValueChanged;
    }


    public void Start()
    {
        DisableWindowCoro = null;

        if (useCustomResources == false)
        {
            mWindowAnimator = gameObject.GetComponent<Animator>();
            //ScrollContent = gameObject.
            PanelToShow = gameObject.transform.Find("ScrollView").GetComponent<RectTransform>();
            ScrollContent = gameObject.transform.Find("ScrollView/Viewport/Content").GetComponent<RectTransform>();
            //TitleTMP = gameObject.transform.Find("Description_Content/Content/Title_TMP").GetComponent<TextMeshProUGUI>();
            //PinsDescriptionTMP = gameObject.transform.Find("Description_Content/Content/Pins_Description_TMP/ScrollView/Viewport/Content").GetComponent<TextMeshProUGUI>();
            //BodyDetailsTMP = gameObject.transform.Find("Description_Content/Content/Body/ScrollView/Viewport/Content").GetComponent<TextMeshProUGUI>();
        }
        ModalWindowOut();
    }
    private void IsDocumentScrollView_BoolVariable_OnValueChanged(bool val)
    {
        if (val) {
            ModalWindowIn();
        }
        else {
            ModalWindowOut(); }
    }

    private void OnDisable()
    {
        OnDocumentsChoosenChanged.OnRaised -= UpdateScrollList;
        IsDocumentScrollView_BoolVariable.OnValueChanged -= IsDocumentScrollView_BoolVariable_OnValueChanged;
    }

    public void OpenDocument(string fileName)
    {
        DocumentFileName_StringVariable.Value = fileName;
        IsDocumentScrollView_BoolVariable.Value = false;
        IsOpenDocument_BoolVariable.Value = true;
       if(OnDocumentOpened!=null) OnDocumentOpened.Raise(fileName);
    }


    public void UpdateScrollList(List<string> fileNameList)
    {
        DocumentsNamesToShowAsItems = fileNameList;

        foreach (RectTransform rectTransform in ScrollContent)
        {
           Destroy( rectTransform.gameObject );
        }

        foreach(string fileName in fileNameList)
        {
            GameObject go = Instantiate(DocumentScrollViewItemGameObject, ScrollContent);
            var dsvgo = go.GetComponent<DocumentScrollViewItem>();
            dsvgo.InitItem(this, fileName);
        }
        IsDocumentScrollView_BoolVariable.Value = true;    
    }

    public void UpdateScrollList(ScriptableListString documentScriptableList)
    {
        UpdateScrollList(documentScriptableList.ToArray<string>().ToList<string>());
    }


    public void ModalWindowIn()
    {
       
        if (DisableWindowCoro != null )
        {
            Debug.Log(transform.name);
            
            StopCoroutine("DisableWindow");
            //DisableWindowCoro = null;
        }

        PanelToShow.gameObject.SetActive(true);
        if (sharpAnimations == false)
            mWindowAnimator.CrossFade("Window In", 0.1f);
        else
            mWindowAnimator.Play("Window In");
    }

    public void ModalWindowOut()
    {
        if (sharpAnimations == false)
            mWindowAnimator.CrossFade("Window Out", 0.1f);
        else
            mWindowAnimator.Play("Window Out");
        DisableWindowCoro = StartCoroutine("DisableWindow");
    }

    IEnumerator DisableWindow()
    {
        yield return new WaitForSeconds(0.5f);
        PanelToShow.gameObject.SetActive(false);
    }

    public void HighlightItemWithIndex(int index)
    {
        if (index < ScrollContent.childCount)
        {
           Transform itemToHighlight =  ScrollContent.GetChild(index);
            DocumentScrollViewItem dsvi = itemToHighlight.GetComponent<DocumentScrollViewItem>();
            dsvi.HighlightDocument(true);
        }
    }
    public void UnHighlightItemWithIndex(int index)
    {
        if (index < ScrollContent.childCount)
        {
            Transform itemToHighlight = ScrollContent.GetChild(index);
            DocumentScrollViewItem dsvi = itemToHighlight.GetComponent<DocumentScrollViewItem>();
            dsvi.HighlightDocument(false);
        }
    }

    public void ToggleHighlight(int index)
    {
        if (index < ScrollContent.childCount)
        {
            Transform itemToHighlight = ScrollContent.GetChild(index);
            DocumentScrollViewItem dsvi = itemToHighlight.GetComponent<DocumentScrollViewItem>();
         
            dsvi.HighlightDocument(!dsvi.IsHighlighted);
        }
    }

  

}
