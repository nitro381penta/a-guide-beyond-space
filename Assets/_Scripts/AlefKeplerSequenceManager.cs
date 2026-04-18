using System.Collections;
using UnityEngine;

public class AlefKeplerSequenceManager : MonoBehaviour
{
    private enum SequencePhase
    {
        None,
        Intro,
        FirstStationSpeech,
        SecondStationSpeech,
        ThirdStationSpeech
    }

    [Header("Prefab / Spawn")]
    [SerializeField] private AlefKeplerActor astronautPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Arrival Visual")]
    [SerializeField] private GameObject blackHolePrefab;
    [SerializeField] private Vector3 blackHoleOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private float blackHoleLifetime = 1.8f;

    [Header("Arrival Audio")]
    [SerializeField] private AudioSource arrivalSfxSource;
    [SerializeField] private AudioClip whooshClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private float delayBetweenWhooshAndLandingSound = 0.35f;

    [Header("Navigation - Station 1")]
    [SerializeField] private Transform firstStationTarget;
    [SerializeField] private AudioClip firstStationVoiceClip;
    [SerializeField] private Transform firstArtworkLookTarget;
    [SerializeField] private float delayBeforeWalkingToFirstStation = 0.25f;
    [SerializeField] private float delayBeforeFirstStationSpeech = 0.25f;

    [Header("Station 1 Timing")]
    [SerializeField] private float firstStationPointingTime = 16.0f;
    [SerializeField] private float firstStationTalking1Time = 31.0f;
    [SerializeField] private float firstStationTalking2Time = 63.0f;
    [SerializeField] private float firstStationTalking3Time = 92.0f;

    [Header("Navigation - Station 2")]
    [SerializeField] private Transform secondStationTarget;
    [SerializeField] private AudioClip secondStationVoiceClip;
    [SerializeField] private Transform secondArtworkLookTarget;
    [SerializeField] private float delayBeforeWalkingToSecondStation = 0.25f;
    [SerializeField] private float delayBeforeSecondStationSpeech = 0.25f;

    [Header("Station 2 Timing")]
    [SerializeField] private float secondStationPointingTime = 8.5f;
    [SerializeField] private float secondStationTalking1Time = 31.0f;
    [SerializeField] private float secondStationTalking2Time = 63.0f;
    [SerializeField] private float secondStationTalking3Time = 92.0f;

    [Header("Navigation - Station 3")]
    [SerializeField] private Transform thirdStationTarget;
    [SerializeField] private AudioClip thirdStationVoiceClip;
    [SerializeField] private Transform thirdArtworkLookTarget;
    [SerializeField] private float delayBeforeWalkingToThirdStation = 0.25f;
    [SerializeField] private float delayBeforeThirdStationSpeech = 0.25f;

    [Header("Station 3 Timing")]
    [SerializeField] private float thirdStationPointingTime = 8.5f;
    [SerializeField] private float thirdStationTalking1Time = 31.0f;
    [SerializeField] private float thirdStationTalking2Time = 63.0f;
    [SerializeField] private float thirdStationTalking3Time = 92.0f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 0.75f;
    [SerializeField] private float rotationSpeed = 3.5f;
    [SerializeField] private float stopBeforeTargetDistance = 0.28f;
    [SerializeField] private float finalNudgeDistance = 0.05f;
    [SerializeField] private float stopWalkingDuration = 0.9f;
    [SerializeField] private float delayBeforeFacingCamera = 0.1f;

    [Header("Turn To Camera")]
    [SerializeField] private bool useRightTurnAnimationToFaceCamera = true;
    [SerializeField] private float rightTurnDuration = 0.8f;

    [Header("Spawn Offset")]
    [SerializeField] private Vector3 spawnPositionOffset = Vector3.zero;

    [Header("Speech Audio")]
    [SerializeField] private AudioClip introVoiceClip;

