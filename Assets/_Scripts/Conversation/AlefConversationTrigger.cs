using UnityEngine;

public class AlefConversationTrigger : MonoBehaviour
{
    [SerializeField] private AlefConversationStateMachine stateMachine;
    [SerializeField] private ConversationUIController uiController;
    [SerializeField] private AlefKeplerSequenceManager sequenceManager;

    [Header("Optional")]
    [SerializeField] private bool logToConsole = true;

    public void EnableConversationMode()
    {
        if (stateMachine != null)
        {
            stateMachine.SetState(AlefConversationStateMachine.ConversationState.ConversationReady);
        }

        Log("[AlefConversationTrigger] Conversation mode enabled.");
    }

    public void OnAlefTapped()
    {
        if (stateMachine == null || uiController == null)
        {
            Log("[AlefConversationTrigger] Missing references.");
            return;
        }

        Log("[AlefConversationTrigger] Alef tapped.");

        if (!stateMachine.IsReadyForTap())
        {
            Log("[AlefConversationTrigger] Tap ignored because state is not ready.");
            return;
        }

        if (sequenceManager != null)
            sequenceManager.FaceAstronautToCameraForConversation();

        uiController.ShowConversationPanel();

        Log("[AlefConversationTrigger] Conversation UI opened.");
    }

    public void OnConversationClosed()
    {
        if (uiController != null)
            uiController.HideConversationPanel();

        if (stateMachine != null)
            stateMachine.SetState(AlefConversationStateMachine.ConversationState.ConversationReady);

        Log("[AlefConversationTrigger] Conversation UI closed. Alef can be tapped again.");
    }

    private void Log(string message)
    {
        if (logToConsole)
            Debug.Log(message);
    }
}