using UnityEngine;

public sealed class WindLanguageSelectionButton : MonoBehaviour
{
    [SerializeField] private string language = "English";

    private void Start()
    {
        if (WindLocalization.Instance == null)
            return;

        var selectedLanguage = WindLocalization.Arabic ? "Arabic" : "English";
        SetCurrentLabelKey(selectedLanguage);
        WindLocalization.Instance.SetLanguage(selectedLanguage);
        UpdateIndicator(selectedLanguage);
    }

    public void SelectLanguage()
    {
        SetCurrentLabelKey(language);

        if (WindLocalization.Instance != null)
            WindLocalization.Instance.SetLanguage(language);

        UpdateIndicator(language);
    }

    private void SetCurrentLabelKey(string selectedLanguage)
    {
        var panel = transform.parent;
        if (!panel)
            return;

        var currentLabel = panel.Find("Language Current")?.GetComponent<TMPro.TMP_Text>();
        if (currentLabel)
            currentLabel.text = selectedLanguage;
    }

    private void UpdateIndicator(string selectedLanguage)
    {
        var panel = transform.parent;
        if (!panel)
            return;

        var indicatorDots = panel.Find("Language Dots")?.GetComponent<TMPro.TMP_Text>();
        if (!indicatorDots)
            return;

        indicatorDots.text = selectedLanguage == "Arabic"
            ? "<color=#FF9D00>●</color>  <color=#FFFFFF>●</color>"
            : "<color=#FFFFFF>●</color>  <color=#FF9D00>●</color>";
    }
}
