using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MultipleChoiceQuiz : MonoBehaviour
{
    [System.Serializable]
    public class Answer
    {
        public Button button;
        public bool correct;
        public GameObject correctHighlight;
        public GameObject incorrectHighlight;
    }

    [System.Serializable]
    public class Question
    {
        public GameObject questionObject;
        public List<Answer> answers = new List<Answer>();
        public GameObject correctFeedbackObject;
        public UnityEvent onCorrectAnswer;
        public GameObject incorrectFeedbackObject;
        public GameObject incorrectFeedbackObject2;
        public UnityEvent onIncorrectAnswer;
    }

    [Header("Questions")]
    public List<Question> questions = new List<Question>();

    [Header("Settings")]
    [SerializeField] private float nextQuestionDelay = 1.5f;

    private int currentQuestion = 0;
    private bool waitingForNext = false;

    private void Start()
    {
        // Hide all questions
        foreach (Question q in questions)
        {
            ResetAnswerHighlights(q);
            if (q.questionObject != null)
                q.questionObject.SetActive(false);
            if (q.correctFeedbackObject != null)
                q.correctFeedbackObject.SetActive(false);
            if (q.incorrectFeedbackObject != null)
                q.incorrectFeedbackObject.SetActive(false);
            if (q.incorrectFeedbackObject2 != null)
                q.incorrectFeedbackObject2.SetActive(false);
        }

        if (questions.Count > 0 && questions[0].questionObject != null)
            questions[0].questionObject.SetActive(true);

        SetupButtons();
    }

    private void SetupButtons()
    {
        for (int i = 0; i < questions.Count; i++)
        {
            int questionIndex = i;

            foreach (Answer answer in questions[i].answers)
            {
                if (answer.button == null)
                    continue;

                Answer localAnswer = answer;

                answer.button.onClick.RemoveAllListeners();
                answer.button.onClick.AddListener(() =>
                {
                    OnAnswerClicked(questionIndex, localAnswer);
                });
            }
        }
    }

    private void OnAnswerClicked(int questionIndex, Answer answer)
    {
        if (waitingForNext)
            return;

        if (questionIndex != currentQuestion)
            return;

        Question question = questions[currentQuestion];
        ResetAnswerHighlights(question);
        SetAnswerHighlight(answer, answer.correct);
        string trackingId = TutorialFramework.HybridStudentTracking.QuizQuestionId(
            TutorialFramework.HybridStudentTracking.CurrentStepNumber, questionIndex + 1);
        TutorialFramework.HybridStudentTracking.AnswerQuestion(
            trackingId, "MultipleChoice", question.answers.IndexOf(answer).ToString(), answer.correct);

        StartCoroutine(answer.correct
            ? CorrectAnswerRoutine(question)
            : IncorrectAnswerRoutine(question));
    }


    private static void ResetAnswerHighlights(Question question)
    {
        if (question.correctFeedbackObject) question.correctFeedbackObject.SetActive(false);
        if (question.incorrectFeedbackObject) question.incorrectFeedbackObject.SetActive(false);
        if (question.incorrectFeedbackObject2) question.incorrectFeedbackObject2.SetActive(false);
        foreach (var answer in question.answers)
        {
            if (answer.correctHighlight) answer.correctHighlight.SetActive(false);
            if (answer.incorrectHighlight) answer.incorrectHighlight.SetActive(false);
        }
    }

    private static void SetAnswerHighlight(Answer answer, bool correct)
    {
        var selected = correct ? answer.correctHighlight : answer.incorrectHighlight;
        if (selected) selected.SetActive(true);
    }

    private IEnumerator CorrectAnswerRoutine(Question question)
    {
        waitingForNext = true;

        // The selected answer owns its green/red highlight.
        // Legacy shared feedback must not highlight a different answer.

        yield return new WaitForSeconds(nextQuestionDelay);

        question.onCorrectAnswer?.Invoke();

        if (question.correctFeedbackObject != null)
            question.correctFeedbackObject.SetActive(false);

        if (questions[currentQuestion].questionObject != null)
            questions[currentQuestion].questionObject.SetActive(false);

        ResetAnswerHighlights(question);
        currentQuestion++;

        if (currentQuestion < questions.Count)
        {
            if (questions[currentQuestion].questionObject != null)
                questions[currentQuestion].questionObject.SetActive(true);
        }

        waitingForNext = false;
    }

    private IEnumerator IncorrectAnswerRoutine(Question question)
    {
        waitingForNext = true;

        // Keep only the clicked wrong answer highlighted during the retry delay.

        yield return new WaitForSeconds(nextQuestionDelay);

        question.onIncorrectAnswer?.Invoke();

        if (question.incorrectFeedbackObject != null)
            question.incorrectFeedbackObject.SetActive(false);
        if (question.incorrectFeedbackObject2 != null)
            question.incorrectFeedbackObject2.SetActive(false);

        ResetAnswerHighlights(question);
        // Keep the current question visible so the learner can try again.
        waitingForNext = false;
    }
}
