using System;
using UnityEngine;

public class QuestMicrophoneRecorder : MonoBehaviour
{
    [Header("Recording")]
    [SerializeField] private int maxRecordingSeconds = 20;
    [SerializeField] private int sampleRate = 16000;

    public event Action<byte[]> OnRecordingFinished;

    private AudioClip currentClip;
    private string microphoneDevice;
    private bool isRecording;

    public void StartRecording()
    {
        Debug.Log("[QuestMicrophoneRecorder] StartRecording called.");

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            Debug.Log("[QuestMicrophoneRecorder] Requested microphone permission.");
            return;
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[QuestMicrophoneRecorder] No microphone device found.");
            return;
        }

        microphoneDevice = Microphone.devices[0];
        Debug.Log($"[QuestMicrophoneRecorder] Using microphone device: {microphoneDevice}");

        currentClip = Microphone.Start(microphoneDevice, false, maxRecordingSeconds, sampleRate);
        isRecording = true;

        Debug.Log("[QuestMicrophoneRecorder] Recording started.");
    }

    public void StopRecording()
    {
        Debug.Log("[QuestMicrophoneRecorder] StopRecording called.");

        if (!isRecording || currentClip == null)
        {
            Debug.LogWarning("[QuestMicrophoneRecorder] StopRecording ignored because nothing is recording.");
            return;
        }

        int position = Microphone.GetPosition(microphoneDevice);
        Debug.Log($"[QuestMicrophoneRecorder] Microphone position: {position}");

        Microphone.End(microphoneDevice);
        isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("[QuestMicrophoneRecorder] Recording position invalid.");
            return;
        }

        float[] samples = new float[position * currentClip.channels];
        currentClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "trimmed_recording",
            position,
            currentClip.channels,
            currentClip.frequency,
            false
        );
        trimmedClip.SetData(samples, 0);

        byte[] wavData = AudioClipWavUtility.FromAudioClip(trimmedClip);

        Debug.Log($"[QuestMicrophoneRecorder] WAV created. Bytes: {wavData.Length}");

        OnRecordingFinished?.Invoke(wavData);
    }
}