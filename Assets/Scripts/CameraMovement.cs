using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] Transform targetPos;
    [SerializeField] float moveSpeed = 1f;
    private bool moveToTarget = false;

    private void Start()
    {
        MoveCamera();
    }
    private void Update()
    {
        if (moveToTarget)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos.position, moveSpeed * Time.deltaTime);
        }

        if(Vector3.Distance(transform.position,targetPos.position) < 0.01f)
        {
            moveToTarget = false;
        }
    }

    public void MoveCamera()
    {
        moveToTarget = true;
    }
}
