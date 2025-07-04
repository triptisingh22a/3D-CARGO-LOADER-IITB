using UnityEngine;

[System.Serializable]
public class CameraController : MonoBehaviour
{
    [Header("Camera Views")]
    public Transform topView;
    public Transform sideView;
    public Transform backView;

    [Header("Subject Vehicle")]
    public Transform truck;

    [Header("Camera Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;

    private Transform targetPosition;
    private bool isRotating = false;

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            isRotating = true;
            targetPosition = null;
        }
        else
        {
            isRotating = false;
        }

        if (isRotating)
        {
            float horizontalInput = Input.GetAxis("Mouse X");
            float verticalInput = Input.GetAxis("Mouse Y");

            transform.RotateAround(truck.position, Vector3.up, horizontalInput * rotateSpeed);
            transform.RotateAround(truck.position, transform.right, -verticalInput * rotateSpeed);
        }
        else if (targetPosition != null)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition.position, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetPosition.rotation, Time.deltaTime * moveSpeed);
        }
    }

    public void MoveToTopView()
    {
        targetPosition = topView;
    }

    public void MoveToSideView()
    {
        targetPosition = sideView;
    }

    public void MoveToBackView()
    {
        targetPosition = backView;
    }
}
