using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ArtworkLineColorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NearFarInteractor nearFarInteractor;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color artworkColor = new Color(1f, 0.2f, 0.7f);
    [SerializeField] private Color invalidColor = new Color(0.65f, 0.65f, 0.65f);

    [Header("Detection")]
    [SerializeField] private string artworkTag = "Artwork";

    private readonly List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable> validTargets = new();

    private void Awake()
    {
        if (nearFarInteractor == null)
            nearFarInteractor = GetComponentInParent<NearFarInteractor>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (nearFarInteractor == null || lineRenderer == null)
            return;

        validTargets.Clear();
        nearFarInteractor.GetValidTargets(validTargets);

        if (validTargets.Count == 0)
        {
            SetLineColor(invalidColor);
            return;
        }

        var targetComponent = validTargets[0] as Component;

        if (targetComponent == null)
        {
            SetLineColor(defaultColor);
            return;
        }

        bool isArtwork =
            targetComponent.CompareTag(artworkTag) ||
            targetComponent.GetComponent<ArtworkInteractable>() != null ||
            targetComponent.GetComponentInParent<ArtworkInteractable>() != null ||
            targetComponent.CompareTag(artworkTag) ||
            targetComponent.transform.root.CompareTag(artworkTag);

        SetLineColor(isArtwork ? artworkColor : defaultColor);
    }

    private void SetLineColor(Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        lineRenderer.colorGradient = g;
    }
}