using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tools.Localization
{
    public enum Language
    {
        EN,
        ES,
        FR
    }

    public class RuntimeLocalizationManager : MonoBehaviour
    {
        public LocalizationData sourceData;
        public Language currentLanguage = Language.EN;

        // Cache components and their original Keys
        private class LocalizedItem
        {
            public Component component; // Text or TMP_Text
            public string key;
        }

        private List<LocalizedItem> items = new List<LocalizedItem>();

        private void Start()
        {
            // If data is missing, try loading from Resources
            if (sourceData == null)
            {
                sourceData = Resources.Load<LocalizationData>("LocalizationData");
            }

            if (sourceData == null)
            {
                Debug.LogError("RuntimeLocalizationManager: Missing LocalizationData!");
                return;
            }

            FindAndCacheLocalizedText();
            UpdateAllText();
        }

        private void FindAndCacheLocalizedText()
        {
            items.Clear();

            // Find all Legacy Text
            var textComps = FindObjectsOfType<Text>(true);
            foreach (var t in textComps)
            {
                if (IsKey(t.text))
                {
                    items.Add(new LocalizedItem { component = t, key = t.text });
                }
            }

            // Find all TMP
            var tmpComps = FindObjectsOfType<TMP_Text>(true);
            foreach (var t in tmpComps)
            {
                if (IsKey(t.text))
                {
                    items.Add(new LocalizedItem { component = t, key = t.text });
                }
            }
            
            Debug.Log($"Localization: Found {items.Count} localized items.");
        }

        private bool IsKey(string text)
        {
            // Simple heuristic: keys usually start with LOC_
            // Adjust this if your logic changes
            return !string.IsNullOrEmpty(text) && text.StartsWith("LOC_");
        }

        public void SetLanguage(Language lang)
        {
            currentLanguage = lang;
            UpdateAllText();
        }

        private void UpdateAllText()
        {
            foreach (var item in items)
            {
                var entry = sourceData.FindEntryByKey(item.key);
                if (entry != null)
                {
                    string textValue = entry.SourceText; // Default EN

                    switch (currentLanguage)
                    {
                        case Language.ES:
                            textValue = entry.Spanish;
                            break;
                        case Language.FR:
                            textValue = entry.French;
                            break;
                        case Language.EN:
                        default:
                            textValue = entry.SourceText;
                            break;
                    }

                    SetText(item.component, textValue);
                }
            }
        }

        private void SetText(Component comp, string text)
        {
            if (comp is Text t) t.text = text;
            else if (comp is TMP_Text tmp) tmp.text = text;
        }

        // Helper methods for Unity UI Buttons
        public void SetLanguageEN() => SetLanguage(Language.EN);
        public void SetLanguageES() => SetLanguage(Language.ES);
        public void SetLanguageFR() => SetLanguage(Language.FR);
    }
}
