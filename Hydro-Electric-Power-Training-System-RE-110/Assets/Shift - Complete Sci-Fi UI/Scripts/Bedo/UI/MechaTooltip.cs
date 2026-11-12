using Bedo.Core;
using Bedo.Core.Physics.Interaction;
//using Bedo.Core.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using System;
using System.Linq;
public class MechaTooltip : MonoBehaviour
{
    [Multiline] public string componentData = " ";
    [SerializeField] MechaTooltipScriptableObject MechaTooltipSO;
    [SerializeField] ClickableObjectOnClick clickableObjectOnClick;
    [SerializeField] bool AutoFindClickableObjectInParent;
    
    [Header("Soap Elements")]

    [SerializeField] BoolVariable IsMechaToolTipShowen;
    [SerializeField] GameObjectVariable MechaTooltipGameObjectOwner;

    const string IsMechaToolTipShowenboolVariableResourceName = "IsShowComponentDescription_BoolVariable";
    const string MechaTooltipGameObjectOwnerboolVariableResourceName = "ComponentDescription_GameObjectVariable";
    //[SerializeField] 
//#if UNITY_EDITOR
    public void OnValidate()
    {

       

        

        if (!AutoFindClickableObjectInParent) return;

        // already assigned?  nothing to do
        if (clickableObjectOnClick != null) return;

        /*  true  ⇒ also search disabled objects
         *  false ⇒ skip inactive parents
         */
        clickableObjectOnClick = GetComponentInParent<ClickableObjectOnClick>(true);


       

    }


    private void Start()
    {
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

        if (MechaTooltipSO == null) { Debug.LogWarning(transform.parent.name + " is missing the MechaTooltipSO"); }
    }
    //#endif
    //private void OnEnable()
    //{
    //    //clickableObjectOnClick.ActionOnClick += clickableObjectOnClick_ActionOnClick;
    //}




    //private void OnDisable()
    //{
    //    //clickableObjectOnClick.ActionOnClick -= clickableObjectOnClick_ActionOnClick;
    //}

    public void clickableObjectOnClick_ActionOnClick()
    {
        if(MechaTooltipGameObjectOwner == null)
        {
            Debug.Log("MechaTooltipGameObjectOwner is null");
        }

        if (IsMechaToolTipShowen == null)
        {
            Debug.Log("IsMechaToolTipShowen is null");
        }
        MechaTooltipGameObjectOwner.Value = gameObject;
        IsMechaToolTipShowen.Value = true;
    }

    public String GetTitle()
    {
        return TagHelper.GetTitle(MechaTooltipSO.EnglishDesciptionStringVariable.Value, TagHelper.TitleTag);
    }

    public String GetPinsDescription()
    {
        return componentData;
    }

    public String GetBodyDetails()
    {
        return TagHelper.GetSlideWithoutTitle(MechaTooltipSO.EnglishDesciptionStringVariable.Value, TagHelper.TitleTag); ;
    }

    public Sprite GetSprite()
    {
        return MechaTooltipSO.Sprite;
    }

}
