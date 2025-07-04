using System.Collections;
using UnityEngine;

// This script should be attached to your truck GameObject
public class TruckContainerFix : MonoBehaviour
{
    public GameObject container; // Assign the black container object in the inspector
    public Transform truckFrame; // Assign the truck frame transform in the inspector
    
    private void Start()
    {
        // Fix container position on startup
        FixContainerPosition();
    }
    
    public void FixContainerPosition()
    {
        if (container == null)
        {
            Debug.LogError("Container object not assigned - please assign it in the inspector");
            
            // Try to find it automatically if not assigned
            container = FindObjectWithNameContaining("container");
            if (container == null)
                container = FindObjectWithNameContaining("box");
                
            if (container == null)
            {
                Debug.LogError("Could not find container automatically - please assign it in the inspector");
                return;
            }
        }
        
        if (truckFrame == null)
        {
            Debug.LogError("Truck frame not assigned - please assign it in the inspector");
            
            // Try to find it automatically
            truckFrame = transform.Find("Frame");
            if (truckFrame == null)
                truckFrame = FindChildWithNameContaining("frame");
            if (truckFrame == null)
                truckFrame = FindChildWithNameContaining("chassis");
                
            if (truckFrame == null)
            {
                Debug.LogError("Could not find truck frame automatically - using truck base");
                truckFrame = transform;
            }
        }
        
        // Get the height of the frame
        float frameHeight = GetObjectTopHeight(truckFrame.gameObject);
        
        // Get the size of the container
        float containerHeight = GetObjectHeight(container);
        
        // Calculate the position where the container bottom sits on the frame top
        Vector3 newPosition = container.transform.position;
        newPosition.y = frameHeight + (containerHeight / 2f);
        
        // Apply the position
        container.transform.position = newPosition;
        
        Debug.Log($"Container repositioned: placing bottom at frame height {frameHeight}, container height = {containerHeight}");
    }
    
    // Helper method to find an object by partial name
    private GameObject FindObjectWithNameContaining(string namePart)
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.name.ToLower().Contains(namePart.ToLower()))
            {
                return go;
            }
        }
        return null;
    }
    
    // Helper method to find a child with partial name
    private Transform FindChildWithNameContaining(string namePart)
    {
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains(namePart.ToLower()))
            {
                return child;
            }
        }
        return null;
    }
    
    // Get the height of an object (Y size)
    private float GetObjectHeight(GameObject obj)
    {
        if (obj == null) return 0f;
        
        // Try to get renderer bounds
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        
        // If no renderer, try collider
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds.size.y;
        }
        
        // If still no luck, use local scale
        return obj.transform.localScale.y;
    }
    
    // Get the top height of an object (Y position + half height)
    private float GetObjectTopHeight(GameObject obj)
    {
        if (obj == null) return 0f;
        
        // Try to get world position top from renderer
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.max.y;
        }
        
        // If no renderer, try collider
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds.max.y;
        }
        
        // If still no luck, use transform position + half scale
        return obj.transform.position.y + (obj.transform.localScale.y / 2f);
    }
}