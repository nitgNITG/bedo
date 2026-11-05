using UnityEngine;

[CreateAssetMenu(fileName = "TrueAndFalseQuestion", menuName = "Bedo/UI/True And False Question")]
public class TrueAndFalseQuestionScriptableObject : ScriptableObject
{
    [Header("Question Data")]
    [Tooltip("The question text to display.")]
    [SerializeField] private string questionText;

    [Tooltip("Whether the correct answer is 'True'.")]
    [SerializeField] private bool isAnswerTrue;

    /// <summary>
    /// The question text.
    /// </summary>
    public string QuestionText => questionText;

    /// <summary>
    /// The correct boolean answer.
    /// </summary>
    public bool IsAnswerTrue => isAnswerTrue;

    /// <summary>
    /// Determines if the provided answer matches the correct one.
    /// </summary>
    /// <param name="givenAnswer">The answer to check.</param>
    /// <returns>True if the answer is correct; otherwise false.</returns>
    public bool IsAnsweredCorrectly(bool givenAnswer)
    {
        return givenAnswer == isAnswerTrue;
    }
}