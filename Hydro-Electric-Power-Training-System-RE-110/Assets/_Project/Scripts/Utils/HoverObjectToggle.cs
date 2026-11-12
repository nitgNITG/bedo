using UnityEngine;

public class HoverObjectToggle : MonoBehaviour
{
    [Header("Object to Toggle")]
    [SerializeField] private GameObject objectToToggle;

    private void Start()
    {
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(false);
        }
    }

    private void OnMouseEnter()
    {
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(false);
        }
    }
}