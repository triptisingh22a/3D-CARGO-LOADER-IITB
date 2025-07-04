using UnityEngine;

public class TruckController : MonoBehaviour
{
    public float rotationSpeed = 100f; // Speed of rotation
    public float movementSpeed = 5f;  // Speed of movement
    public float zoomSpeed = 10f;     // Speed for zooming
    public float minZoom = 5f;        // Minimum zoom distance
    public float maxZoom = 50f;       // Maximum zoom distance

    private Camera mainCamera;
    private float currentZoom;

    void Start()
    {
        mainCamera = Camera.main; // Ensure you have a Camera tagged as MainCamera
        currentZoom = mainCamera.orthographic ? mainCamera.orthographicSize : mainCamera.fieldOfView;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
        HandleZoom();
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // Right mouse button for rotation
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Rotate the truck based on mouse movement
            transform.Rotate(Vector3.up, -mouseX * rotationSpeed * Time.deltaTime, Space.World); // Horizontal rotation
            transform.Rotate(Vector3.right, mouseY * rotationSpeed * Time.deltaTime, Space.Self); // Vertical rotation
        }
    }

    private void HandleMovement()
    {
        if (Input.GetMouseButton(2)) // Middle mouse button for movement
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Move the truck in world space
            Vector3 movement = new Vector3(-mouseX, -mouseY, 0) * movementSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = currentZoom;
            }
            else
            {
                mainCamera.fieldOfView = currentZoom;
            }
        }
    }
}
