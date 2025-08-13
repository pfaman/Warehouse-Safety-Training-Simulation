using UnityEngine;

public class DragRotate : MonoBehaviour
{
    [Tooltip("Higher = faster rotation")]
    public float sensitivity = 200f;

    private bool dragging = false;
    private Vector3 lastMouse;

    void OnMouseDown()
    {
        dragging = true;
        lastMouse = Input.mousePosition;
    }

    void OnMouseUp()
    {
        dragging = false;
    }

    void Update()
    {
        if (!dragging) return;

        Vector3 delta = Input.mousePosition - lastMouse;

        // vertical mouse = rotate around camera's right axis
        float rotX = (delta.y / Screen.height) * sensitivity;
        // horizontal mouse = rotate around world up
        float rotY = -(delta.x / Screen.width) * sensitivity;

        // world-space rotations feel natural in front of camera
        transform.Rotate(Camera.main.transform.right, rotX, Space.World);
        transform.Rotate(Vector3.up, rotY, Space.World);

        lastMouse = Input.mousePosition;
    }
}
