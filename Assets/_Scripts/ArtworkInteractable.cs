using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class ArtworkInteractable : MonoBehaviour
{
    [Header("Artwork Data")]
    [SerializeField] private string artworkTitle = "Untitled";
    [SerializeField] private string artistName = "Unknown Artist";
    [SerializeField] private string year = "Unknown Year";

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    [Header("Animation")]
    [SerializeField] private float scaleMultiplier = 1.08f;
    [SerializeField] private float animationDuration = 0.22f;

    [Header("Shared Label")]
    [SerializeField] private ArtworkLabel3D artworkLabel;

    [Header("Per-Artwork Label Placement")]
    [SerializeField] private Vector3 labelWorldOffset = new Vector3(0f, -0.6f, 0.05f);
    [SerializeField] private Vector3 labelWorldEulerRotation = Vector3.zero;

    [Header("Artwork-specific Animation")]
    [SerializeField] private MonoBehaviour waveFieldBehaviour;
    [SerializeField] private StartAnimationButton3D animationButton;
    [SerializeField] private Transform animationButtonAnchor;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Vector3 originalVisualScale;
    private Coroutine animationCoroutine;
    private bool isSelected;

    private static ArtworkInteractable currentlySelected;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (visualRoot == null)
        {
            Debug.LogWarning($"[ArtworkInteractable:{name}] visualRoot is not assigned.");
            return;
        }

        originalVisualScale = visualRoot.localScale;

        if (logToConsole)
        {
            Debug.Log($"[ArtworkInteractable:{name}] Awake");
            Debug.Log($"[ArtworkInteractable:{name}] Original visual scale: {originalVisualScale}");
        }
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (logToConsole)
            Debug.Log($"[ArtworkInteractable:{name}] OnSelected fired");

        if (currentlySelected != null && currentlySelected != this)
            currentlySelected.DeselectArtwork();

        if (isSelected)
        {
            DeselectArtwork();
            currentlySelected = null;
        }
        else
        {
            SelectArtwork();
            currentlySelected = this;
        }
    }

    private void SelectArtwork()
    {
        isSelected = true;

        Vector3 targetScale = originalVisualScale * scaleMultiplier;
        StartScaleAnimation(targetScale);

        if (artworkLabel != null)
        {
            Vector3 labelPosition = transform.position + labelWorldOffset;
            artworkLabel.Show(artworkTitle, artistName, year, labelPosition, labelWorldEulerRotation);
        }

        if (waveFieldBehaviour != null && animationButton != null && animationButtonAnchor != null)
            animationButton.ShowFor(waveFieldBehaviour, animationButtonAnchor);
    }

    public void DeselectArtwork()
    {
        isSelected = false;

        StartScaleAnimation(originalVisualScale);

        if (artworkLabel != null)
            artworkLabel.Hide();

        if (animationButton != null)
            animationButton.Hide();

        IWaveField waveField = waveFieldBehaviour as IWaveField;
        if (waveField != null && waveField.IsPlaying)
            waveField.Stop();
    }

    private void StartScaleAnimation(Vector3 endScale)
    {
        if (visualRoot == null)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateScale(endScale));
    }

    private IEnumerator AnimateScale(Vector3 endScale)
    {
        Vector3 startScale = visualRoot.localScale;
        float time = 0f;

        while (time < animationDuration)
        {
            float t = time / animationDuration;
            t = t * t * (3f - 2f * t);

            visualRoot.localScale = Vector3.Lerp(startScale, endScale, t);

            time += Time.deltaTime;
            yield return null;
        }

        visualRoot.localScale = endScale;
        animationCoroutine = null;

        if (logToConsole)
            Debug.Log($"[ArtworkInteractable:{name}] Scale animation finished -> {endScale}");
    }
}