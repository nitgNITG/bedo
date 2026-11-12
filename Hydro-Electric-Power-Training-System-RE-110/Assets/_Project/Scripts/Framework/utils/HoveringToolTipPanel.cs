using UnityEngine;
using TMPro;

public class HoveringToolTipPanel : MonoBehaviour
{
    public static HoveringToolTipPanel Instance;


    [Header("References")]
    [SerializeField] private RectTransform tooltipWindow;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;


    [Header("Offset")]
    [SerializeField] private Vector2 mouseOffset = new Vector2(10f, -10f);


    private bool showing;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        tooltipWindow.gameObject.SetActive(false);
        showing = false;
    }


    private void Update()
    {
        if (!showing || tooltipWindow == null)
            return;

        tooltipWindow.position =
            Input.mousePosition + (Vector3)mouseOffset;
    }



    public void Show(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;

        showing = true;

        tooltipWindow.gameObject.SetActive(true);

        tooltipWindow.position =
            Input.mousePosition + (Vector3)mouseOffset;
    }



    public void Hide()
    {
        showing = false;

        if (tooltipWindow != null)
            tooltipWindow.gameObject.SetActive(false);
    }
}