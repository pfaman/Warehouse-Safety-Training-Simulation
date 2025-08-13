using System.Collections;
using UnityEngine;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private Transform inspectionAnchor;   // assign the InspectionAnchor
    [SerializeField] private Canvas inspectionUICanvas;    // assign the InspectionUI canvas
    [SerializeField] private float moveDuration = 0.35f;   // move animation time (seconds)

    [Header("Options")]
    [SerializeField] private bool addDragRotateIfMissing = true;
    [SerializeField] private bool lockCursorDuringInspection = false;

    public bool IsInspecting => isInspecting;

    // runtime
    private bool isInspecting = false;
    private ItemHoverGlow currentItem;   // the item script you already use
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Collider[] itemColliders;
    private DragRotate dragRotate;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (inspectionUICanvas) inspectionUICanvas.enabled = false;
    }

    // ===== Public API =====

    public void StartInspection(ItemHoverGlow item)
    {
        if (isInspecting || item == null || inspectionAnchor == null) return;

        currentItem = item;

        // store original transform state
        var t = currentItem.transform;
        originalParent = t.parent;
        originalPosition = t.position;
        originalRotation = t.rotation;
        originalScale = t.localScale;

        // disable colliders while we animate/inspect
        itemColliders = currentItem.GetComponentsInChildren<Collider>(includeInactive: false);
        foreach (var col in itemColliders) col.enabled = false;

        if (inspectionUICanvas) inspectionUICanvas.enabled = true;
        if (lockCursorDuringInspection)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        StartCoroutine(MoveToAnchor(t));
    }

    public void EndInspection()
    {
        if (!isInspecting || currentItem == null) return;

        // remove drag component during return
        if (dragRotate != null) dragRotate.enabled = false;

        StartCoroutine(MoveBack(currentItem.transform));
    }

    // Hook this to the Back button
    public void OnBackButton()
    {
        EndInspection();
    }

    // ===== Coroutines =====

    private IEnumerator MoveToAnchor(Transform t)
    {
        isInspecting = true;

        // animate position/rotation to anchor
        float elapsed = 0f;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / moveDuration);
            t.position = Vector3.Lerp(startPos, inspectionAnchor.position, p);
            t.rotation = Quaternion.Slerp(startRot, inspectionAnchor.rotation, p);
            yield return null;
        }

        // parent to anchor so rotating is simple and stable
        t.SetParent(inspectionAnchor, worldPositionStays: false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = originalScale; // preserve scale

        // add/enable rotation script
        dragRotate = t.GetComponent<DragRotate>();
        if (dragRotate == null && addDragRotateIfMissing)
            dragRotate = t.gameObject.AddComponent<DragRotate>();
        if (dragRotate != null) dragRotate.enabled = true;
    }

    private IEnumerator MoveBack(Transform t)
    {
        // detach to world, animate back
        t.SetParent(null, worldPositionStays: true);

        float elapsed = 0f;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / moveDuration);
            t.position = Vector3.Lerp(startPos, originalPosition, p);
            t.rotation = Quaternion.Slerp(startRot, originalRotation, p);
            yield return null;
        }

        // restore original hierarchy & transform
        t.SetParent(originalParent, worldPositionStays: true);
        t.position = originalPosition;
        t.rotation = originalRotation;
        t.localScale = originalScale;

        // re-enable item colliders
        if (itemColliders != null)
            foreach (var col in itemColliders) col.enabled = true;

        if (inspectionUICanvas) inspectionUICanvas.enabled = false;
        if (lockCursorDuringInspection)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // optional: mark inspected in your checklist
        if (ChecklistManager.Instance != null && currentItem != null)
            ChecklistManager.Instance.MarkInspected(currentItem.itemName);

        // clear state
        currentItem = null;
        dragRotate = null;
        isInspecting = false;
    }
}
