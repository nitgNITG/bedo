using System.Collections.Generic;
using UnityEngine;

public class OptionSwitcher : MonoBehaviour
{
    [Header("Option 1")]
    [SerializeField]
    private List<GameObject> option1Objects = new List<GameObject>();

    [Header("Option 2")]
    [SerializeField]
    private List<GameObject> option2Objects = new List<GameObject>();

    [Header("Startup")]
    [SerializeField]
    private bool option1ActiveAtStart = true;

    private void Start()
    {
        if (option1ActiveAtStart)
        {
            SwitchToOption1();
        }
        else
        {
            SwitchToOption2();
        }
    }

    // =========================================================
    // BUTTON 1
    // =========================================================

    public void SwitchToOption1()
    {
        SetObjectsActive(option1Objects, true);
        SetObjectsActive(option2Objects, false);
    }

    // =========================================================
    // BUTTON 2
    // =========================================================

    public void SwitchToOption2()
    {
        SetObjectsActive(option1Objects, false);
        SetObjectsActive(option2Objects, true);
    }

    // =========================================================
    // SET OBJECTS ACTIVE / INACTIVE
    // =========================================================

    private void SetObjectsActive(
        List<GameObject> objects,
        bool active)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }
}