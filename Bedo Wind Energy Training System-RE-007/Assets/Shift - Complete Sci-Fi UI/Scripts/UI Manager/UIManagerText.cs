using UnityEngine;
using TMPro;
using System;

namespace Michsky.UI.Shift
{
    [ExecuteInEditMode]
    public class UIManagerText : MonoBehaviour
    {
        [Header("Resources")]
        public UIManager UIManagerAsset;
        public TextMeshProUGUI textObject;

        [Header("Settings")]
        public bool keepAlphaValue = false;
        public bool useCustomColor = false;
        public ColorType colorType;
        public FontType fontType;

        bool dynamicUpdateEnabled;


        private void Awake()
        {
            if (textObject == null)
            {
                textObject = gameObject.GetComponent<TextMeshProUGUI>();
            }
            //if (textObject == null)
            //{
            //    Destroy(this);
            //}
        }
        public enum ColorType
        {
            Primary = 0,
            Secondary = 1,
            PrimaryReversed = 2,
            Negative = 3,
            Background = 4
        }

        public enum FontType
        {
            Light = 0,
            Regular = 1,
            Medium = 2,
            Semibold = 3,
            Bold = 4
        }

        void OnEnable()
        {
            if (UIManagerAsset == null)
            {
                try { UIManagerAsset = Resources.Load<UIManager>("Shift UI Manager"); }
                catch { Debug.Log("No UI Manager found. Assign it manually, otherwise it won't work properly.", this); }
            }

        }

        void Start()
        {

            if (textObject == null)
            {
                return;
            }
                if (dynamicUpdateEnabled == false)
            {
                this.enabled = true;
                UpdateButton();
            }

          
        }

        void LateUpdate()
        {
            if (textObject == null)
            {
                return;
            }
            if (UIManagerAsset != null)
            {
                if (UIManagerAsset.enableDynamicUpdate == true)
                    dynamicUpdateEnabled = true;
                else
                    dynamicUpdateEnabled = false;

                if (dynamicUpdateEnabled == true)
                    UpdateButton();
            }
        }

        void UpdateButton()
        {
            if (textObject == null)
            {
                return;
            }
            try
            {
                // Colors
                //Debug.Log(GetHierarchyPath(gameObject));
                //Debug.Log("here1");
                if (useCustomColor == false)
                {
                    if (keepAlphaValue == false)
                    {
                        //Debug.Log("here2");
                        if (colorType == ColorType.Primary)
                            textObject.color = UIManagerAsset.primaryColor;
                        else if (colorType == ColorType.Secondary)
                            textObject.color = UIManagerAsset.secondaryColor;
                        else if (colorType == ColorType.PrimaryReversed)
                            textObject.color = UIManagerAsset.primaryReversed;
                        else if (colorType == ColorType.Negative)
                            textObject.color = UIManagerAsset.negativeColor;
                        else if (colorType == ColorType.Background)
                            textObject.color = UIManagerAsset.backgroundColor;
                    }

                    else
                    {
                        //Debug.Log("here3");
                        if (colorType == ColorType.Primary)
                            textObject.color = new Color(UIManagerAsset.primaryColor.r, UIManagerAsset.primaryColor.g, UIManagerAsset.primaryColor.b, textObject.color.a);
                        else if (colorType == ColorType.Secondary)
                            textObject.color = new Color(UIManagerAsset.secondaryColor.r, UIManagerAsset.secondaryColor.g, UIManagerAsset.secondaryColor.b, textObject.color.a);
                        else if (colorType == ColorType.PrimaryReversed)
                            textObject.color = new Color(UIManagerAsset.primaryReversed.r, UIManagerAsset.primaryReversed.g, UIManagerAsset.primaryReversed.b, textObject.color.a);
                        else if (colorType == ColorType.Negative)
                            textObject.color = new Color(UIManagerAsset.negativeColor.r, UIManagerAsset.negativeColor.g, UIManagerAsset.negativeColor.b, textObject.color.a);
                        else if (colorType == ColorType.Background)
                            textObject.color = new Color(UIManagerAsset.backgroundColor.r, UIManagerAsset.backgroundColor.g, UIManagerAsset.backgroundColor.b, textObject.color.a);
                    }
                }

                // Fonts
                //Debug.Log("here4");
                if (fontType == FontType.Light)
                    textObject.font = UIManagerAsset.lightFont;
                else if (fontType == FontType.Regular)
                    textObject.font = UIManagerAsset.regularFont;
                else if (fontType == FontType.Medium)
                    textObject.font = UIManagerAsset.mediumFont;
                else if (fontType == FontType.Semibold)
                    textObject.font = UIManagerAsset.semiBoldFont;
                else if (fontType == FontType.Bold)
                    textObject.font = UIManagerAsset.boldFont;
            }

            catch (Exception e)
            {
                GetHierarchyPath(gameObject);
            }
        }
        private string GetHierarchyPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path; // Append parent's name to the path
                parent = parent.parent;         // Move up to the next parent
            }

            return path;
        }

        public void ChangeColorType(int ctIndex)
        {
            if (textObject == null)
            {
                return;
            }
            colorType = (ColorType)ctIndex;
        }
    }
}