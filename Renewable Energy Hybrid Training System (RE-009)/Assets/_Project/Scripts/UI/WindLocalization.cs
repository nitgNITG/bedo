using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lean.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TextCore;

/// <summary>Localizes displayed text only: experiment IDs, wiring and scientific values stay unchanged.</summary>
[DefaultExecutionOrder(10000)]
public sealed class WindLocalization : MonoBehaviour
{
    [Serializable] public sealed class Entry { public string english, arabic, source; }
    [Serializable] public sealed class Table { public Entry[] entries; }
    private static Table catalogue;
    public static Table Catalogue => catalogue ?? (catalogue = JsonUtility.FromJson<Table>(Resources.Load<TextAsset>("WindLocalization/Catalogue").text));
    public static WindLocalization Instance { get; private set; }
    public static bool Arabic => Instance && Instance.lean.CurrentLanguage == "Arabic";
    public static string Key(string text) => Normalize(text);
    public static string Normalize(string value) => Regex.Replace(Regex.Replace(value ?? "", "<[^>]*>", "").ToLowerInvariant(), @"[\s\p{P}\p{S}]+", "").Replace("windturbine", "windmill");
    private LeanLocalization lean;
    private TMP_FontAsset arabicFont;
    private Font legacyFont;
    private float nextScan;
    private bool applying;
    private readonly Dictionary<TMP_Text, TextState> texts = new Dictionary<TMP_Text, TextState>();
    private readonly Dictionary<Text, LegacyState> legacy = new Dictionary<Text, LegacyState>();
    private readonly HashSet<TMP_Text> ownedLabels = new HashSet<TMP_Text>();
    private readonly HashSet<TMP_Text> pending = new HashSet<TMP_Text>();
    private readonly List<Button> switches = new List<Button>();
    private sealed class TextState
    {
        public string original, rendered; public TMP_FontAsset font; public TextAlignmentOptions alignment;
        public List<OTL_FeatureTag> features;
        public bool rtl; public bool autoSize; public float minimum, fontSize, maximum, width;
        public bool hasApplied, appliedArabic; public float alpha; public TMP_Text verticalLabel; public RectTransform verticalOwner; public Vector2 verticalOwnerSize;
    }
    private sealed class LegacyState { public string original, rendered; public Font font; public TextAnchor alignment; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Instance) return;
        new GameObject("Wind EN-AR Localization").AddComponent<WindLocalization>();
    }
    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        var settings = Resources.Load<WindLocalizationSettings>("WindLocalization/Settings");
        arabicFont = settings ? settings.arabicFont : null;
        legacyFont = settings ? settings.legacyFont : null;
        // Configure before enabling Lean so a saved language is loaded consistently.
        var host = new GameObject("Lean Localization"); host.transform.SetParent(transform); host.SetActive(false);
        lean = host.AddComponent<LeanLocalization>();
        lean.DetectLanguage = LeanLocalization.DetectType.None;
        lean.DefaultLanguage = "English";
        lean.SaveLoad = LeanLocalization.SaveLoadType.WhenChanged;
        foreach (string language in new[] { "English", "Arabic" })
        {
            var child = new GameObject(language); child.transform.SetParent(host.transform);
            child.AddComponent<LeanLanguage>().TranslationCode = language == "Arabic" ? "ar" : "en";
        }
        host.AddComponent<WindTranslationSource>(); host.SetActive(true);
        LeanLocalization.OnLocalizationChanged += LanguageChanged;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(TextChanged);
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private void OnDestroy()
    {
        LeanLocalization.OnLocalizationChanged -= LanguageChanged;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(TextChanged);
        SceneManager.sceneLoaded -= SceneLoaded;
        if (Instance == this) Instance = null;
    }
    public void ToggleLanguage() => SetLanguage(Arabic ? "English" : "Arabic");
    public void SetLanguage(string language)
    {
        lean.CurrentLanguage = language == "Arabic" ? "Arabic" : "English";
        Scan(); LanguageChanged();
    }
    private void SceneLoaded(Scene scene, LoadSceneMode mode) { nextScan = 0; }
    private void TextChanged(UnityEngine.Object target)
    {
        // TMP also raises this event for mesh/layout rebuilds. Only content edits need translation.
        if (!applying && target is TMP_Text text && !ownedLabels.Contains(text) && (!texts.TryGetValue(text, out var state) || text.text != state.rendered)) pending.Add(text);
    }
    private void LanguageChanged()
    {
        foreach (var text in texts.Keys.ToArray()) if (text) Apply(text);
        foreach (var text in legacy.Keys.ToArray()) if (text) ApplyLegacy(text);
        foreach (var button in switches) if (button) button.GetComponentInChildren<TMP_Text>(true).text = Arabic ? "AR" : "EN";
    }
    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + .5f; Scan(); }
        foreach (var text in pending.ToArray()) if (text) Apply(text);
        pending.Clear();
        if (Arabic) foreach (var pair in texts.ToArray())
            if (pair.Key && pair.Key.isActiveAndEnabled && Mathf.Abs(LayoutWidth(pair.Key) - pair.Value.width) > 1) Apply(pair.Key);
        foreach (var pair in texts) if (pair.Value.verticalLabel && pair.Value.verticalOwner && pair.Value.verticalLabel.gameObject.activeSelf)
        {
            UpdateVerticalBounds(pair.Value);
            if (pair.Key && pair.Key.alpha != 0) pair.Key.alpha = 0;
        }
        // Tooltip writers may replace text every frame; legacy UI has no text-changed event.
        foreach (var text in legacy.Keys.ToArray()) if (text && text.text != legacy[text].rendered) ApplyLegacy(text);
    }
    private void Scan()
    {
        foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            if (text.gameObject.scene.IsValid() && !texts.ContainsKey(text) && !ownedLabels.Contains(text) && !(text.GetComponentInParent<TMP_InputField>() is TMP_InputField input && input.textComponent == text))
            {
                texts[text] = new TextState { original = text.text, font = text.font, alignment = text.alignment,
                    features = text.fontFeatures != null ? new List<OTL_FeatureTag>(text.fontFeatures) : new List<OTL_FeatureTag>(),
                    rtl = text.isRightToLeftText, autoSize = text.enableAutoSizing, minimum = text.fontSizeMin, fontSize = text.fontSize, maximum = text.fontSizeMax, alpha=text.alpha };
                Apply(text);
            }
        foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            if (text.gameObject.scene.IsValid() && !legacy.ContainsKey(text) && !(text.GetComponentInParent<InputField>() is InputField input && input.textComponent == text))
            {
                legacy[text] = new LegacyState { original = text.text, font = text.font, alignment = text.alignment }; ApplyLegacy(text);
            }
        foreach (var text in texts.Keys.Where(t => !t).ToArray()) texts.Remove(text);
        foreach (var text in legacy.Keys.Where(t => !t).ToArray()) legacy.Remove(text);
        EnsureSwitch();
    }
    public static bool TryTranslate(string english, out string translated)
    {
        translated = english;
        if (string.IsNullOrEmpty(english)) return false;
        string key = Key(english);
        if (LeanLocalization.CurrentTranslations.TryGetValue(key, out var value) && value.Data is string result)
        { translated = result; return true; }
        // Dynamic labels keep their numbers and scientific unit abbreviations.
        var dynamic = Regex.Match(english.Trim(), @"^(Experiment|Camera|View)\s*(\d+)(.*)$", RegexOptions.IgnoreCase);
        if (dynamic.Success)
        {
            string word = dynamic.Groups[1].Value.ToLowerInvariant();
            if (Arabic) translated = (word == "experiment" ? "Ø§Ù„ØªØ¬Ø±Ø¨Ø©" : word == "camera" ? "Ø§Ù„ÙƒØ§Ù…ÙŠØ±Ø§" : "Ø§Ù„Ù…Ù†Ø¸ÙˆØ±") + " " + dynamic.Groups[2].Value + dynamic.Groups[3].Value;
            return true;
        }
        // Progress counters in the PDF viewer.
        var progress = Regex.Match(english, @"^(\(?\d+) of (\d+\)?)$");
        if (progress.Success) { if (Arabic) translated = progress.Groups[1].Value + " Ù…Ù† " + progress.Groups[2].Value; return true; }
        return false;
    }
    private void Apply(TMP_Text text)
    {
        if (!texts.TryGetValue(text, out var state)) return;
        if (state.hasApplied && state.appliedArabic == Arabic && text.text == state.rendered && Mathf.Abs(LayoutWidth(text) - state.width) <= 1) return;
        if (text.text != state.rendered) state.original = text.text;
        bool found = TryTranslate(state.original, out var translation);
        applying = true;
        if (Arabic && found && ArabicTextLayout.ContainsArabic(translation))
        {
            if (translation.Contains("<rotate="))
            {
                ApplyVerticalLabel(text, state, translation);
                state.rendered = text.text; state.width = LayoutWidth(text); state.hasApplied = true; state.appliedArabic = true; applying = false;
                return;
            }
            if (arabicFont) text.font = arabicFont;
            // Presentation-form text is already shaped. OpenType positioning on that
            // visual-order sequence can move Arabic glyphs backwards a second time.
            text.fontFeatures = new List<OTL_FeatureTag>();
            text.isRightToLeftText = false;
            text.alignment = Mirror(state.alignment);
            // Give translated buttons the same bounds and allow a modest font reduction.
            text.enableAutoSizing = false; text.fontSize = state.fontSize;
            text.text = ArabicTextLayout.Render(translation, text, LayoutWidth(text));
            // Fit short fixed-height controls without forcing long scrollable prose smaller.
            bool shortControl=state.original.Length<140 && text.rectTransform.rect.height<100;
            var fitter=text.GetComponent<ContentSizeFitter>();
            bool flexible=((fitter && fitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained) || text.GetComponentInParent<AutoSizeSubtitleScrollContent>()) && !text.GetComponentInParent<Button>() && !shortControl;
            if(!flexible && text.rectTransform.rect.height>0)
            {
                for(int i=0;i<16 && text.GetPreferredValues(text.text,text.rectTransform.rect.width,float.PositiveInfinity).y>text.rectTransform.rect.height-.5f && text.fontSize>8;i++)
                { text.fontSize=Mathf.Max(8,text.fontSize*.9f); text.text=ArabicTextLayout.Render(translation,text, LayoutWidth(text)); }
            }
            if(shortControl || !text.GetComponentInParent<AutoSizeSubtitleScrollContent>() || text.GetComponentInParent<Button>()){
                text.fontSizeMax=state.fontSize; text.fontSizeMin=Mathf.Min(state.minimum,8);
                text.enableAutoSizing=true;
            }
        }
        else
        {
            if(state.verticalLabel) state.verticalLabel.gameObject.SetActive(false);
            text.alpha = state.alpha;
            text.font = state.font; text.fontFeatures = new List<OTL_FeatureTag>(state.features); text.alignment = state.alignment; text.isRightToLeftText = state.rtl;
            text.fontSize = state.fontSize; text.fontSizeMax = state.maximum;
            text.enableAutoSizing = state.autoSize; text.fontSizeMin = state.minimum; text.text = state.original;
        }
        state.rendered = text.text; state.width = LayoutWidth(text); state.hasApplied = true; state.appliedArabic = Arabic; applying = false;
    }
    private void ApplyVerticalLabel(TMP_Text source, TextState state, string translation)
    {
        if (!state.verticalLabel)
        {
            RectTransform owner = null;
            for (var parent = source.transform.parent; parent; parent = parent.parent)
                if (parent.name == "Documents" || parent.name == "Objectives") { owner = parent as RectTransform; break; }
            if (!owner) owner = source.transform.parent as RectTransform;
            if (!owner) return;
            state.verticalOwner = owner;
            var go = new GameObject("Arabic Vertical Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = source.gameObject.layer; go.transform.SetParent(owner, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            state.verticalLabel = label; ownedLabels.Add(label);
            label.font = arabicFont ? arabicFont : source.font; label.fontFeatures = new List<OTL_FeatureTag>(); label.color = source.color;
            label.fontStyle = source.fontStyle; label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false; label.isRightToLeftText = false;
            label.enableWordWrapping = false; label.enableAutoSizing = true;
            label.fontSizeMax = state.fontSize; label.fontSizeMin = Mathf.Min(12, state.fontSize);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = Vector2.zero; rect.localRotation = Quaternion.Euler(0,0,90);
            state.verticalOwnerSize = new Vector2(-1,-1);
        }
        state.verticalLabel.gameObject.SetActive(true);
        state.verticalLabel.text = ArabicTextLayout.Visual(ArabicTextLayout.Shape(Regex.Replace(translation,"<[^>]*>","")));
        UpdateVerticalBounds(state);
        // Keep the authored title in its original layout so its fitters and button
        // scripts retain their normal measurements. Only its rendering is hidden.
        source.alpha = 0;
    }
    private static void UpdateVerticalBounds(TextState state)
    {
        var size = state.verticalOwner.rect.size;
        state.verticalLabel.enabled = size.x > 0 && size.y > 0;
        if (size == state.verticalOwnerSize) return;
        state.verticalOwnerSize = size;
        state.verticalLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(1,size.y-16),Mathf.Max(1,size.x-4));
    }
    private void ApplyLegacy(Text text)
    {
        var state = legacy[text]; if (text.text != state.rendered) state.original = text.text;
        if (Arabic && TryTranslate(state.original, out var translated) && ArabicTextLayout.ContainsArabic(translated))
        {
            text.font = legacyFont ? legacyFont : state.font;
            text.text = ArabicTextLayout.Visual(ArabicTextLayout.Shape(translated));
            text.alignment = state.alignment == TextAnchor.MiddleLeft ? TextAnchor.MiddleRight : state.alignment;
        }
        else { text.font = state.font; text.alignment = state.alignment; text.text = state.original; }
        state.rendered = text.text;
    }
    private static TextAlignmentOptions Mirror(TextAlignmentOptions alignment)
    {
        int value = (int)alignment, horizontal = value & 0xFF;
        return horizontal == 1 ? (TextAlignmentOptions)((value & ~0xFF) | 4) : alignment;
    }
    private static float LayoutWidth(TMP_Text text)
    {
        var fitter = text.GetComponent<ContentSizeFitter>();
        // A preferred-width fitter reads this text's preferred width. Do not feed its
        // shrinking result back into Arabic wrapping; use the containing layout width.
        if (fitter && fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained && text.transform.parent is RectTransform parent)
            return Mathf.Max(1, parent.rect.width);
        return text.rectTransform.rect.width;
    }
    private void EnsureSwitch()
    {
        var scene = SceneManager.GetActiveScene();
        switches.RemoveAll(b => !b);
        if (switches.Any(b => b.gameObject.scene == scene)) return;
        var reload = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b => b.gameObject.scene == scene && b.name == "Restart Button Variant" && b.transform.parent && b.transform.parent.name == "Right Panel");
        if (!reload || !(reload.transform.parent is RectTransform sourcePanel)) return;

        // Use a transform-only sibling of the right panel so the language choice also
        // remains available when Main Menu hides its gameplay controls.
        var panel = new GameObject("Language Controls", typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(sourcePanel.parent, false);
        panel.anchorMin = sourcePanel.anchorMin; panel.anchorMax = sourcePanel.anchorMax;
        panel.pivot = sourcePanel.pivot; panel.sizeDelta = sourcePanel.sizeDelta;
        panel.anchoredPosition3D = sourcePanel.anchoredPosition3D;
        panel.localRotation = sourcePanel.localRotation; panel.localScale = sourcePanel.localScale;

        // Copy the actual control, including its outline, glow, hover states and UI audio.
        // Replacing the entire event object removes every inherited Reload callback.
        var go = Instantiate(reload.gameObject, panel, false);
        go.SetActive(false); go.name = "EN AR Language Button";
        var button = go.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ToggleLanguage); button.interactable = true;
        var icon = go.transform.Find("Icon") as RectTransform;
        if (!icon) { Destroy(panel.gameObject); return; }
        var rect = (RectTransform)go.transform;
        rect.anchoredPosition += Vector2.up * (icon.rect.height + 12);
        var oldIcon = icon.GetComponent<Image>(); if (oldIcon) oldIcon.enabled = false;
        var label = new GameObject("Language", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(icon, false);
        var lr = (RectTransform)label.transform;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        var tmp = label.GetComponent<TextMeshProUGUI>();
        var styledText = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.gameObject.scene == scene && t.font && t.font.name.Contains("Rajdhani"));
        if (styledText) tmp.font = styledText.font;
        tmp.text = Arabic ? "AR" : "EN"; tmp.fontSize = 20; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white; tmp.raycastTarget = true;
        if (button.targetGraphic == oldIcon || !button.targetGraphic) button.targetGraphic = tmp;
        go.SetActive(true); switches.Add(button);
    }
}
