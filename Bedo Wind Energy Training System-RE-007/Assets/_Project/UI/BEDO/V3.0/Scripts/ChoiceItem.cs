using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Obvious.Soap;

/// <summary>
/// Represents a UI choice item: text, button, and outline for wrong answer.
/// </summary>
public class ChoiceItem : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI choiceText;
    [SerializeField] private Button choiceButton;
    [SerializeField] private GameObject wrongAnswerOutline;

    /// <summary>The button component to listen for clicks.</summary>
    public Button ChoiceButton => choiceButton;

    /// <summary>The text component to set the choice label.</summary>
    public TextMeshProUGUI ChoiceText => choiceText;

    /// <summary>Show or hide the outline indicating a wrong answer.</summary>
    public void SetWrongOutline(bool visible)
    {
        if (wrongAnswerOutline != null)
            wrongAnswerOutline.SetActive(visible);
    }
}