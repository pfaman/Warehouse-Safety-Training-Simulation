using UnityEngine;

public class PackagingZone : MonoBehaviour
{
    [Tooltip("Slots where items will snap into")]
    public Transform[] slotTransforms;

    [Tooltip("Optional: If length == slotTransforms length, each slot expects this itemName; otherwise any order is accepted.")]
    public string[] expectedItemNames;

    private GameObject[] placedItems;

    public float targetSize = 0.1f;

    void Awake()
    {
        placedItems = new GameObject[slotTransforms.Length];
    }

    /// <summary>
    /// Try to place the item into an appropriate slot. Returns true if placed and sets slotIndex.
    /// </summary>
    public bool TryPlaceItem(GameObject item, out int slotIndex)
    {
        slotIndex = -1;
        if (item == null) return false;

        var it = item.GetComponent<ItemCollectable>();
        if (it == null || it.isPlaced) return false;

        string name = it.itemName;

        // If expectedItemNames is provided and length equals slots, find matching slot index
        if (expectedItemNames != null && expectedItemNames.Length == slotTransforms.Length)
        {
            for (int i = 0; i < expectedItemNames.Length; i++)
            {
                if (placedItems[i] == null && expectedItemNames[i] == name)
                {
                    PlaceAtSlot(item, i);
                    slotIndex = i;
                    return true;
                }
            }
            // no matching slot found
            return false;
        }

        // Otherwise place into first empty slot
        for (int i = 0; i < slotTransforms.Length; i++)
        {
            if (placedItems[i] == null)
            {
                PlaceAtSlot(item, i);
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    void PlaceAtSlot(GameObject item, int index)
    {
        var t = item.transform;
        t.SetParent(slotTransforms[index], worldPositionStays: false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;


        //FitToSlot(item, 0.1f);/// Set Slot Size

        t.localScale = Vector3.one;

        var col = item.GetComponent<Collider>();
        if (col) col.enabled = false;
        var rb = item.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        placedItems[index] = item;

        var ic = item.GetComponent<ItemCollectable>();
        if (ic != null) ic.isPlaced = true;


    }

    void FitToSlot(GameObject obj, float targetSize)
    {
        Renderer rend = obj.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float maxDimension = Mathf.Max(rend.bounds.size.x, rend.bounds.size.y, rend.bounds.size.z);
            float scaleFactor = targetSize / maxDimension;
            obj.transform.localScale = Vector3.one * scaleFactor;
        }
    }
    public int GetPlacedCount()
    {
        int c = 0;
        foreach (var p in placedItems) if (p != null) c++;
        return c;
    }

    public int GetSlotCount() => slotTransforms.Length;
}
