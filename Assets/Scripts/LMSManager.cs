using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LMSManager : MonoBehaviour
{
    public static LMSManager Instance { get; private set; }

    [Header("xAPI / LRS Settings")]
    [Tooltip("Full xAPI endpoint")]
    public string xApiEndpoint = "";

    [Tooltip("Either set an auth header directly (e.g. 'Basic ...' or 'Bearer ...') OR provide credentials below")]
    [TextArea(1, 2)]
    public string authHeader = "";

    [Tooltip("If you prefer username/password, set them (will build Basic header). Leave empty if using token in authHeader.")]
    public string lrsUsername = "";
    public string lrsPassword = "";

    [Header("Retry / Queue")]
    public bool enableQueueing = true;
    public float retryIntervalSeconds = 20f; // how often to attempt queue flush

    // Internal
    private readonly List<string> pendingJson = new List<string>();
    private string queueFilePath => Path.Combine(Application.persistentDataPath, "lrs_queue.json");
    private bool isPosting = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep alive across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Prefer explicit authHeader, else build Basic from credentials
        if (string.IsNullOrEmpty(authHeader) && !string.IsNullOrEmpty(lrsUsername))
        {
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{lrsUsername}:{lrsPassword}"));
            authHeader = "Basic " + b64;
        }

        LoadQueueFromDisk();
        StartCoroutine(PeriodicQueueProcessor());
    }

    #region Public API
    public void SendProgress(string moduleId, float percent)
    {
        string actorName = "Aman"; // Replace with dynamic learner name
        string actorMbox = "amantheiit@gmail.com"; // Should be in "mailto:" format

        string verbId = "http://adlnet.gov/expapi/verbs/completed";
        string objectId = $"http://example.com/{moduleId}";

        string json = BuildProgressJson(actorName, actorMbox, verbId, objectId, percent);
        EnqueueOrPost(json);
        Debug.Log("[LMS] Queued progress for " + moduleId);
    }

    public void SendRawXApiJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        EnqueueOrPost(json);
    }
    #endregion

    #region JSON Builder
    private string BuildProgressJson(string actorName, string actorMbox, string verbId, string objectId, float percent)
    {
        actorName = EscapeJson(actorName);
        actorMbox = EscapeJson(actorMbox.StartsWith("mailto:") ? actorMbox : "mailto:" + actorMbox);
        verbId = EscapeJson(verbId);
        objectId = EscapeJson(objectId);

        float scoreVal = Mathf.Clamp(percent, 0f, 100f);
        return $"{{\"actor\":{{\"name\":\"{actorName}\",\"mbox\":\"{actorMbox}\"}},\"verb\":{{\"id\":\"{verbId}\"}},\"object\":{{\"id\":\"{objectId}\"}},\"result\":{{\"score\":{{\"raw\":{scoreVal}}}}}}}";
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
    #endregion

    #region Posting & Queue
    private void EnqueueOrPost(string json)
    {
        if (string.IsNullOrEmpty(xApiEndpoint))
        {
            Debug.LogWarning("[LMS] xApiEndpoint not set. Skipping send.");
            return;
        }
        StartCoroutine(PostJsonRoutine(json));
    }

    private IEnumerator PostJsonRoutine(string json)
    {
        if (this == null) yield break;

        while (isPosting) yield return null;
        isPosting = true;

        using (UnityWebRequest uwr = new UnityWebRequest(xApiEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(authHeader))
                uwr.SetRequestHeader("Authorization", authHeader);

            uwr.timeout = 15;
            yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool success = (uwr.result == UnityWebRequest.Result.Success ||
                           ((int)uwr.responseCode >= 200 && (int)uwr.responseCode < 300));
#else
            bool success = !uwr.isNetworkError && !uwr.isHttpError;
#endif

            if (success)
            {
                Debug.Log("[LMS] POST success: " + uwr.responseCode + " - " + uwr.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("[LMS] POST failed: " + uwr.responseCode + " - " + uwr.error);
                if (enableQueueing)
                {
                    pendingJson.Add(json);
                    SaveQueueToDisk();
                }
            }
        }

        isPosting = false;
    }

    private IEnumerator PeriodicQueueProcessor()
    {
        while (true)
        {
            yield return new WaitForSeconds(retryIntervalSeconds);

            if (enableQueueing && pendingJson.Count > 0 && !string.IsNullOrEmpty(xApiEndpoint))
            {
                Debug.Log("[LMS] Attempting to flush queue (" + pendingJson.Count + " items)");

                for (int i = pendingJson.Count - 1; i >= 0; i--)
                {
                    string item = pendingJson[i];
                    yield return StartCoroutine(AttemptPostAndRemoveOnSuccess(item, i));
                }
            }
        }
    }

    private IEnumerator AttemptPostAndRemoveOnSuccess(string json, int indexInList)
    {
        using (UnityWebRequest uwr = new UnityWebRequest(xApiEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(authHeader))
                uwr.SetRequestHeader("Authorization", authHeader);

            uwr.timeout = 15;
            yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool success = (uwr.result == UnityWebRequest.Result.Success ||
                           (int)uwr.responseCode >= 200 && (int)uwr.responseCode < 300);
#else
            bool success = !uwr.isNetworkError && !uwr.isHttpError;
#endif
            if (success)
            {
                Debug.Log("[LMS] Queue item posted successfully.");
                if (indexInList >= 0 && indexInList < pendingJson.Count && pendingJson[indexInList] == json)
                    pendingJson.RemoveAt(indexInList);
                else
                    pendingJson.Remove(json);

                SaveQueueToDisk();
            }
            else
            {
                Debug.LogWarning("[LMS] Queue post failed: " + uwr.error);
            }
        }
    }
    #endregion

    #region Save/Load Queue
    private void SaveQueueToDisk()
    {
        try
        {
            var wrapper = new QueueWrapper { items = pendingJson };
            string json = JsonUtility.ToJson(wrapper);
            File.WriteAllText(queueFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[LMS] SaveQueue error: " + ex.Message);
        }
    }

    private void LoadQueueFromDisk()
    {
        try
        {
            if (!File.Exists(queueFilePath)) return;
            string txt = File.ReadAllText(queueFilePath);
            var wrapper = JsonUtility.FromJson<QueueWrapper>(txt);
            if (wrapper != null && wrapper.items != null)
            {
                pendingJson.Clear();
                pendingJson.AddRange(wrapper.items);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[LMS] LoadQueue error: " + ex.Message);
        }
    }

    [Serializable]
    private class QueueWrapper { public List<string> items = new List<string>(); }
    #endregion

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
