using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ConversationApiClient : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string backendUrl = "https://beyond-space-rag-api.fly.dev/ask";
    [SerializeField] private int requestTimeoutSeconds = 120;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    public event Action<ConversationResponse> OnResponseReceived;
    public event Action<string> OnRequestFailed;

    private void Awake()
    {
        Log($"[ConversationApiClient] Awake. backendUrl = {backendUrl}");
    }

    private void Start()
    {
        Log($"[ConversationApiClient] Start. backendUrl = {backendUrl}");
    }

    public void SendAudioToBackend(byte[] wavData)
    {
        Log("[ConversationApiClient] SendAudioToBackend called.");

        if (wavData == null)
        {
            Fail("wavData is NULL.");
            return;
        }

        if (wavData.Length == 0)
        {
            Fail("wavData is empty.");
            return;
        }

        Log($"[ConversationApiClient] WAV bytes = {wavData.Length}");
        Log($"[ConversationApiClient] Sending request to: {backendUrl}");

        StartCoroutine(SendAudioCoroutine(wavData));
    }

    private IEnumerator SendAudioCoroutine(byte[] wavData)
    {
        WWWForm form = new WWWForm();

        // Muss exakt "audio" heißen, weil FastAPI erwartet:
        // async def ask(audio: UploadFile = File(...))
        form.AddBinaryData("audio", wavData, "question.wav", "audio/wav");

        using UnityWebRequest request = UnityWebRequest.Post(backendUrl, form);
        request.timeout = requestTimeoutSeconds;

        Log("[ConversationApiClient] UnityWebRequest created. Sending now...");

        UnityWebRequestAsyncOperation op = null;

        try
        {
            op = request.SendWebRequest();
        }
        catch (Exception e)
        {
            Fail($"Exception during SendWebRequest: {e.GetType().Name}: {e.Message}");
            yield break;
        }

        yield return op;

        Log($"[ConversationApiClient] Request finished. Result = {request.result}");
        Log($"[ConversationApiClient] Response code = {request.responseCode}");

        string rawResponse = request.downloadHandler != null ? request.downloadHandler.text : "";
        Log($"[ConversationApiClient] Raw response: {rawResponse}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorMessage =
                $"Request failed. Result={request.result}, Code={request.responseCode}, Error={request.error}, Body={rawResponse}";
            Fail(errorMessage);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            Fail("Backend returned empty response.");
            yield break;
        }

        ConversationResponse response = null;

        try
        {
            response = JsonUtility.FromJson<ConversationResponse>(rawResponse);
        }
        catch (Exception e)
        {
            Fail($"JSON parse failed: {e.Message}");
            yield break;
        }

        if (response == null)
        {
            Fail("Parsed ConversationResponse is null.");
            yield break;
        }

        Log($"[ConversationApiClient] Parsed transcript: {response.transcript}");
        Log($"[ConversationApiClient] Parsed answer_text: {response.answer_text}");
        Log($"[ConversationApiClient] audio_base64 length: {(response.audio_base64 != null ? response.audio_base64.Length : 0)}");
        Log("[ConversationApiClient] Invoking OnResponseReceived.");

        OnResponseReceived?.Invoke(response);
    }

    private void Fail(string message)
    {
        Debug.LogError($"[ConversationApiClient] {message}");
        OnRequestFailed?.Invoke(message);
    }

    private void Log(string message)
    {
        if (logToConsole)
            Debug.Log(message);
    }
}