    [Header("Background Music")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private float normalMusicVolume = 0.3f;
    [SerializeField] private float duckedMusicVolume = 0.04f;
    [SerializeField] private float musicFadeDuration = 1f;
    [SerializeField] private bool duckMusicDuringStationSpeech = true;

    [Header("Timing")]
    [SerializeField] private float delayBeforeSequence = 0.75f;
    [SerializeField] private float delayAfterSpawnBeforeLanding = 0.05f;
    [SerializeField] private float delayAfterLandingBeforeSpeech = 0.15f;

    [Header("Facing")]
    [SerializeField] private bool faceMainCameraOnSpawn = true;
    [SerializeField] private bool add180DegreeRotationFix = true;
    [SerializeField] private bool faceCameraAgainAfterLanding = true;
    [SerializeField] private bool faceCameraAfterWalk = true;
    [SerializeField] private bool faceCameraBeforeStationSpeech = true;

    [Header("Skip UI")]
    [SerializeField] private GameObject introAndStation1SkipUiRoot;
    [SerializeField] private GameObject station2SkipUiRoot;
    [SerializeField] private GameObject station3SkipUiRoot;

    [Header("Conversation")]
    [SerializeField] private AlefConversationStateMachine conversationStateMachine;
    [SerializeField] private ConversationUIController conversationUIController;
    [SerializeField] private AlefAudioPlayer alefAudioPlayer;

    [Header("Debug")]
    [SerializeField] private bool logAstronautPosition = true;
    [SerializeField] private bool logToConsole = true;

    private AlefKeplerActor astronautInstance;
    private GameObject spawnedBlackHoleInstance;

    private bool sequenceStarted;
    private bool landingHandled;
    private bool speechStarted;
    private bool introSkipped;

    private bool walkToFirstStationStarted;
    private bool walkToSecondStationStarted;
    private bool walkToThirdStationStarted;
    private bool walkFinished;

    private bool firstStationSpeechStarted;
    private bool secondStationSpeechStarted;
    private bool thirdStationSpeechStarted;

    private bool waitingForTapToStartFirstStationSpeech;
    private bool waitingForTapToStartSecondStation;
    private bool waitingForTapToStartThirdStation;

    private Coroutine musicFadeRoutine;
    private Coroutine walkRoutine;

    private AlefKeplerSkipInputXR skipInput;
    private SequencePhase currentPhase = SequencePhase.None;

    private void Awake()
    {
        skipInput = GetComponent<AlefKeplerSkipInputXR>();
        Log("[AlefKeplerSequenceManager] Awake");
    }

    private void Start()
    {
        Log("[AlefKeplerSequenceManager] Start");

        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = normalMusicVolume;

            if (backgroundMusicSource.clip != null && !backgroundMusicSource.isPlaying)
                backgroundMusicSource.Play();
        }

        if (arrivalSfxSource != null)
        {
            arrivalSfxSource.playOnAwake = false;
            arrivalSfxSource.loop = false;
        }

        HideAllSkipUI();
        StartArrivalSequence();
    }

    public void StartArrivalSequence()
    {
        if (sequenceStarted)
            return;

        if (skipInput != null)
            skipInput.ResetSkip();

        sequenceStarted = true;
        introSkipped = false;
        landingHandled = false;
        speechStarted = false;

        walkToFirstStationStarted = false;
        walkToSecondStationStarted = false;
        walkToThirdStationStarted = false;
        walkFinished = false;

        firstStationSpeechStarted = false;
        secondStationSpeechStarted = false;
        thirdStationSpeechStarted = false;

        waitingForTapToStartFirstStationSpeech = false;
        waitingForTapToStartSecondStation = false;
        waitingForTapToStartThirdStation = false;

        currentPhase = SequencePhase.Intro;

        Log("[AlefKeplerSequenceManager] StartArrivalSequence -> Intro phase");
        StartCoroutine(ArrivalRoutine());
    }

    public void SkipCurrentSequence()
    {
        Log($"[AlefKeplerSequenceManager] SkipCurrentSequence called. Current phase: {currentPhase}");

        switch (currentPhase)
        {
            case SequencePhase.Intro:
                SkipIntro();
                break;

            case SequencePhase.FirstStationSpeech:
            case SequencePhase.SecondStationSpeech:
            case SequencePhase.ThirdStationSpeech:
                SkipStationSpeech();
                break;
        }
    }

