using System.Collections.Generic;
using UnityEngine;

namespace Michsky.UI.Heat
{
    [CreateAssetMenu(fileName = "New Localization Table", menuName = "Heat UI/Localization/New Language")]
    public class LocalizationLanguage : ScriptableObject
    {
        public LocalizationSettings localizationSettings;
        public string languageID;
        public string languageName;
        public string localizedName;
        public List<TableList> tableList = new List<TableList>();

        [System.Serializable]
        public class TableList
        {
            public LocalizationTable table;
            public List<TableContent> tableContent = new List<TableContent>();
        }

        [System.Serializable]
        public class TableContent
        {
            public string key;
            [TextArea] public string value;
            public AudioClip audioValue;
            public Sprite spriteValue;
        }

        /// <summary>
        /// Finds the index of a LocalizationTable inside this language's tableList
        /// by matching the table reference. Returns -1 if not found.
        /// Use this instead of assuming settings.tables[] order matches tableList[] order.
        /// </summary>
        public int FindTableListIndex(LocalizationTable table)
        {
            if (table == null || tableList == null) return -1;
            for (int i = 0; i < tableList.Count; i++)
            {
                if (tableList[i].table == table) return i;
            }
            return -1;
        }

        /// <summary>
        /// Resolves a settings table index to the correct tableList index in this language.
        /// settingsTableIndex is the index into LocalizationSettings.tables[].
        /// Returns -1 if the table cannot be found.
        /// </summary>
        public int ResolveTableIndex(int settingsTableIndex)
        {
            if (localizationSettings == null) return -1;
            if (settingsTableIndex < 0 || settingsTableIndex >= localizationSettings.tables.Count) return -1;
            var table = localizationSettings.tables[settingsTableIndex].localizationTable;
            return FindTableListIndex(table);
        }
    }
}