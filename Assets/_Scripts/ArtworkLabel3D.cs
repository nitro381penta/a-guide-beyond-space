using TMPro;
using UnityEngine;

public class ArtworkLabel3D : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private TMP_Text yearText;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;

    private void Awake()
    {
        gameObject.SetActive(false);

        if (logToConsole)
            Debug.Log("[ArtworkLabel3D] Awake -> hidden");
    }

    public void Show(string title, string artist, string year, Vector3 worldPosition, Vector3 worldEulerRotation)
    {
        if (titleText == null || artistText == null || yearText == null)
        {
            Debug.LogWarning("[ArtworkLabel3D] Missing TMP references.");
            return;
        }

        titleText.text = title;
        artistText.text = artist;
        yearText.text = year;

        transform.position = worldPosition;
        transform.rotation = Quaternion.Euler(worldEulerRotation);

        gameObject.SetActive(true);

        if (logToConsole)
        {
            Debug.Log("[ArtworkLabel3D] Show called");
            Debug.Log($"[ArtworkLabel3D] Position: {worldPosition}");
            Debug.Log($"[ArtworkLabel3D] Rotation: {worldEulerRotation}");
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);

        if (logToConsole)
            Debug.Log("[ArtworkLabel3D] Hide called");
    }
}