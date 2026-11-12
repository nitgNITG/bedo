using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Obvious.Soap;

public class ButtonCooldown : MonoBehaviour
{
    [SerializeField] private Button targetButton; // Reference to the button
    [SerializeField] private float cooldownTime = 5f; // Cooldown time in seconds

    [SerializeField] private BoolVariable[] ArrOfOtherConditions;

    private float lastCooldownTime = -Mathf.Infinity;

    [SerializeField] private float cooldown_afterAllConditionsTrue = 0f;

    private void Start()
    {
        // If the targetButton is not assigned, try to find the Button component on the same GameObject
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();

            if (targetButton == null)
            {
                Debug.LogError("Button component not found on this GameObject. Please assign it in the Inspector.");
                return; // Exit if no Button is found
            }
        }


        foreach (BoolVariable otherCondition in ArrOfOtherConditions)
        {
            otherCondition.OnValueChanged += OnOneOfConditionsChanged;
        }
        // Add listener to the button's onClick event
        targetButton.onClick.AddListener(HandleButtonClick);
    }

    private void HandleButtonClick()
    {
        StartCooldown();
        //StartCoroutine(ButtonCooldownRoutine());
    }



    //private IEnumerator ButtonCooldownRoutine(float time)
    //{
    //    // Disable the button
    //    targetButton.interactable = false;

    //    // Wait for the cooldown time
    //    yield return new WaitForSeconds(time);

    //    // Re-enable the button
    //    targetButton.interactable = true;
    //}

    private void ButtonCooldownRoutine()
    {
        // Disable the button
        //targetButton.interactable = false;

        // Wait for the cooldown time
        //yield return new WaitForSeconds(time);

        // Re-enable the button
        targetButton.interactable = true;
    }


    private void StartCooldown()
    {
        lastCooldownTime = Time.time;
        targetButton.interactable = false;
        Invoke(nameof(CheckCooldown), cooldownTime + 0.1f);
    }

    private void OnEnable()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();

            if (targetButton == null)
            {
                Debug.LogError("Button component not found on this GameObject. Please assign it in the Inspector.");
                return; // Exit if no Button is found
            }



        }
        CheckCooldown();
    }

    // Optionally, when the component is disabled, cancel any pending Invoke calls.
    private void OnDisable()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();

            if (targetButton == null)
            {
                Debug.LogError("Button component not found on this GameObject. Please assign it in the Inspector.");
                return; // Exit if no Button is found
            }

            //foreach (BoolVariable otherCondition in ArrOfOtherConditions)
            //{
            //    otherCondition.OnValueChanged -= OnOneOfConditionsChanged;
            //}
        }
        CancelInvoke(nameof(EnableButton));
    }

    // Checks whether enough time has passed since the last cooldown started.
    private void CheckCooldown()
    {
        float elapsed = Time.time - lastCooldownTime;

        bool isTimePassed = (lastCooldownTime == -Mathf.Infinity || elapsed >= cooldownTime);
        // If no cooldown was initiated or the cooldown period has passed, ensure the button is interactable.
        //if ((lastCooldownTime == -Mathf.Infinity || elapsed >= cooldownTime)  )
        //{
        bool isAllConditionsValid = true;

        foreach (BoolVariable otherCondition in ArrOfOtherConditions)
        {
            if (!otherCondition.Value)
            {
                isAllConditionsValid = false;
                break;
            }
        }
        //    if (isAllConditionsValid)
        //    {
        //        targetButton.interactable = true;
        //    }
        //}
        //else 
        //{
        //    // Calculate remaining time and schedule re-enabling the button
        //    float remainingTime = cooldownTime - elapsed;
        //    Invoke(nameof(CheckCooldown), remainingTime);
        //}

        if (isTimePassed && isAllConditionsValid)
        {

          if(  cooldown_afterAllConditionsTrue < 0.001f)
            {
                targetButton.interactable = true;
            }else
            {
                Invoke(nameof(ButtonCooldownRoutine), cooldown_afterAllConditionsTrue);
            }     


            return;
        }
        else
        {
            targetButton.interactable = false;
        }

        if (!isTimePassed)
        {
            float remainingTime = cooldownTime - elapsed;
            Invoke(nameof(CheckCooldown), remainingTime);
        }
    }


    private void OnOneOfConditionsChanged(bool Condition)
    {
        //bool isAllConditionsValid = true;

        //foreach (BoolVariable otherCondition in ArrOfOtherConditions)
        //{
        //    if(!otherCondition.Value)
        //    {
        //        isAllConditionsValid = false;
        //        break;
        //    }
        //}

        //float elapsed = Time.time - lastCooldownTime;

        //if (isAllConditionsValid)
        //{
        //    if(lastCooldownTime == -Mathf.Infinity || elapsed >= cooldownTime)
        //    {
        //        targetButton.interactable = true;
        //    }
        //    else
        //    {
        //        float remainingTime = cooldownTime - elapsed;
        //        Invoke(nameof(CheckCooldown), remainingTime);
        //    }
        //}
        //else
        //{
        //    targetButton.interactable = false;
        //}

        float elapsed = Time.time - lastCooldownTime;

        bool isTimePassed = (lastCooldownTime == -Mathf.Infinity || elapsed >= cooldownTime);

        bool isAllConditionsValid = true;

        foreach (BoolVariable otherCondition in ArrOfOtherConditions)
        {
            if (!otherCondition.Value)
            {
                isAllConditionsValid = false;
                break;
            }
        }

        if (isTimePassed && isAllConditionsValid)
        {
            if (cooldown_afterAllConditionsTrue < 0.001f)
            {
                targetButton.interactable = true;
            }
            else
            {
                Invoke(nameof(ButtonCooldownRoutine), cooldown_afterAllConditionsTrue);
            }
            return;
        }
        else
        {
            targetButton.interactable = false;
        }

        if (!isTimePassed)
        {
            float remainingTime = cooldownTime - elapsed;
            Invoke(nameof(CheckCooldown), remainingTime);
        }


    }

    private void EnableButton()
    {
        targetButton.interactable = true;
    }

    public void OnDestroy()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        foreach (BoolVariable otherCondition in ArrOfOtherConditions)
        {
            otherCondition.OnValueChanged -= OnOneOfConditionsChanged;

        }
        targetButton.onClick.RemoveListener(HandleButtonClick);
    }
}