    public void OnAstronautTapped()
    {
        Log("[AlefKeplerSequenceManager] OnAstronautTapped called.");

        if (astronautInstance == null)
        {
            Log("[AlefKeplerSequenceManager] OnAstronautTapped aborted: astronautInstance is null.");
            return;
        }

        Log($"[AlefKeplerSequenceManager] waiting states -> firstSpeech: {waitingForTapToStartFirstStationSpeech}, secondStation: {waitingForTapToStartSecondStation}, thirdStation: {waitingForTapToStartThirdStation}");

        if (waitingForTapToStartFirstStationSpeech)
        {
            waitingForTapToStartFirstStationSpeech = false;
            waitingForTapToStartSecondStation = false;
            waitingForTapToStartThirdStation = false;

            astronautInstance.SetTapEnabled(false);
            Log("[AlefKeplerSequenceManager] Starting first station speech.");
            StartCoroutine(BeginFirstStationSpeechRoutine());
            return;
        }

        if (waitingForTapToStartSecondStation)
        {
            waitingForTapToStartSecondStation = false;
            waitingForTapToStartFirstStationSpeech = false;
            waitingForTapToStartThirdStation = false;

            astronautInstance.SetTapEnabled(false);

            if (!walkToSecondStationStarted && secondStationTarget != null)
            {
                walkToSecondStationStarted = true;
                Log("[AlefKeplerSequenceManager] Starting walk to second station.");
                StartCoroutine(WalkToSecondStationRoutine());
            }

            return;
        }

        if (waitingForTapToStartThirdStation)
        {
            waitingForTapToStartThirdStation = false;
            waitingForTapToStartFirstStationSpeech = false;
            waitingForTapToStartSecondStation = false;

            astronautInstance.SetTapEnabled(false);

            if (!walkToThirdStationStarted && thirdStationTarget != null)
            {
                walkToThirdStationStarted = true;
                Log("[AlefKeplerSequenceManager] Starting walk to third station.");
                StartCoroutine(WalkToThirdStationRoutine());
            }
        }
    }

    private void SkipIntro()
    {
        if (introSkipped)
            return;

        Log("[AlefKeplerSequenceManager] SkipIntro called.");

        introSkipped = true;
        currentPhase = SequencePhase.None;

        waitingForTapToStartFirstStationSpeech = false;
        waitingForTapToStartSecondStation = false;
        waitingForTapToStartThirdStation = false;

        HideAllSkipUI();

        StopAllCoroutines();

        musicFadeRoutine = null;
        walkRoutine = null;

        if (arrivalSfxSource != null)
            arrivalSfxSource.Stop();

        if (spawnedBlackHoleInstance != null)
            Destroy(spawnedBlackHoleInstance);

        if (astronautInstance == null && astronautPrefab != null && spawnPoint != null)
        {
            Vector3 spawnPosition = spawnPoint.position + spawnPositionOffset;
            Quaternion spawnRotation = spawnPoint.rotation;

            astronautInstance = Instantiate(astronautPrefab, spawnPosition, spawnRotation);
            astronautInstance.Initialize(this);
            InitializeAstronautConversationReferences();

            Log("[AlefKeplerSequenceManager] Astronaut instantiated during SkipIntro.");
        }

        if (astronautInstance == null)
            return;

        astronautInstance.CancelAllPresentation();
        astronautInstance.SetTapEnabled(false);

        if (firstStationTarget != null)
        {
            Vector3 targetPos = firstStationTarget.position;
            targetPos.y = astronautInstance.transform.position.y;
            astronautInstance.transform.position = targetPos;
        }

        astronautInstance.PlayStopWalk();
        StartCoroutine(FinishSkipIntroRoutine());
    }

