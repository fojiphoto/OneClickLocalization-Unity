using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tools.Localization
{
    [System.Serializable]
    public class LocalizationEntry
    {
        public string Key;      // The ID
        [TextArea(2, 5)]
        public string SourceText; // English/Original
        [TextArea(2, 5)]
        public string Spanish;
        [TextArea(2, 5)]
        public string French;
    }

    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Data", order = 1)]
    public class LocalizationData : ScriptableObject
    {
        public List<LocalizationEntry> Entries = new List<LocalizationEntry>();

        public LocalizationEntry FindEntry(string sourceText)
        {
            return Entries.Find(e => e.SourceText == sourceText);
        }

        public LocalizationEntry FindEntryByKey(string key)
        {
            return Entries.Find(e => e.Key == key);
        }
    }
}
