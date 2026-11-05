using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.Heat;
using Obvious.Soap;

/// <summary>
/// ScriptableObject holding tooltip data (title & description).
/// </summary>
[CreateAssetMenu(fileName = "TooltipComponentDescription", menuName = "Bedo/UI/Tooltip Component Description")]
public class TooltipComponentDescription : ScriptableObject
{
    [Header("Tooltip Data")]
    public string Title;
    [Multiline] public string Description;

    
    [SerializeField] BoolReference IsUselocalizationSettings = new BoolReference(false,true);
    [SerializeField] public LocalizationSettings localizationSettings;
    [SerializeField] public int tableIndex = -1;
    [SerializeField] public string localizationKeyTitle;
    [SerializeField] public string localizationKeyDef;

    public bool UseLocalization => IsUselocalizationSettings != null && IsUselocalizationSettings.Value;
    public string GetTitle()
    {
        if (!UseLocalization) return Title;
        return GetLocalizedOrFallback(localizationKeyTitle, Title);
    }

    public string GetDescription()
    {
        if (!UseLocalization) return Description;
        return GetLocalizedOrFallback(localizationKeyDef, Description);
    }

    private string GetLocalizedOrFallback(string key, string fallback)
    {
        if (string.IsNullOrEmpty(key))
            return fallback;

        // Make sure manager exists & language is initialized
        if (LocalizationManager.instance == null)
            return fallback;

        if (LocalizationManager.instance.currentLanguageAsset == null)
            LocalizationManager.instance.InitializeLanguage();

        var lang = LocalizationManager.instance.currentLanguageAsset;
        if (lang == null || lang.tableList == null)
            return fallback;

        // Resolve settings tableIndex to the correct language tableList index
        int resolvedIdx = lang.ResolveTableIndex(tableIndex);
        if (resolvedIdx < 0 || resolvedIdx >= lang.tableList.Count)
            return fallback;

        var table = lang.tableList[resolvedIdx];
        if (table == null || table.tableContent == null)
            return fallback;

        for (int i = 0; i < table.tableContent.Count; i++)
        {
            if (table.tableContent[i].key == key)
            {
                var val = table.tableContent[i].value;
                return string.IsNullOrEmpty(val) ? fallback : val;
            }
        }

        return fallback;
    }
}
