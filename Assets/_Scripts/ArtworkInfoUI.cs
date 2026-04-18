using TMPro;
using UnityEngine;

public class ArtworkInfoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private TMP_Text yearText;

    [Header("Placement")]
    [SerializeField] private float distanceFromCamera = 1.2f;
    [SerializeField] private float verticalOffset = -0.12f;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    private void Awake()
    {
        gameObject.SetActive(false);

        if (logToConsole)
            Debug.Log("[ArtworkInfoUI] Awake -> hidden");
    }

    public void ShowInFrontOfCamera(string title, string artist, string year, Transform cameraTransform)
    {
        if (titleText == null || artistText == null || yearText == null)
        {
            Debug.LogWarning("[ArtworkInfoUI] Missing TMP references.");
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("[ArtworkInfoUI] cameraTransform is null.");
            return;
        }

        titleText.text = title;
        artistText.text = artist;
        yearText.text = year;

        Vector3 targetPosition =
            cameraTransform.position +
            cameraTransform.forward * distanceFromCamera +
            Vector3.up * verticalOffset;

        transform.position = targetPosition;

        Vector3 lookDir = cameraTransform.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        gameObject.SetActive(true);

        if (logToConsole)
        {
            Debug.Log("[ArtworkInfoUI] ShowInFrontOfCamera called");
            Debug.Log($"[ArtworkInfoUI] Position: {transform.position}");
            Debug.Log($"[ArtworkInfoUI] Rotation: {transform.rotation.eulerAngles}");
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);

        if (logToConsole)
            Debug.Log("[ArtworkInfoUI] Hide called");
    }
}