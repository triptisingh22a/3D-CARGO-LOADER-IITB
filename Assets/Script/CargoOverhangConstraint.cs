using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles overhanging constraints for cargo items.
/// Ensures a cargo object has sufficient support from objects below it.
/// Also prevents moving boxes that have other boxes stacked on top.
/// </summary>
public class CargoOverhangConstraint : MonoBehaviour
{
    [Header("Overhang Settings")]
    [Tooltip("Minimum percentage (0-1) of this object that must be supported")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumSupportPercentage = 0.7f;
    
    [Tooltip("Layer mask for objects that can provide support")]
    [SerializeField] private LayerMask supportLayers = -1; // Default to "Everything"
    
    [Tooltip("Distance to check below for supporting objects")]
    [SerializeField] private float supportCheckDistance = 0.1f;
    
    [Tooltip("Distance to check above for stacked boxes")]
    [SerializeField] private float stackCheckDistance = 0.5f; // Increased to ensure detection
    
    [Tooltip("Overlap check buffer for stacked boxes")]
    [SerializeField] private float stackOverlapBuffer = 0.1f;
    
    [Tooltip("Visual feedback when constraints are violated")]
    [SerializeField] private bool showVisualFeedback = true;
    
    [Tooltip("Color when constraints are violated")]
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    
    [Tooltip("Color when box cannot be moved due to stacked boxes")]
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    // Skip base layer check if object is on the ground
    [Tooltip("Automatically pass constraint check if object is at ground level")]
    [SerializeField] private bool ignoreForGroundLevel = true;
    [SerializeField] private float groundLevelThreshold = 0.01f;

    // References
    private CargoInteractable cargoInteractable;
    private Renderer objectRenderer;
    private Material material;
    private Color originalColor;
    private Vector3 originalPosition;
    private Bounds objectBounds;
    private BoxCollider boxCollider;
    
    // Raycast settings
    [SerializeField] private int raySamplesX = 5;
    [SerializeField] private int raySamplesZ = 5;

    // State tracking
    private bool isDragging = false;
    private bool isBeingDragged = false;
    private bool isLocked = false;
    
    void Start()
    {
        // Get required components
        cargoInteractable = GetComponent<CargoInteractable>();
        objectRenderer = GetComponent<Renderer>();
        boxCollider = GetComponent<BoxCollider>();
        
        if (objectRenderer != null)
        {
            material = objectRenderer.material;
            originalColor = material.color;
            objectBounds = objectRenderer.bounds;
        }
        else
        {
            Debug.LogWarning($"CargoOverhangConstraint on {gameObject.name} - No renderer found.");
        }
        
        if (boxCollider == null)
        {
            Debug.LogWarning($"CargoOverhangConstraint on {gameObject.name} - No BoxCollider found.");
        }
        
        // Store original position for reset - we need to delay this slightly to ensure
        // the object has settled into its initial position
        Invoke("StoreInitialPosition", 0.1f);
        
        // Subscribe to cargo events
        if (cargoInteractable != null)
        {
            cargoInteractable.onBeginDrag += OnBeginDrag;
            cargoInteractable.onDrag += OnDrag;
            cargoInteractable.onEndDrag += OnEndDrag;
        }
        else
        {
            Debug.LogError($"CargoOverhangConstraint on {gameObject.name} - No CargoInteractable component found");
        }

        // Initial check - only log, don't enforce (assumes valid initial placement)
        CheckOverhangConstraint(false, true);
    }
    
    /// <summary>
    /// Stores the initial position after the object has settled
    /// </summary>
    private void StoreInitialPosition()
    {
        originalPosition = transform.position;
        Debug.Log($"Stored initial position for {gameObject.name}: {originalPosition}");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (cargoInteractable != null)
        {
            cargoInteractable.onBeginDrag -= OnBeginDrag;
            cargoInteractable.onDrag -= OnDrag;
            cargoInteractable.onEndDrag -= OnEndDrag;
        }
    }
    
    /// <summary>
    /// Checks if there are any boxes stacked on top of this box using both raycast and overlap checks
    /// </summary>
    private bool CheckForStackedBoxes()
    {
        if (objectRenderer == null || boxCollider == null) return false;

        // First, check using box overlap
        Vector3 center = boxCollider.bounds.center;
        Vector3 halfExtents = boxCollider.bounds.extents + new Vector3(stackOverlapBuffer, stackOverlapBuffer, stackOverlapBuffer);
        Vector3 topOffset = new Vector3(0, halfExtents.y + 0.01f, 0); // Slight offset upward
        
        // Get all colliders in the area above this box
        Collider[] hitColliders = Physics.OverlapBox(
            center + topOffset, 
            new Vector3(halfExtents.x, stackCheckDistance, halfExtents.z),
            Quaternion.identity,
            supportLayers
        );

        // Check if any of the found colliders are valid cargo boxes
        foreach (Collider col in hitColliders)
        {
            if (col.gameObject != gameObject && col.GetComponent<CargoOverhangConstraint>() != null)
            {
                // Check if the box is actually above us (not just overlapping on sides)
                if (col.bounds.min.y > boxCollider.bounds.max.y - 0.01f)
                {
                    Debug.Log($"Found stacked box: {col.gameObject.name} above {gameObject.name}");
                    return true;
                }
            }
        }

        // Fallback to raycast check for extra reliability
        objectBounds = objectRenderer.bounds;
        Vector3 topCenter = new Vector3(
            objectBounds.center.x,
            objectBounds.max.y,
            objectBounds.center.z
        );

        float width = objectBounds.size.x;
        float depth = objectBounds.size.z;
        float stepX = width / (raySamplesX - 1);
        float stepZ = depth / (raySamplesZ - 1);

        for (int x = 0; x < raySamplesX; x++)
        {
            for (int z = 0; z < raySamplesZ; z++)
            {
                Vector3 rayOrigin = new Vector3(
                    topCenter.x - width / 2 + x * stepX,
                    topCenter.y + 0.05f, // Increased offset to ensure we start above
                    topCenter.z - depth / 2 + z * stepZ
                );

                Ray ray = new Ray(rayOrigin, Vector3.up);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, stackCheckDistance, supportLayers))
                {
                    if (hit.collider.gameObject != gameObject && 
                        hit.collider.GetComponent<CargoOverhangConstraint>() != null)
                    {
                        Debug.DrawRay(rayOrigin, Vector3.up * stackCheckDistance, Color.red, 1f);
                        Debug.Log($"Raycast found stacked box: {hit.collider.gameObject.name} above {gameObject.name}");
                        return true;
                    }
                }
                Debug.DrawRay(rayOrigin, Vector3.up * stackCheckDistance, Color.green, 0.1f);
            }
        }
        return false;
    }

    private void OnBeginDrag()
    {
        // Check if there are boxes stacked on top
        isLocked = CheckForStackedBoxes();
        if (isLocked)
        {
            Debug.Log($"Cannot move {gameObject.name} - boxes are stacked on top");
            if (showVisualFeedback && material != null)
            {
                material.color = lockedColor;
            }
            // Prevent dragging by disabling the CargoInteractable component
            if (cargoInteractable != null)
            {
                cargoInteractable.enabled = false;
                // Re-enable after a short delay to prevent getting stuck
                Invoke("ReenableInteraction", 0.5f);
            }
            return;
        }

        // Store the current bounds before dragging
        if (objectRenderer != null)
        {
            objectBounds = objectRenderer.bounds;
        }
        
        isDragging = true;
        isBeingDragged = true;
        
        // Store the position before dragging begins
        originalPosition = transform.position;
        Debug.Log($"Started dragging {gameObject.name} from position: {originalPosition}");
    }
    
    private void OnDrag()
    {
        // Object is currently being dragged
        isDragging = true;
    }
    
    private void OnEndDrag()
    {
        isDragging = false;
        isBeingDragged = false;
        
        // Check constraints when the object is dropped
        // This is critical - must return the object if invalid
        bool placementValid = CheckOverhangConstraint(true);
        Debug.Log($"Finished dragging {gameObject.name} - valid placement: {placementValid}");
        
        if (!placementValid)
        {
            Debug.Log($"Invalid placement - resetting {gameObject.name} to {originalPosition}");
        }
    }
    
    /// <summary>
    /// Public method that can be called by CargoInteractable.ValidatePlacementDuringDrag
    /// </summary>
    public void ValidatePlacementDuringDrag()
    {
        if (isDragging)
        {
            CheckOverhangConstraint(false);
        }
    }
    
    /// <summary>
    /// Public method that can be called by CargoInteractable.ValidatePlacementOnDrop
    /// </summary>
    public bool ValidatePlacementOnDrop()
    {
        if (isBeingDragged)
        {
            bool result = CheckOverhangConstraint(true);
            isBeingDragged = false;
            return result;
        }
        
        // If we weren't actually dragging, don't validate
        return true;
    }
    
    /// <summary>
    /// Checks if the current placement violates overhang constraints
    /// </summary>
    /// <param name="enforcePlacement">If true, will reset position if constraints are violated</param>
    /// <param name="onlyLog">If true, only log results without visual feedback</param>
    /// <returns>True if constraints are satisfied, false otherwise</returns>
    private bool CheckOverhangConstraint(bool enforcePlacement = false, bool onlyLog = false)
    {
        // We need a renderer to get bounds
        if (objectRenderer == null)
        {
            Debug.LogWarning($"CargoOverhangConstraint on {gameObject.name} - No renderer available for bounds check");
            return true;
        }
        
        // Update bounds
        objectBounds = objectRenderer.bounds;
        
        // Skip check if object is at ground level and we want to ignore that case
        if (ignoreForGroundLevel && IsAtGroundLevel())
        {
            Debug.Log($"Object {gameObject.name} is at ground level - skipping overhang check");
            return true;
        }
        
        // Calculate supported area
        float supportedArea = CalculateSupportedArea();
        float totalArea = objectBounds.size.x * objectBounds.size.z;
        float supportPercentage = supportedArea / totalArea;
        
        bool isValid = supportPercentage >= minimumSupportPercentage;
        
        // Debug information
        Debug.Log($"Object {gameObject.name} - Support: {supportPercentage:P2} (Required: {minimumSupportPercentage:P2}) - Valid: {isValid}");
        
        // Visual feedback (skip if onlyLog)
        if (!onlyLog && showVisualFeedback && material != null)
        {
            material.color = isValid ? originalColor : invalidPlacementColor;
        }
        
        // Reset position if constraints are violated and we're enforcing
        if (!isValid && enforcePlacement)
        {
            Debug.Log($"Object {gameObject.name} has insufficient support. Resetting position from {transform.position} to {originalPosition}");
            transform.position = originalPosition;
            
            // Reset color after position is reset
            if (showVisualFeedback && material != null)
            {
                material.color = originalColor;
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Calculate how much of the object's base is supported by objects below
    /// </summary>
    /// <returns>The supported area in square units</returns>
    private float CalculateSupportedArea()
    {
        // Get bottom face center and size
        Vector3 bottomCenter = new Vector3(
            objectBounds.center.x,
            objectBounds.min.y,
            objectBounds.center.z
        );
        
        float width = objectBounds.size.x;
        float depth = objectBounds.size.z;
        
        // Calculate step size for raycasts
        float stepX = width / (raySamplesX - 1);
        float stepZ = depth / (raySamplesZ - 1);
        
        // Count supported points
        int supportedPoints = 0;
        int totalPoints = raySamplesX * raySamplesZ;
        
        // Cast rays from the bottom face to check for support
        for (int x = 0; x < raySamplesX; x++)
        {
            for (int z = 0; z < raySamplesZ; z++)
            {
                // Calculate point on bottom face
                Vector3 rayOrigin = new Vector3(
                    bottomCenter.x - width / 2 + x * stepX,
                    bottomCenter.y + 0.05f, // Increased offset to avoid self-collision issues
                    bottomCenter.z - depth / 2 + z * stepZ
                );
                
                // Cast ray downward
                Ray ray = new Ray(rayOrigin, Vector3.down);
                RaycastHit hit;
                
                // Check if ray hits a supporting object
                if (Physics.Raycast(ray, out hit, supportCheckDistance + 0.1f, supportLayers))
                {
                    // Skip if we hit ourselves
                    if (hit.collider.gameObject != gameObject)
                    {
                        supportedPoints++;
                        Debug.DrawRay(rayOrigin, Vector3.down * supportCheckDistance, Color.green, 0.1f);
                    }
                }
                else
                {
                    Debug.DrawRay(rayOrigin, Vector3.down * supportCheckDistance, Color.red, 0.1f);
                }
            }
        }
        
        // Calculate supported area
        return (float)supportedPoints / totalPoints * width * depth;
    }

    /// <summary>
    /// Checks if the object is approximately at ground level
    /// </summary>
    private bool IsAtGroundLevel()
    {
        if (objectRenderer == null) return false;
        
        float bottomY = objectBounds.min.y;
        return bottomY <= groundLevelThreshold;
    }

    private void ReenableInteraction()
    {
        if (cargoInteractable != null)
        {
            cargoInteractable.enabled = true;
            if (showVisualFeedback && material != null)
            {
                material.color = originalColor;
            }
        }
    }
}