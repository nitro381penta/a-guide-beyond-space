using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ShihLiWaveField : MonoBehaviour, IWaveField
{
    [Header("Volume")]
    [Range(2, 40)] public int sliceCount = 14;
    [Range(2, 40)] public int linesPerSlice = 10;
    [Range(32, 256)] public int pointsPerLine = 160;

    public float volumeWidth = 7.5f;
    public float volumeHeight = 3.6f;
    public float volumeDepth = 8.0f;

    [Header("Wave Motion")]
    public float waveAmplitudeY = 0.26f;
    public float waveFrequencyX = 2.0f;
    public float waveFrequencyZ = 0.55f;
    public float timeSpeed = 0.9f;

    public float secondaryAmplitudeY = 0.05f;
    public float secondaryFrequencyX = 4.8f;
    public float secondaryTimeSpeed = 0.5f;

    public float driftAmplitudeY = 0.02f;
    public float driftSpeed = 0.3f;

    [Header("Side Weighting")]
    public bool useSideWeight = true;
    public float sideWeightLeft = 1.2f;
    public float sideWeightRight = 0.9f;

    public bool useXSag = true;
    public float xSagAmplitude = 0.08f;
    public float xSagSpeed = 0.35f;
    public float xSagDepthFrequency = 0.22f;

    [Header("Reveal")]
    public float revealDuration = 2.0f;
    public AnimationCurve revealCurve;

    [Header("Look")]
    public Material lineMaterial;
    public Gradient pastelGradientA;
    public Gradient pastelGradientB;
    public Gradient pastelGradientC;

    public float lineWidth = 0.014f;

    [Range(0, 16)] public int cornerVertices = 8;
    [Range(0, 16)] public int capVertices = 8;

    public bool fadeWithDepth = true;
    [Range(0f, 1f)] public float alphaNear = 0.9f;
    [Range(0f, 1f)] public float alphaFar = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private float normalBackgroundVolume = 0.3f;
    [SerializeField] private float duckedBackgroundVolume = 0.03f;
    [SerializeField] private AudioSource waterAudioSource;
    [SerializeField] private AudioClip waterLoopClip;
    [SerializeField] private float waterVolume = 0.55f;
    [SerializeField] private float audioFadeDuration = 1.0f;

    [Header("Debug")]
    public bool logToConsole = true;

    private class WaveLine
    {
        public GameObject go;
        public LineRenderer lr;
        public Vector3[] positions;
        public int sliceIndex;
        public int lineIndex;
    }

    private WaveLine[] _lines;
    private bool _isPlaying;
    private float _playTime;
    private Coroutine _bgFadeRoutine;
    private Coroutine _waterFadeRoutine;

    public bool IsPlaying => _isPlaying;

    void Awake()
    {
        if (revealCurve == null || revealCurve.length == 0)
            revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (pastelGradientA == null || pastelGradientA.colorKeys.Length == 0)
            pastelGradientA = CreateDefaultPastelGradientA();

        if (pastelGradientB == null || pastelGradientB.colorKeys.Length == 0)
            pastelGradientB = CreateDefaultPastelGradientB();

        if (pastelGradientC == null || pastelGradientC.colorKeys.Length == 0)
            pastelGradientC = CreateDefaultPastelGradientC();

        if (waterAudioSource != null)
        {
            waterAudioSource.playOnAwake = false;
            waterAudioSource.loop = true;
            waterAudioSource.volume = 0f;

            if (waterLoopClip != null)
                waterAudioSource.clip = waterLoopClip;
        }

        CreateLines();
        SetVisible(false);

        if (logToConsole)
            Debug.Log("[ShihLiWaveField] Awake -> line field created");
    }

    void Update()
    {
        if (!_isPlaying || _lines == null)
            return;

        _playTime += Time.deltaTime;

        float reveal01 = Mathf.Clamp01(_playTime / Mathf.Max(0.01f, revealDuration));
        reveal01 = Mathf.Clamp01(revealCurve.Evaluate(reveal01));
        float visibleDepth = Mathf.Lerp(0f, volumeDepth, reveal01);

        float timeNow = Time.time;

        foreach (var wl in _lines)
        {
            float zSlice = GetSliceZ(wl.sliceIndex);
            bool visible = zSlice <= visibleDepth + 0.001f;

            if (wl.lr.enabled != visible)
                wl.lr.enabled = visible;

            if (!visible)
                continue;

            UpdateLine(wl, zSlice, timeNow);
        }
    }

    public void Play()
    {
        _isPlaying = true;
        _playTime = 0f;
        SetVisible(false);
        HandleAudioOnPlay();

        if (logToConsole)
            Debug.Log("[ShihLiWaveField] Play");
    }

    public void Stop()
    {
        _isPlaying = false;
        SetVisible(false);
        HandleAudioOnStop();

        if (logToConsole)
            Debug.Log("[ShihLiWaveField] Stop");
    }

    public void Toggle()
    {
        if (_isPlaying) Stop();
        else Play();
    }

    void HandleAudioOnPlay()
    {
        if (backgroundMusicSource != null)
            StartBackgroundFade(duckedBackgroundVolume);

        if (waterAudioSource != null && waterLoopClip != null)
        {
            if (waterAudioSource.clip != waterLoopClip)
                waterAudioSource.clip = waterLoopClip;

            if (!waterAudioSource.isPlaying)
                waterAudioSource.Play();

            StartWaterFade(waterVolume);
        }
    }

    void HandleAudioOnStop()
    {
        if (backgroundMusicSource != null)
            StartBackgroundFade(normalBackgroundVolume);

        if (waterAudioSource != null)
            StartWaterFade(0f, true);
    }

    void StartBackgroundFade(float targetVolume)
    {
        if (_bgFadeRoutine != null)
            StopCoroutine(_bgFadeRoutine);

        _bgFadeRoutine = StartCoroutine(FadeAudioRoutine(backgroundMusicSource, targetVolume, audioFadeDuration));
    }

    void StartWaterFade(float targetVolume, bool stopAfterFade = false)
    {
        if (_waterFadeRoutine != null)
            StopCoroutine(_waterFadeRoutine);

        _waterFadeRoutine = StartCoroutine(FadeAudioRoutine(waterAudioSource, targetVolume, audioFadeDuration, stopAfterFade));
    }

    IEnumerator FadeAudioRoutine(AudioSource source, float targetVolume, float duration, bool stopAfterFade = false)
    {
        if (source == null)
            yield break;

        float startVolume = source.volume;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            source.volume = Mathf.Lerp(startVolume, targetVolume, k);
            yield return null;
        }

        source.volume = targetVolume;

        if (stopAfterFade && targetVolume <= 0.001f && source.isPlaying)
            source.Stop();
    }

    void CreateLines()
    {
        if (_lines != null)
            return;

        int total = sliceCount * linesPerSlice;
        _lines = new WaveLine[total];

        int idx = 0;

        for (int s = 0; s < sliceCount; s++)
        {
            for (int l = 0; l < linesPerSlice; l++)
            {
                GameObject go = new GameObject($"WaveLine_S{s}_L{l}");
                go.transform.SetParent(transform, false);

                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = false;
                lr.positionCount = pointsPerLine;
                lr.alignment = LineAlignment.View;
                lr.textureMode = LineTextureMode.Stretch;
                lr.numCornerVertices = cornerVertices;
                lr.numCapVertices = capVertices;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.allowOcclusionWhenDynamic = false;
                lr.widthMultiplier = lineWidth;

                if (lineMaterial != null)
                    lr.material = new Material(lineMaterial);
                else
                    lr.material = CreateFallbackLineMaterial();

                lr.startColor = Color.white;
                lr.endColor = Color.white;

                Vector3[] pos = new Vector3[pointsPerLine];

                _lines[idx++] = new WaveLine
                {
                    go = go,
                    lr = lr,
                    positions = pos,
                    sliceIndex = s,
                    lineIndex = l
                };
            }
        }
    }

    void UpdateLine(WaveLine wl, float zSlice, float timeNow)
    {
        float lineT = linesPerSlice <= 1 ? 0f : wl.lineIndex / (float)(linesPerSlice - 1);
        float baseY = Mathf.Lerp(-volumeHeight * 0.5f, volumeHeight * 0.5f, lineT);

        float sliceT = sliceCount <= 1 ? 0f : wl.sliceIndex / (float)(sliceCount - 1);

        Gradient activeGradient;
        if (sliceT < 0.33f)
            activeGradient = pastelGradientA;
        else if (sliceT < 0.66f)
            activeGradient = pastelGradientB;
        else
            activeGradient = pastelGradientC;

        float phase = lineT * Mathf.PI * 2f * 0.75f + wl.sliceIndex * 0.33f;

        float alpha = 1f;
        if (fadeWithDepth)
        {
            float depth01 = Mathf.Clamp01(zSlice / Mathf.Max(0.001f, volumeDepth));
            alpha = Mathf.Lerp(alphaNear, alphaFar, depth01);
        }

        Color c = activeGradient.Evaluate(lineT);
        c.a = alpha;
        ApplyGradient(wl.lr, c);

        for (int p = 0; p < pointsPerLine; p++)
        {
            float tx = p / (float)(pointsPerLine - 1);
            float x = Mathf.Lerp(-volumeWidth * 0.5f, volumeWidth * 0.5f, tx);

            float sideWeight = 1f;
            if (useSideWeight)
                sideWeight = Mathf.Lerp(sideWeightLeft, sideWeightRight, tx);

            float primary =
                Mathf.Sin(x * waveFrequencyX + zSlice * waveFrequencyZ + timeNow * timeSpeed + phase)
                * waveAmplitudeY;

            float secondary =
                Mathf.Sin(x * secondaryFrequencyX - zSlice * 0.65f + timeNow * secondaryTimeSpeed + phase * 1.6f)
                * secondaryAmplitudeY;

            float drift =
                Mathf.Sin(timeNow * driftSpeed + zSlice * 0.18f + phase) * driftAmplitudeY;

            float y = baseY + (primary + secondary + drift) * sideWeight;

            if (useXSag)
            {
                float xSag = Mathf.Sin(zSlice * xSagDepthFrequency + timeNow * xSagSpeed + phase)
                             * xSagAmplitude * (1f - tx);
                x -= xSag;
            }

            wl.positions[p] = new Vector3(x, y, zSlice);
        }

        wl.lr.SetPositions(wl.positions);
    }

    float GetSliceZ(int sliceIndex)
    {
        if (sliceCount <= 1)
            return 0f;

        return (sliceIndex / (float)(sliceCount - 1)) * volumeDepth;
    }

    void SetVisible(bool visible)
    {
        if (_lines == null)
            return;

        foreach (var wl in _lines)
            if (wl?.lr != null)
                wl.lr.enabled = visible;
    }

    void ApplyGradient(LineRenderer lr, Color c)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(c, 0f),
                new GradientColorKey(c, 1f)
            },
            new[]
            {
                new GradientAlphaKey(c.a, 0f),
                new GradientAlphaKey(c.a, 1f)
            }
        );

        lr.colorGradient = g;
        lr.startColor = c;
        lr.endColor = c;
    }

    Gradient CreateDefaultPastelGradientA()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.78f, 0.86f, 0.97f), 0.00f),
                new GradientColorKey(new Color(0.86f, 0.94f, 0.80f), 0.35f),
                new GradientColorKey(new Color(0.96f, 0.88f, 0.72f), 0.70f),
                new GradientColorKey(new Color(0.91f, 0.82f, 0.96f), 1.00f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }

    Gradient CreateDefaultPastelGradientB()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.95f, 0.79f, 0.83f), 0.00f),
                new GradientColorKey(new Color(0.80f, 0.89f, 0.97f), 0.35f),
                new GradientColorKey(new Color(0.85f, 0.95f, 0.87f), 0.70f),
                new GradientColorKey(new Color(0.96f, 0.89f, 0.76f), 1.00f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }

    Gradient CreateDefaultPastelGradientC()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.89f, 0.83f, 0.97f), 0.00f),
                new GradientColorKey(new Color(0.80f, 0.94f, 0.91f), 0.30f),
                new GradientColorKey(new Color(0.97f, 0.84f, 0.74f), 0.65f),
                new GradientColorKey(new Color(0.79f, 0.85f, 0.98f), 1.00f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }

    Material CreateFallbackLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);

        return mat;
    }
}