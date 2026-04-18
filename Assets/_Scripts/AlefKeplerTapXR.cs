using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(XRSimpleInteractable))]
public class AlefKeplerTapXR : MonoBehaviour
{
    private AlefKeplerSequenceManager sequenceManager;
    private XRSimpleInteractable interactable;
    private bool tapEnabled;

    private AlefConversationStateMachine conversationStateMachine;
    private ConversationUIController conversationUIController;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelected);
    }

    public void Initialize(AlefKeplerSequenceManager manager)
    {
        sequenceManager = manager;
        SetTapEnabled(false);
    }

    public void InitializeConversation(
        AlefConversationStateMachine stateMachine,
        ConversationUIController uiController)
    {
        conversationStateMachine = stateMachine;
        conversationUIController = uiController;

        Debug.Log("[AlefKeplerTapXR] Conversation references initialized.");
    }

    public void SetTapEnabled(bool enabled)
    {
        tapEnabled = enabled;

        if (interactable != null)
            interactable.enabled = enabled;
    }

    public void EnableConversationMode()
    {
        tapEnabled = true;

        if (interactable != null)
            interactable.enabled = true;

        if (conversationStateMachine != null)
            conversationStateMachine.SetState(AlefConversationStateMachine.ConversationState.ConversationReady);

        Debug.Log("[AlefKeplerTapXR] Conversation mode enabled.");
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Debug.Log("[AlefKeplerTapXR] OnSelected fired.");

        if (!tapEnabled)
            return;

        if (conversationStateMachine != null &&
            conversationStateMachine.CurrentState == AlefConversationStateMachine.ConversationState.ConversationReady)
        {
            Debug.Log("[AlefKeplerTapXR] Alef tapped in conversation mode.");

            if (conversationUIController != null)
                conversationUIController.ShowConversationPanel();
            else
                Debug.LogWarning("[AlefKeplerTapXR] conversationUIController is null.");

            return;
        }

        if (sequenceManager != null)
        {
            Debug.Log("[AlefKeplerTapXR] Alef tapped in sequence mode.");
            sequenceManager.OnAstronautTapped();
        }
    }
}