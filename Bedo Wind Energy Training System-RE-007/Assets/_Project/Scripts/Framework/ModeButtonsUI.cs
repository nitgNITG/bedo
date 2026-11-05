using UnityEngine;
using UnityEngine.UI;

namespace TutorialFramework
{
    public class ModeButtonsUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button guidedButton;
        public Button freeButton;


        [Header("Guided Only Objects")]
        public GameObject[] guidedOnlyObjects;


        [Header("Free Only Objects")]
        public GameObject[] freeOnlyObjects;



        private void Start()
        {
            if (guidedButton)
                guidedButton.onClick.AddListener(
                    InteractionManager.Instance.SetGuided
                );


            if (freeButton)
                freeButton.onClick.AddListener(
                    InteractionManager.Instance.SetFree
                );


            UpdateModeObjects();
            UpdateButtons();
        }




        private void OnEnable()
        {
            if (InteractionManager.Instance != null)
            {
                UpdateModeObjects();
                UpdateButtons();
            }
        }



        public void UpdateModeObjects()
        {
            if (InteractionManager.Instance == null)
                return;


            bool guided =
                InteractionManager.Instance.Guided;



            if (guidedOnlyObjects != null)
            {
                foreach (GameObject obj in guidedOnlyObjects)
                {
                    if (obj != null)
                        obj.SetActive(guided);
                }
            }



            if (freeOnlyObjects != null)
            {
                foreach (GameObject obj in freeOnlyObjects)
                {
                    if (obj != null)
                        obj.SetActive(!guided);
                }
            }
        }



        public void UpdateButtons()
        {
            if (InteractionManager.Instance == null)
                return;


            if (guidedButton)
            {
                guidedButton.interactable =
                    !InteractionManager.Instance.Guided;
            }


            if (freeButton)
            {
                freeButton.interactable =
                    !InteractionManager.Instance.Free;
            }
        }
    }
}