using UnityEngine;

public class CanvasVisibilityProbe : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float distance = 1.2f;
    [SerializeField] private float verticalOffset = -0.15f;
    [SerializeField] private Vector3 forcedScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
    [SerializeField] private bool logToConsole = true;

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        transform.position = targetCamera.position
                           + targetCamera.forward * distance
                           + Vector3.up * verticalOffset;

        Vector3 lookDir = targetCamera.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized);

        transform.localScale = forcedScale;
    }

    private void OnEnable()
    {
        if (logToConsole)
            Debug.Log("[CanvasVisibilityProbe] Enabled");
    }
}