using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Obvious.Soap;
using System.Collections;
using Bedo.UI_V3_2;


//TODO Auto Find the audio and animator

/// <summary>
/// Listens for True/False question events and displays the panel for user interaction.
/// Uses a BoolVariable to store the user's answer correctness.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class TrueFalseQuestionPanel : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If false, component references will be found at runtime via hierarchy paths.")]
    [SerializeField] private bool useCustomResources = false;
    [SerializeField] private bool sharpAnimations = false;

    [Header("Resources")]
    [SerializeField] private Transform questionWindow;
    [SerializeField] private TextMeshProUGUI questionMessage;
    [SerializeField] private Button trueButton;
    [SerializeField] private Button falseButton;

    [SerializeField] Animator mWindowAnimator;

    [Header("Events & Data")]
    [SerializeField] BoolVariable IsTrueFalseQuestionPanelShowen;

    [Tooltip("Event raised and listened to for question flow. Trigger this event with a question SO to display it.")]
    [SerializeField] private ScriptableEventTrueAndFalseQuestionScriptableObject OnTrueFalseQuestionChosen;

    [Tooltip("BoolVariable that will store whether the user's answer was correct.")]
    [SerializeField] private ScriptableEventBool answerResult;


    [Header("Custom SFX")]
    public AudioClip CorrectAsnwerSFX;
    public AudioClip WrongAnswerSFX;
    public AudioSource audioObject;


    private TrueAndFalseQuestionScriptableObject currentQuestion;


    [Space(10)]
    [Header("Localiztion")]
    [SerializeField] private BoolReference IsUsingLocaliztion = new BoolReference(false,true);
    [SerializeField] private ScriptableEnumExperimentVariable CurrentScriptableEnumExperimentVariable;
    private void OnEnable()
    {
        if (!useCustomResources)
            FindReferences();

        // Listen for activation via the same event
        OnTrueFalseQuestionChosen.OnRaised += HandleQuestionActivated;

        IsTrueFalseQuestionPanelShowen.OnValueChanged += IsTrueFalseQuestionPanelShowen_OnValueChanged;

        // Hook up button callbacks once when panel is enabled
        trueButton.onClick.RemoveAllListeners();
        trueButton.onClick.AddListener(() => OnAnswerSelected(true, trueButton));
        falseButton.onClick.RemoveAllListeners();
        falseButton.onClick.AddListener(() => OnAnswerSelected(false, falseButton));
    }

    private void IsTrueFalseQuestionPanelShowen_OnValueChanged(bool val)
    {
        if (val)
        {
            ShowPanel();
            ModalWindowIn();
        }
        else
        {
            HideHighlightWrong(trueButton);
            HideHighlightWrong(falseButton);
            ModalWindowOut();
        }
    }

    private void OnDisable()
    {
        // Stop listening for activation
        OnTrueFalseQuestionChosen.OnRaised -= HandleQuestionActivated;

        IsTrueFalseQuestionPanelShowen.OnValueChanged -= IsTrueFalseQuestionPanelShowen_OnValueChanged;

        // Clean up button listeners
        trueButton.onClick.RemoveAllListeners();
        falseButton.onClick.RemoveAllListeners();
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
            Debug.LogError("TrueFalseQuestionPanel: 'Question window' not found.");
            return;
        }

        var msg = questionWindow.Find("Question Message");
        if (msg != null)
            questionMessage = msg.GetComponent<TextMeshProUGUI>();

        var buttonsRoot = questionWindow.Find("Buttons");
        if (buttonsRoot != null)
        {
            var t = buttonsRoot.Find("True Button");
            if (t != null) trueButton = t.GetComponent<Button>();

            var f = buttonsRoot.Find("False Button");
            if (f != null) falseButton = f.GetComponent<Button>();
        }
    }

    /// <summary>
    /// Handler called when event is raised with a question. Shows panel for the question.
    /// </summary>
    private void HandleQuestionActivated(TrueAndFalseQuestionScriptableObject question)
    {
        currentQuestion = question;
        IsTrueFalseQuestionPanelShowen.Value = true;
        //ShowPanel();
    }

    /// <summary>
    /// Displays the question window and sets the question text.
    /// </summary>
    private void ShowPanel()
    {
        if (currentQuestion == null)
            return;

        if(CurrentScriptableEnumExperimentVariable == null || !IsUsingLocaliztion)
        {
            questionMessage.text = currentQuestion.QuestionText;
        }
        else
        {
            questionMessage.text = CurrentScriptableEnumExperimentVariable.Value.GetKeyOutput(currentQuestion.QuestionText);
        }
    }

    /// <summary>
    /// Called when the user selects True or False. Stores the result in a BoolVariable, then hides window.
    /// </summary>
    private void OnAnswerSelected(bool answer, Button buttonPressed)
    {
        bool correct = currentQuestion.IsAnsweredCorrectly(answer);
        // Store result in BoolVariable
        if (correct)
        {
            if (audioObject != null)
            {
                audioObject.PlayOneShot(CorrectAsnwerSFX);
            }
            IsTrueFalseQuestionPanelShowen.Value = false;

            currentQuestion = null;
        }
        else
        {
            ShowHighlightWrong(buttonPressed);
            if (audioObject!= null)
            {
                audioObject.PlayOneShot(WrongAnswerSFX);
            }

        }

        if (answerResult != null)
            answerResult.Raise(correct);
        else
            Debug.LogWarning("TrueFalseQuestionPanel: answerResult BoolVariable is not assigned.");


    }

    public void ShowHighlightWrong(Button buttonPressed)
    {
        Transform t = buttonPressed.transform.Find("Normal/Glow Outline Wrong");
        t.gameObject.SetActive(true);
    }
    public void HideHighlightWrong(Button buttonPressed)
    {
        Transform t = buttonPressed.transform.Find("Normal/Glow Outline Wrong");
        t.gameObject.SetActive(false);
    }

    public void ModalWindowIn()
    {
        StopCoroutine(DisableWindow());
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
        StartCoroutine(DisableWindow());
    }

    IEnumerator DisableWindow()
    {
        yield return new WaitForSeconds(0.5f);
        questionWindow.gameObject.SetActive(false);
    }
}
