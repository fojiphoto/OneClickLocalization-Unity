using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tools.Localization
{
    public class ScriptScanner
    {
        public struct ScriptScanResult
        {
            public string filePath;
            public int lineNumber;
            public string matchedString;
        }

        public static List<ScriptScanResult> ScanScripts(string directoryPath)
        {
            List<ScriptScanResult> results = new List<ScriptScanResult>();
            
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogError("ScriptScanner: Directory not found!");
                return results;
            }

            string[] files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
            
            // Regex to match strings inside double quotes
            // Avoiding basic escaped quotes
            Regex stringRegex = new Regex("\"([^\"]*)\"");

            // Ignore list for common Unity strings
            HashSet<string> ignoredStrings = new HashSet<string>()
            {
                "", " ", "Text", "Image", "Button", "Player", "GameManager", "Untagged", "Default"
            };

            foreach (var file in files)
            {
                // Skip Editor scripts or Tool scripts to avoid scanning ourselves
                if (file.Contains("Editor") || file.Contains("Tools/Localization")) 
                    continue;

                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    
                    // Skip comments
                    if (line.StartsWith("//") || line.StartsWith("/*")) continue;
                    if (line.Contains("Debug.Log")) continue; // Optional: Skip logs

                    MatchCollection matches = stringRegex.Matches(line);

                    foreach (Match match in matches)
                    {
                        string content = match.Groups[1].Value;

                        if (!string.IsNullOrEmpty(content) && !ignoredStrings.Contains(content) && content.Length > 2)
                        {
                            results.Add(new ScriptScanResult
                            {
                                filePath = file,
                                lineNumber = i + 1,
                                matchedString = content
                            });
                        }
                    }
                }
            }

            return results;
        }
    }
}
