using UnityEngine;

namespace TutorialFramework
{
    public class ModeObjectsController : MonoBehaviour
    {
        [Header("Guided Only")]
        public GameObject[] guidedOnlyObjects;


        [Header("Free Only")]
        public GameObject[] freeOnlyObjects;



        private void Start()
        {
            UpdateMode();
        }



        private void OnEnable()
        {
            UpdateMode();
        }



        public void UpdateMode()
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
    }
}