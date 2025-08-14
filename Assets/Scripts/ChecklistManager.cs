using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChecklistManager : MonoBehaviour
{
    public static ChecklistManager Instance { get; private set; }

    [Header("UI Refs")]
    [SerializeField] private Transform listParent;   // Drag: ScrollView/Viewport/Content
    [SerializeField] private GameObject rowPrefab;   // Drag: RowPrefab
    [SerializeField] private Button proceedButton;   // Drag: ProceedButton

    // Keep track of rows and completion
    private readonly Dictionary<string, GameObject> rows = new Dictionary<string, GameObject>();
    private readonly HashSet<string> completed = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (proceedButton != null) proceedButton.interactable = false;
    }

    /// <summary>
    /// Call this once per item (e.g., from ItemHoverGlow.Start()).
    /// Creates a row if it doesn't already exist.
    /// </summary>

    public void RegisterItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return;
        if (rows.ContainsKey(itemName)) return;

        var row = Instantiate(rowPrefab, listParent);
        row.name = itemName;

        // Find specific child objects
        //var nameText = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        var statusText = row.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

        ///


        Debug.Log("itemName" + itemName);

        var nameText = row.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = itemName;
        }

        Debug.Log("itemName: " + itemName);

/*        if (nameText != null)
            nameText.text = itemName;*/

        if (statusText != null)
            statusText.text = "Pending";

        rows[itemName] = row;
    }

    public void MarkInspected(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return;
        if (!rows.ContainsKey(itemName)) return;

        var row = rows[itemName];

        var statusText = row.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

        if (statusText != null)
        {
            statusText.text = "Inspection Completed";
            statusText.color = new Color(0.12f, 0.6f, 0.18f); // green-ish
        }
        else
        {
            Debug.LogError($"StatusText not found or missing TextMeshProUGUI in {itemName}");
        }

        completed.Add(itemName);
        CheckAllCompleted();
    }


    private void CheckAllCompleted()
    {
        if (proceedButton == null) return;
        // All registered rows must be completed
        if (rows.Count > 0 && completed.Count == rows.Count)
            proceedButton.interactable = true;
    }

    // Wire this to ProceedButton.OnClick
    public void OnProceed()
    {
        if (LMSManager.Instance != null)
            LMSManager.Instance.SendProgress("TrayInspection", 100f);

        // Load next scene (make sure it's in Build Settings)
        SceneManager.LoadScene("Scene2_Warehouse");
    }
}
