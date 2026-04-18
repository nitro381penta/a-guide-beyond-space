using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class StartAnimationButton3D : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private string startLabel = "Start Animation";
    [SerializeField] private string stopLabel = "Stop Animation";
    [SerializeField] private Vector3 localEulerOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private bool logToConsole = true;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private IWaveField currentTarget;
    private MonoBehaviour currentTargetComponent;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnPressed);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnPressed);
    }

    public void ShowFor(MonoBehaviour targetComponent, Transform anchor)
    {
        currentTargetComponent = targetComponent;
        currentTarget = targetComponent as IWaveField;

        if (currentTarget == null)
        {
            Debug.LogWarning("[StartAnimationButton3D] Target does not implement IWaveField.");
            gameObject.SetActive(false);
            return;
        }

        if (anchor != null)
        {
            transform.position = anchor.position;
            transform.rotation = anchor.rotation * Quaternion.Euler(localEulerOffset);
        }

        RefreshLabel();
        gameObject.SetActive(true);

        if (logToConsole)
            Debug.Log($"[StartAnimationButton3D] ShowFor -> rot {transform.rotation.eulerAngles}");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentTarget = null;
        currentTargetComponent = null;

        if (logToConsole)
            Debug.Log("[StartAnimationButton3D] Hide");
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        if (currentTarget == null)
            return;

        currentTarget.Toggle();
        RefreshLabel();

        if (logToConsole)
            Debug.Log("[StartAnimationButton3D] Pressed");
    }

    private void RefreshLabel()
    {
        if (labelText == null)
            return;

        labelText.text = (currentTarget != null && currentTarget.IsPlaying)
            ? stopLabel
            : startLabel;
    }
}