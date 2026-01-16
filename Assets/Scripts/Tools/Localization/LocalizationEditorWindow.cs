using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Tools.Localization;
using System.IO;
using UnityEngine.UI;
using TMPro;

namespace Tools.Localization.Editor
{
    public class LocalizationEditorWindow : EditorWindow
    {
        private string deepLAuthKey = "";
        private LocalizationData locData;
        private List<SceneScanner.ScanResult> scanResults = new List<SceneScanner.ScanResult>();
        private List<ScriptScanner.ScriptScanResult> scriptResults = new List<ScriptScanner.ScriptScanResult>();
        private Vector2 scrollPos;
        private const string DATA_PATH = "Assets/Resources/LocalizationData.asset";

        [MenuItem("Tools/Localization/Localization Manager")]
        public static void ShowWindow()
        {
            GetWindow<LocalizationEditorWindow>("Localization Manager");
        }

        private void OnEnable()
        {
            LoadData();
            deepLAuthKey = EditorPrefs.GetString("DeepL_AuthKey", "");
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("DeepL_AuthKey", deepLAuthKey);
        }

        private void OnGUI()
        {
            GUILayout.Label("Localization Configuration", EditorStyles.boldLabel);
            
            deepLAuthKey = EditorGUILayout.TextField("DeepL API Key", deepLAuthKey);
            
            // Display ref to ScriptableObject
            EditorGUILayout.ObjectField("Data File", locData, typeof(LocalizationData), false);

            if (locData == null)
            {
                if (GUILayout.Button("Create Data File"))
                {
                    CreateDataAsset();
                }
                return;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Scan Scene", GUILayout.Height(30)))
            {
                ScanScene();
            }
            
            if (GUILayout.Button("Experimental: Scan Scripts for Strings"))
            {
                ScanScripts();
            }

            EditorGUILayout.Space();

            if (scriptResults != null && scriptResults.Count > 0)
            {
                GUILayout.Label($"Found {scriptResults.Count} strings in Scripts:", EditorStyles.boldLabel);
                GUILayout.Label("(Note: Not all should be translated!)", EditorStyles.miniLabel);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

                foreach (var res in scriptResults)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    // Show file name nicely
                    string fileName = Path.GetFileName(res.filePath);
                    EditorGUILayout.LabelField($"{fileName}:{res.lineNumber}", GUILayout.Width(200));
                    EditorGUILayout.SelectableLabel(res.matchedString, GUILayout.Height(20));
                    
                    if(GUILayout.Button("Add", GUILayout.Width(40)))
                    {
                        AddToData(res.matchedString);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            if (scanResults != null && scanResults.Count > 0)
            {
                GUILayout.Label($"Found {scanResults.Count} strings not yet keys:", EditorStyles.boldLabel);
                
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                foreach (var res in scanResults)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField(res.gameObject.name, GUILayout.Width(150));
                    EditorGUILayout.LabelField(res.text);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                
                if (GUILayout.Button("Process & Translate", GUILayout.Height(40)))
                {
                    ProcessAndTranslate();
                }
            }
            else
            {
                GUILayout.Label("No scanning results or scene clean.");
            }

            EditorGUILayout.Space();
            GUILayout.Label("Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Replace Scene Text With Keys"))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Replace text with keys?", "Yes", "Cancel"))
                {
                    ReplaceSceneText();
                }
            }
        }

        private void ScanScene()
        {
            scanResults = SceneScanner.ScanCurrentScene();
            Debug.Log($"Scanned {scanResults.Count} items.");
        }

        private async void ProcessAndTranslate()
        {
            if (string.IsNullOrEmpty(deepLAuthKey))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a DeepL API Key.", "OK");
                return;
            }

            foreach (var res in scanResults)
            {
                if (locData.FindEntryByKey(res.suggestedKey) == null)
                {
                    LocalizationEntry entry = new LocalizationEntry();
                    entry.Key = res.suggestedKey;
                    entry.SourceText = res.text;
                    
                    entry.Spanish = await DeepLService.Translate(res.text, "ES", deepLAuthKey);
                    entry.French = await DeepLService.Translate(res.text, "FR", deepLAuthKey);

                    locData.Entries.Add(entry);
                }
            }

            SaveData();
            EditorUtility.DisplayDialog("Success", "Translation complete!", "OK");
            scanResults.Clear();
        }

        private void ReplaceSceneText()
        {
            var currentObjects = SceneScanner.ScanCurrentScene();
            int count = 0;

            foreach (var item in currentObjects)
            {
                // Try to find by Key first (Primary Method - matches what Process step just added)
                var entry = locData.FindEntryByKey(item.suggestedKey);

                // Fallback: Find by content (Secondary Method - if key generation changed but text is same)
                if (entry == null)
                {
                    entry = locData.FindEntry(item.text);
                }

                if (entry != null)
                {
                    if (item.component is Text txt)
                    {
                        Undo.RecordObject(txt, "Localize Text");
                        txt.text = entry.Key;
                    }
                    else if (item.component is TMP_Text tmp)
                    {
                        Undo.RecordObject(tmp, "Localize Text");
                        tmp.text = entry.Key;
                    }
                    count++;
                }
                else
                {
                    Debug.LogWarning($"[Localization] Could not find entry for: '{item.text}'. Suggested Key was: {item.suggestedKey}");
                }
            }
            Debug.Log($"[Localization] Replaced {count} texts with keys.");
        }

        private void LoadData()
        {
            locData = AssetDatabase.LoadAssetAtPath<LocalizationData>(DATA_PATH);
        }

        private void CreateDataAsset()
        {
            locData = ScriptableObject.CreateInstance<LocalizationData>();
            string dir = Path.GetDirectoryName(DATA_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(locData, DATA_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SaveData()
        {
            EditorUtility.SetDirty(locData);
            AssetDatabase.SaveAssets();
        }

        private void ScanScripts()
        {
            string path = Application.dataPath + "/Scripts"; 
            scriptResults = ScriptScanner.ScanScripts(path);
            Debug.Log($"Scanned scripts. Found {scriptResults.Count} strings.");
        }

        private async void AddToData(string text)
        {
            string cleanKey = "LOC_" + System.Text.RegularExpressions.Regex.Replace(text.ToUpper(), "[^A-Z0-9]", "_");
             if (cleanKey.Length > 30) cleanKey = cleanKey.Substring(0, 30); 
             cleanKey += "_" + Mathf.Abs(text.GetHashCode() % 1000);

            if (locData.FindEntryByKey(cleanKey) == null)
            {
                LocalizationEntry entry = new LocalizationEntry();
                entry.Key = cleanKey;
                entry.SourceText = text;

                if (!string.IsNullOrEmpty(deepLAuthKey))
                {
                    entry.Spanish = await DeepLService.Translate(text, "ES", deepLAuthKey);
                    entry.French = await DeepLService.Translate(text, "FR", deepLAuthKey);
                }
                else
                {
                    Debug.LogWarning("No API Key, skipping translation.");
                }

                locData.Entries.Add(entry);
                SaveData();
                Debug.Log($"Added {text} to data.");
            }
        }
    }
}