    private IEnumerator FinishSkipIntroRoutine()
    {
        yield return new WaitForSeconds(stopWalkingDuration);

        if (astronautInstance != null && faceCameraAfterWalk)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                if (useRightTurnAnimationToFaceCamera && IsTargetOnRightSide(astronautInstance.transform, cam.transform))
                {
                    yield return astronautInstance.PlayRightTurnAndFaceTarget(
                        cam.transform,
                        rightTurnDuration,
                        add180DegreeRotationFix
                    );
                }
                else
                {
                    astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);
                }
            }

            astronautInstance.ReturnToIdle();
        }

        waitingForTapToStartFirstStationSpeech = true;
        waitingForTapToStartSecondStation = false;
        waitingForTapToStartThirdStation = false;

        if (astronautInstance != null)
            astronautInstance.SetTapEnabled(true);

        HideAllSkipUI();
        Log("[AlefKeplerSequenceManager] FinishSkipIntroRoutine complete. Waiting for tap to start first station speech.");
    }

    private void SkipStationSpeech()
    {
        SequencePhase skippedPhase = currentPhase;
        currentPhase = SequencePhase.None;

        Log($"[AlefKeplerSequenceManager] SkipStationSpeech called. Skipped phase: {skippedPhase}");

        HideAllSkipUI();

        if (astronautInstance != null)
        {
            astronautInstance.CancelAllPresentation();
            astronautInstance.SetTapEnabled(false);

            Camera cam = Camera.main;
            if (cam != null)
                astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);

            astronautInstance.ReturnToIdle();
        }

        FadeBackgroundMusic(normalMusicVolume, musicFadeDuration);

        waitingForTapToStartSecondStation = false;
        waitingForTapToStartThirdStation = false;

        if (skippedPhase == SequencePhase.FirstStationSpeech)
        {
            waitingForTapToStartSecondStation = true;

            if (astronautInstance != null)
                astronautInstance.SetTapEnabled(true);

            Log("[AlefKeplerSequenceManager] Station 1 skipped. Waiting for tap to start second station.");
        }
        else if (skippedPhase == SequencePhase.SecondStationSpeech)
        {
            waitingForTapToStartThirdStation = true;

            if (astronautInstance != null)
                astronautInstance.SetTapEnabled(true);

            Log("[AlefKeplerSequenceManager] Station 2 skipped. Waiting for tap to start third station.");
        }
        else if (skippedPhase == SequencePhase.ThirdStationSpeech)
        {
            ActivateConversationMode();
        }
        else
        {
            if (astronautInstance != null)
                astronautInstance.SetTapEnabled(false);
        }
    }

    private IEnumerator ArrivalRoutine()
    {
        if (spawnPoint == null || astronautPrefab == null)
            yield break;

        yield return new WaitForSeconds(delayBeforeSequence);

        FadeBackgroundMusic(duckedMusicVolume, musicFadeDuration);

        Vector3 spawnPosition = spawnPoint.position + spawnPositionOffset;
        Quaternion spawnRotation = spawnPoint.rotation;

        astronautInstance = Instantiate(astronautPrefab, spawnPosition, spawnRotation);
        astronautInstance.Initialize(this);
        astronautInstance.SetTapEnabled(false);

        InitializeAstronautConversationReferences();
        AlignAstronautToViewer();

        if (logAstronautPosition)
            Log($"[AlefKeplerSequenceManager] Spawned astronaut at {astronautInstance.transform.position}");

        StartCoroutine(PlayArrivalPresentationRoutine(spawnPosition, spawnRotation));

        yield return new WaitForSeconds(delayAfterSpawnBeforeLanding);
        astronautInstance.PlayLanding();
    }

    private void InitializeAstronautConversationReferences()
    {
        if (astronautInstance == null)
            return;

        AlefKeplerTapXR tap = astronautInstance.GetComponent<AlefKeplerTapXR>();
        if (tap != null)
        {
            tap.InitializeConversation(conversationStateMachine, conversationUIController);
            Log("[AlefKeplerSequenceManager] Conversation references passed to AlefKeplerTapXR.");
        }
        else
        {
            Debug.LogWarning("[AlefKeplerSequenceManager] AlefKeplerTapXR not found on astronaut instance.");
        }

        if (alefAudioPlayer != null)
        {
            AudioSource source = astronautInstance.GetComponent<AudioSource>();

            if (source != null)
            {
                alefAudioPlayer.SetAudioSource(source);
                Log("[AlefKeplerSequenceManager] Alef AudioSource passed to AlefAudioPlayer.");
            }
            else
            {
                Debug.LogWarning("[AlefKeplerSequenceManager] No AudioSource found on astronaut instance.");
            }
        }
        else
        {
            Debug.LogWarning("[AlefKeplerSequenceManager] alefAudioPlayer reference is NULL.");
        }
    }

    private IEnumerator PlayArrivalPresentationRoutine(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (blackHolePrefab != null)
        {
            spawnedBlackHoleInstance = Instantiate(
                blackHolePrefab,
                spawnPosition + blackHoleOffset,
                spawnRotation
            );

            Destroy(spawnedBlackHoleInstance, blackHoleLifetime);
        }

        if (arrivalSfxSource != null && whooshClip != null)
            arrivalSfxSource.PlayOneShot(whooshClip);

        yield return new WaitForSeconds(delayBetweenWhooshAndLandingSound);

        if (arrivalSfxSource != null && landingClip != null)
            arrivalSfxSource.PlayOneShot(landingClip);
    }

    private void AlignAstronautToViewer()
    {
        if (astronautInstance == null || !faceMainCameraOnSpawn)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);
    }

    public void OnAstronautLandingFinished()
    {
        if (astronautInstance == null || landingHandled || introSkipped)
            return;

        landingHandled = true;
        Log("[AlefKeplerSequenceManager] OnAstronautLandingFinished called.");

        if (faceCameraAgainAfterLanding)
            AlignAstronautToViewer();

        StartCoroutine(BeginSpeechAfterLandingRoutine());
    }

    private IEnumerator BeginSpeechAfterLandingRoutine()
    {
        yield return new WaitForSeconds(delayAfterLandingBeforeSpeech);

        if (astronautInstance == null || speechStarted)
            yield break;

        speechStarted = true;
        currentPhase = SequencePhase.Intro;

        if (skipInput != null)
            skipInput.ResetSkip();

        Log("[AlefKeplerSequenceManager] Intro speech begins. Showing intro skip UI.");
        ShowSkipUIForIntroOrStation1();

        astronautInstance.StartSpeechWithGreeting(introVoiceClip);
    }

    public void OnAstronautSpeechFinished()
    {
        if (introSkipped)
            return;

        currentPhase = SequencePhase.None;
        HideAllSkipUI();

        FadeBackgroundMusic(normalMusicVolume, musicFadeDuration);

        if (walkToFirstStationStarted || walkFinished || walkRoutine != null)
            return;

        walkToFirstStationStarted = true;
        walkRoutine = StartCoroutine(WalkToFirstStationRoutine());

        Log("[AlefKeplerSequenceManager] Intro speech finished. Starting walk to first station.");
    }

    public void OnAstronautStationSpeechFinished()
    {
        SequencePhase finishedPhase = currentPhase;

        currentPhase = SequencePhase.None;
        HideAllSkipUI();

        FadeBackgroundMusic(normalMusicVolume, musicFadeDuration);

        if (astronautInstance != null)
            astronautInstance.ReturnToIdle();

        waitingForTapToStartSecondStation = false;
        waitingForTapToStartThirdStation = false;

        Log($"[AlefKeplerSequenceManager] OnAstronautStationSpeechFinished. Finished phase: {finishedPhase}");

        if (finishedPhase == SequencePhase.FirstStationSpeech)
        {
            waitingForTapToStartSecondStation = true;

            if (astronautInstance != null)
                astronautInstance.SetTapEnabled(true);

            Log("[AlefKeplerSequenceManager] Waiting for tap to start second station.");
        }
        else if (finishedPhase == SequencePhase.SecondStationSpeech)
        {
            waitingForTapToStartThirdStation = true;

            if (astronautInstance != null)
                astronautInstance.SetTapEnabled(true);

            Log("[AlefKeplerSequenceManager] Waiting for tap to start third station.");
        }
        else if (finishedPhase == SequencePhase.ThirdStationSpeech)
        {
            ActivateConversationMode();
        }
        else
        {
            if (astronautInstance != null)
                astronautInstance.SetTapEnabled(false);
        }
    }

    private IEnumerator WalkToFirstStationRoutine()
    {
        yield return new WaitForSeconds(delayBeforeWalkingToFirstStation);

        if (astronautInstance == null || firstStationTarget == null)
        {
            walkRoutine = null;
            yield break;
        }

        Log("[AlefKeplerSequenceManager] WalkToFirstStationRoutine started.");
        yield return MoveAstronautToTarget(firstStationTarget);

        walkFinished = true;
        walkToFirstStationStarted = false;
        walkRoutine = null;

        HideAllSkipUI();

        if (firstStationVoiceClip != null)
            StartCoroutine(BeginFirstStationSpeechRoutine());
    }

    private IEnumerator WalkToSecondStationRoutine()
    {
        yield return new WaitForSeconds(delayBeforeWalkingToSecondStation);

        if (astronautInstance == null || secondStationTarget == null)
        {
            walkToSecondStationStarted = false;
            yield break;
        }

        Log("[AlefKeplerSequenceManager] WalkToSecondStationRoutine started.");
        yield return MoveAstronautToTarget(secondStationTarget);

        walkToSecondStationStarted = false;

        if (secondStationVoiceClip != null)
            StartCoroutine(BeginSecondStationSpeechRoutine());
    }

    private IEnumerator WalkToThirdStationRoutine()
    {
        yield return new WaitForSeconds(delayBeforeWalkingToThirdStation);

        if (astronautInstance == null || thirdStationTarget == null)
        {
            walkToThirdStationStarted = false;
            yield break;
        }

        Log("[AlefKeplerSequenceManager] WalkToThirdStationRoutine started.");
        yield return MoveAstronautToTarget(thirdStationTarget);

        walkToThirdStationStarted = false;

        if (thirdStationVoiceClip != null)
            StartCoroutine(BeginThirdStationSpeechRoutine());
    }

    private IEnumerator MoveAstronautToTarget(Transform stationTarget)
    {
        astronautInstance.PlayWalk();

        Vector3 targetPos = new Vector3(
            stationTarget.position.x,
            astronautInstance.transform.position.y,
            stationTarget.position.z
        );

        Log($"[AlefKeplerSequenceManager] MoveAstronautToTarget -> target: {stationTarget.name}");

        while (HorizontalDistance(astronautInstance.transform.position, targetPos) > stopBeforeTargetDistance)
        {
            Vector3 direction = targetPos - astronautInstance.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

                if (add180DegreeRotationFix)
                    targetRotation *= Quaternion.Euler(0f, 180f, 0f);

                astronautInstance.transform.rotation = Quaternion.Slerp(
                    astronautInstance.transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            astronautInstance.transform.position = Vector3.MoveTowards(
                astronautInstance.transform.position,
                targetPos,
                walkSpeed * Time.deltaTime
            );

            yield return null;
        }

        astronautInstance.PlayStopWalk();

        yield return new WaitForSeconds(stopWalkingDuration);

        float remaining = HorizontalDistance(astronautInstance.transform.position, targetPos);
        if (remaining > finalNudgeDistance)
        {
            Vector3 adjustedPos = Vector3.MoveTowards(
                astronautInstance.transform.position,
                targetPos,
                remaining - finalNudgeDistance
            );

            adjustedPos.y = astronautInstance.transform.position.y;
            astronautInstance.transform.position = adjustedPos;
        }

        yield return new WaitForSeconds(delayBeforeFacingCamera);

        if (faceCameraAfterWalk)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                if (useRightTurnAnimationToFaceCamera && IsTargetOnRightSide(astronautInstance.transform, cam.transform))
                {
                    yield return astronautInstance.PlayRightTurnAndFaceTarget(
                        cam.transform,
                        rightTurnDuration,
                        add180DegreeRotationFix
                    );
                }
                else
                {
                    astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);
                }
            }
        }

        astronautInstance.ReturnToIdle();
        Log("[AlefKeplerSequenceManager] MoveAstronautToTarget finished.");
    }

    private IEnumerator BeginFirstStationSpeechRoutine()
    {
        yield return new WaitForSeconds(delayBeforeFirstStationSpeech);

        if (astronautInstance == null || firstStationSpeechStarted)
            yield break;

        firstStationSpeechStarted = true;
        currentPhase = SequencePhase.FirstStationSpeech;

        astronautInstance.ConfigureStationTiming(
            firstStationPointingTime,
            firstStationTalking1Time,
            firstStationTalking2Time,
            firstStationTalking3Time
        );

        if (skipInput != null)
            skipInput.ResetSkip();

        Log("[AlefKeplerSequenceManager] Station 1 speech begins. Showing intro/station1 skip UI.");
        ShowSkipUIForIntroOrStation1();

        if (duckMusicDuringStationSpeech)
            FadeBackgroundMusic(duckedMusicVolume, musicFadeDuration);

        Camera cam = Camera.main;

        if (faceCameraBeforeStationSpeech && cam != null)
            astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);

        astronautInstance.StartStationSpeech(
            firstStationVoiceClip,
            firstArtworkLookTarget,
            cam != null ? cam.transform : null,
            add180DegreeRotationFix
        );
    }

    private IEnumerator BeginSecondStationSpeechRoutine()
    {
        yield return new WaitForSeconds(delayBeforeSecondStationSpeech);

        if (astronautInstance == null || secondStationSpeechStarted)
            yield break;

        secondStationSpeechStarted = true;
        currentPhase = SequencePhase.SecondStationSpeech;

        astronautInstance.ConfigureStationTiming(
            secondStationPointingTime,
            secondStationTalking1Time,
            secondStationTalking2Time,
            secondStationTalking3Time
        );

        if (skipInput != null)
            skipInput.ResetSkip();

        Log("[AlefKeplerSequenceManager] Station 2 speech begins. Showing station 2 skip UI.");
        ShowSkipUIForStation2();

        if (duckMusicDuringStationSpeech)
            FadeBackgroundMusic(duckedMusicVolume, musicFadeDuration);

        Camera cam = Camera.main;

        if (faceCameraBeforeStationSpeech && cam != null)
            astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);

        astronautInstance.StartStationSpeech(
            secondStationVoiceClip,
            secondArtworkLookTarget,
            cam != null ? cam.transform : null,
            add180DegreeRotationFix
        );
    }

    private IEnumerator BeginThirdStationSpeechRoutine()
    {
        yield return new WaitForSeconds(delayBeforeThirdStationSpeech);

        if (astronautInstance == null || thirdStationSpeechStarted)
            yield break;

        thirdStationSpeechStarted = true;
        currentPhase = SequencePhase.ThirdStationSpeech;

        astronautInstance.ConfigureStationTiming(
            thirdStationPointingTime,
            thirdStationTalking1Time,
            thirdStationTalking2Time,
            thirdStationTalking3Time
        );

        if (skipInput != null)
            skipInput.ResetSkip();

        Log("[AlefKeplerSequenceManager] Station 3 speech begins. Showing station 3 skip UI.");
        ShowSkipUIForStation3();

        if (duckMusicDuringStationSpeech)
            FadeBackgroundMusic(duckedMusicVolume, musicFadeDuration);

        Camera cam = Camera.main;

        if (faceCameraBeforeStationSpeech && cam != null)
            astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);

        astronautInstance.StartStationSpeech(
            thirdStationVoiceClip,
            thirdArtworkLookTarget,
            cam != null ? cam.transform : null,
            add180DegreeRotationFix
        );
    }

    private void ActivateConversationMode()
    {
        if (astronautInstance == null)
            return;

        astronautInstance.SetTapEnabled(true);

        AlefKeplerTapXR tap = astronautInstance.GetComponent<AlefKeplerTapXR>();
        if (tap != null)
        {
            tap.EnableConversationMode();
            Log("[AlefKeplerSequenceManager] Conversation mode activated.");
        }
        else
        {
            Debug.LogWarning("[AlefKeplerSequenceManager] AlefKeplerTapXR not found on astronaut instance.");
        }
    }

    public void FaceAstronautToCameraForConversation()
    {
        if (astronautInstance == null)
        {
            Log("[AlefKeplerSequenceManager] FaceAstronautToCameraForConversation aborted: astronautInstance is null.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Log("[AlefKeplerSequenceManager] FaceAstronautToCameraForConversation aborted: Camera.main is null.");
            return;
        }

        if (useRightTurnAnimationToFaceCamera && IsTargetOnRightSide(astronautInstance.transform, cam.transform))
        {
            StartCoroutine(FaceCameraWithOptionalTurnRoutine(cam.transform));
        }
        else
        {
            astronautInstance.FaceTargetInstant(cam.transform, add180DegreeRotationFix);
            astronautInstance.ReturnToIdle();
            Log("[AlefKeplerSequenceManager] Astronaut faced camera instantly for conversation.");
        }
    }

    private IEnumerator FaceCameraWithOptionalTurnRoutine(Transform cam)
    {
        yield return astronautInstance.PlayRightTurnAndFaceTarget(
            cam,
            rightTurnDuration,
            add180DegreeRotationFix
        );

        astronautInstance.ReturnToIdle();
        Log("[AlefKeplerSequenceManager] Astronaut finished turning toward camera for conversation.");
    }

    private void ShowSkipUIForIntroOrStation1()
    {
        Log("[AlefKeplerSequenceManager] ShowSkipUIForIntroOrStation1");

        if (introAndStation1SkipUiRoot != null)
            introAndStation1SkipUiRoot.SetActive(true);
        else
            Debug.LogWarning("[AlefKeplerSequenceManager] introAndStation1SkipUiRoot is NULL.");

        if (station2SkipUiRoot != null)
            station2SkipUiRoot.SetActive(false);

        if (station3SkipUiRoot != null)
            station3SkipUiRoot.SetActive(false);
    }

    private void ShowSkipUIForStation2()
    {
        Log("[AlefKeplerSequenceManager] ShowSkipUIForStation2");

        if (introAndStation1SkipUiRoot != null)
            introAndStation1SkipUiRoot.SetActive(false);

        if (station2SkipUiRoot != null)
            station2SkipUiRoot.SetActive(true);
        else
            Debug.LogWarning("[AlefKeplerSequenceManager] station2SkipUiRoot is NULL.");

        if (station3SkipUiRoot != null)
            station3SkipUiRoot.SetActive(false);
    }

    private void ShowSkipUIForStation3()
    {
        Log("[AlefKeplerSequenceManager] ShowSkipUIForStation3");

        if (introAndStation1SkipUiRoot != null)
            introAndStation1SkipUiRoot.SetActive(false);

        if (station2SkipUiRoot != null)
            station2SkipUiRoot.SetActive(false);

        if (station3SkipUiRoot != null)
            station3SkipUiRoot.SetActive(true);
        else
            Debug.LogWarning("[AlefKeplerSequenceManager] station3SkipUiRoot is NULL.");
    }

    private void HideAllSkipUI()
    {
        Log("[AlefKeplerSequenceManager] HideAllSkipUI");

        if (introAndStation1SkipUiRoot != null)
            introAndStation1SkipUiRoot.SetActive(false);

        if (station2SkipUiRoot != null)
            station2SkipUiRoot.SetActive(false);

        if (station3SkipUiRoot != null)
            station3SkipUiRoot.SetActive(false);
    }

    private bool IsTargetOnRightSide(Transform actor, Transform target)
    {
        if (actor == null || target == null)
            return false;

        Vector3 toTarget = target.position - actor.position;
        toTarget.y = 0f;

        float side = Vector3.Dot(actor.right, toTarget.normalized);
        return side > 0f;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void FadeBackgroundMusic(float targetVolume, float duration)
    {
        if (backgroundMusicSource == null)
            return;

        if (!backgroundMusicSource.isPlaying && backgroundMusicSource.clip != null)
            backgroundMusicSource.Play();

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        Log($"[AlefKeplerSequenceManager] FadeBackgroundMusic -> targetVolume: {targetVolume}, duration: {duration}");
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

        if (backgroundMusicSource.volume > 0.001f &&
            !backgroundMusicSource.isPlaying &&
            backgroundMusicSource.clip != null)
        {
            backgroundMusicSource.Play();
        }

        musicFadeRoutine = null;
    }

    private void Log(string message)
    {
        if (logToConsole)
            Debug.Log(message);
    }
}