using UnityEngine;

[CreateAssetMenu(fileName = "Multiple Choice Quesiton", menuName = "Bedo/UI/Multiple Choice Quesiton")]
public class MultipleChoiceQuestionScriptableObject : ScriptableObject
{
    [Header("Question Data")]
    [Tooltip("The question text to display.")]
    [SerializeField] private string questionText;

    [Tooltip("Array of choice texts.")]
    [SerializeField] private string[] choices;

    [Tooltip("Index of the correct choice in the 'choices' array.")]
    [SerializeField] private int correctChoiceIndex;

    /// <summary>The question text.</summary>
    public string QuestionText => questionText;
    /// <summary>The available choice texts.</summary>
    public string[] Choices => choices;
    /// <summary>The index of the correct choice.</summary>
    public int CorrectChoiceIndex => correctChoiceIndex;

    /// <summary>Checks if the selected index is the correct answer.</summary>
    public bool IsAnsweredCorrectly(int selectedIndex)
    {
        return selectedIndex == correctChoiceIndex;
    }
}