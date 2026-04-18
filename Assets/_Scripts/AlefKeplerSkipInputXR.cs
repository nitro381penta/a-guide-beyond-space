using UnityEngine;
using UnityEngine.InputSystem;

public class AlefKeplerSkipInputXR : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AlefKeplerSequenceManager sequenceManager;

    [Header("XR Skip Action")]
    [SerializeField] private InputActionProperty skipAction;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    private bool skipTriggered;

    private void OnEnable()
    {
        skipTriggered = false;

        if (skipAction.action == null)
        {
            Debug.LogWarning("[AlefKeplerSkipInputXR] Skip Action is not assigned.");
            return;
        }

        skipAction.action.performed -= OnSkipPerformed;
        skipAction.action.performed += OnSkipPerformed;
        skipAction.action.Enable();

        if (logToConsole)
        {
            Debug.Log($"[AlefKeplerSkipInputXR] Registered action: {skipAction.action.name}");
            Debug.Log("[AlefKeplerSkipInputXR] Waiting for skip input...");
        }
    }

    private void OnDisable()
    {
        if (skipAction.action == null)
            return;

        skipAction.action.performed -= OnSkipPerformed;

        if (logToConsole)
            Debug.Log("[AlefKeplerSkipInputXR] Unregistered skip action.");
    }

    private void OnSkipPerformed(InputAction.CallbackContext context)
    {
        if (skipTriggered)
            return;

        if (sequenceManager == null)
        {
            Debug.LogWarning("[AlefKeplerSkipInputXR] SequenceManager is null.");
            return;
        }

        skipTriggered = true;

        if (logToConsole)
            Debug.Log($"[AlefKeplerSkipInputXR] Skip triggered by action: {context.action.name}");

        sequenceManager.SkipCurrentSequence();
    }

    public void ResetSkip()
    {
        skipTriggered = false;

        if (logToConsole)
            Debug.Log("[AlefKeplerSkipInputXR] Skip reset.");
    }
}