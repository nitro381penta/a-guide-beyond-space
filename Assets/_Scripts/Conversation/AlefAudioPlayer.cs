using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AlefAudioPlayer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    public event Action OnSpeechPlaybackFinished;

    private void Awake()
    {
        if (audioSource != null)
        {
            Log($"[AlefAudioPlayer] Awake. AudioSource already assigned on {audioSource.gameObject.name}");
        }
        else
        {
            Log("[AlefAudioPlayer] Awake. No AudioSource assigned yet.");
        }
    }

    public void SetAudioSource(AudioSource source)
    {
        audioSource = source;

        if (audioSource != null)
        {
            Log($"[AlefAudioPlayer] AudioSource set to object: {audioSource.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[AlefAudioPlayer] SetAudioSource called with NULL.");
        }
    }

    public void PlayResponse(ConversationResponse response)
    {
        Log("[AlefAudioPlayer] PlayResponse called.");

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError($"[AlefAudioPlayer] Cannot play response because this GameObject '{gameObject.name}' is inactive.");
            OnSpeechPlaybackFinished?.Invoke();
            return;
        }

        if (response == null)
        {
            Debug.LogError("[AlefAudioPlayer] Response is NULL.");
            OnSpeechPlaybackFinished?.Invoke();
            return;
        }

        if (string.IsNullOrWhiteSpace(response.audio_base64))
        {
            Debug.LogError("[AlefAudioPlayer] audio_base64 is missing.");
            OnSpeechPlaybackFinished?.Invoke();
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[AlefAudioPlayer] AudioSource is NULL. Assign it before calling PlayResponse.");
            OnSpeechPlaybackFinished?.Invoke();
            return;
        }

        if (!audioSource.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[AlefAudioPlayer] AudioSource GameObject '{audioSource.gameObject.name}' is inactive.");
            OnSpeechPlaybackFinished?.Invoke();
            return;
        }

        StartCoroutine(PlayMp3FromBase64(response.audio_base64));
    }

    private IEnumerator PlayMp3FromBase64(string base64Audio)
    {
        Log("[AlefAudioPlayer] PlayMp3FromBase64 started.");

        byte[] audioBytes;
        try
        {
            audioBytes = Convert.FromBase64String(base64Audio);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AlefAudioPlayer] Failed to decode base64 audio: {e.Message}");
            OnSpeechPlaybackFinished?.Invoke();
            yield break;
        }

        string tempPath = Path.Combine(Application.persistentDataPath, "alef_reply.mp3");
        File.WriteAllBytes(tempPath, audioBytes);

        Log($"[AlefAudioPlayer] MP3 written to temp path: {tempPath}");

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AlefAudioPlayer] Failed to load audio clip: {request.error}");
            OnSpeechPlaybackFinished?.Invoke();
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

        if (clip == null)
        {
            Debug.LogError("[AlefAudioPlayer] Loaded AudioClip is NULL.");
            OnSpeechPlaybackFinished?.Invoke();
            yield break;
        }

        if (audioSource == null)
        {
            Debug.LogError("[AlefAudioPlayer] AudioSource became NULL before playback.");
            OnSpeechPlaybackFinished?.Invoke();
            yield break;
        }

        Log($"[AlefAudioPlayer] Playing clip on AudioSource object: {audioSource.gameObject.name}");

        audioSource.clip = clip;
        audioSource.Play();

        yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);

        Log("[AlefAudioPlayer] Playback finished.");
        OnSpeechPlaybackFinished?.Invoke();
    }

    private void Log(string message)
    {
        if (logToConsole)
            Debug.Log(message);
    }
}