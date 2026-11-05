using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LoadSceneWithFreeMode : MonoBehaviour
{
    [SerializeField] BoolVariable isGuided;

    [SerializeField] Toggle FreeModeToggle;
    [SerializeField] Toggle GuidedMdoeToggle;

    public void Start()
    {

        GuidedMdoeToggle.SetIsOnWithoutNotify(isGuided.Value);
        FreeModeToggle.SetIsOnWithoutNotify(!isGuided.Value);

        GuidedMdoeToggle.onValueChanged.AddListener(OnToggleIsGuidedIsPressed);
        isGuided.OnValueChanged += IsGuided_OnValueChanged;
    }

    private void OnToggleIsGuidedIsPressed(bool val)
    {
        isGuided.Value = val;
    }

    private void IsGuided_OnValueChanged(bool val)
    {
        if (val && !isGuided.PreviousValue)
        {
            StartCoroutine( LoadSceneAfterTime(0.1f));
        }
        else if (!val && isGuided.PreviousValue)
        {
            StartCoroutine( LoadSceneAfterTime(0.1f));
        }
    }


    private IEnumerator LoadSceneAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        isGuided.OnValueChanged -= IsGuided_OnValueChanged;
    }

}
