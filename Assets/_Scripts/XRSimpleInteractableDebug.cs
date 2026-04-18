using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class XRSimpleInteractableDebug : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
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
        Debug.Log("HOVER ENTER: " + gameObject.name);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log("HOVER EXIT: " + gameObject.name);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("SELECT ENTER: " + gameObject.name);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log("SELECT EXIT: " + gameObject.name);
    }
}