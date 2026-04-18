using UnityEngine;

public class CanvasDebugFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float distance = 1.2f;
    [SerializeField] private float verticalOffset = -0.15f;
    [SerializeField] private bool faceCamera = true;

    private void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        transform.position = targetCamera.position
                           + targetCamera.forward * distance
                           + Vector3.up * verticalOffset;

        if (faceCamera)
        {
            Vector3 lookDir = targetCamera.position - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(lookDir.normalized);
        }
    }
}