using UnityEngine;
using TMPro;
using System.Collections;

namespace TutorialFramework
{
    public class WarningManager : MonoBehaviour
    {
        public static WarningManager Instance;


        [Header("Warning UI")]
        [SerializeField] private GameObject warningObject;

        [SerializeField] private TMP_Text warningText;


        [Header("Settings")]
        [SerializeField] private float displayTime = 3f;


        private Coroutine hideRoutine;



        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }


            Instance = this;


            if (warningObject != null)
                warningObject.SetActive(false);
        }



        public void ShowWarning(string message)
        {
            if (warningObject == null ||
               warningText == null)
                return;



            warningText.text = message;


            warningObject.SetActive(true);



            if (hideRoutine != null)
                StopCoroutine(hideRoutine);


            hideRoutine =
                StartCoroutine(HideAfterDelay());
        }




        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayTime);


            warningObject.SetActive(false);
        }
    }
}