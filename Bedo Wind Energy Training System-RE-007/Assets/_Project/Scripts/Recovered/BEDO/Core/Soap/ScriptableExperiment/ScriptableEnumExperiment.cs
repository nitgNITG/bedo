using Michsky.UI.Heat;
using Obvious.Soap;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Localization.Settings;

namespace Bedo.UI_V3_2
{
    [CreateAssetMenu(fileName = "ScriptableEnumExperiment", menuName = "Soap/ScriptableEnums/Experiment")]
    public class ScriptableEnumExperiment : ScriptableEnumBase
    {
        [SerializeField] public string experimentHeader;
        [SerializeField] public string experimentName;
        [SerializeField][TextArea] public string Objectives;
        [Space(15)]
        [SerializeField] public ScriptableListString StepsTextScriptableList;
        [Space(15)]
        [SerializeField] public ScriptableListString PopUpTextScriptableList;
        [SerializeField] public List<IntReference> PopUpIndexOfSteps;
        [Space(15)]
        [SerializeField] public List<StringReference> DocumentsNames;
        [SerializeField] public ScriptableListString DocumentsNamesScriptableList;
        [Space(15)]
        [SerializeField] public List<MultipleChoiceQuestionScriptableObject> MCQuestionsList;
        [SerializeField] public List<IntReference> MCQuestionsPositions;
        [Space(5)]
        [SerializeField] public List<TrueAndFalseQuestionScriptableObject> TrueAndFalseQuestionsList;
        [SerializeField] public List<IntReference> TrueAndFalseQuestionsPositions;

        [Space(25)]

        [SerializeField] public bool UseLocalization = false;
        [SerializeField] public LocalizationSettings localizationSettings;
        [SerializeField] public int tableIndex = -1;

        public string GetKeyOutput(string key)
        {
            string keyValue = null;
            bool keyFound = false;

            if (LocalizationManager.instance != null && LocalizationManager.instance.currentLanguageAsset == null) { LocalizationManager.instance.InitializeLanguage(); }
            if ( LocalizationManager.instance == null || LocalizationManager.instance.currentLanguageAsset == null || LocalizationManager.instance.currentLanguageAsset.tableList.Count == 0)
                return LocalizationSettings.notInitializedText;

            // Resolve settings tableIndex to the correct language tableList index
            var lang = LocalizationManager.instance.currentLanguageAsset;
            int resolvedIdx = lang.ResolveTableIndex(tableIndex);
            if (resolvedIdx < 0 || resolvedIdx >= lang.tableList.Count)
                return "MISSING_TABLE: " + tableIndex;

            for (int i = 0; i < lang.tableList[resolvedIdx].tableContent.Count; i++)
            {
                if (key == lang.tableList[resolvedIdx].tableContent[i].key)
                {
                    keyValue = lang.tableList[resolvedIdx].tableContent[i].value;
                    keyFound = true;
                    break;
                }
            }

            if (keyFound && string.IsNullOrEmpty(keyValue))
            {
                if (LocalizationManager.enableLogs == true) { Debug.Log("<b>[Localized Object]</b> The output value for '" + key + "' is empty in " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                return "EMPTY_KEY_IN_" + LocalizationManager.instance.currentLanguageAsset.languageID + ": " + key;
            }

            else if (!keyFound)
            {
                if (LocalizationManager.enableLogs == true) { Debug.Log("<b>[Localized Object]</b> The specified key '" + key + "' could not be found in " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                return "MISSING_KEY_IN_" + LocalizationManager.instance.currentLanguageAsset.languageID + ": " + key;
            }

            return keyValue;
        }




    }
}
