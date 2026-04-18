using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;

    private void Awake()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        Vector3 lookDir = targetCamera.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(-lookDir.normalized, Vector3.up);
    }
}