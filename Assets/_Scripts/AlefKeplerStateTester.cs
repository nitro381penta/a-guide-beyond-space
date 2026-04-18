using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AlefKeplerStateTester : MonoBehaviour
{
    [SerializeField] private string stateName = "Standard Idle";
    [SerializeField] private float crossFadeDuration = 0.1f;
    [SerializeField] private float delay = 0.2f;

    private Animator animatorComponent;

    private void Awake()
    {
        animatorComponent = GetComponent<Animator>();
    }

    private void Start()
    {
        Invoke(nameof(PlayTestState), delay);
    }

    private void PlayTestState()
    {
        if (animatorComponent == null)
        {
            Debug.LogError("[StateTester] Kein Animator gefunden.");
            return;
        }

        animatorComponent.CrossFadeInFixedTime(stateName, crossFadeDuration);
        Debug.Log($"[StateTester] Spiele State: {stateName}");
    }
}