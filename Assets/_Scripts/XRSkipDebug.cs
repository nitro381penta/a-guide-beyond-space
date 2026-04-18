using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
[RequireComponent(typeof(Collider))]
public class XRSkipDebug : MonoBehaviour
{
    [Header("Optional Visual Feedback")]
    [SerializeField] private TMP_Text skipText;
    [SerializeField] private SpriteRenderer arrowSpriteRenderer;
    [SerializeField] private MeshRenderer arrowMeshRenderer;

    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.cyan;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color selectColor = Color.green;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool showOnScreenStatus = true;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Collider col;

    private string currentStatus = "Idle";
    private float lastEventTime;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        col = GetComponent<Collider>();

        if (logToConsole)
        {
            Debug.Log($"[XRSkipDebug] Awake on {name}");
            Debug.Log($"[XRSkipDebug] Layer: {LayerMask.LayerToName(gameObject.layer)}");
            Debug.Log($"[XRSkipDebug] Collider: {col.GetType().Name}");
            Debug.Log($"[XRSkipDebug] Bounds Center: {col.bounds.center}, Size: {col.bounds.size}");
        }

        ApplyColor(idleColor);
    }

    private void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        currentStatus = $"Hover by {args.interactorObject.transform.name}";
        lastEventTime = Time.time;
        ApplyColor(hoverColor);

        if (logToConsole)
            Debug.Log($"[XRSkipDebug] Hover Entered by {args.interactorObject.transform.name}");
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        currentStatus = $"Hover Exit by {args.interactorObject.transform.name}";
        lastEventTime = Time.time;
        ApplyColor(idleColor);

        if (logToConsole)
            Debug.Log($"[XRSkipDebug] Hover Exited by {args.interactorObject.transform.name}");
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        currentStatus = $"Selected by {args.interactorObject.transform.name}";
        lastEventTime = Time.time;
        ApplyColor(selectColor);

        if (logToConsole)
            Debug.Log($"[XRSkipDebug] Select Entered by {args.interactorObject.transform.name}");
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        currentStatus = $"Select Exit by {args.interactorObject.transform.name}";
        lastEventTime = Time.time;
        ApplyColor(hoverColor);

        if (logToConsole)
            Debug.Log($"[XRSkipDebug] Select Exited by {args.interactorObject.transform.name}");
    }

    private void ApplyColor(Color color)
    {
        if (skipText != null)
            skipText.color = color;

        if (arrowSpriteRenderer != null)
            arrowSpriteRenderer.color = color;

        if (arrowMeshRenderer != null && arrowMeshRenderer.material != null)
            arrowMeshRenderer.material.color = color;
    }

    private void OnGUI()
    {
        if (!showOnScreenStatus)
            return;

        GUI.Label(
            new Rect(20, 20, 700, 30),
            $"[XRSkipDebug] {name} | Status: {currentStatus} | Last Event: {lastEventTime:F2}"
        );
    }

    private void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        if (c == null)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }

    [ContextMenu("Log Setup Now")]
    public void LogSetupNow()
    {
        Collider c = GetComponent<Collider>();
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable xri = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        Debug.Log($"[XRSkipDebug] Object: {name}");
        Debug.Log($"[XRSkipDebug] Layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log($"[XRSkipDebug] Position: {transform.position}");
        Debug.Log($"[XRSkipDebug] Rotation: {transform.rotation.eulerAngles}");
        Debug.Log($"[XRSkipDebug] Scale: {transform.lossyScale}");
        Debug.Log($"[XRSkipDebug] Collider Bounds Center: {c.bounds.center}, Size: {c.bounds.size}");
        Debug.Log($"[XRSkipDebug] Interaction Layer Mask: {xri.interactionLayers}");
    }
}