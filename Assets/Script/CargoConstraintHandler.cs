using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced CargoConstraintHandler that integrates raycast physics with existing overhang constraints
/// </summary>
public class CargoConstraintHandler : MonoBehaviour
{
    [Header("Active Constraints")]
    [SerializeField] private bool checkOverhangConstraint = true;
    [SerializeField] private bool checkGroundSupport = true;
    [SerializeField] private bool checkStability = true;
    [SerializeField] private bool checkClearance = true;
    [SerializeField] private bool checkHeightConstraint = true;
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private LayerMask obstacleLayer = 1 << 1;
    [SerializeField] private LayerMask cargoLayer = 1 << 2;
    [SerializeField] private float maxRaycastDistance = 10f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    
    [Header("Physics Constraints")]
    [SerializeField] private float minStackHeight = 0.5f;
    [SerializeField] private float maxStackHeight = 5f;
    [SerializeField] private float stabilityAngle = 30f;
    [SerializeField] private float clearanceRadius = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool skipInitialCheck = true;
    
    // References to constraint components
    private CargoOverhangConstraint overhangConstraint;
    private CargoItem cargoItem;
    private bool isInitialized = false;
    
    void Start()
    {
        // Get existing constraint components
        overhangConstraint = GetComponent<CargoOverhangConstraint>();
        cargoItem = GetComponent<CargoItem>();
        
        // Add CargoItem if missing
        if (cargoItem == null)
        {
            cargoItem = gameObject.AddComponent<CargoItem>();
            cargoItem.InitializeFromRenderer();
        }
        
        // Log warnings for missing components
        if (checkOverhangConstraint && overhangConstraint == null)
        {
            Debug.LogWarning($"Overhang constraint enabled but CargoOverhangConstraint missing on {gameObject.name}");
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Validates placement during dragging - integrates both systems
    /// </summary>
    public void ValidatePlacementDuringDrag()
    {
        if (!isInitialized || (skipInitialCheck && Time.timeSinceLevelLoad < 1.0f))
            return;
        
        // Keep existing overhang validation
        if (checkOverhangConstraint && overhangConstraint != null)
        {
            overhangConstraint.ValidatePlacementDuringDrag();
        }
        
        // Add raycast-based visual feedback during drag
        if (showDebugRays)
        {
            PerformRaycastChecks(false); // Visual only, don't enforce
        }
    }
    
    /// <summary>
    /// Validates placement on drop - combines all constraint systems
    /// </summary>
    public bool ValidatePlacementOnDrop()
    {
        if (!isInitialized)
            return true;
        
        bool isValid = true;
        
        // Keep existing overhang constraint check
        if (checkOverhangConstraint && overhangConstraint != null)
        {
            isValid = overhangConstraint.ValidatePlacementOnDrop() && isValid;
        }
        
        // Add new raycast-based constraints
        if (checkGroundSupport || checkStability || checkClearance || checkHeightConstraint)
        {
            isValid = PerformRaycastChecks(true) && isValid;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Performs raycast-based constraint checks
    /// </summary>
    private bool PerformRaycastChecks(bool enforce)
    {
        Vector3 position = transform.position;
        Bounds bounds = cargoItem.GetBounds();
        bool allValid = true;
        
        // Ground support check
        if (checkGroundSupport)
        {
            bool hasSupport = CheckGroundSupport(position, bounds);
            if (!hasSupport)
            {
                if (enforce) Debug.Log($"{gameObject.name}: Failed ground support check");
                allValid = false;
            }
        }
        
        // Stability check
        if (checkStability)
        {
            bool isStable = CheckStability(position, bounds);
            if (!isStable)
            {
                if (enforce) Debug.Log($"{gameObject.name}: Failed stability check");
                allValid = false;
            }
        }
        
        // Clearance check
        if (checkClearance)
        {
            bool hasClearance = CheckClearance(position, bounds);
            if (!hasClearance)
            {
                if (enforce) Debug.Log($"{gameObject.name}: Failed clearance check");
                allValid = false;
            }
        }
        
        // Height constraint check
        if (checkHeightConstraint)
        {
            bool withinHeight = CheckHeightConstraint(position);
            if (!withinHeight)
            {
                if (enforce) Debug.Log($"{gameObject.name}: Failed height constraint check");
                allValid = false;
            }
        }
        
        return allValid;
    }
    
    private bool CheckGroundSupport(Vector3 position, Bounds bounds)
    {
        Vector3 bottomCenter = position - Vector3.up * (bounds.size.y * 0.5f);
        
        // Check key support points
        Vector3[] checkPoints = {
            bottomCenter,
            bottomCenter + Vector3.forward * bounds.size.z * 0.3f,
            bottomCenter - Vector3.forward * bounds.size.z * 0.3f,
            bottomCenter + Vector3.right * bounds.size.x * 0.3f,
            bottomCenter - Vector3.right * bounds.size.x * 0.3f
        };
        
        int supportPoints = 0;
        
        foreach (Vector3 point in checkPoints)
        {
            if (Physics.Raycast(point, Vector3.down, groundCheckDistance, groundLayer | cargoLayer))
            {
                supportPoints++;
                if (showDebugRays)
                    Debug.DrawRay(point, Vector3.down * groundCheckDistance, Color.green, 1f);
            }
            else
            {
                if (showDebugRays)
                    Debug.DrawRay(point, Vector3.down * groundCheckDistance, Color.red, 1f);
            }
        }
        
        return supportPoints >= 3; // Need majority support
    }
    
    private bool CheckStability(Vector3 position, Bounds bounds)
    {
        Vector3 bottomCenter = position - Vector3.up * (bounds.size.y * 0.5f);
        
        if (Physics.Raycast(bottomCenter, Vector3.down, out RaycastHit hit, groundCheckDistance * 2, groundLayer | cargoLayer))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            
            if (showDebugRays)
            {
                Color rayColor = angle <= stabilityAngle ? Color.green : Color.red;
                Debug.DrawRay(hit.point, hit.normal * 2f, rayColor, 1f);
            }
            
            return angle <= stabilityAngle;
        }
        
        return false;
    }
    
    private bool CheckClearance(Vector3 position, Bounds bounds)
    {
        Vector3 expandedSize = bounds.size + Vector3.one * clearanceRadius;
        
        Collider[] overlapping = Physics.OverlapBox(position, expandedSize * 0.5f, Quaternion.identity, cargoLayer | obstacleLayer);
        
        // Filter out self
        int validOverlaps = 0;
        foreach (var col in overlapping)
        {
            if (col.gameObject != gameObject)
                validOverlaps++;
        }
        
        return validOverlaps == 0;
    }
    
    private bool CheckHeightConstraint(Vector3 position)
    {
        return position.y >= minStackHeight && position.y <= maxStackHeight;
    }
    
    /// <summary>
    /// Helper method to get snapped position to valid surface
    /// </summary>
    public Vector3 GetValidPlacementPosition(Vector3 targetPosition)
    {
        Vector3 adjustedPosition = targetPosition;
        
        if (Physics.Raycast(targetPosition + Vector3.up * maxRaycastDistance, Vector3.down, out RaycastHit hit, maxRaycastDistance * 2, groundLayer | cargoLayer))
        {
            adjustedPosition.y = hit.point.y;
            
            if (showDebugRays)
                Debug.DrawRay(hit.point, Vector3.up * 2f, Color.blue, 1f);
        }
        
        return adjustedPosition;
    }
}

/// <summary>
/// Enhanced CargoItem component that works with both constraint systems
/// </summary>
[System.Serializable]
public class CargoItem : MonoBehaviour
{
    [Header("Cargo Properties")]
    public string cargoID;
    public float weight = 1f;
    public CargoType type = CargoType.General;
    public bool isFragile = false;
    public bool isHazardous = false;
    
    [Header("Physics")]
    public Bounds bounds;
    
    private Renderer cachedRenderer;
    private BoxCollider cachedCollider;
    
    void Start()
    {
        if (bounds.size == Vector3.zero)
        {
            InitializeFromRenderer();
        }
    }
    
    public void InitializeFromRenderer()
    {
        cachedRenderer = GetComponent<Renderer>();
        cachedCollider = GetComponent<BoxCollider>();
        
        if (cachedRenderer != null)
        {
            bounds = cachedRenderer.bounds;
        }
        else if (cachedCollider != null)
        {
            bounds = cachedCollider.bounds;
        }
        
        // Auto-generate ID if empty
        if (string.IsNullOrEmpty(cargoID))
        {
            cargoID = gameObject.name + "_" + GetInstanceID();
        }
    }
    
    public Bounds GetBounds()
    {
        // Update bounds if renderer exists
        if (cachedRenderer != null)
        {
            bounds.center = cachedRenderer.bounds.center;
            bounds.size = cachedRenderer.bounds.size;
        }
        else if (cachedCollider != null)
        {
            bounds.center = cachedCollider.bounds.center;
            bounds.size = cachedCollider.bounds.size;
        }
        
        return bounds;
    }
    
    public bool CanStackWith(CargoItem other)
    {
        if (isFragile) return false;
        if (isHazardous && other.isHazardous && type != other.type) return false;
        return true;
    }
}

public enum CargoType
{
    General,
    Fragile,
    Hazardous,
    Liquid,
    Heavy
}