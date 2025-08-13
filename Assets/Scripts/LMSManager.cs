using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class LMSManager : MonoBehaviour
{
    public static LMSManager Instance { get; private set; }

    [Header("Optional LRS settings (leave blank for offline demo)")]
    public string xApiEndpoint = ""; // e.g. https://lrs.example.com/xapi/statements
    public string authHeader = ""; // e.g. "Basic BASE64" or "Bearer TOKEN"

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public void SendProgress(string moduleId, float percent)
    {
        Debug.Log($"[LMS] progress: {moduleId} = {percent}%");
        if (string.IsNullOrEmpty(xApiEndpoint)) return;

        // simple JSON body for xAPI-like statement
        string json = "{\"actor\":{\"name\":\"Test User\",\"mbox\":\"mailto:test@example.com\"}," +
                      "\"verb\":{\"id\":\"http://adlnet.gov/expapi/verbs/completed\",\"display\":{\"en-US\":\"completed\"}}," +
                      "\"object\":{\"id\":\"http://example.com/" + moduleId + "\"}," +
                      "\"result\":{\"score\":{\"raw\":" + percent + "}}}";

        StartCoroutine(PostJson(xApiEndpoint, json));
    }

    IEnumerator PostJson(string url, string json)
    {
        var uwr = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(authHeader)) uwr.SetRequestHeader("Authorization", authHeader);

        yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (uwr.result != UnityWebRequest.Result.Success)
#else
        if (uwr.isNetworkError || uwr.isHttpError)
#endif
        {
            Debug.LogWarning("[LMS] POST failed: " + uwr.error);
        }
        else
        {
            Debug.Log("[LMS] POST success: " + uwr.downloadHandler.text);
        }
    }
}
