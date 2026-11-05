using UnityEngine;
using UnityEngine.UI;

public class ButtonSelection : MonoBehaviour
{
    private static ButtonSelection currentSelected;

    [Header("Selection Object")]
    [SerializeField] private GameObject selectedObject;


    private void Awake()
    {
        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(SelectButton);
        }


        if (selectedObject != null)
        {
            selectedObject.SetActive(false);
        }
    }



    public void SelectButton()
    {
        SelectSpecific(this);
    }



    public static void SelectSpecific(ButtonSelection button)
    {
        if (currentSelected != null &&
            currentSelected != button)
        {
            currentSelected.Deselect();
        }


        currentSelected = button;


        if (currentSelected != null)
        {
            currentSelected.Select();
        }
    }



    private void Select()
    {
        if (selectedObject != null)
            selectedObject.SetActive(true);
    }



    public void Deselect()
    {
        if (selectedObject != null)
            selectedObject.SetActive(false);
    }
}