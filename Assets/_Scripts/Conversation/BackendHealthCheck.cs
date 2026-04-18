using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BackendHealthCheck : MonoBehaviour
{
    [SerializeField] private string healthUrl = "http://192.168.2.137:8000/health";

    private void Start()
    {
        StartCoroutine(CheckHealth());
    }

    private IEnumerator CheckHealth()
    {
        Debug.Log("Checking backend: " + healthUrl);

        using UnityWebRequest request = UnityWebRequest.Get(healthUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Health check failed: " + request.error);
        }
        else
        {
            Debug.Log("Health response: " + request.downloadHandler.text);
        }
    }
}