using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Obvious.Soap;
using Bedo.UI_V3_2;
/// <summary>
/// Panel UI for displaying a multiple-choice question and capturing the user's answer.
/// </summary>


public class MultipleChoiceQuestionPanel : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If false, component references will be found at runtime via hierarchy paths.")]
    [SerializeField] private bool useCustomResources = false;
    [SerializeField] private bool sharpAnimations = false;

    [Header("Resources")]
    [SerializeField] private Transform questionWindow;
    [SerializeField] private TextMeshProUGUI questionMessage;
    [SerializeField] private ChoiceItem[] choiceItems;

    [SerializeField] Animator mWindowAnimator;

    [Header("Events & Data")]
    [SerializeField] BoolVariable IsMultipleChoiceQuestionPanelShowen;


    [Tooltip("Event asset to trigger display of a multiple-choice question.")]
    [SerializeField] private ScriptableEventMultipleChoiceQuestionScriptableObject OnMultipleChoiceQuestionChosen;

    [Tooltip("BoolVariable that will store whether the user's answer was correct.")]
    [SerializeField] private ScriptableEventBool answerResult;

    private MultipleChoiceQuestionScriptableObject currentQuestion;


    [Header("Custom SFX")]
    public AudioClip CorrectAsnwerSFX;
    public AudioClip WrongAnswerSFX;
    public AudioSource audioObject;

    [Space(10)]
    [Header("Localiztion")]
    [SerializeField] private BoolReference IsUsingLocaliztion = new BoolReference(false, true);
    [SerializeField] private ScriptableEnumExperimentVariable CurrentScriptableEnumExperimentVariable;
    private void Awake()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        if (!useCustomResources)
            FindReferences();
    }

    private void OnEnable()
    {

        OnMultipleChoiceQuestionChosen.OnRaised += HandleQuestionActivated;
        IsMultipleChoiceQuestionPanelShowen.OnValueChanged += IsMultipleChoiceQuestionPanelShowen_OnValueChanged;
    }


    private void OnDisable()
    {
        OnMultipleChoiceQuestionChosen.OnRaised -= HandleQuestionActivated;
        IsMultipleChoiceQuestionPanelShowen.OnValueChanged -= IsMultipleChoiceQuestionPanelShowen_OnValueChanged;
    }

    /// <summary>
    /// Finds UI children by hierarchy if inspector refs are not set.
    /// </summary>
    private void FindReferences()
    {
        Transform root = transform;
        questionWindow = root.Find("Question window");
        if (questionWindow == null)
        {
            Debug.LogError("MultipleChoiceQuestionPanel: 'Question window' not found.");
            return;
        }

        var msg = questionWindow.Find("Question Message");
        if (msg != null)
            questionMessage = msg.GetComponent<TextMeshProUGUI>();

        var buttonsRoot = questionWindow.Find("Buttons");
        if (buttonsRoot != null)
        {
            int count = buttonsRoot.childCount;
            choiceItems = new ChoiceItem[count];
            for (int i = 0; i < count; i++)
            {
                var ci = buttonsRoot.GetChild(i).GetComponent<ChoiceItem>();
                if (ci != null)
                    choiceItems[i] = ci;
                else
                    Debug.LogError($"ChoiceItem component missing on child {{i}}.");
            }
        }
    }

    /// <summary>
    /// Handler called when event is raised with a question. Shows panel for the question.
    /// </summary>
    private void HandleQuestionActivated(MultipleChoiceQuestionScriptableObject question)
    {
        currentQuestion = question;
        IsMultipleChoiceQuestionPanelShowen.Value = true;
    }


    private void IsMultipleChoiceQuestionPanelShowen_OnValueChanged(bool obj)
    {
        if (obj)
        {
            ShowPanel();
            ModalWindowIn();
        }
        else
        {
            ModalWindowOut();
        }
    }


    /// <summary>
    /// Displays the question window, sets texts, and hooks button callbacks.
    /// </summary>
    private void ShowPanel()
    {
        if (currentQuestion == null)
            return;

        questionWindow.gameObject.SetActive(true);
        SetInputFieldText(questionMessage, currentQuestion.QuestionText);

        string[] choices = currentQuestion.Choices;
        int choicesCount = choices.Length;
        for (int i = 0; i < choiceItems.Length; i++)
        {
            var ci = choiceItems[i];
            if (i < choicesCount)
            {
                ci.gameObject.SetActive(true);
                SetInputFieldText(ci.ChoiceText , choices[i]);
                ci.SetWrongOutline(false);

                ci.ChoiceButton.onClick.RemoveAllListeners();
                int index = i;
                ci.ChoiceButton.onClick.AddListener(() => OnChoiceSelected(index));
            }
            else
            {
                ci.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when the user selects a choice. Stores the result and hides panel.
    /// </summary>
    private void OnChoiceSelected(int selectedIndex)
    {
        bool correct = currentQuestion.IsAnsweredCorrectly(selectedIndex);
        if (answerResult != null)
            answerResult.Raise(correct)  ;
        else
            Debug.LogWarning("MultipleChoiceQuestionPanel: answerResult BoolVariable is not assigned.");

        // Optionally show wrong outline on incorrect choices
        if (!correct && selectedIndex < choiceItems.Length)
        {
            if (audioObject != null) { audioObject.PlayOneShot(WrongAnswerSFX); }
            choiceItems[selectedIndex].SetWrongOutline(true);
        }
        else
        {
            if (audioObject != null) { audioObject.PlayOneShot(CorrectAsnwerSFX); }
            IsMultipleChoiceQuestionPanelShowen.Value = false;
            currentQuestion = null;
        }
    }

    public void ModalWindowIn()
    {
        StopCoroutine("DisableWindow");
        questionWindow.gameObject.SetActive(true);
        if (sharpAnimations == false)
            mWindowAnimator.CrossFade("Window In", 0.1f);
        else
            mWindowAnimator.Play("Window In");
    }

    public void ModalWindowOut()
    {
        if (sharpAnimations == false)
            mWindowAnimator.CrossFade("Window Out", 0.1f);
        else
            mWindowAnimator.Play("Window Out");
        StartCoroutine("DisableWindow");
    }

    IEnumerator DisableWindow()
    {
        yield return new WaitForSeconds(0.5f);
        questionWindow.gameObject.SetActive(false);
    }

    public void SetInputFieldText(TextMeshProUGUI tmp_InputField, string text)
    {
        if (CurrentScriptableEnumExperimentVariable == null || !IsUsingLocaliztion)
        {
            questionMessage.text = currentQuestion.QuestionText;
        }
        else
        {
            tmp_InputField.text = CurrentScriptableEnumExperimentVariable.Value.GetKeyOutput(text);
        }
    }
}
