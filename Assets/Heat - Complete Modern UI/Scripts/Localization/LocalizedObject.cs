using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace Michsky.UI.Heat
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Heat UI/Localization/Localized Object")]
    public class LocalizedObject : MonoBehaviour
    {
        // Resources
        public LocalizationSettings localizationSettings;
        public TextMeshProUGUI textObj;
        public AudioSource audioObj;
        public Image imageObj;

        // Settings
        public int tableIndex = -1;
        public string localizationKey;
        public ObjectType objectType = ObjectType.TextMeshPro;
        public UpdateMode updateMode = UpdateMode.OnEnable;
        public bool rebuildLayoutOnUpdate;
        [SerializeField] private bool forceAddToManager = false;
#if UNITY_EDITOR
        public bool showOutputOnEditor = true;
#endif

        // Events
        public LanguageChangedEvent onLanguageChanged = new LanguageChangedEvent();

        [System.Serializable]
        public class LanguageChangedEvent : UnityEvent<string> { }

        // Helpers
        public bool isInitialized = false;
        private int _resolvedTableIndex = -1;
        private LocalizationLanguage _resolvedForLang = null;

        public enum UpdateMode { OnEnable, OnDemand }
        public enum ObjectType { TextMeshPro, Custom, ComponentDriven, Audio, Image }

        /// <summary>
        /// Resolves the settings tableIndex to the correct index in the given language's tableList.
        /// Caches the result per language to avoid repeated lookups.
        /// </summary>
        private int GetResolvedTableIndex(LocalizationLanguage lang)
        {
            if (lang == null) return -1;
            if (_resolvedForLang == lang && _resolvedTableIndex >= 0) return _resolvedTableIndex;

            _resolvedTableIndex = lang.ResolveTableIndex(tableIndex);
            _resolvedForLang = lang;
            return _resolvedTableIndex;
        }

        void Awake()
        {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
            if (LocalizationManager.instance != null && !LocalizationManager.instance.UIManagerAsset.enableLocalization)
            {
                Destroy(this);
                return;
            }

            InitializeItem();
        }

        void OnEnable()
        {
            if (LocalizationManager.instance == null) { Destroy(this); return; }
            if (!isInitialized || LocalizationManager.instance.currentLanguageAsset == null) { return; }
            if (updateMode == UpdateMode.OnEnable) { UpdateItem(); }
        }

        public void InitializeItem()
        {
            if (isInitialized)
                return;

            if (LocalizationManager.instance == null)
            {
                UIManager tempUIM = (UIManager)Resources.FindObjectsOfTypeAll(typeof(UIManager))[0];
                if (tempUIM == null || !tempUIM.enableLocalization) { return; }

                GameObject newLM = new GameObject("Localization Manager [Auto Generated]");
                newLM.AddComponent<LocalizationManager>();
            }

            if (LocalizationManager.instance != null && !LocalizationManager.instance.UIManagerAsset.enableLocalization) { Destroy(this); return; }
            if (LocalizationManager.instance == null || LocalizationManager.instance.UIManagerAsset == null || !LocalizationManager.instance.UIManagerAsset.enableLocalization) { return; }
            if (forceAddToManager && !LocalizationManager.instance.localizedItems.Contains(this)) { LocalizationManager.instance.localizedItems.Add(this); }

            if (objectType == ObjectType.TextMeshPro && textObj == null) { textObj = gameObject.GetComponent<TextMeshProUGUI>(); }
            else if (objectType == ObjectType.Audio && audioObj == null) { audioObj = gameObject.GetComponent<AudioSource>(); }
            else if (objectType == ObjectType.Image && imageObj == null) { imageObj = gameObject.GetComponent<Image>(); }

            LocalizationManager.instance.localizedItems.Add(this);
            isInitialized = true;
        }

        public void ReInitializeItem()
        {
            isInitialized = false;
            InitializeItem();
        }

        public void UpdateItem()
        {
            if (!isInitialized || LocalizationManager.instance == null || LocalizationManager.instance.currentLanguageAsset == null || LocalizationManager.instance.currentLanguageAsset.tableList.Count == 0)
                return;

            int resolved = GetResolvedTableIndex(LocalizationManager.instance.currentLanguageAsset);
            if (resolved < 0 || resolved >= LocalizationManager.instance.currentLanguageAsset.tableList.Count)
                return;

            var tableContent = LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent;

            if (objectType == ObjectType.TextMeshPro && textObj != null)
            {
                for (int i = 0; i < tableContent.Count; i++)
                {
                    if (localizationKey == tableContent[i].key)
                    {
                        if (string.IsNullOrEmpty(tableContent[i].value))
                        {
                            if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> The specified key '" + localizationKey + "' could not be found or the output value is empty for " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                            break;
                        }

                        textObj.text = tableContent[i].value;
                        onLanguageChanged.Invoke(tableContent[i].value);
                        break;
                    }
                }
            }

            else if (objectType == ObjectType.Audio && audioObj != null)
            {
                for (int i = 0; i < tableContent.Count; i++)
                {
                    if (localizationKey == tableContent[i].key)
                    {
                        if (tableContent[i].audioValue == null)
                        {
                            if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> The specified key '" + localizationKey + "' could not be found or the output value is empty for " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                            break;
                        }

                        audioObj.clip = tableContent[i].audioValue;
                        break;
                    }
                }
            }

            else if (objectType == ObjectType.Image && imageObj != null)
            {
                for (int i = 0; i < tableContent.Count; i++)
                {
                    if (localizationKey == tableContent[i].key)
                    {
                        if (tableContent[i].spriteValue == null)
                        {
                            if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> The specified key '" + localizationKey + "' could not be found or the output value is empty for " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                            break;
                        }

                        imageObj.sprite = tableContent[i].spriteValue;
                        break;
                    }
                }
            }

            else if (objectType == ObjectType.Custom || objectType == ObjectType.ComponentDriven)
            {
                for (int i = 0; i < tableContent.Count; i++)
                {
                    if (localizationKey == tableContent[i].key)
                    {
                        if (string.IsNullOrEmpty(tableContent[i].value))
                            break;

                        onLanguageChanged.Invoke(tableContent[i].value);
                        break;
                    }
                }
            }

            if (rebuildLayoutOnUpdate && gameObject.activeInHierarchy) { StartCoroutine("RebuildLayout"); }
        }

        public bool CheckLocalizationStatus()
        {
            if (!isInitialized) { InitializeItem(); }
            if (LocalizationManager.instance == null || LocalizationManager.instance.UIManagerAsset == null || !LocalizationManager.instance.UIManagerAsset.enableLocalization) { return false; }
            else { return true; }
        }

        public string GetKeyOutput(string key)
        {
            string keyValue = null;
            bool keyFound = false;

            if (LocalizationManager.instance != null && LocalizationManager.instance.currentLanguageAsset == null) { LocalizationManager.instance.InitializeLanguage(); }
            if (!isInitialized || LocalizationManager.instance == null || LocalizationManager.instance.currentLanguageAsset == null || LocalizationManager.instance.currentLanguageAsset.tableList.Count == 0)
                return LocalizationSettings.notInitializedText;

            int resolved = GetResolvedTableIndex(LocalizationManager.instance.currentLanguageAsset);
            if (resolved < 0 || resolved >= LocalizationManager.instance.currentLanguageAsset.tableList.Count)
                return "MISSING_TABLE: " + tableIndex;

            for (int i = 0; i < LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent.Count; i++)
            {
                if (key == LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent[i].key)
                {
                    keyValue = LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent[i].value;
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

        public AudioClip GetKeyOutputAudio(string key)
        {
            AudioClip keyValue = null;
            bool keyFound = false;

            if (LocalizationManager.instance != null && LocalizationManager.instance.currentLanguageAsset == null) { LocalizationManager.instance.InitializeLanguage(); }
            if (!isInitialized || LocalizationManager.instance == null || LocalizationManager.instance.currentLanguageAsset == null || LocalizationManager.instance.currentLanguageAsset.tableList.Count == 0)
                return null;

            int resolved = GetResolvedTableIndex(LocalizationManager.instance.currentLanguageAsset);
            if (resolved < 0 || resolved >= LocalizationManager.instance.currentLanguageAsset.tableList.Count)
                return null;

            for (int i = 0; i < LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent.Count; i++)
            {
                if (key == LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent[i].key)
                {
                    keyValue = LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent[i].audioValue;
                    keyFound = true;
                    break;
                }
            }

            if (keyFound && keyValue == null)
            {
                if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> The output value for '" + key + "' is empty in " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                return null;
            }

            else if (!keyFound)
            {
                if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> The specified key '" + key + "' could not be found in " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                return null;
            }

            return keyValue;
        }

        public Sprite GetKeyOutputSprite(string key)
        {
            Sprite keyValue = null;
            bool keyFound = false;

            if (LocalizationManager.instance != null && LocalizationManager.instance.currentLanguageAsset == null) { LocalizationManager.instance.InitializeLanguage(); }
            if (!isInitialized || LocalizationManager.instance == null || LocalizationManager.instance.currentLanguageAsset == null || LocalizationManager.instance.currentLanguageAsset.tableList.Count == 0)
                return null;

            int resolved = GetResolvedTableIndex(LocalizationManager.instance.currentLanguageAsset);
            if (resolved < 0 || resolved >= LocalizationManager.instance.currentLanguageAsset.tableList.Count)
                return null;

            for (int i = 0; i < LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent.Count; i++)
            {
                if (key == LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent[i].key)
                {
                    keyValue = LocalizationManager.instance.currentLanguageAsset.tableList[resolved].tableContent[i].spriteValue;
                    keyFound = true;
                    break;
                }
            }

            if (keyFound && keyValue == null)
            {
                if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> The output value for '" + key + "' is empty in " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                return null;
            }

            else if (!keyFound)
            {
                if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> The specified key '" + key + "' could not be found in " + LocalizationManager.instance.currentLanguageAsset.languageName + ".", this); }
                return null;
            }

            return keyValue;
        }

        public static string GetKeyOutput(string tableID, string tableKey)
        {
            UIManager tempUIM = (UIManager)Resources.FindObjectsOfTypeAll(typeof(UIManager))[0];

            if (tempUIM == null || !tempUIM.enableLocalization)
                return null;

            int tableIndex = -1;
            string keyValue = null;

            for (int i = 0; i < tempUIM.currentLanguage.tableList.Count; i++)
            {
                if (tempUIM.currentLanguage.tableList[i].table.tableID == tableID)
                {
                    tableIndex = i;
                    break;
                }
            }

            if (tableIndex == -1) { return null; }
            else
            {
                for (int i = 0; i < tempUIM.currentLanguage.tableList[tableIndex].tableContent.Count; i++)
                {
                    if (tempUIM.currentLanguage.tableList[tableIndex].tableContent[i].key == tableKey)
                    {
                        keyValue = tempUIM.currentLanguage.tableList[tableIndex].tableContent[i].value;
                        break;
                    }
                }
            }

            return keyValue;
        }

        public static AudioClip GetKeyOutputAudio(string tableID, string tableKey)
        {
            UIManager tempUIM = (UIManager)Resources.FindObjectsOfTypeAll(typeof(UIManager))[0];

            if (tempUIM == null || !tempUIM.enableLocalization)
                return null;

            int tableIndex = -1;
            AudioClip keyValue = null;

            for (int i = 0; i < tempUIM.currentLanguage.tableList.Count; i++)
            {
                if (tempUIM.currentLanguage.tableList[i].table.tableID == tableID)
                {
                    tableIndex = i;
                    break;
                }
            }

            if (tableIndex == -1) { return null; }
            else
            {
                for (int i = 0; i < tempUIM.currentLanguage.tableList[tableIndex].tableContent.Count; i++)
                {
                    if (tempUIM.currentLanguage.tableList[tableIndex].tableContent[i].key == tableKey)
                    {
                        keyValue = tempUIM.currentLanguage.tableList[tableIndex].tableContent[i].audioValue;
                        break;
                    }
                }
            }

            return keyValue;
        }

        public static Sprite GetKeyOutputSprite(string tableID, string tableKey)
        {
            UIManager tempUIM = (UIManager)Resources.FindObjectsOfTypeAll(typeof(UIManager))[0];

            if (tempUIM == null || !tempUIM.enableLocalization)
                return null;

            int tableIndex = -1;
            Sprite keyValue = null;

            for (int i = 0; i < tempUIM.currentLanguage.tableList.Count; i++)
            {
                if (tempUIM.currentLanguage.tableList[i].table.tableID == tableID)
                {
                    tableIndex = i;
                    break;
                }
            }

            if (tableIndex == -1) { return null; }
            else
            {
                for (int i = 0; i < tempUIM.currentLanguage.tableList[tableIndex].tableContent.Count; i++)
                {
                    if (tempUIM.currentLanguage.tableList[tableIndex].tableContent[i].key == tableKey)
                    {
                        keyValue = tempUIM.currentLanguage.tableList[tableIndex].tableContent[i].spriteValue;
                        break;
                    }
                }
            }

            return keyValue;
        }

        IEnumerator RebuildLayout()
        {
            yield return new WaitForSecondsRealtime(0.025f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}