using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Tools.Localization
{
    public static class DeepLService
    {
        private const string API_URL_FREE = "https://api-free.deepl.com/v2/translate";
        private const string API_URL_PRO = "https://api.deepl.com/v2/translate";

        [System.Serializable]
        private class DeepLResponse
        {
            public List<DeepLTranslation> translations;
        }

        [System.Serializable]
        private class DeepLTranslation
        {
            public string detected_source_language;
            public string text;
        }

        public static async Task<string> Translate(string text, string targetLang, string authKey)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(authKey))
                return text;

            string url = authKey.EndsWith(":fx") ? API_URL_FREE : API_URL_PRO;

            // Simple form encoding
            WWWForm form = new WWWForm();
            form.AddField("auth_key", authKey);
            form.AddField("text", text);
            form.AddField("target_lang", targetLang);
            form.AddField("source_lang", "EN"); // Assuming source is English for now

            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"DeepL Error: {www.error} | Response: {www.downloadHandler.text}");
                    return null;
                }
                else
                {
                    string json = www.downloadHandler.text;
                    try
                    {
                        DeepLResponse response = JsonUtility.FromJson<DeepLResponse>(json);
                        if (response != null && response.translations != null && response.translations.Count > 0)
                        {
                            return response.translations[0].text;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to parse DeepL response: {ex.Message}");
                    }
                }
            }
            return null;
        }
    }
}
