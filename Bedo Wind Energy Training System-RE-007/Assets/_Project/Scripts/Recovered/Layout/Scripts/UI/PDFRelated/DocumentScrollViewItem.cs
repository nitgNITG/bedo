using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;


public class DocumentScrollViewItem : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("UI Elements")]
    [SerializeField] Button DocumentOpenButton;
    [SerializeField] TextMeshProUGUI FileNameTMP;
    [SerializeField] GameObject HighlightImage;

    private string _FileName;
    private DocumentScorllView _ScorllView;
    public string FileName { get { return _FileName; } }
    public bool IsHighlighted { get { return HighlightImage.active; } }
  
    private void OnEnable()
    {
        if(DocumentOpenButton == null)
        {
            DocumentOpenButton =  GetComponent<Button>();
        }

        if(FileNameTMP == null)
        {
            FileNameTMP = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
        }
       

        DocumentOpenButton.onClick.AddListener(OnDocumentButtonPressed);

    }

    private void OnDisable()
    {
        DocumentOpenButton.onClick.RemoveListener(OnDocumentButtonPressed);
    }

    public void InitItem(DocumentScorllView parentScrollView,string fileName)
    {
        _ScorllView = parentScrollView;
        _FileName = fileName;
        if(FileNameTMP == null)
        {
            FileNameTMP = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
        }
        FileNameTMP.text = _FileName;
    }

    public void OnDocumentButtonPressed()
    {
        _ScorllView.OpenDocument(_FileName);
    }

    public void HighlightDocument( bool isHighlighted)
    {
        HighlightImage.SetActive(isHighlighted);
    }
    //public void UpdateVisuals()
    //{

    //}
}
