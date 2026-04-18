using System.Collections;
using UnityEngine;

public class LandingFXController : MonoBehaviour
{
    [Header("FX References")]
    [SerializeField] private ParticleSystem groundSmoke;
    [SerializeField] private ParticleSystem materializeSparkles;
    [SerializeField] private Light arrivalLight;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource whooshSource;
    [SerializeField] private AudioSource landingSource;

    [Header("Light Settings")]
    [SerializeField] private Color lightColor = new Color(0.75f, 1f, 0.55f, 1f);
    [SerializeField] private float startIntensity = 20f;
    [SerializeField] private float endIntensity = 0f;
    [SerializeField] private float lightFadeDuration = 2f;

    [Header("Audio Timing")]
    [SerializeField] private float delayBeforeLandingSound = 0.35f;

    [Header("Lifetime")]
    [SerializeField] private float destroyAfter = 5f;

    public void Begin(AudioClip whooshClip = null, AudioClip landingClip = null)
    {
        if (groundSmoke != null)
            groundSmoke.Play(true);

        if (materializeSparkles != null)
            materializeSparkles.Play(true);

        if (arrivalLight != null)
        {
            arrivalLight.color = lightColor;
            arrivalLight.intensity = startIntensity;
            StartCoroutine(FadeLightRoutine());
        }

        StartCoroutine(PlayAudioSequence(whooshClip, landingClip));
        Destroy(gameObject, destroyAfter);
    }

    private IEnumerator PlayAudioSequence(AudioClip whooshClip, AudioClip landingClip)
    {
        if (whooshSource != null && whooshClip != null)
        {
            whooshSource.clip = whooshClip;
            whooshSource.Play();
        }

        yield return new WaitForSeconds(delayBeforeLandingSound);

        if (landingSource != null && landingClip != null)
        {
            landingSource.clip = landingClip;
            landingSource.Play();
        }
    }

    private IEnumerator FadeLightRoutine()
    {
        if (arrivalLight == null)
            yield break;

        float t = 0f;
        float duration = Mathf.Max(0.01f, lightFadeDuration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            arrivalLight.intensity = Mathf.Lerp(startIntensity, endIntensity, k);
            yield return null;
        }

        arrivalLight.intensity = endIntensity;
    }
}