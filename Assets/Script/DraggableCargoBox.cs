using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DraggableCargoBox : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Layer Setup")]
    public LayerMask groundLayer;

    [Header("Drag Settings")]
    public float dragSpeed = 10f;
    public float gridSize = 1f;

    [Header("Visuals")]
    public Color highlightColor = Color.yellow;

    private Camera cam;
    private Vector3 originalPosition;
    private bool isDragging = false;

    private Renderer rend;
    private Color originalColor;
    private Rigidbody rb;
    private Collider col;

    private Vector3 dragTarget;

    void Start()
    {
        cam = Camera.main;

        if (cam.GetComponent<PhysicsRaycaster>() == null)
            cam.gameObject.AddComponent<PhysicsRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        originalPosition = transform.position;
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rend != null)
            originalColor = rend.material.color;

        // Rigidbody setup
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.freezeRotation = true;
        col.isTrigger = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;

        if (rend != null)
            rend.material.color = highlightColor;

        // Slight lift on pick
        Vector3 lifted = transform.position;
        lifted.y += 0.2f;
        rb.MovePosition(lifted);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Ray ray = cam.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Debug.Log("Dragging hit: " + hit.collider.gameObject.name);

            Vector3 point = hit.point;
            point = SnapToGrid(point);
            point.y = transform.position.y;

            dragTarget = point;
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
            Debug.Log("No drag raycast hit — check groundLayer setup.");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        if (rend != null)
            rend.material.color = originalColor;

        rb.velocity = Vector3.zero;
        dragTarget = transform.position; // reset target
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            Vector3 direction = (dragTarget - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, dragTarget);
            rb.velocity = direction * dragSpeed * Mathf.Clamp01(distance);
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float y = pos.y;
        float z = Mathf.Round(pos.z / gridSize) * gridSize;
        return new Vector3(x, y, z);
    }
}
