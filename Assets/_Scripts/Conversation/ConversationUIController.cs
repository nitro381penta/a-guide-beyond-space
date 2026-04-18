using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConversationUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AlefConversationStateMachine stateMachine;
    [SerializeField] private QuestMicrophoneRecorder microphoneRecorder;
    [SerializeField] private ConversationApiClient apiClient;
    [SerializeField] private AlefAudioPlayer audioPlayer;
    [SerializeField] private AlefKeplerSequenceManager sequenceManager;
    [SerializeField] private AlefConversationTrigger conversationTrigger;

    [Header("Background Music")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private float normalMusicVolume = 0.3f;
    [SerializeField] private float duckedMusicVolume = 0.04f;
    [SerializeField] private float musicFadeDuration = 1f;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button startRecordingButton;
    [SerializeField] private Button stopRecordingButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text transcriptText;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    private Coroutine musicFadeRoutine;

    private void Awake()
    {
        Log("[ConversationUIController] Awake");

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            Log("[ConversationUIController] Conversation panel hidden on Awake.");
        }
        else
        {
            Debug.LogWarning("[ConversationUIController] panelRoot is NULL.");
        }

        if (startRecordingButton != null)
        {
            startRecordingButton.onClick.RemoveListener(OnStartRecordingClicked);
            startRecordingButton.onClick.AddListener(OnStartRecordingClicked);
            Log("[ConversationUIController] Start button listener assigned.");
        }
        else
        {
            Debug.LogWarning("[ConversationUIController] startRecordingButton is NULL.");
        }

        if (stopRecordingButton != null)
        {
            stopRecordingButton.onClick.RemoveListener(OnStopRecordingClicked);
            stopRecordingButton.onClick.AddListener(OnStopRecordingClicked);
            Log("[ConversationUIController] Stop button listener assigned.");
        }
        else
        {
            Debug.LogWarning("[ConversationUIController] stopRecordingButton is NULL.");
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            Log("[ConversationUIController] Close button listener assigned.");
        }
        else
        {
            Debug.LogWarning("[ConversationUIController] closeButton is NULL.");
        }
    }

    private void OnEnable()
    {
        if (stateMachine != null)
            stateMachine.OnStateChanged += HandleStateChanged;

        if (microphoneRecorder != null)
            microphoneRecorder.OnRecordingFinished += HandleRecordingFinished;

        if (apiClient != null)
            apiClient.OnResponseReceived += HandleApiResponse;

        if (apiClient != null)
            apiClient.OnRequestFailed += HandleRequestFailed;

        if (audioPlayer != null)
            audioPlayer.OnSpeechPlaybackFinished += HandleSpeechPlaybackFinished;
    }

    private void OnDisable()
    {
        if (stateMachine != null)
            stateMachine.OnStateChanged -= HandleStateChanged;

        if (microphoneRecorder != null)
            microphoneRecorder.OnRecordingFinished -= HandleRecordingFinished;

        if (apiClient != null)
            apiClient.OnResponseReceived -= HandleApiResponse;

        if (apiClient != null)
            apiClient.OnRequestFailed -= HandleRequestFailed;

        if (audioPlayer != null)
            audioPlayer.OnSpeechPlaybackFinished -= HandleSpeechPlaybackFinished;
    }

    public void ShowConversationPanel()
    {
        Log("[ConversationUIController] ShowConversationPanel called.");

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (sequenceManager != null)
            sequenceManager.FaceAstronautToCameraForConversation();

        FadeBackgroundMusic(duckedMusicVolume, musicFadeDuration);

        SetStatus("Du kannst mir jetzt eine Frage stellen.");
        SetTranscript(string.Empty);

        RefreshButtons();
    }

    public void HideConversationPanel()
    {
        Log("[ConversationUIController] HideConversationPanel called.");

        if (panelRoot != null)
            panelRoot.SetActive(false);

        FadeBackgroundMusic(normalMusicVolume, musicFadeDuration);
    }

    public void OnCloseButtonClicked()
    {
        Log("[ConversationUIController] Close button clicked.");

        if (stateMachine != null && stateMachine.IsBusy())
        {
            Log("[ConversationUIController] Close ignored because system is busy.");
            return;
        }

        HideConversationPanel();

        SetStatus(string.Empty);
        SetTranscript(string.Empty);

        if (stateMachine != null)
            stateMachine.SetState(AlefConversationStateMachine.ConversationState.ConversationReady);

        if (sequenceManager != null)
            sequenceManager.FaceAstronautToCameraForConversation();

        if (conversationTrigger != null)
            conversationTrigger.EnableConversationMode();

        RefreshButtons();

        Log("[ConversationUIController] Panel closed. Conversation is ready again.");
    }

    private void OnStartRecordingClicked()
    {
        Log("[ConversationUIController] Start Recording clicked.");

        if (stateMachine == null || microphoneRecorder == null)
        {
            Debug.LogWarning("[ConversationUIController] stateMachine or microphoneRecorder is NULL.");
            return;
        }

        stateMachine.SetState(AlefConversationStateMachine.ConversationState.Recording);
        Log("[ConversationUIController] State set to Recording.");

        SetStatus("Ich höre zu …");
        RefreshButtons();

        microphoneRecorder.StartRecording();
    }

    private void OnStopRecordingClicked()
    {
        Log("[ConversationUIController] Stop Recording clicked.");

        if (stateMachine == null || microphoneRecorder == null)
        {
            Debug.LogWarning("[ConversationUIController] stateMachine or microphoneRecorder is NULL.");
            return;
        }

        stateMachine.SetState(AlefConversationStateMachine.ConversationState.Uploading);
        Log("[ConversationUIController] State set to Uploading.");

        SetStatus("Ich verarbeite deine Frage …");
        RefreshButtons();

        microphoneRecorder.StopRecording();
    }

    private void HandleRecordingFinished(byte[] wavData)
    {
        Log("[ConversationUIController] HandleRecordingFinished called.");

        if (wavData == null || wavData.Length == 0)
        {
            Debug.LogWarning("[ConversationUIController] WAV data is empty.");
            HandleRequestFailed("Leere Audioaufnahme.");
            return;
        }

        if (apiClient == null || stateMachine == null)
        {
            Debug.LogWarning("[ConversationUIController] apiClient or stateMachine is NULL.");
            return;
        }

        stateMachine.SetState(AlefConversationStateMachine.ConversationState.Thinking);
        Log("[ConversationUIController] State set to Thinking.");

        SetStatus("Ich denke nach …");
        RefreshButtons();

        apiClient.SendAudioToBackend(wavData);
    }

    private void HandleApiResponse(ConversationResponse response)
    {
        Log("[ConversationUIController] HandleApiResponse called.");

        if (response == null)
        {
            HandleRequestFailed("Leere Antwort vom Backend.");
            return;
        }

        if (stateMachine == null)
        {
            Debug.LogWarning("[ConversationUIController] stateMachine is NULL.");
            return;
        }

        string shownQuestion = !string.IsNullOrWhiteSpace(response.display_text)
            ? response.display_text
            : response.transcript;

        SetTranscript(shownQuestion);
        SetStatus("Ich antworte …");

        stateMachine.SetState(AlefConversationStateMachine.ConversationState.Speaking);
        Log("[ConversationUIController] State set to Speaking.");

        RefreshButtons();

        if (sequenceManager != null)
            sequenceManager.FaceAstronautToCameraForConversation();

        if (audioPlayer != null)
        {
            audioPlayer.PlayResponse(response);
            Log("[ConversationUIController] Forwarded response to AlefAudioPlayer.");
        }
        else
        {
            Debug.LogWarning("[ConversationUIController] audioPlayer is NULL.");
            HandleRequestFailed("Audio Player fehlt.");
        }
    }

    private void HandleRequestFailed(string errorMessage)
    {
        Debug.LogError($"[ConversationUIController] Request failed: {errorMessage}");

        if (stateMachine != null)
            stateMachine.SetState(AlefConversationStateMachine.ConversationState.Error);

        SetStatus("Es ist ein Fehler aufgetreten.");
        RefreshButtons();
    }

    private void HandleSpeechPlaybackFinished()
    {
        Log("[ConversationUIController] Speech playback finished.");

        if (stateMachine != null)
            stateMachine.SetState(AlefConversationStateMachine.ConversationState.ConversationReady);

        SetStatus("Du kannst mir eine weitere Frage stellen.");
        RefreshButtons();
    }

    private void HandleStateChanged(AlefConversationStateMachine.ConversationState newState)
    {
        Log($"[ConversationUIController] State changed to: {newState}");

        switch (newState)
        {
            case AlefConversationStateMachine.ConversationState.Recording:
            case AlefConversationStateMachine.ConversationState.Uploading:
            case AlefConversationStateMachine.ConversationState.Thinking:
            case AlefConversationStateMachine.ConversationState.Speaking:
                FadeBackgroundMusic(duckedMusicVolume, musicFadeDuration);
                break;

            case AlefConversationStateMachine.ConversationState.ConversationReady:
            case AlefConversationStateMachine.ConversationState.Error:
                FadeBackgroundMusic(normalMusicVolume, musicFadeDuration);
                break;
        }

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (stateMachine == null)
            return;

        bool isRecording = stateMachine.CurrentState == AlefConversationStateMachine.ConversationState.Recording;
        bool canStart = stateMachine.CurrentState == AlefConversationStateMachine.ConversationState.ConversationReady;
        bool canClose = !stateMachine.IsBusy();

        if (startRecordingButton != null)
            startRecordingButton.gameObject.SetActive(canStart);

        if (stopRecordingButton != null)
            stopRecordingButton.gameObject.SetActive(isRecording);

        if (closeButton != null)
            closeButton.interactable = canClose;

        Log($"[ConversationUIController] RefreshButtons -> state: {stateMachine.CurrentState}, canStart: {canStart}, isRecording: {isRecording}, canClose: {canClose}");
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    private void SetTranscript(string text)
    {
        if (transcriptText != null)
        {
            transcriptText.text = string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : $"Frage: {text}";
        }
    }

    private void FadeBackgroundMusic(float targetVolume, float duration)
    {
        if (backgroundMusicSource == null)
            return;

        if (!backgroundMusicSource.isPlaying && backgroundMusicSource.clip != null)
            backgroundMusicSource.Play();

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeMusicRoutine(targetVolume, duration));
    }

    private IEnumerator FadeMusicRoutine(float targetVolume, float duration)
    {
        float startVolume = backgroundMusicSource.volume;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, k);
            yield return null;
        }

        backgroundMusicSource.volume = targetVolume;
        musicFadeRoutine = null;
    }

    private void Log(string message)
    {
        if (logToConsole)
            Debug.Log(message);
    }
}