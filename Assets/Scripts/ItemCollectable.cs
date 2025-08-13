using UnityEngine;

public class ItemCollectable : MonoBehaviour
{
    [Tooltip("Must match the itemName used in Scene1/checklist")]
    public string itemName;

    [HideInInspector]
    public bool isPlaced = false;
}
