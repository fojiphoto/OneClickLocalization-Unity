using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Tools.Localization
{
    public class SceneScanner
    {
        public struct ScanResult
        {
            public GameObject gameObject;
            public Component component;
            public string text;
            public string suggestedKey;
        }

        public static List<ScanResult> ScanCurrentScene()
        {
            List<ScanResult> results = new List<ScanResult>();
            Scene currentScene = SceneManager.GetActiveScene();

            GameObject[] rootObjects = currentScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                ScanRecursive(root.transform, results);
            }

            return results;
        }

        private static void ScanRecursive(Transform parent, List<ScanResult> results)
        {
            // Check for Legacy Text
            Text legacyText = parent.GetComponent<Text>();
            if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
            {
                AddResult(results, parent.gameObject, legacyText, legacyText.text, "Text");
            }

            // Check for TMP
            TMP_Text tmpText = parent.GetComponent<TMP_Text>();
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
            {
                AddResult(results, parent.gameObject, tmpText, tmpText.text, "TMP");
            }

            foreach (Transform child in parent)
            {
                ScanRecursive(child, results);
            }
        }

        private static void AddResult(List<ScanResult> results, GameObject obj, Component comp, string text, string type)
        {
            // Avoid scanning text that is already a key (heuristic: starts with LOC_ and has no spaces)
            if (text.StartsWith("LOC_") && !text.Contains(" "))
                return;

            ScanResult result = new ScanResult();
            result.gameObject = obj;
            result.component = comp;
            result.text = text;
            
            // Clean object name to form a valid key part
            string cleanName = System.Text.RegularExpressions.Regex.Replace(obj.name.ToUpper(), "[^A-Z0-9]", "_");
            string sceneName = obj.scene.name.ToUpper().Replace(" ", "_");
            
            // Simple hash for uniqueness based on content
            int textHash = Mathf.Abs(text.GetHashCode() % 1000);

            result.suggestedKey = $"LOC_{cleanName}_{textHash}";
            
            results.Add(result);
        }
    }
}
