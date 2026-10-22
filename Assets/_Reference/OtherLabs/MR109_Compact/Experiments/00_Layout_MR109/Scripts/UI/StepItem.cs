using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI IndexTMP;
    [SerializeField] Image NotifyIcon;
    [SerializeField] TextMeshProUGUI DescriptionTMP;
    public void InitItem(string indexStr, string descriptionStr)
    {
        if (indexStr == "*")
        {
            NotifyIcon.gameObject.SetActive(true);
            IndexTMP.gameObject.SetActive(false);
        }
        else
        {
            NotifyIcon.gameObject.SetActive(false);
            
            IndexTMP.gameObject.SetActive(true);

            IndexTMP.text = indexStr;
        }

        DescriptionTMP.text = descriptionStr;
    }


}
