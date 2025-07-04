using UnityEngine;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(Renderer))]
public class CargoInteractable : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Truck Bounds (min/max)")]
    public Vector3 truckMinBounds;
    public Vector3 truckMaxBounds;

    [Header("Highlight Settings")]
    public bool enableHighlight = true;
    public Color highlightColor = new Color(1f, 0.92f, 0.016f, 0.5f);

    // Cached
    private Camera mainCamera;
    private Renderer rend;
    private Color originalColor;
    private Vector3 originalPosition;
    private Vector3 dragOffset;

    // Constraint handler (optional)
    private CargoConstraintHandler constraintHandler;

    public event Action onBeginDrag;
    public event Action onDrag;
    public event Action onEndDrag;

    void Awake()
    {
        mainCamera = Camera.main;
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
        originalPosition = transform.position;
        constraintHandler = GetComponent<CargoConstraintHandler>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragOffset = transform.position - GetMouseWorldPosition(eventData);
        Highlight(true);
        onBeginDrag?.Invoke();
        Debug.Log($"Drag started: {name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 targetPos = GetMouseWorldPosition(eventData) + dragOffset;
        transform.position = ClampToBounds(targetPos);
        onDrag?.Invoke();
        constraintHandler?.ValidatePlacementDuringDrag();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Highlight(false);
        bool valid = constraintHandler != null
            ? InvokeAndCheck(() => { constraintHandler.ValidatePlacementOnDrop(); return true; })
            : CheckLegacyPlacement();

        if (!valid)
            transform.position = originalPosition;

        onEndDrag?.Invoke();
        Debug.Log($"Drag ended: {name} (Valid: {valid})");
    }

    private Vector3 GetMouseWorldPosition(PointerEventData data)
    {
        Ray ray = mainCamera.ScreenPointToRay(data.position);
        Plane plane = new Plane(Vector3.up, Vector3.up * transform.position.y);
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return transform.position;
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, truckMinBounds.x, truckMaxBounds.x);
        pos.y = Mathf.Clamp(pos.y, truckMinBounds.y, truckMaxBounds.y);
        pos.z = Mathf.Clamp(pos.z, truckMinBounds.z, truckMaxBounds.z);
        return pos;
    }

    private bool CheckLegacyPlacement()
    {
        bool inside = transform.position.x >= truckMinBounds.x && transform.position.x <= truckMaxBounds.x
                   && transform.position.y >= truckMinBounds.y && transform.position.y <= truckMaxBounds.y
                   && transform.position.z >= truckMinBounds.z && transform.position.z <= truckMaxBounds.z;
        if (!inside)
            Debug.LogWarning($"Out of bounds: resetting {name}");
        else
            Debug.Log($"Placed inside: {name}");
        return inside;
    }

    private void Highlight(bool on)
    {
        if (enableHighlight)
            rend.material.color = on ? highlightColor : originalColor;
    }

    private bool InvokeAndCheck(Func<bool> action)
    {
        action.Invoke();
        return true; // Assume handler logs or adjusts as needed
    }
}