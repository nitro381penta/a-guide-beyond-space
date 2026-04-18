using System;
using UnityEngine;

public class AlefConversationStateMachine : MonoBehaviour
{
    public enum ConversationState
    {
        TourMode,
        ConversationReady,
        Recording,
        Uploading,
        Thinking,
        Speaking,
        Error
    }

    [SerializeField] private ConversationState currentState = ConversationState.TourMode;

    public ConversationState CurrentState => currentState;

    public event Action<ConversationState> OnStateChanged;

    public void SetState(ConversationState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        Debug.Log($"[ConversationStateMachine] New State: {currentState}");
        OnStateChanged?.Invoke(currentState);
    }

    public bool IsReadyForTap()
    {
        return currentState == ConversationState.ConversationReady;
    }

    public bool IsBusy()
    {
        return currentState == ConversationState.Recording ||
               currentState == ConversationState.Uploading ||
               currentState == ConversationState.Thinking ||
               currentState == ConversationState.Speaking;
    }
}