using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class AlefKeplerActor : MonoBehaviour
{
    [Header("Animator State Names")]
    [SerializeField] private string idleState = "Standard Idle";
    [SerializeField] private string landingState = "Landing";
    [SerializeField] private string greetingSaluteState = "Salute";
    [SerializeField] private string talking1State = "Talking_1";
    [SerializeField] private string talking2State = "Talking_2";
    [SerializeField] private string walkState = "Standard Walk";
    [SerializeField] private string stopWalkState = "Stop Walking";
    [SerializeField] private string rightTurnState = "Right Turn";
    [SerializeField] private string pointing1State = "Pointing_1";
    [SerializeField] private string pointing2State = "Pointing_2";

    [Header("Animation")]
    [SerializeField] private float crossFadeDuration = 0.15f;
    [SerializeField] private float greetingDuration = 1.1f;

    [Header("Speech Gesture Timing - Intro")]
    [SerializeField] private bool allowSecondTalkingGesture = true;
    [SerializeField] private float firstGestureNormalizedTime = 0.60f;
    [SerializeField] private float secondGestureNormalizedTime = 0.88f;
    [SerializeField] private float minimumClipLengthForSecondGesture = 11f;
    [SerializeField] private float talking1Duration = 0.95f;
    [SerializeField] private float talking2Duration = 1.05f;

    [Header("Speech Gesture Timing - Defaults")]
    [SerializeField] private float defaultStationPointingTime = 13.0f;
    [SerializeField] private float defaultStationFirstTalkingTime = 31.0f;
    [SerializeField] private float defaultStationSecondTalkingTime = 63.0f;
    [SerializeField] private float defaultStationThirdTalkingTime = 92.0f;
    [SerializeField] private bool useThirdTalkingGesture = true;

    [SerializeField] private float pointing1Duration = 1.1f;
    [SerializeField] private float pointing2Duration = 1.25f;
    [SerializeField] private bool usePointing2 = false;

    [Header("Voice")]
    [SerializeField] private AudioSource voiceSource;

    private Animator animatorComponent;
    private AlefKeplerSequenceManager manager;
    private AlefKeplerTapXR tapInteraction;
    private Coroutine speechRoutine;

    private bool speechSequenceRunning = false;
    private bool landingFinishedEventSent = false;
    private string lastTalkingStatePlayed = "";

    private float activeStationPointingTime;
    private float activeStationFirstTalkingTime;
    private float activeStationSecondTalkingTime;
    private float activeStationThirdTalkingTime;

    private void Awake()
    {
        animatorComponent = GetComponent<Animator>();
        tapInteraction = GetComponent<AlefKeplerTapXR>();

        if (animatorComponent != null)
            animatorComponent.applyRootMotion = false;

        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();

        if (voiceSource != null)
        {
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.volume = 1f;
            voiceSource.spatialBlend = 0.35f;
            voiceSource.minDistance = 2f;
            voiceSource.maxDistance = 12f;
            voiceSource.Stop();
        }

        ResetStationTimingToDefaults();
    }

    public void Initialize(AlefKeplerSequenceManager sequenceManager)
    {
        manager = sequenceManager;
        landingFinishedEventSent = false;
        speechSequenceRunning = false;
        lastTalkingStatePlayed = "";
        ReturnToIdle();

        if (tapInteraction != null)
            tapInteraction.Initialize(sequenceManager);

        ResetStationTimingToDefaults();
    }

    public void SetTapEnabled(bool enabled)
    {
        if (tapInteraction != null)
            tapInteraction.SetTapEnabled(enabled);
    }

    public void ConfigureStationTiming(
        float pointingTime,
        float firstTalkingTime,
        float secondTalkingTime,
        float thirdTalkingTime)
    {
        activeStationPointingTime = pointingTime;
        activeStationFirstTalkingTime = firstTalkingTime;
        activeStationSecondTalkingTime = secondTalkingTime;
        activeStationThirdTalkingTime = thirdTalkingTime;
    }

    public void ResetStationTimingToDefaults()
    {
        activeStationPointingTime = defaultStationPointingTime;
        activeStationFirstTalkingTime = defaultStationFirstTalkingTime;
        activeStationSecondTalkingTime = defaultStationSecondTalkingTime;
        activeStationThirdTalkingTime = defaultStationThirdTalkingTime;
    }

    public void PlayLanding()
    {
        landingFinishedEventSent = false;
        PlayState(landingState);
    }

    public void PlayWalk()
    {
        PlayState(walkState);
    }

    public void PlayStopWalk()
    {
        PlayState(stopWalkState);
    }

    public void PlayRightTurn()
    {
        PlayState(rightTurnState);
    }

    public void PlayPointing1()
    {
        PlayState(pointing1State);
    }

    public void PlayPointing2()
    {
        PlayState(pointing2State);
    }

    public IEnumerator PlayRightTurnAndFaceTarget(
        Transform target,
        float turnDuration,
        bool add180DegreeFix = true)
    {
        if (target == null)
            yield break;

        PlayRightTurn();

        Vector3 lookTarget = target.position;
        lookTarget.y = transform.position.y;

        Vector3 direction = lookTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            yield break;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        if (add180DegreeFix)
            targetRotation *= Quaternion.Euler(0f, 180f, 0f);

        float duration = Mathf.Max(0.01f, turnDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, k);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    public void StartSpeechWithGreeting(AudioClip clip)
    {
        if (speechSequenceRunning)
            return;

        if (voiceSource == null || clip == null)
        {
            ReturnToIdle();
            manager?.OnAstronautSpeechFinished();
            return;
        }

        if (speechRoutine != null)
            StopCoroutine(speechRoutine);

        speechRoutine = StartCoroutine(SpeechPresentationRoutine(clip));
    }

    public void StartStationSpeech(
        AudioClip clip,
        Transform artworkTarget,
        Transform viewerTarget,
        bool add180DegreeFix = true)
    {
        if (speechSequenceRunning)
            return;

        if (voiceSource == null || clip == null)
        {
            ReturnToIdle();
            manager?.OnAstronautStationSpeechFinished();
            return;
        }

        if (speechRoutine != null)
            StopCoroutine(speechRoutine);

        speechRoutine = StartCoroutine(StationSpeechRoutine(
            clip,
            artworkTarget,
            viewerTarget,
            add180DegreeFix
        ));
    }

    public void CancelAllPresentation()
    {
        if (speechRoutine != null)
        {
            StopCoroutine(speechRoutine);
            speechRoutine = null;
        }

        if (voiceSource != null && voiceSource.isPlaying)
            voiceSource.Stop();

        speechSequenceRunning = false;
        ReturnToIdle();
    }

    private IEnumerator SpeechPresentationRoutine(AudioClip clip)
    {
        speechSequenceRunning = true;
        lastTalkingStatePlayed = "";

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();

        PlayState(greetingSaluteState);

        if (greetingDuration > 0f)
            yield return new WaitForSeconds(greetingDuration);

        ReturnToIdle();

        float clipLength = clip.length;

        float firstGestureTime = Mathf.Clamp(
            clipLength * firstGestureNormalizedTime,
            greetingDuration + 1.5f,
            clipLength - 1.2f
        );

        float firstWait = firstGestureTime - greetingDuration;
        if (firstWait > 0f)
            yield return new WaitForSeconds(firstWait);

        if (voiceSource != null && voiceSource.isPlaying)
            yield return PlayTalkingGesture();

        if (allowSecondTalkingGesture && clipLength >= minimumClipLengthForSecondGesture)
        {
            float secondGestureTime = Mathf.Clamp(
                clipLength * secondGestureNormalizedTime,
                firstGestureTime + 2.8f,
                clipLength - 0.4f
            );

            float secondWait = secondGestureTime - firstGestureTime;
            if (secondWait > 0f)
                yield return new WaitForSeconds(secondWait);

            if (voiceSource != null && voiceSource.isPlaying)
                yield return PlayTalkingGesture();
        }

        while (voiceSource != null && voiceSource.isPlaying)
            yield return null;

        ReturnToIdle();

        speechSequenceRunning = false;
        speechRoutine = null;
        manager?.OnAstronautSpeechFinished();
    }

    private IEnumerator StationSpeechRoutine(
        AudioClip clip,
        Transform artworkTarget,
        Transform viewerTarget,
        bool add180DegreeFix)
    {
        speechSequenceRunning = true;
        lastTalkingStatePlayed = "";

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();

        ReturnToIdle();

        bool pointingDone = false;
        bool firstTalkingDone = false;
        bool secondTalkingDone = false;
        bool thirdTalkingDone = false;

        while (voiceSource != null && voiceSource.isPlaying)
        {
            float elapsed = voiceSource.time;

            if (!pointingDone && elapsed >= activeStationPointingTime)
            {
                pointingDone = true;

                if (artworkTarget != null)
                    FaceTargetInstant(artworkTarget, add180DegreeFix);

                if (usePointing2)
                    yield return PlayPointingGesture2();
                else
                    yield return PlayPointingGesture1();

                if (viewerTarget != null)
                    FaceTargetInstant(viewerTarget, add180DegreeFix);

                ReturnToIdle();
            }

            if (!firstTalkingDone && elapsed >= activeStationFirstTalkingTime)
            {
                firstTalkingDone = true;
                yield return PlayTalkingGesture();
                ReturnToIdle();
            }

            if (!secondTalkingDone && elapsed >= activeStationSecondTalkingTime)
            {
                secondTalkingDone = true;
                yield return PlayTalkingGesture();
                ReturnToIdle();
            }

            if (useThirdTalkingGesture && !thirdTalkingDone && elapsed >= activeStationThirdTalkingTime)
            {
                thirdTalkingDone = true;
                yield return PlayTalkingGesture();
                ReturnToIdle();
            }

            yield return null;
        }

        if (viewerTarget != null)
            FaceTargetInstant(viewerTarget, add180DegreeFix);

        ReturnToIdle();

        speechSequenceRunning = false;
        speechRoutine = null;
        manager?.OnAstronautStationSpeechFinished();
    }

    private IEnumerator PlayTalkingGesture()
    {
        string chosenState = GetNextTalkingState();
        float chosenDuration = chosenState == talking1State ? talking1Duration : talking2Duration;

        PlayState(chosenState);
        lastTalkingStatePlayed = chosenState;

        yield return new WaitForSeconds(chosenDuration);
        ReturnToIdle();
    }

    private IEnumerator PlayPointingGesture1()
    {
        PlayPointing1();
        yield return new WaitForSeconds(pointing1Duration);
    }

    private IEnumerator PlayPointingGesture2()
    {
        PlayPointing2();
        yield return new WaitForSeconds(pointing2Duration);
    }

    private string GetNextTalkingState()
    {
        bool hasTalking1 = !string.IsNullOrWhiteSpace(talking1State);
        bool hasTalking2 = !string.IsNullOrWhiteSpace(talking2State);

        if (hasTalking1 && hasTalking2)
        {
            if (lastTalkingStatePlayed == talking1State)
                return talking2State;

            if (lastTalkingStatePlayed == talking2State)
                return talking1State;

            return Random.value < 0.5f ? talking1State : talking2State;
        }

        if (hasTalking1) return talking1State;
        if (hasTalking2) return talking2State;

        return idleState;
    }

    public void ReturnToIdle()
    {
        PlayState(idleState);
    }

    public void FaceTargetInstant(Transform target, bool add180DegreeFix = true)
    {
        if (target == null)
            return;

        Vector3 lookTarget = target.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);

        if (add180DegreeFix)
            transform.Rotate(0f, 180f, 0f);
    }

    private void PlayState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || animatorComponent == null)
            return;

        PlayState(stateName, crossFadeDuration);
    }

    private void PlayState(string stateName, float fadeDuration)
    {
        if (string.IsNullOrWhiteSpace(stateName) || animatorComponent == null)
            return;

        animatorComponent.CrossFadeInFixedTime(stateName, fadeDuration);
    }

    public void AE_LandingFinished()
    {
        if (landingFinishedEventSent)
            return;

        landingFinishedEventSent = true;
        manager?.OnAstronautLandingFinished();
    }
}