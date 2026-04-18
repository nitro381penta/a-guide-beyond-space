using UnityEngine;

public class LineVisualColorProxy : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color artworkHoverColor = new Color(1f, 0.2f, 0.7f);

    private int hoverCount = 0;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        ApplyColor(normalColor);
    }

    public void SetArtworkHover(bool isHovering)
    {
        hoverCount += isHovering ? 1 : -1;
        hoverCount = Mathf.Max(0, hoverCount);

        ApplyColor(hoverCount > 0 ? artworkHoverColor : normalColor);
    }

    private void ApplyColor(Color color)
    {
        if (lineRenderer == null)
            return;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        lineRenderer.colorGradient = gradient;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}