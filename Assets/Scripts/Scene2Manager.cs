using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scene2Manager : MonoBehaviour
{
    public static Scene2Manager Instance { get; private set; }

    [Header("Spawning")]
    public Transform[] spawnPoints;               // empty GameObjects in scene
    public GameObject[] itemPrefabs;              // prefabs with ItemCollectable
    public int itemsToSpawn = 0;                  // 0 => itemPrefabs.Length

    [Header("Hold / Interaction")]
    public Transform holdAnchor;                  // in front of camera
    public LayerMask itemLayer;                   // layer for items (optional)
    public float pickupMoveSpeed = 12f;

    [Header("Packaging")]
    public PackagingZone packagingZone;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public Slider progressBar;                    // 0..1 slider
    public TextMeshProUGUI progressPercentText;
    public TextMeshProUGUI instructionText;

    [Header("Timer")]
    public float timeLimitSeconds = 300f;         // default 5 minutes

    /*
    [Header("Audio (optional)")]
    public AudioClip placeSuccessSfx;
    public AudioClip placeFailSfx;
    public AudioSource audioSource;
    */

    [Header("Quiz")]
    public GameObject quizPanel;                  // set inactive by default

    // runtime
    private List<GameObject> spawnedItems = new List<GameObject>();
    private GameObject heldItem;
    private ItemCollectable heldItemComp;
    private bool isHolding => heldItem != null;

    private float timeRemaining;
    private Coroutine timerRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    void Start()
    {
        if (itemsToSpawn <= 0) itemsToSpawn = itemPrefabs.Length;
        SpawnItemsRandom();
        StartTimer(timeLimitSeconds);
        UpdateProgressUI();
        ShowInstruction("Collect the items shown in Scene 1 and place them correctly in the packaging zone. Click item to pick it up. Click packaging zone to place.");
    }

    #region Spawning
    void SpawnItemsRandom()
    {
        // simple: pick N random spawn points, instantiate each item once (or if itemPrefabs < spawn, random pick)
        spawnedItems.Clear();
        var availableSpawn = new List<Transform>(spawnPoints);

        for (int i = 0; i < itemsToSpawn; i++)
        {
            if (availableSpawn.Count == 0) break;
            // choose spawn point randomly
            int si = Random.Range(0, availableSpawn.Count);
            Transform sp = availableSpawn[si];
            availableSpawn.RemoveAt(si);

            // choose item prefab (cycle or random)
            GameObject prefab = itemPrefabs[i % itemPrefabs.Length];
            var go = Instantiate(prefab, sp.position, Quaternion.identity);
            // ensure it has a collider and ItemCollectable
            var ic = go.GetComponent<ItemCollectable>();
            if (ic == null) Debug.LogWarning("Spawned prefab missing ItemCollectable script: " + prefab.name);
            spawnedItems.Add(go);
        }
    }
    #endregion

    #region Timer
    void StartTimer(float seconds)
    {
        timeRemaining = seconds;
        if (timerRoutine != null) StopCoroutine(timerRoutine);
        timerRoutine = StartCoroutine(TimerCoroutine());
    }

    IEnumerator TimerCoroutine()
    {
        while (timeRemaining > 0f)
        {
            UpdateTimerUI();
            yield return new WaitForSeconds(1f);
            timeRemaining -= 1f;
        }
        UpdateTimerUI();
        OnTimeExpired();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int mins = Mathf.FloorToInt(timeRemaining / 60f);
        int secs = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = "Left Time :" + $"{mins:00}:{secs:00}";
    }
    #endregion

    #region Input / Pickup / Place
    void Update()
    {
        // On left click:
        if (Input.GetMouseButtonDown(0))
        {
            if (!isHolding)
            {
                // pick up item if clicked
                TryPickupFromRaycast();
            }
            else
            {
                // try place into packaging zone
                TryPlaceHeldItem();
            }
        }

        // smooth follow of held item to anchor
        if (isHolding && holdAnchor != null)
        {
            heldItem.transform.position = Vector3.Lerp(heldItem.transform.position, holdAnchor.position, Time.deltaTime * pickupMoveSpeed);
            heldItem.transform.rotation = Quaternion.Slerp(heldItem.transform.rotation, holdAnchor.rotation, Time.deltaTime * pickupMoveSpeed);
        }
    }

    void TryPickupFromRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("No Main Camera found (tag as MainCamera)."); return; }

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(r, out RaycastHit hit, 100f, itemLayer.value == 0 ? ~0 : itemLayer))
        {
            var go = hit.collider.gameObject;
            var ic = go.GetComponentInParent<ItemCollectable>();
            if (ic != null && !ic.isPlaced)
            {
                PickupItem(go);
            }
        }
    }

    void PickupItem(GameObject go)
    {
        heldItem = go;
        heldItemComp = go.GetComponent<ItemCollectable>();
        // disable physics if present
        var rb = heldItem.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; }

        // parent to holdAnchor so rotation/movement is easy (keep worldPositionStays false)
        if (holdAnchor != null) heldItem.transform.SetParent(holdAnchor, worldPositionStays: true);

        // optionally add DragRotate so user can rotate while holding
        var dr = heldItem.GetComponent<DragRotate>();
        if (dr == null) dr = heldItem.AddComponent<DragRotate>();
        dr.enabled = true;

        ShowInstruction($"Holding: {heldItemComp.itemName} — click Packaging Zone to place.");
    }

    void TryPlaceHeldItem()
    {
        if (heldItem == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(r, out RaycastHit hit, 200f))
        {
            // check the hit is inside the packaging zone collider
            var zone = hit.collider.GetComponentInParent<PackagingZone>();
            if (zone == null)
            {
                // maybe we clicked the zone's collider directly
                var zone2 = packagingZone != null ? packagingZone.GetComponent<Collider>() : null;
                ShowInstruction("Click accurately on the Packaging Zone to place the item.");
               // PlaySfx(placeFailSfx);
                return;
            }

            // attempt to place
            if (packagingZone.TryPlaceItem(heldItem, out int slotIndex))
            {
                OnItemPlacedSuccessfully(slotIndex);
            }
            else
            {
                // not allowed to place here (wrong object or slot full)
                ShowInstruction("Cannot place item here. Place the correct item into the packaging zone.");
                //PlaySfx(placeFailSfx);
            }

            // clear held item reference in both cases (we either placed or leave the item as child of holdAnchor)
            // If placement failed, keep the item in hand (so don't null it). We'll only clear if succeeded.
        }
        else
        {
            ShowInstruction("Point at the packaging zone to place the item.");
        }
    }

    void OnItemPlacedSuccessfully(int slotIndex)
    {
       // PlaySfx(placeSuccessSfx);

        // disable drag rotate
        var dr = heldItem.GetComponent<DragRotate>();
        if (dr) dr.enabled = false;

        heldItem = null;
        heldItemComp = null;

        UpdateProgressUI();

        // check completion
        if (packagingZone.GetPlacedCount() >= packagingZone.GetSlotCount())
        {
            OnAllItemsPlaced();
        }
        else
        {
            ShowInstruction("Good. Continue collecting other items.");
        }
    }
    #endregion

    #region Progress / Completion / Timer End
    void UpdateProgressUI()
    {
        if (progressBar != null && packagingZone != null)
        {
            float fraction = (float)packagingZone.GetPlacedCount() / Mathf.Max(1, packagingZone.GetSlotCount());
            progressBar.value = fraction;
            if (progressPercentText != null) progressPercentText.text = $"{Mathf.RoundToInt(fraction * 100f)}%";
            // Optional: inform a central ProgressManager to keep cross-scene progress
            // ProgressManager.Instance?.SetSceneProgress(fraction);
        }
    }

    void OnAllItemsPlaced()
    {
        ShowInstruction("All items placed! Preparing quiz...");
        if (timerRoutine != null) StopCoroutine(timerRoutine); // stop timer
        OpenQuiz();
    }

    void OnTimeExpired()
    {
        ShowInstruction("Time is up! You failed the task. Retry?");
        // Show a retry dialog or restart Scene2
        // For now: pause and let user click a Retry UI (you must implement Retry UI button and call RetryScene)
    }

    public void RetryScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    #endregion

    #region UI helpers & SFX
    void ShowInstruction(string text)
    {
        if (instructionText != null) instructionText.text = text;
    }

    void PlaySfx(AudioClip clip)
    {
       // if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
    #endregion

    #region Quiz
    void OpenQuiz()
    {
        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
            // QuizManager should be on the panel and will start itself
            var qm = quizPanel.GetComponent<QuizManager>();
            if (qm != null) qm.StartQuiz();
        }
    }
    #endregion
}
