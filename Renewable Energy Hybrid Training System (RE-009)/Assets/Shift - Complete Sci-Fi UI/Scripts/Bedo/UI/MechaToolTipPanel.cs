using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Obvious.Soap;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class MechaToolTipPanel : MonoBehaviour
{

    [Header("Settings")]
    public bool useCustomResources = false;
    public bool sharpAnimations = false;


    [Header("UI Elements")]
    [SerializeField] RectTransform PanelToShow;
    [SerializeField] TextMeshProUGUI TitleTMP;
    [SerializeField] TextMeshProUGUI PinsDescriptionTMP;
    [SerializeField] TextMeshProUGUI BodyDetailsTMP;
    [SerializeField] Image ComponentImage;

    [SerializeField] Animator mWindowAnimator;
    

    [Header("Soap Elements")]
    [SerializeField] BoolVariable IsMechaToolTipShowen;
    [SerializeField] GameObjectVariable MechaTooltipGameObjectOwner;

    const string IsMechaToolTipShowenboolVariableResourceName = "IsShowComponentDescription_BoolVariable";
    const string MechaTooltipGameObjectOwnerboolVariableResourceName = "ComponentDescription_GameObjectVariable";
    //[SerializeField] 
    // Start is called before the first frame update

    

    private void OnEnable()
    {
        if (useCustomResources == false)
        {
            mWindowAnimator = gameObject.GetComponent<Animator>();
            PanelToShow = gameObject.transform.Find("Description_Content").GetComponent<RectTransform>();
            ComponentImage = gameObject.transform.Find("Description_Content/Content/Device_Image")  .GetComponent<Image>();
            TitleTMP = gameObject.transform.Find("Description_Content/Content/Title_TMP").GetComponent<TextMeshProUGUI>();
            PinsDescriptionTMP = gameObject.transform.Find("Description_Content/Content/Pins_Description_TMP/ScrollView/Viewport/Content").GetComponent<TextMeshProUGUI>();
            BodyDetailsTMP = gameObject.transform.Find("Description_Content/Content/Body/ScrollView/Viewport/Content").GetComponent<TextMeshProUGUI>();
        }
        if (MechaTooltipGameObjectOwner == null)
        {
            //var gameobjectVariables = ScriptableObjectFinder.FindAllScriptableObjects<GameObjectVariable>();
            //MechaTooltipGameObjectOwner = gameobjectVariables.Where(obj => obj.name.Equals("ComponentDescription_GameObjectVariable")).ToArray()[0];
            MechaTooltipGameObjectOwner = Resources.Load<GameObjectVariable>(MechaTooltipGameObjectOwnerboolVariableResourceName);
        }

        if (IsMechaToolTipShowen == null)
        {
            //var gameobjectBool = ScriptableObjectFinder.FindAllScriptableObjects<BoolVariable>();
            //IsMechaToolTipShowen = gameobjectBool.Where(obj => obj.name.Equals("IsShowComponentDescription_BoolVariable")).ToArray()[0];
            IsMechaToolTipShowen = Resources.Load<BoolVariable>(IsMechaToolTipShowenboolVariableResourceName);
        }
        IsMechaToolTipShowen.OnValueChanged += IsMechaToolTipShowen_OnValueChanged;
        MechaTooltipGameObjectOwner.OnValueChanged += MechaTooltipGameObjectOwner_OnValueChanged;
    }

    private void Start()
    {
        
        IsMechaToolTipShowen_OnValueChanged(false);
    }


    private void OnDisable()
    {
        IsMechaToolTipShowen.OnValueChanged -= IsMechaToolTipShowen_OnValueChanged;
        MechaTooltipGameObjectOwner.OnValueChanged -= MechaTooltipGameObjectOwner_OnValueChanged;
    }

    private void MechaTooltipGameObjectOwner_OnValueChanged(GameObject obj)
    {
        MechaTooltip MTT = obj.GetComponent<MechaTooltip>();

        TitleTMP.text = MTT.GetTitle();
        PinsDescriptionTMP.text = MTT.GetPinsDescription();
        BodyDetailsTMP.text = MTT.GetBodyDetails();

        ComponentImage.sprite = MTT.GetSprite();
        if (ComponentImage.sprite == null)
        {
            ComponentImage.color = new Color(ComponentImage.color.r, ComponentImage.color.g, ComponentImage.color.b, 0);
        }
        else
        {
            ComponentImage.color = new Color(ComponentImage.color.r, ComponentImage.color.g, ComponentImage.color.b, 1);
            ComponentImage.type = Image.Type.Simple;
            ComponentImage.preserveAspect = true;
        }
    }
        

    private void IsMechaToolTipShowen_OnValueChanged(bool obj)
    {
        if (obj)
        {
            ModalWindowIn();
        }
        else
        {
            ModalWindowOut();
        }
    }


    //bool isOn = false;

    //void Start()
    //{
    //    mWindowAnimator = gameObject.GetComponent<Animator>();

    //    if (useCustomTexts == false)
    //    {
    //        windowTitle.text = titleText;
    //        windowDescription.text = descriptionText;
    //    }

    //    gameObject.SetActive(false);
    //}

    public void ModalWindowIn()
    {
        StopCoroutine("DisableWindow");
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
        StartCoroutine("DisableWindow");
    }

    IEnumerator DisableWindow()
    {
        yield return new WaitForSeconds(0.5f);
        PanelToShow.gameObject.SetActive(false);
    }

}
