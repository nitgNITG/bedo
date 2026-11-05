using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Obvious.Soap;
public class SampleASampleUi : MonoBehaviour
{
    public TMP_Dropdown ColorDropdown;
    public GameObjectVariable ColorDropdownGameobjectVariable;

    public void Start()
    {
        if (ColorDropdown != null && ColorDropdownGameobjectVariable != null)
        {
            ColorDropdownGameobjectVariable.Value = ColorDropdown.gameObject;
        }
            
    }
    public void Ui_OnCloseButtonClicked()
    {

    }

    public void Ui_OnSampleHeightSlider(float val)
    {

    }

    public void Ui_OnSampleTypeDropDownChanged(int val)
    {

    }

    public void Ui_OnSampleColorDropDownChanged(int val)
    {

    }

    public void Ui_OnSampleDrillToggleChanged(bool val)
    {

    }

    public void Ui_OnAddSamplePressed()
    {

    }

    public void Ui_OnAddCupPressed()
    {

    }


}
