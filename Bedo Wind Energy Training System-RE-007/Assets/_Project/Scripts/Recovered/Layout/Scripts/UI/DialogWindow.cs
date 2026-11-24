using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class DialogWindow : MonoBehaviour
{
    [Header("Soap Elements")]
    [SerializeField] ScriptableEventDialogRequest DialogRequestScriptableEvent;
    [SerializeField] BoolVariable IsDialogWindowOpened;
    [SerializeField] BoolVariable IsLoadingScreenOn;

    [Space(5)]

    [Header("UI Elements")]
    [SerializeField] GameObject DialogWindowGO;
    [SerializeField] TextMeshProUGUI MessageText;
    [SerializeField] Button OkButton;
    [SerializeField] Button YesButton;
    [SerializeField] Button NoButton;
    [SerializeField] Button CancelButton;


    [SerializeField] UnityEvent OnCancelUnityEvent =new UnityEvent();

    private void Awake()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        MessageText.text = "Resetting the experiment will return it to its initial state.";


    }

    private void OnEnable()
    {
        DialogRequestScriptableEvent.OnRaised += DialogRequestScriptableEvent_OnRaised;

        IsDialogWindowOpened.OnValueChanged += IsDialogWindowOpened_OnValueChanged;
        if (OkButton != null)
        {
            OkButton.onClick.AddListener(OnOkPressed);
        }
        CancelButton.onClick.AddListener(OnCancelPressed);
    }

    private void IsDialogWindowOpened_OnValueChanged(bool val)
    {
        DialogWindowGO.SetActive(val);
    }

    private void OnDisable()
    {
        DialogRequestScriptableEvent.OnRaised -= DialogRequestScriptableEvent_OnRaised;
        IsDialogWindowOpened.OnValueChanged -= IsDialogWindowOpened_OnValueChanged;
        if (OkButton != null)
        {
            OkButton.onClick.RemoveListener(OnOkPressed);
        }
        CancelButton.onClick.RemoveListener(OnCancelPressed);
    }


    private void DialogRequestScriptableEvent_OnRaised(DialogRequest obj) => Show(obj);


    public void Show(DialogRequest req)
    {

        //OkButton.gameObject.SetActive(true);
        //CancelButton.gameObject.SetActive(true);

        //s

        //Configure(OkButton, req.ShowOk, req.OnOk);
        //Configure(YesButton, req.ShowYes, req.OnYes);
        //Configure(NoButton, req.ShowNo, req.OnNo);
        //Configure(CancelButton, req.ShowCancel, req.OnCancel);

        IsDialogWindowOpened.Value = true;

    }

    void Configure(Button btn, bool visible, UnityEvent callback)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(visible);
        btn.onClick.RemoveAllListeners();

        if (visible && callback != null)
        {
            // invoke the UnityEvent when the UI button is clicked
            btn.onClick.AddListener(() =>
            {
                callback.Invoke();
                //IsDialogWindowOpened.Value = false;  // auto-close after firing
            });
        }
    }


    public void OnOkPressed()
    {

        //yield return new WaitForEndOfFrame();
        StartCoroutine(PushPullEmergencyAndReset());
    }
    public IEnumerator PushPullEmergencyAndReset()
    {
        IsLoadingScreenOn.Value = true;

        //foreach (BoolVariable variable in EmergencyVariables)
        //{
        //    variable.Value = true;
        //}



        yield return new WaitForSeconds(1f);

        //foreach (BoolVariable variable in EmergencyVariables)
        //{
        //    variable.Value = false;
        //}

        //yield return new WaitForEndOfFrame();


        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void OnCancelPressed()
    {
        IsDialogWindowOpened.Value = false;
        OnCancelUnityEvent?.Invoke();
    }

}
