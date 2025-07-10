using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class GeminiChatManager : MonoBehaviour
{
    [Header("API Settings")]
    public string apiKey = "YOUR_API_KEY_HERE"; // Replace this with your Gemini API key

    [Header("UI References")]
    public TMP_InputField inputField;
    public Transform contentArea;
    public GameObject userMessagePrefab;
    public GameObject aiMessagePrefab;

    private string geminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    public void OnSendClicked()
    {
        string userMessage = inputField.text.Trim();
        if (string.IsNullOrEmpty(userMessage)) return;

        AddUserMessage(userMessage);
        inputField.text = "";
        inputField.ActivateInputField();

        StartCoroutine(SendToGemini(userMessage));
    }

    void AddUserMessage(string message)
    {
        GameObject go = Instantiate(userMessagePrefab, contentArea);
        go.GetComponentInChildren<TMP_Text>().text = message;
        StartCoroutine(FixLayout());
    }

    void AddAIMessage(string message)
    {
        GameObject go = Instantiate(aiMessagePrefab, contentArea);
        go.GetComponentInChildren<TMP_Text>().text = message;
        StartCoroutine(FixLayout());
    }

    IEnumerator SendToGemini(string userPrompt)
    {
        string json = "{\"contents\":[{\"parts\":[{\"text\":\"" + EscapeJson(userPrompt) + "\"}]}]}";

        using (UnityWebRequest req = new UnityWebRequest(geminiEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-goog-api-key", apiKey); // Matches curl format

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string responseJson = req.downloadHandler.text;
                string reply = ParseGeminiResponse(responseJson);
                AddAIMessage(reply);
            }
            else
            {
                Debug.LogError("Gemini API Error: " + req.error);
                Debug.LogError("Response Body: " + req.downloadHandler.text);
                AddAIMessage("Oops, something went wrong.");
            }
        }
    }

    string EscapeJson(string input)
    {
        return input.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "");
    }

    string ParseGeminiResponse(string json)
    {
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
            return response.candidates[0].content.parts[0].text.Trim();
        }
        catch
        {
            return "Error parsing Gemini response.";
        }
    }

    IEnumerator FixLayout()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea as RectTransform);
    }

    // Gemini JSON Wrappers
    [System.Serializable]
    public class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    public class Candidate
    {
        public Content content;
    }

    [System.Serializable]
    public class Content
    {
        public Part[] parts;
        public string role;
    }

    [System.Serializable]
    public class Part
    {
        public string text;
    }
}
