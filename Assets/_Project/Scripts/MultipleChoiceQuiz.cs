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

        StartCoroutine(answer.correct
            ? CorrectAnswerRoutine(question)
            : IncorrectAnswerRoutine(question));
    }


    private IEnumerator CorrectAnswerRoutine(Question question)
    {
        waitingForNext = true;

        if (question.correctFeedbackObject != null)
            question.correctFeedbackObject.SetActive(true);

        yield return new WaitForSeconds(nextQuestionDelay);

        question.onCorrectAnswer?.Invoke();

        if (question.correctFeedbackObject != null)
            question.correctFeedbackObject.SetActive(false);

        if (questions[currentQuestion].questionObject != null)
            questions[currentQuestion].questionObject.SetActive(false);

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

        if (question.incorrectFeedbackObject != null)
            question.incorrectFeedbackObject.SetActive(true);
        if (question.incorrectFeedbackObject2 != null)
            question.incorrectFeedbackObject2.SetActive(true);

        yield return new WaitForSeconds(nextQuestionDelay);

        question.onIncorrectAnswer?.Invoke();

        if (question.incorrectFeedbackObject != null)
            question.incorrectFeedbackObject.SetActive(false);
        if (question.incorrectFeedbackObject2 != null)
            question.incorrectFeedbackObject2.SetActive(false);

        // Keep the current question visible so the learner can try again.
        waitingForNext = false;
    }
}
