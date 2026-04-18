using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FangorPulseController : MonoBehaviour, IWaveField
{
    [Header("Existing Aura Objects")]
    [SerializeField] private Transform auraInner;
    [SerializeField] private Transform auraMiddle;
    [SerializeField] private Transform auraOuter;

    [Header("Renderers")]
    [SerializeField] private Renderer innerRenderer;
    [SerializeField] private Renderer middleRenderer;
    [SerializeField] private Renderer outerRenderer;

    [Header("Start State")]
    [SerializeField] private bool hiddenAtStart = true;

    [Header("Pulse Speed")]
    [SerializeField] private float pulseSpeed = 0.9f;

    [Header("Scale Pulse Around Current Size")]
    [SerializeField] private float innerScalePulse = 0.008f;
    [SerializeField] private float middleScalePulse = 0.014f;
    [SerializeField] private float outerScalePulse = 0.022f;

    [Header("Opacity Pulse")]
    [SerializeField] private bool pulseAlpha = true;
    [SerializeField] private float innerAlphaMin = 0.10f;
    [SerializeField] private float innerAlphaMax = 0.22f;
    [SerializeField] private float middleAlphaMin = 0.08f;
    [SerializeField] private float middleAlphaMax = 0.18f;
    [SerializeField] private float outerAlphaMin = 0.06f;
    [SerializeField] private float outerAlphaMax = 0.14f;

    [Header("Emission Pulse")]
    [SerializeField] private bool pulseEmission = true;
    [SerializeField] private float innerEmissionMulMin = 1.2f;
    [SerializeField] private float innerEmissionMulMax = 2.2f;
    [SerializeField] private float middleEmissionMulMin = 1.3f;
    [SerializeField] private float middleEmissionMulMax = 2.6f;
    [SerializeField] private float outerEmissionMulMin = 1.4f;
    [SerializeField] private float outerEmissionMulMax = 3.0f;

    [Header("Audio")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private float normalBackgroundVolume = 0.3f;
    [SerializeField] private float duckedBackgroundVolume = 0.03f;

    [SerializeField] private AudioSource pulseAudioSource;
    [SerializeField] private AudioClip pulseLoopClip;
    [SerializeField] private float pulseAudioVolume = 0.55f;
    [SerializeField] private float audioFadeDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    private bool isPlaying;

    private Vector3 innerBaseScale;
    private Vector3 middleBaseScale;
    private Vector3 outerBaseScale;

    private Material innerMat;
    private Material middleMat;
    private Material outerMat;

    private Color innerBaseColor;
    private Color middleBaseColor;
    private Color outerBaseColor;

    private Color innerBaseEmission;
    private Color middleBaseEmission;
    private Color outerBaseEmission;

    private Coroutine bgFadeRoutine;
    private Coroutine pulseFadeRoutine;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        BindReferences();
        CacheCurrentState();

        if (pulseAudioSource != null)
        {
            pulseAudioSource.playOnAwake = false;
            pulseAudioSource.loop = true;
            pulseAudioSource.volume = 0f;

            if (pulseLoopClip != null)
                pulseAudioSource.clip = pulseLoopClip;
        }

        if (hiddenAtStart)
            SetAuraVisible(false);
        else
            SetAuraVisible(true);

        if (logToConsole)
            Debug.Log("[FangorPulseController] Awake complete");
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        float t = Time.time * pulseSpeed;

        PulseLayer(auraInner, innerBaseScale, innerScalePulse, t + 0.0f);
        PulseLayer(auraMiddle, middleBaseScale, middleScalePulse, t + 0.9f);
        PulseLayer(auraOuter, outerBaseScale, outerScalePulse, t + 1.8f);

        if (pulseAlpha)
        {
            PulseAlpha(innerMat, innerBaseColor, innerAlphaMin, innerAlphaMax, t + 0.0f);
            PulseAlpha(middleMat, middleBaseColor, middleAlphaMin, middleAlphaMax, t + 0.9f);
            PulseAlpha(outerMat, outerBaseColor, outerAlphaMin, outerAlphaMax, t + 1.8f);
        }

        if (pulseEmission)
        {
            PulseEmission(innerMat, innerBaseEmission, innerEmissionMulMin, innerEmissionMulMax, t + 0.0f);
            PulseEmission(middleMat, middleBaseEmission, middleEmissionMulMin, middleEmissionMulMax, t + 0.9f);
            PulseEmission(outerMat, outerBaseEmission, outerEmissionMulMin, outerEmissionMulMax, t + 1.8f);
        }
    }

    public void Play()
    {
        BindReferences();
        CacheCurrentState();

        isPlaying = true;
        SetAuraVisible(true);
        HandleAudioOnPlay();

        if (logToConsole)
            Debug.Log("[FangorPulseController] Play");
    }

    public void Stop()
    {
        isPlaying = false;

        RestoreState();
        HandleAudioOnStop();

        if (hiddenAtStart)
            SetAuraVisible(false);

        if (logToConsole)
            Debug.Log("[FangorPulseController] Stop");
    }

    public void Toggle()
    {
        if (isPlaying) Stop();
        else Play();
    }

    [ContextMenu("Recache Current State")]
    public void RecacheCurrentState()
    {
        BindReferences();
        CacheCurrentState();

        if (logToConsole)
            Debug.Log("[FangorPulseController] Recached current state");
    }

    private void BindReferences()
    {
        if (auraInner != null && innerRenderer == null)
            innerRenderer = auraInner.GetComponent<Renderer>();

        if (auraMiddle != null && middleRenderer == null)
            middleRenderer = auraMiddle.GetComponent<Renderer>();

        if (auraOuter != null && outerRenderer == null)
            outerRenderer = auraOuter.GetComponent<Renderer>();

        if (innerRenderer != null && innerMat == null)
            innerMat = innerRenderer.material;

        if (middleRenderer != null && middleMat == null)
            middleMat = middleRenderer.material;

        if (outerRenderer != null && outerMat == null)
            outerMat = outerRenderer.material;
    }

    private void CacheCurrentState()
    {
        if (auraInner != null) innerBaseScale = auraInner.localScale;
        if (auraMiddle != null) middleBaseScale = auraMiddle.localScale;
        if (auraOuter != null) outerBaseScale = auraOuter.localScale;

        if (innerMat != null)
        {
            innerBaseColor = GetBaseColor(innerMat);
            innerBaseEmission = GetEmissionColor(innerMat);
        }

        if (middleMat != null)
        {
            middleBaseColor = GetBaseColor(middleMat);
            middleBaseEmission = GetEmissionColor(middleMat);
        }

        if (outerMat != null)
        {
            outerBaseColor = GetBaseColor(outerMat);
            outerBaseEmission = GetEmissionColor(outerMat);
        }
    }

    private void RestoreState()
    {
        if (auraInner != null) auraInner.localScale = innerBaseScale;
        if (auraMiddle != null) auraMiddle.localScale = middleBaseScale;
        if (auraOuter != null) auraOuter.localScale = outerBaseScale;

        SetBaseColor(innerMat, innerBaseColor);
        SetBaseColor(middleMat, middleBaseColor);
        SetBaseColor(outerMat, outerBaseColor);

        SetEmissionColor(innerMat, innerBaseEmission);
        SetEmissionColor(middleMat, middleBaseEmission);
        SetEmissionColor(outerMat, outerBaseEmission);
    }

    private void PulseLayer(Transform target, Vector3 baseScale, float amount, float phase)
    {
        if (target == null)
            return;

        float mul = 1f + Mathf.Sin(phase) * amount;
        target.localScale = baseScale * mul;
    }

    private void PulseAlpha(Material mat, Color baseColor, float aMin, float aMax, float phase)
    {
        if (mat == null)
            return;

        float k = 0.5f + 0.5f * Mathf.Sin(phase);
        float a = Mathf.Lerp(aMin, aMax, k);

        Color c = baseColor;
        c.a = a;
        SetBaseColor(mat, c);
    }

    private void PulseEmission(Material mat, Color baseEmission, float mulMin, float mulMax, float phase)
    {
        if (mat == null || !HasEmission(mat))
            return;

        float k = 0.5f + 0.5f * Mathf.Sin(phase);
        float mul = Mathf.Lerp(mulMin, mulMax, k);
        SetEmissionColor(mat, baseEmission * mul);
    }

    private void SetAuraVisible(bool visible)
    {
        if (innerRenderer != null) innerRenderer.enabled = visible;
        if (middleRenderer != null) middleRenderer.enabled = visible;
        if (outerRenderer != null) outerRenderer.enabled = visible;
    }

    private Color GetBaseColor(Material mat)
    {
        if (mat == null) return Color.white;
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return Color.white;
    }

    private void SetBaseColor(Material mat, Color c)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
    }

    private bool HasEmission(Material mat)
    {
        return mat != null && mat.HasProperty("_EmissionColor");
    }

    private Color GetEmissionColor(Material mat)
    {
        if (!HasEmission(mat)) return Color.black;
        return mat.GetColor("_EmissionColor");
    }

    private void SetEmissionColor(Material mat, Color c)
    {
        if (!HasEmission(mat)) return;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", c);
    }

    private void HandleAudioOnPlay()
    {
        if (backgroundMusicSource != null)
            StartBackgroundFade(duckedBackgroundVolume);

        if (pulseAudioSource != null && pulseLoopClip != null)
        {
            if (pulseAudioSource.clip != pulseLoopClip)
                pulseAudioSource.clip = pulseLoopClip;

            if (!pulseAudioSource.isPlaying)
                pulseAudioSource.Play();

            StartPulseFade(pulseAudioVolume);
        }
    }

    private void HandleAudioOnStop()
    {
        if (backgroundMusicSource != null)
            StartBackgroundFade(normalBackgroundVolume);

        if (pulseAudioSource != null)
            StartPulseFade(0f, true);
    }

    private void StartBackgroundFade(float targetVolume)
    {
        if (backgroundMusicSource == null)
            return;

        if (bgFadeRoutine != null)
            StopCoroutine(bgFadeRoutine);

        bgFadeRoutine = StartCoroutine(FadeAudioRoutine(backgroundMusicSource, targetVolume, audioFadeDuration));
    }

    private void StartPulseFade(float targetVolume, bool stopAfterFade = false)
    {
        if (pulseAudioSource == null)
            return;

        if (pulseFadeRoutine != null)
            StopCoroutine(pulseFadeRoutine);

        pulseFadeRoutine = StartCoroutine(FadeAudioRoutine(pulseAudioSource, targetVolume, audioFadeDuration, stopAfterFade));
    }

    private IEnumerator FadeAudioRoutine(AudioSource source, float targetVolume, float duration, bool stopAfterFade = false)
    {
        float startVolume = source.volume;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        source.volume = targetVolume;

        if (stopAfterFade && targetVolume <= 0.001f && source.isPlaying)
            source.Stop();
    }
}